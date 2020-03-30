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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Utilities;
using Rhetos;
using System.IO;
using Rhetos.Configuration.Autofac;
using System;

namespace CommonConcepts.Test
{
    [TestClass]
    public class PathsTest
    {
        [TestMethod]
        public void PathsInitializationTest()
        {
            string rootPath;
            using (var rhetos = new RhetosTestContainer_Accessor())
                rootPath = rhetos.GetDefaultRhetosServerRootFolder();
            Console.WriteLine($"rootPath: {rootPath}");

            var configurationProvider = new ConfigurationBuilder()
                .AddRhetosAppEnvironment(new RhetosAppEnvironment {
                    AssemblyFolder = Path.Combine(rootPath, "bin"),
                    AssetsFolder = Path.Combine(rootPath, "bin", "Generated"),
                    LegacyPluginsFolder = Path.Combine(rootPath, "bin", "Plugins"),
                    LegacyAssetsFolder = Path.Combine(rootPath, "Resources")
                }).Build();

            LegacyUtilities.Initialize(configurationProvider);
            Console.WriteLine($"Paths: {Paths.RhetosServerRootPath}");

            Assert.AreEqual(Normalize(rootPath), Normalize(Paths.RhetosServerRootPath));
            Assert.AreEqual(Path.Combine(rootPath, "bin"), Paths.BinFolder);
            Assert.AreEqual(Path.Combine(rootPath, "bin\\Generated"), Paths.GeneratedFolder);
            Assert.AreEqual(Path.Combine(rootPath, "bin\\Plugins"), Paths.PluginsFolder);
            Assert.AreEqual(Path.Combine(rootPath, "Resources"), Paths.ResourcesFolder);
        }

        private string Normalize(string path)
        {
            return Path.GetFullPath(Path.Combine(path, "."));
        }

        private class RhetosTestContainer_Accessor : RhetosTestContainer
        {
            public string GetDefaultRhetosServerRootFolder()
            {
                return base.SearchForRhetosServerRootFolder();
            }
        }
    }
}
