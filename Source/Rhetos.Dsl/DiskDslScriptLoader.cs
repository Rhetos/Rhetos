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

using Rhetos.Deployment;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Rhetos.Dsl
{
    public class DiskDslScriptLoader : IDslScriptsProvider
    {
        private readonly Lazy<IEnumerable<DslScript>> _scripts;

        public DiskDslScriptLoader(IInstalledPackages installedPackages)
        {
            _scripts = new Lazy<IEnumerable<DslScript>>(() => LoadScripts(installedPackages));
        }

        public IEnumerable<DslScript> DslScripts => _scripts.Value;

        const string DslScriptsSubfolder = "DslScripts";

        private List<DslScript> LoadScripts(IInstalledPackages installedPackages)
        {
            var scripts = new List<DslScript>();

            foreach (var package in installedPackages.Packages)
            {
                if (!Directory.Exists(package.Folder))
                    throw new FrameworkException($"Source folder for package '{package.Id}' does not exist: '{package.Folder}'.");
                string dslScriptsFolder = Path.Combine(package.Folder, DslScriptsSubfolder);
                if (Directory.Exists(dslScriptsFolder))
                {
                    var baseFolder = Path.GetFullPath(dslScriptsFolder);
                    if (baseFolder.Last() != '\\') baseFolder += '\\';

                    var files = Directory.GetFiles(baseFolder, "*.rhe", SearchOption.AllDirectories).OrderBy(path => path);

                    var packageScripts = files.Select(file =>
                        new DslScript
                        {
                            // Using package.Id instead of full package subfolder name, in order to keep the same script path between different versions of the package (the folder name will contain the version number).
                            Name = package.Id + "\\" + file.Substring(baseFolder.Length),
                            Script = File.ReadAllText(file, Encoding.Default),
                            Path = file
                        });

                    scripts.AddRange(packageScripts);
                }
            }

            return scripts;
        }
    }
}