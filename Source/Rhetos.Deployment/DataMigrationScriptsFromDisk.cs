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
        const string DataMigrationScriptsSubfolder = "DataMigration";

        protected readonly IInstalledPackages _installedPackages;

        public DataMigrationScriptsFromDisk(IInstalledPackages installedPackages)
        {
            _installedPackages = installedPackages;
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
                if (!Directory.Exists(package.Folder))
                    throw new FrameworkException($"Source folder for package '{package.Id}' does not exist: '{package.Folder}'.");
                string dataMigrationScriptsFolder = Path.Combine(package.Folder, DataMigrationScriptsSubfolder);
                if (Directory.Exists(dataMigrationScriptsFolder))
                {
                    var files = Directory.GetFiles(dataMigrationScriptsFolder, "*.*", SearchOption.AllDirectories);

                    const string expectedExtension = ".sql";
                    var badFile = files.FirstOrDefault(file => Path.GetExtension(file).ToLower() != expectedExtension);
                    if (badFile != null)
                        throw new FrameworkException("Data migration script '" + badFile + "' does not have expected extension '" + expectedExtension + "'.");

                    int baseFolderLength = GetFullPathLength(dataMigrationScriptsFolder);

                    var packageScripts =
                        (from file in files
                         let scriptRelativePath = Path.GetFullPath(file).Substring(baseFolderLength)
                         let scriptContent = File.ReadAllText(file, Encoding.Default)
                         select new DataMigrationScript
                         {
                             Tag = ParseScriptTag(scriptContent, file),
                             // Using package.Id instead of full package subfolder name, in order to keep the same script path between different versions of the package (the folder name will contain the version number).
                             Path = package.Id + "\\" + scriptRelativePath,
                             Content = scriptContent
                         }).ToList();

                    packageScripts.Sort();
                    allScripts.AddRange(packageScripts);
                }
            }

            var badGroup = allScripts.GroupBy(s => s.Tag).FirstOrDefault(g => g.Count() >= 2);
            if (badGroup != null)
                throw new FrameworkException(string.Format(
                    "Data migration scripts '{0}' and '{1}' have same tag '{2}' in their headers.",
                    badGroup.First().Path, badGroup.ElementAt(1).Path, badGroup.Key));

            return allScripts;
        }

        protected static int GetFullPathLength(string dataMigrationScriptsFolder)
        {
            dataMigrationScriptsFolder = Path.GetFullPath(dataMigrationScriptsFolder);
            if (dataMigrationScriptsFolder.Last() != '\\')
                dataMigrationScriptsFolder = dataMigrationScriptsFolder + '\\';
            return Path.GetFullPath(dataMigrationScriptsFolder).Length;
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
