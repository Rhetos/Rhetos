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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Rhetos.Deployment
{
    public class DataMigrationScriptsFile
    {
        const string DataMigrationScriptsFileName = "DataMigrationScripts.json";

        private readonly ILogger _performanceLogger;
        private readonly AssetsOptions _assetsOptions;

        public IEnumerable<string> Dependencies => new List<string>();

        public DataMigrationScriptsFile(AssetsOptions assetsOptions, ILogProvider logProvider)
        {
            _performanceLogger = logProvider.GetLogger("Performance");
            _assetsOptions = assetsOptions;
        }

        /// <summary>
        /// The scripts are sorted by the intended execution order,
        /// respecting package dependencies and natural sort order by path.
        /// </summary>
        public DataMigrationScripts Load()
        {
            var stopwatch = Stopwatch.StartNew();
            if (!File.Exists(DataMigrationScriptsFilePath))
                throw new FrameworkException($@"The file {DataMigrationScriptsFilePath} that is used to execute the data migration is missing. Please check that the build has completed successfully before updating the database.");
            var serializedConcepts = File.ReadAllText(DataMigrationScriptsFilePath, Encoding.UTF8);
            var dataMigrationScripts = JsonConvert.DeserializeObject<DataMigrationScripts>(serializedConcepts);
            _performanceLogger.Write(stopwatch, $@"DataMigrationScriptsFromDisk: Loaded {dataMigrationScripts.Scripts.Count} scripts from generated file.");
            return dataMigrationScripts;
        }

        public void Save(DataMigrationScripts dataMigrationScripts)
        {
            var stopwatch = Stopwatch.StartNew();
            string serializedMigrationScripts = JsonConvert.SerializeObject(dataMigrationScripts, Formatting.Indented);
            File.WriteAllText(DataMigrationScriptsFilePath, serializedMigrationScripts, Encoding.UTF8);
            _performanceLogger.Write(stopwatch, $@"DataMigrationScriptsFromDisk: Saved {dataMigrationScripts.Scripts.Count} scripts to generated file.");
        }

        private string DataMigrationScriptsFilePath => Path.Combine(_assetsOptions.AssetsFolder, DataMigrationScriptsFileName);
    }
}
