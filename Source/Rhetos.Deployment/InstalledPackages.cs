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
        private readonly ILogger _logger;

        public InstalledPackages(ILogProvider logProvider)
        {
            _logger = logProvider.GetLogger(GetType().Name);
            _packages = new Lazy<IEnumerable<InstalledPackage>>(Load);
        }

        public IEnumerable<InstalledPackage> Packages { get { return _packages.Value; } }

        private Lazy<IEnumerable<InstalledPackage>> _packages;
        private const string PackagesFileName = "InstalledPackages.json";
        private static string GetPackagesFilePath() { return Path.Combine(Paths.BinFolder, PackagesFileName); }

        private IEnumerable<InstalledPackage> Load()
        {
            string serialized = File.ReadAllText(GetPackagesFilePath(), Encoding.UTF8);
            var packages = (IEnumerable<InstalledPackage>)JsonConvert.DeserializeObject(serialized, _serializerSettings);

            foreach (var package in packages)
                _logger.Trace(() => package.Report());

            return packages;
        }

        public static void Save(IEnumerable<InstalledPackage> packages)
        {
            CsUtility.Materialize(ref packages);
            string serialized = JsonConvert.SerializeObject(packages, _serializerSettings);
            File.WriteAllText(GetPackagesFilePath(), serialized, Encoding.UTF8);
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
