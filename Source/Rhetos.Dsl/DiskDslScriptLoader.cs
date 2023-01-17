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

namespace Rhetos.Dsl
{
    public class DiskDslScriptLoader : IDslScriptsProvider
    {
        private readonly Lazy<List<DslScript>> _scripts;
        private readonly FilesUtility _filesUtility;
        private readonly string _projectFolderPrefix;

        public DiskDslScriptLoader(InstalledPackages installedPackages, FilesUtility filesUtility, RhetosBuildEnvironment rhetosBuildEnvironment)
        {
            _scripts = new Lazy<List<DslScript>>(() => LoadScripts(installedPackages));
            _filesUtility = filesUtility;

            _projectFolderPrefix = rhetosBuildEnvironment?.ProjectFolder;
            if (_projectFolderPrefix != null && !_projectFolderPrefix.EndsWith(Path.DirectorySeparatorChar) && !_projectFolderPrefix.EndsWith(Path.AltDirectorySeparatorChar))
                _projectFolderPrefix += Path.DirectorySeparatorChar;
        }

        public IEnumerable<DslScript> DslScripts => _scripts.Value;

        const string DslScriptsSubfolder = "DslScripts"; // For referenced projects and packages.
        private static readonly string DslScriptsSubfolderPrefix = DslScriptsSubfolder + Path.DirectorySeparatorChar;

        private List<DslScript> LoadScripts(InstalledPackages installedPackages)
        {
            return installedPackages.Packages.SelectMany(LoadPackageScripts).ToList();
        }

        private IEnumerable<DslScript> LoadPackageScripts(InstalledPackage package)
        {
            return from file in package.ContentFiles
                   let dslScriptSubpath = TryGetDslScriptSubpath(file)
                   where dslScriptSubpath != null
                   orderby file.InPackagePath
                   select new DslScript
                   {
                       // Using package.Id instead of full package folder name, in order to keep the same script path between different versions of the package (the folder name will contain the version number).
                       Name = Path.Combine(package.Id, dslScriptSubpath),
                       Script = _filesUtility.ReadAllText(file.PhysicalPath),
                       Path = file.PhysicalPath
                   };
        }

        private string TryGetDslScriptSubpath(ContentFile file)
        {
            if (!string.Equals(Path.GetExtension(file.InPackagePath), ".rhe", StringComparison.OrdinalIgnoreCase))
                return null;

            if (file.InPackagePath.StartsWith(DslScriptsSubfolderPrefix, StringComparison.OrdinalIgnoreCase))
                return file.InPackagePath.Substring(DslScriptsSubfolderPrefix.Length);

            // DSL scripts in current project don't need to be in the specific subfolder.
            if (file.PhysicalPath.StartsWith(_projectFolderPrefix, StringComparison.OrdinalIgnoreCase))
                return file.PhysicalPath.Substring(_projectFolderPrefix.Length);

            return null;
        }
    }
}
