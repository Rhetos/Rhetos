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
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Rhetos.Deployment
{
    public class DataMigrationScriptsFromDisk : IDataMigrationScriptsProvider
    {
        const string DataMigrationScriptsFileName = "DataMigrationScripts.json";

        private readonly RhetosAppOptions _rhetosAppOptions;
        private readonly ILogger _performanceLogger;
        private List<DataMigrationScript> _scripts;

        public IEnumerable<string> Dependencies => new List<string>();

        public DataMigrationScriptsFromDisk(RhetosAppOptions rhetosAppOptions,
            ILogProvider logProvider)
        {
            _rhetosAppOptions = rhetosAppOptions;
            _performanceLogger = logProvider.GetLogger("Performance");
        }

        /// <summary>
        /// The scripts are sorted by the intended execution order,
        /// respecting package dependencies and natural sort order by path.
        /// </summary>
        public List<DataMigrationScript> Load()
        {
            if (_scripts == null)
            {
                var stopwatch = Stopwatch.StartNew();
                var dataMigrationScriptsFilePath = Path.Combine(_rhetosAppOptions.AssetsFolder, DataMigrationScriptsFileName);
                if (!File.Exists(dataMigrationScriptsFilePath))
                    throw new FrameworkException($@"The file {dataMigrationScriptsFilePath} that is used to execute the data migration is missing.");
                var serializedConcepts = File.ReadAllText(dataMigrationScriptsFilePath, Encoding.UTF8);
                _scripts =  JsonConvert.DeserializeObject<List<DataMigrationScript>>(serializedConcepts);
                _performanceLogger.Write(stopwatch, $@"DataMigrationScriptsFromDisk: Loaded {_scripts.Count} scripts from generated file.");
            }

            return _scripts;
        }
    }
}
