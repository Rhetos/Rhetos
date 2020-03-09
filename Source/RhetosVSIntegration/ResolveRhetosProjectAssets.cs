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

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Rhetos;
using Rhetos.Deployment;
using System.Linq;

namespace RhetosVSIntegration
{
    public class ResolveRhetosProjectAssets : Task
    {
        [Required]
        public string ProjectDirectory { get; set; }

        [Required]
        public string AssemblyName { get; set; }

        [Required]
        public ITaskItem[] Assemblies { get; set; }

        [Required]
        public ITaskItem[] ProjectContentFiles { get; set; }

        public override bool Execute()
        {
            var nuget = new NuGetUtilities(ProjectDirectory, ProjectContentFiles.Select(x => x.ItemSpec), new NuGetLogger(Log), null);
            var rhetosProjectAssets = new RhetosProjectAssets
            {
                InstalledPackages = new InstalledPackages { Packages = nuget.GetInstalledPackages() },
                Assemblies = Assemblies.Select(x => x.ItemSpec),
                OutputAssemblyName = AssemblyName
            };

            new RhetosProjectAssetsFileProvider(ProjectDirectory, new VSLogProvider(Log)).Save(rhetosProjectAssets);

            return true;
        }
    }
}
