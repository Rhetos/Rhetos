/*
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

using Rhetos.Logging;
using Rhetos.Persistence;
using Rhetos.Utilities;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Rhetos.Dom.DefaultConcepts.Persistence
{
    public class EntityFrameworkMetadata
    {
        private readonly ILogger _performanceLogger;
        private readonly ILogger _logger;
        private MetadataWorkspace _metadataWorkspace;
        private bool _initialized;
        private readonly object _initializationLock = new object();
        private readonly RhetosAppEnvironment _rhetosAppEnvironment;
        private readonly ConnectionString _connectionString;

        public EntityFrameworkMetadata(RhetosAppEnvironment rhetosAppEnvironment, ILogProvider logProvider, ConnectionString connectionString)
        {
            _performanceLogger = logProvider.GetLogger("Performance");
            _logger = logProvider.GetLogger(nameof(EntityFrameworkMetadata));
            _rhetosAppEnvironment = rhetosAppEnvironment;
            _connectionString = connectionString;
        }

        public MetadataWorkspace MetadataWorkspace
        {
            get
            {
                if (!_initialized)
                    lock (_initializationLock)
                        if (!_initialized)
                        {
                            var sw = Stopwatch.StartNew();

                            var modelFilesPath = EntityFrameworkMapping.ModelFiles.Select(fileName => Path.Combine(_rhetosAppEnvironment.AssetsFolder, fileName)).ToList();
                            SetProviderManifestTokenIfNeeded(sw, modelFilesPath);

                            _metadataWorkspace = new MetadataWorkspace(modelFilesPath, new Assembly[] { });
                            _performanceLogger.Write(sw, $"{nameof(EntityFrameworkMetadata)}: Load EDM files.");

                            _initialized = true;
                        }

                return _metadataWorkspace;
            }
        }

        private void SetProviderManifestTokenIfNeeded(Stopwatch sw, List<string> modelFilesPath)
        {
            string expectedManifestToken = GetDatabaseManifestToken();

            var ssdlFile = modelFilesPath.Single(path => path.EndsWith(".ssdl"));
            string ssdlFirstLine = ReadFirstLine(ssdlFile);
            var existingManifestToken = _manifestTokenRegex.Match(ssdlFirstLine).Groups["token"];
            _performanceLogger.Write(sw, $@"{nameof(EntityFrameworkMetadata)}: Checked if ProviderManifestToken is set.");

            if (!existingManifestToken.Success)
                throw new FrameworkException($"Cannot find ProviderManifestToken attribute in '{ssdlFile}'.");

            if (existingManifestToken.Value != expectedManifestToken)
            {
                if (existingManifestToken.Value == EntityFrameworkMappingGenerator.ProviderManifestTokenPlaceholder)
                    _logger.Trace($@"Setting ProviderManifestToken to {expectedManifestToken}.");
                else
                    _logger.Info($@"Changing ProviderManifestToken from {existingManifestToken.Value} to {expectedManifestToken}.");

                var lines = File.ReadAllLines(ssdlFile, Encoding.UTF8);
                lines[0] = ssdlFirstLine.Substring(0, existingManifestToken.Index)
                    + expectedManifestToken
                    + ssdlFirstLine.Substring(existingManifestToken.Index + existingManifestToken.Length);
                File.WriteAllLines(ssdlFile, lines, Encoding.UTF8);

                _performanceLogger.Write(sw, $@"{nameof(EntityFrameworkMetadata)}: Initialized {Path.GetFileName(ssdlFile)}.");
            }
        }

        private static readonly Regex _manifestTokenRegex = new Regex(@"ProviderManifestToken=""(?<token>.*?)""");

        private string GetDatabaseManifestToken()
        {
            _logger.Trace("Resolving ProviderManifestToken.");
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
