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
using Rhetos.Utilities;
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
        private readonly FilesUtility _filesUtility;

        public DiskDslScriptLoader(IInstalledPackages installedPackages, FilesUtility filesUtility)
        {
            _scripts = new Lazy<IEnumerable<DslScript>>(() => LoadScripts(installedPackages));
            _filesUtility = filesUtility;
        }

        public IEnumerable<DslScript> DslScripts => _scripts.Value;

        const string DslScriptsSubfolder = "DslScripts";
        const string DslScriptsSubfolderPrefix = DslScriptsSubfolder + @"\";

        private List<DslScript> LoadScripts(IInstalledPackages installedPackages)
        {
            return installedPackages.Packages.SelectMany(LoadPackageScripts).ToList();
        }

        private IEnumerable<DslScript> LoadPackageScripts(InstalledPackage package)
        {
            return package.ContentFiles
                .Where(file => file.InPackagePath.StartsWith(DslScriptsSubfolderPrefix, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(Path.GetExtension(file.InPackagePath), ".rhe", StringComparison.OrdinalIgnoreCase))
                .OrderBy(file => file.InPackagePath)
                .Select(file =>
                    new DslScript
                    {
                        // Using package.Id instead of full package subfolder name, in order to keep the same script path between different versions of the package (the folder name will contain the version number).
                        Name = package.Id + "\\" + file.InPackagePath.Substring(DslScriptsSubfolderPrefix.Length),
                        Script = _filesUtility.ReadAllText(file.PhysicalPath),
                        Path = file.PhysicalPath
                    });
        }
    }
}