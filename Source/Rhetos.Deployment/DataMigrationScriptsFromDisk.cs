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
    public class DataMigrationScriptsFromDisk : IDataMigrationScriptsProvider, IGenerator
    {
        const string DataMigrationScriptsFileName = "DataMigrationScripts.json";
        const string DataMigrationSubfolder = "DataMigration";
        const string DataMigrationSubfolderPrefix = DataMigrationSubfolder + @"\";

        protected readonly IInstalledPackages _installedPackages;
        private readonly FilesUtility _filesUtility;
        private readonly RhetosAppEnvironment _rhetosAppEnvironment;
        private readonly ILogger _performanceLogger;
        private List<DataMigrationScript> _scripts;

        public IEnumerable<string> Dependencies => new List<string>();

        public DataMigrationScriptsFromDisk(IInstalledPackages installedPackages,
            FilesUtility filesUtility,
            RhetosAppEnvironment rhetosAppEnvironment,
            ILogProvider logProvider)
        {
            _installedPackages = installedPackages;
            _filesUtility = filesUtility;
            _rhetosAppEnvironment = rhetosAppEnvironment;
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
                var dataMigrationScriptsFilePath = Path.Combine(_rhetosAppEnvironment.GeneratedFolder, DataMigrationScriptsFileName);
                if (!File.Exists(dataMigrationScriptsFilePath))
                    throw new FrameworkException($@"The file {dataMigrationScriptsFilePath} that is used to execute the data migration is missing.");
                var serializedConcepts = File.ReadAllText(dataMigrationScriptsFilePath, Encoding.UTF8);
                _scripts =  JsonConvert.DeserializeObject<List<DataMigrationScript>>(serializedConcepts);
                _performanceLogger.Write(stopwatch, $@"DataMigrationScriptsFromDisk: Loaded {_scripts.Count} scripts from generated file.");
            }

            return _scripts;
        }

        public void Generate()
        {
            var allScripts = new List<DataMigrationScript>();

            // The packages are sorted by their dependencies, so the data migration scripts from one module may use the data that was prepared by the module it depends on.
            foreach (var package in _installedPackages.Packages)
            {
                var files = package.ContentFiles.Where(file => file.InPackagePath.StartsWith(DataMigrationSubfolderPrefix, StringComparison.OrdinalIgnoreCase));

                const string expectedExtension = ".sql";
                var badFile = files.FirstOrDefault(file => !string.Equals(Path.GetExtension(file.InPackagePath), expectedExtension, StringComparison.OrdinalIgnoreCase));
                if (badFile != null)
                    throw new FrameworkException("Data migration script '" + badFile.PhysicalPath + "' does not have expected extension '" + expectedExtension + "'.");

                var packageScripts =
                    (from file in files
                     let scriptContent = _filesUtility.ReadAllText(file.PhysicalPath)
                     select new DataMigrationScript
                     {
                         Tag = ParseScriptTag(scriptContent, file.PhysicalPath),
                         // Using package.Id instead of full package subfolder name, in order to keep the same script path between different versions of the package (the folder name will contain the version number).
                         Path = package.Id + "\\" + file.InPackagePath.Substring(DataMigrationSubfolderPrefix.Length),
                         Content = scriptContent
                     }).ToList();

                packageScripts.Sort();
                allScripts.AddRange(packageScripts);
            }

            var badGroup = allScripts.GroupBy(s => s.Tag).FirstOrDefault(g => g.Count() >= 2);
            if (badGroup != null)
                throw new FrameworkException(string.Format(
                    "Data migration scripts '{0}' and '{1}' have same tag '{2}' in their headers.",
                    badGroup.First().Path, badGroup.ElementAt(1).Path, badGroup.Key));

            _scripts = allScripts;
            var stopwatch = Stopwatch.StartNew();
            string serializedMigrationScripts = JsonConvert.SerializeObject(_scripts, Formatting.Indented);
            File.WriteAllText(Path.Combine(_rhetosAppEnvironment.GeneratedFolder, DataMigrationScriptsFileName), serializedMigrationScripts, Encoding.UTF8);
            _performanceLogger.Write(stopwatch, $@"DataMigrationScriptsFromDisk: Saved {_scripts.Count} scripts to generated file.");
        }

        protected static readonly Regex ScriptIdRegex = new Regex(@"^/\*DATAMIGRATION (.+)\*/");

        protected static string ParseScriptTag(string scriptContent, string file)
        {
            if (!ScriptIdRegex.IsMatch(scriptContent))
                throw new FrameworkException("Data migration script '" + file + "' should start with a header '/*DATAMIGRATION unique_script_identifier*/'.");
            string tag = ScriptIdRegex.Match(scriptContent).Groups[1].Value.Trim();
            if (string.IsNullOrEmpty(tag) || tag.Contains("\n"))
                throw new FrameworkException("Data migration script '" + file + "' has invalid header. It should start with a header '/*DATAMIGRATION unique_script_identifier*/'");
            return tag;
        }
    }
}
