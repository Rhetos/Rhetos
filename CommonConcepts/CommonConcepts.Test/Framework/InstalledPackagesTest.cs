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
using Rhetos.Deployment;
using Rhetos.Dom.DefaultConcepts;
using System;
using System.Linq;

namespace CommonConcepts.Test.Framework
{
    [TestClass]
    public class InstalledPackagesTest
    {
        [TestMethod]
        public void RemovedPhysicalPathsAtRuntime()
        {
            using (var scope = TestScope.Create())
            {
                var installedPackages = scope.Resolve<InstalledPackages>();

                Assert.AreEqual("", string.Join(", ",
                    installedPackages.Packages.Where(p => p.Folder != null).Select(p => $"{p.Id}: {p.Folder}")));

                Assert.AreEqual("", string.Join(", ",
                    installedPackages.Packages.SelectMany(p => p.ContentFiles).Where(f => f.PhysicalPath != null).Select(f => $"{f.InPackagePath}: {f.PhysicalPath}")));
            }
        }
    }
}