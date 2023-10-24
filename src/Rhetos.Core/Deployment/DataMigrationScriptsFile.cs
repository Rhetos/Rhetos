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

using Newtonsoft.Json;
using Rhetos.Logging;
using Rhetos.Utilities;
using System.Diagnostics;
using System.IO;

namespace Rhetos.Deployment
{
    public class DataMigrationScriptsFile : IDataMigrationScriptsFile
    {
        private const string DataMigrationScriptsFileName = "DataMigrationScripts.json";

        private readonly ILogger _performanceLogger;
        private readonly string _dataMigrationScriptsFilePath;

        public DataMigrationScriptsFile(IAssetsOptions assetsOptions, ILogProvider logProvider)
        {
            _performanceLogger = logProvider.GetLogger("Performance." + GetType().Name);
            _dataMigrationScriptsFilePath = Path.Combine(assetsOptions.AssetsFolder, DataMigrationScriptsFileName);
        }

        /// <summary>
        /// The scripts are sorted by the intended execution order,
        /// respecting package dependencies and natural sort order by path.
        /// </summary>
        public DataMigrationScripts Load()
        {
            var stopwatch = Stopwatch.StartNew();

            if (!File.Exists(_dataMigrationScriptsFilePath))
                throw new FrameworkException($@"The file '{_dataMigrationScriptsFilePath}' with data-migration scripts is missing. Please check that the build has completed successfully before updating the database.");

            var dataMigrationScripts = JsonUtility.DeserializeFromFile<DataMigrationScripts>(_dataMigrationScriptsFilePath);

            dataMigrationScripts.ConvertToCrossPlatformPaths();

            _performanceLogger.Write(stopwatch, $"Loaded {dataMigrationScripts.Scripts.Count} scripts from generated file.");

            return dataMigrationScripts;
        }

        public void Save(DataMigrationScripts dataMigrationScripts)
        {
            var stopwatch = Stopwatch.StartNew();
            JsonUtility.SerializeToFile(dataMigrationScripts, _dataMigrationScriptsFilePath);
            _performanceLogger.Write(stopwatch, $"Saved {dataMigrationScripts.Scripts.Count} scripts to generated file.");
        }
    }
}
