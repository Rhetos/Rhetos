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
using Rhetos.TestCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Deployment.Test
{
    [TestClass]
    public class PackageDownloaderTest
    {
        [TestMethod]
        public void SortPackagesByDependencies()
        {
            TestDependencies("a, b, c", "a, b, c");
            TestDependencies("c, b, a", "a, b, c"); // Default sort by name.
            TestDependencies("a, b, c, b-a", "b, a, c");
            TestDependencies("", "");
            TestDependencies("b-a, c-b, d-c", "d, c, b, a");
            TestDependencies("a-b, b-c, c-d", "a, b, c, d");
            TestDependencies("A, b-a, c-B, d-C", "d, c, b, a");
        }

        private static void TestDependencies(string test, params string[] expectedOrder)
        {
            var packagesById = new Dictionary<string, InstalledPackage>(StringComparer.OrdinalIgnoreCase);
            var entries = test.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            var allPackageIds = entries.Where(e => !e.Contains('-'))
                .Concat(entries.Where(e => e.Contains('-')).SelectMany(e => e.Split('-')));
            foreach (var packageId in allPackageIds.Distinct(StringComparer.OrdinalIgnoreCase))
                packagesById.Add(packageId, new InstalledPackage(packageId, "0.0", new List<PackageRequest>(), "folder", new PackageRequest(), "source", "0.0"));

            foreach (var dependency in entries.Where(e => e.Contains('-')).Select(e => e.Split('-')))
                ((List<PackageRequest>)(packagesById[dependency[1]].Dependencies))
                    .Add(new PackageRequest { Id = dependency[0] });

            var packages = packagesById.Values.ToList();
            var sortMethod = typeof(PackageDownloader).GetMethod("SortByDependencies", BindingFlags.Static | BindingFlags.NonPublic);
            sortMethod.Invoke(null, new object[] { packages });

            var actualOrder = TestUtility.Dump(packages, p => p.Id);
            Assert.IsTrue(expectedOrder.Contains(actualOrder, StringComparer.OrdinalIgnoreCase), "Test '" + test + "' resulted with '" + actualOrder + "'.");
        }
    }
}
