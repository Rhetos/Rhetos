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
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Rhetos.Dom.DefaultConcepts.Persistence
{
    public class EntityFrameworkMetadata
    {
        private readonly ILogger _performanceLogger;
        private readonly ILogger _logger;
        private MetadataWorkspace _metadataWorkspace;
        private bool _initialized;
        private readonly object _initializationLock = new object();
        private readonly AssetsOptions _assetsOptions;
        private readonly ConnectionString _connectionString;

        public EntityFrameworkMetadata(AssetsOptions assetsOptions, ILogProvider logProvider, ConnectionString connectionString)
        {
            _performanceLogger = logProvider.GetLogger("Performance");
            _logger = logProvider.GetLogger(nameof(EntityFrameworkMetadata));
            _assetsOptions = assetsOptions;
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

                            SetProviderManifestTokenIfNeeded(sw);

                            var modelFilesPath = EntityFrameworkMapping.ModelFiles.Select(fileName => Path.Combine(_assetsOptions.AssetsFolder, fileName));
                            _metadataWorkspace = new MetadataWorkspace(modelFilesPath, new Assembly[] { });
                            _performanceLogger.Write(sw, $@"{nameof(EntityFrameworkMetadata)}: Load EDM files.");

                            _initialized = true;
                        }

                return _metadataWorkspace;
            }
        }

        private void SetProviderManifestTokenIfNeeded(Stopwatch sw)
        {
            var ssdlFileName = EntityFrameworkMapping.ModelFiles.First(x => x.EndsWith(".ssdl"));
            var ssdlFile = Path.Combine(_assetsOptions.AssetsFolder, ssdlFileName);
            string firstLineInSsdl;
            _logger.Trace("Checking if ProviderManifestToken is set.");
            using (StreamReader reader = new StreamReader(ssdlFile))
            {
                firstLineInSsdl = reader.ReadLine();
            }

            if (firstLineInSsdl.Contains(EntityFrameworkMappingGenerator.ProviderManifestTokenPlaceholder))
            {
                _logger.Info("Resolving ProviderManifestToken.");
                var lines = File.ReadAllLines(ssdlFile);
                using (var connection = new SqlConnection(_connectionString))
                {
                    var manifestToken = new DefaultManifestTokenResolver().ResolveManifestToken(connection);
                    lines[0] = lines[0].Replace(EntityFrameworkMappingGenerator.ProviderManifestTokenPlaceholder, $@"ProviderManifestToken=""{manifestToken}""");
                    File.WriteAllLines(ssdlFile, lines);
                    _logger.Info($@"Set ProviderManifestToken to {manifestToken}.");
                }

                _performanceLogger.Write(sw, $@"{nameof(EntityFrameworkMetadata)}: Initialized {ssdlFileName} file.");
            }
        }
    }
}
