﻿/*
    Copyright (C) 2014 Omega software d.o.o.

    This file is part of Rhetos.

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as
    published by the Free Software Foundation, either version 3 of the
    License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Rhetos.Logging;
using Rhetos.Persistence;
using Rhetos.Utilities;

namespace Rhetos.Dom.DefaultConcepts.Persistence
{
    public class MetadataWorkspaceFileProvider : IMetadataWorkspaceFileProvider
    {
        private readonly RhetosAppOptions _rhetosAppOptions;
        private readonly ConnectionString _connectionString;
        private readonly ILogger _performanceLogger;
        private readonly ILogger _logger;
        private readonly Lazy<MetadataWorkspace> _loadedMetadata;
        private static readonly object _fileLock = new();

        public MetadataWorkspace MetadataWorkspace => _loadedMetadata.Value;

        public MetadataWorkspaceFileProvider(
            RhetosAppOptions rhetosAppOptions,
            ILogProvider logProvider,
            ConnectionString connectionString,
#pragma warning disable CA1801 // Review unused parameters. DbCofiguration is injected here in order to globally initialize EntityFrameworkConfiguration before doing any operations on EF objects.
            DbConfiguration dbConfiguration)
#pragma warning restore CA1801 // Review unused parameters
        {
            _rhetosAppOptions = rhetosAppOptions;
            _connectionString = connectionString;
            _performanceLogger = logProvider.GetLogger("Performance." + GetType().Name);
            _logger = logProvider.GetLogger(nameof(MetadataWorkspaceFileProvider));

            _loadedMetadata = new Lazy<MetadataWorkspace>(LoadFromFiles);
        }

        private MetadataWorkspace LoadFromFiles()
        {
            var sw = Stopwatch.StartNew();

            var modelFilesPath = EntityFrameworkMapping.ModelFiles.Select(fileName => Path.Combine(_rhetosAppOptions.AssetsFolder, fileName)).ToList();
            SetProviderManifestTokenIfNeeded(sw, modelFilesPath);

            var metadataWorkspace = new MetadataWorkspace(modelFilesPath, Array.Empty<Assembly>());
            _performanceLogger.Write(sw, "Load EDM files.");

            return metadataWorkspace;
        }

        private void SetProviderManifestTokenIfNeeded(Stopwatch sw, List<string> modelFilesPath)
        {
            // The locking is needed to avoid parallel file writes. Even though the MetadataWorkspaceFileProvider class
            // is registered as singleton, in some plugin unit tests there can be multiple parallel instances which causes issues.
            lock (_fileLock)
            {
                string expectedManifestToken = GetDatabaseManifestToken();

                var ssdlFile = modelFilesPath.Single(path => path.EndsWith(".ssdl"));
                string ssdlFirstLine = ReadFirstLine(ssdlFile);
                var existingManifestToken = _manifestTokenRegex.Match(ssdlFirstLine).Groups["token"];
                _performanceLogger.Write(sw, "Checked if ProviderManifestToken is set.");

                if (!existingManifestToken.Success)
                    throw new FrameworkException($"Cannot find ProviderManifestToken attribute in '{ssdlFile}'.");

                if (existingManifestToken.Value != expectedManifestToken)
                {
                    if (existingManifestToken.Value == EntityFrameworkMappingGenerator.ProviderManifestTokenPlaceholder)
                        _logger.Trace($@"Setting ProviderManifestToken to {expectedManifestToken}.");
                    else
                        _logger.Warning($@"Changing ProviderManifestToken from {existingManifestToken.Value} to {expectedManifestToken}.");

                    var lines = File.ReadAllLines(ssdlFile, Encoding.UTF8);
                    lines[0] = string.Concat(
                        ssdlFirstLine.AsSpan(0, existingManifestToken.Index),
                        expectedManifestToken,
                        ssdlFirstLine.AsSpan(existingManifestToken.Index + existingManifestToken.Length));
                    File.WriteAllLines(ssdlFile, lines, Encoding.UTF8);

                    _performanceLogger.Write(sw, $"Initialized {Path.GetFileName(ssdlFile)}.");
                }
            }
        }

        private static readonly Regex _manifestTokenRegex = new Regex(@"ProviderManifestToken=""(?<token>.*?)""");

        private string GetDatabaseManifestToken()
        {
            _logger.Trace("Resolving ProviderManifestToken.");
            if (string.IsNullOrEmpty(_connectionString))
                throw new FrameworkException("Connection string is not provided.");

            using (var connection = new SqlConnection(_connectionString))
            {
                return new DefaultManifestTokenResolver().ResolveManifestToken(connection);
            }
        }

        private string ReadFirstLine(string ssdlFile)
        {
            using (var reader = new StreamReader(ssdlFile, Encoding.UTF8))
            {
                return reader.ReadLine();
            }
        }
    }
}
