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
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Rhetos.Deployment
{
    public class InstalledPackages : IInstalledPackages
    {
        private readonly RhetosAppEnvironment _rhetosAppEnvironment;
        private readonly ILogger _logger;

        public InstalledPackages(RhetosAppEnvironment rhetosAppEnvironment, ILogProvider logProvider)
        {
            _rhetosAppEnvironment = rhetosAppEnvironment;
            _logger = logProvider.GetLogger(GetType().Name);
            _packages = new Lazy<IEnumerable<InstalledPackage>>(Load);
        }

        public IEnumerable<InstalledPackage> Packages => _packages.Value;

        private Lazy<IEnumerable<InstalledPackage>> _packages;

        private const string PackagesFileName = "InstalledPackages.json";

        private static string PackagesFilePath(RhetosAppEnvironment rhetosAppEnvironment) => Path.Combine(rhetosAppEnvironment.GeneratedFolder, PackagesFileName);

        private IEnumerable<InstalledPackage> Load()
        {
            string serialized = File.ReadAllText(PackagesFilePath(_rhetosAppEnvironment), Encoding.UTF8);
            var packages = (IEnumerable<InstalledPackage>)JsonConvert.DeserializeObject(serialized, _serializerSettings);

            // Package folder is saved as relative path, to allow moving the deployed folder.
            foreach (var package in packages)
                package.SetAbsoluteFolderPath(_rhetosAppEnvironment.RootPath);

            foreach (var package in packages)
                _logger.Trace(() => package.Report());

            return packages;
        }

        public static void Save(IEnumerable<InstalledPackage> packages, RhetosAppEnvironment rhetosAppEnvironment)
        {
            CsUtility.Materialize(ref packages);

            // Package folder is saved as relative path, to allow moving the deployed folder.
            foreach (var package in packages)
                package.SetRelativeFolderPath(rhetosAppEnvironment.RootPath);

            string serialized = JsonConvert.SerializeObject(packages, _serializerSettings);

            foreach (var package in packages)
                package.SetAbsoluteFolderPath(rhetosAppEnvironment.RootPath);

            File.WriteAllText(PackagesFilePath(rhetosAppEnvironment), serialized, Encoding.UTF8);
        }

        private static readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            PreserveReferencesHandling = PreserveReferencesHandling.All,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
            TypeNameHandling = TypeNameHandling.All,
            Formatting = Formatting.Indented
        };
    }
}
