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

using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Rhetos.Deployment
{
    public class DataMigrationScriptsFromDisk : IDataMigrationScriptsProvider
    {
        const string DataMigrationSubfolder = "DataMigration";
        const string DataMigrationSubfolderPrefix = DataMigrationSubfolder + @"\";

        protected readonly IInstalledPackages _installedPackages;
        private readonly FilesUtility _filesUtility;

        public DataMigrationScriptsFromDisk(IInstalledPackages installedPackages, FilesUtility filesUtility)
        {
            _installedPackages = installedPackages;
            _filesUtility = filesUtility;
        }

        /// <summary>
        /// The scripts are sorted by the intended execution order,
        /// respecting package dependencies and natural sort order by path.
        /// </summary>
        public List<DataMigrationScript> Load()
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

            return allScripts;
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
