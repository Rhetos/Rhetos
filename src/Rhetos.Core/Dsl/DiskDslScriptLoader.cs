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
using Rhetos.Logging;
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
        private readonly ILogger _logger;
        private readonly string _projectFolderPrefix;

        public DiskDslScriptLoader(InstalledPackages installedPackages, FilesUtility filesUtility, RhetosBuildEnvironment rhetosBuildEnvironment, ILogProvider logProvider)
        {
            _scripts = new Lazy<List<DslScript>>(() => LoadScripts(installedPackages));
            _filesUtility = filesUtility;
            _logger = logProvider.GetLogger(GetType().Name);

            _projectFolderPrefix = rhetosBuildEnvironment?.ProjectFolder;
            if (_projectFolderPrefix != null && !_projectFolderPrefix.EndsWith(Path.DirectorySeparatorChar) && !_projectFolderPrefix.EndsWith(Path.AltDirectorySeparatorChar))
                _projectFolderPrefix += Path.DirectorySeparatorChar;
        }

        public IEnumerable<DslScript> DslScripts => _scripts.Value;

        const string DslScriptsSubfolder = "DslScripts"; // For referenced projects and packages.
        private static readonly string DslScriptsSubfolderPrefix = DslScriptsSubfolder + Path.DirectorySeparatorChar;

        private List<DslScript> LoadScripts(InstalledPackages installedPackages)
        {
            var scripts = installedPackages.Packages.SelectMany(LoadPackageScripts).ToList();
            _logger.Trace(() => $"Loaded {scripts.Count} DSL scripts:" + string.Concat(scripts.Select(s => $"{Environment.NewLine}{s.Path}: {s.Name}")));
            return scripts;
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
