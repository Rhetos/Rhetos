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
using Newtonsoft.Json.Serialization;
using Rhetos.Logging;
using Rhetos.Utilities;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Rhetos.Deployment
{
    public class InstalledPackagesProvider
    {
        private readonly ILogger _logger;
        private readonly string _packagesFilePath;

        public InstalledPackagesProvider(ILogProvider logProvider, IAssetsOptions assetsOptions)
        {
            _logger = logProvider.GetLogger(GetType().Name);
            _packagesFilePath = Path.Combine(assetsOptions.AssetsFolder, PackagesFileName);
        }

        private const string PackagesFileName = "InstalledPackages.json";

        public InstalledPackages Load()
        {
            var installedPackages = JsonUtility.DeserializeFromFile<InstalledPackages>(_packagesFilePath, _serializerSettings);

            foreach (var package in installedPackages.Packages)
                _logger.Trace(() => package.Report());

            foreach (var package in installedPackages.Packages)
                package.ConvertToCrossPlatformPaths();

            return installedPackages;
        }

        internal void Save(InstalledPackages installedPackages)
        {
            // The _serializerSettings are configured to remove the physical paths of folders and files,
            // because those paths should only be available at build-time, and any plugin that is trying to use it should get an exception.
            // Package content files are not available at runtime, they are considered as a part of local cache on build machine.
            JsonUtility.SerializeToFile(installedPackages, _packagesFilePath, _serializerSettings);
        }

        private static readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.None,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            Formatting = Formatting.Indented,
            ContractResolver = new RemoveBuildTimePathsOnSerialization()
        };

        /// <summary>
        /// Removing folder and file paths that were available at build-time, to avoid accidentally using them at run-time.
        /// This includes <see cref="InstalledPackage.Folder"/> and <see cref="ContentFile.PhysicalPath"/>.
        /// </summary>
        private class RemoveBuildTimePathsOnSerialization : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                JsonProperty property = base.CreateProperty(member, memberSerialization);

                if (member.DeclaringType == typeof(InstalledPackage) && member.Name == nameof(InstalledPackage.Folder)
                    || member.DeclaringType == typeof(ContentFile) && member.Name == nameof(ContentFile.PhysicalPath))
                {
                    property.ShouldSerialize = _ => false;
                }
                return property;
            }
        }
    }
}
