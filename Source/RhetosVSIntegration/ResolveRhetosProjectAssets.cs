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

        public override bool Execute()
        {
            var nuget = new NuGetUtilities(ProjectDirectory, new NuGetLogger(Log), null);
            var rhetosProjectAssets = new RhetosProjectAssets
            {
                InstalledPackages = new InstalledPackages { Packages = nuget.GetInstalledPackages() },
                Assemblies = Assemblies.Select(x => x.ItemSpec),
                OutputAssemblyName = AssemblyName
            };

            var assetsFileHasChanged = new RhetosProjectAssetsFileProvider(ProjectDirectory).Save(rhetosProjectAssets);
            if (!assetsFileHasChanged)
                Log.LogMessage(MessageImportance.High, $"Writing to file {RhetosProjectAssetsFileProvider.ProjectAssetsFileName} will be skipped because the file has not been changed.");

            return true;
        }
    }
}
