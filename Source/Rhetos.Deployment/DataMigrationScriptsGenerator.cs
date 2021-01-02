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

using Rhetos.Extensibility;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Rhetos.Deployment
{
    /// <summary>
    /// Generates data migration script from provided SQL scripts.
    /// </summary>
    public class DataMigrationScriptsGenerator : IGenerator
    {
        const string DataMigrationSubfolder = "DataMigration";
        const string DataMigrationSubfolderPrefix = DataMigrationSubfolder + @"\";

        protected readonly InstalledPackages _installedPackages;
        private readonly FilesUtility _filesUtility;
        private readonly IDataMigrationScriptsFile _dataMigrationScriptsFile;

        public IEnumerable<string> Dependencies => new List<string>();

        public DataMigrationScriptsGenerator(InstalledPackages installedPackages,
            FilesUtility filesUtility,
            IDataMigrationScriptsFile dataMigrationScriptsFile)
        {
            _installedPackages = installedPackages;
            _filesUtility = filesUtility;
            _dataMigrationScriptsFile = dataMigrationScriptsFile;
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
                    throw new FrameworkException($"Data migration script '{badFile.PhysicalPath}' does not have expected extension '{expectedExtension}'.");

                var packageSqlFiles =
                    (from file in files
                     let scriptContent = _filesUtility.ReadAllText(file.PhysicalPath)
                     select new
                     {
                         Header = ParseScriptHeader(scriptContent, file.PhysicalPath),
                         // Using package.Id instead of full package subfolder name, in order to keep the same script path between different versions of the package (the folder name will contain the version number).
                         Path = package.Id + "\\" + file.InPackagePath.Substring(DataMigrationSubfolderPrefix.Length),
                         Content = scriptContent
                     }).ToList();

                // Early check for better error messages:
                CheckDuplicateTags(packageSqlFiles.Where(s => s.Header.IsDowngradeScript).Select(s => (s.Header.Tag, s.Path)));
                CheckDuplicateTags(packageSqlFiles.Where(s => !s.Header.IsDowngradeScript).Select(s => (s.Header.Tag, s.Path)));

                var packageScripts = packageSqlFiles.Where(sqlFile => !sqlFile.Header.IsDowngradeScript).Select(upSqlFile => new DataMigrationScript
                {
                    Tag = upSqlFile.Header.Tag,
                    Path = upSqlFile.Path,
                    Content = upSqlFile.Content,
                    Down = null
                }).ToDictionary(script => script.Tag);

                foreach (var downSqlFile in packageSqlFiles.Where(sqlFile => sqlFile.Header.IsDowngradeScript))
                {
                    if (!packageScripts.TryGetValue(downSqlFile.Header.Tag, out var upScript))
                        throw new FrameworkException($"There is no matching 'up' data-migration script for the 'down' script '{downSqlFile.Path}': Cannot find the same tag '{downSqlFile.Header.Tag}' in the up scripts.");

                    string expectedDownPath = upScript.Path.Insert(upScript.Path.Length - 4, ".down");
                    if (!downSqlFile.Path.Equals(expectedDownPath, StringComparison.OrdinalIgnoreCase))
                        throw new FrameworkException($"Data-migration 'down' script '{downSqlFile.Path}' should have same file name as the related 'up' script with added suffix \".down\": {expectedDownPath}.");

                    if (upScript.Down != null) // This is just an internal consistency validation, this error is not expected to occur.
                        throw new FrameworkException($"The 'up' data-migration script with tag '{downSqlFile.Header.Tag}' is already mapped to another down script. Cannot map '{downSqlFile.Path}'.");

                    upScript.Down = downSqlFile.Content;
                }

                var packageScriptsSorted = packageScripts.Values.ToList();
                packageScriptsSorted.Sort();
                allScripts.AddRange(packageScriptsSorted);
            }

            CheckDuplicateTags(allScripts.Select(s => (s.Tag, s.Path)));

            _dataMigrationScriptsFile.Save(new DataMigrationScripts { Scripts = allScripts });
        }

        private void CheckDuplicateTags(IEnumerable<(string Tag, string Path)> scripts)
        {
            var badGroup = scripts.GroupBy(s => s.Tag).FirstOrDefault(g => g.Count() >= 2);
            if (badGroup != null)
            {
                var script1 = badGroup.First();
                var script2 = badGroup.ElementAt(1);
                string message = $"Data migration scripts '{script1.Path}' and '{script2.Path}' have same tag '{badGroup.Key}' in their headers.";

                if (script1.Path.IndexOf(".down.", StringComparison.OrdinalIgnoreCase) >= 0
                    != script2.Path.IndexOf(".down.", StringComparison.OrdinalIgnoreCase) >= 0)
                    message += $" Note that the 'down' script should have \"DATAMIGRATION-DOWN\" label in the header.";
                
                throw new FrameworkException(message);
            }
        }

        private static readonly Regex ScriptHeaderRegex = new Regex(@"^/\*DATAMIGRATION(?<down>-DOWN)? (?<tag>.+)\*/");

        private (string Tag, bool IsDowngradeScript) ParseScriptHeader(string scriptContent, string file)
        {
            var match = ScriptHeaderRegex.Match(scriptContent);
            if (!match.Success)
                throw new FrameworkException($"Data migration script '{file}' should start with a header '/*DATAMIGRATION unique_script_identifier*/'.");
            bool down = match.Groups["down"].Success;
            string tag = match.Groups["tag"].Value.Trim();
            if (string.IsNullOrEmpty(tag) || tag.Contains("\n"))
                throw new FrameworkException($"Data migration script '{file}' has invalid header. It should start with a header '/*DATAMIGRATION unique_script_identifier*/'");
            return (tag, down);
        }
    }
}
