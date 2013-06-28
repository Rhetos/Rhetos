/*
    Copyright (C) 2013 Omega software d.o.o.

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
using System.IO;
using System.Linq;
using Ionic.Zip;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Deployment;
using Rhetos.TestCommon;

namespace Rhetos.Deployment.Test
{
    [TestClass]
    public class PackageCompilerShould
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        [DeploymentItem("TestData")]
        public void CreatePackage()
        {
            string packageSourceFolder = Path.Combine(TestContext.TestDeploymentDir, "TestPackage1");
            string packageFileName = PackageCompiler.CreatePackage(packageSourceFolder);
            Assert.IsTrue(File.Exists(packageFileName));

            string[] sourceFiles = Directory.GetFiles(packageSourceFolder, "*", SearchOption.AllDirectories);
            var folderPrefiks = Path.GetFullPath(packageSourceFolder) + "/";
            var expectedFiles = sourceFiles.Select(file =>
                    file.Substring(folderPrefiks.Length)
                    .Replace(@"\", "/")
                    .Replace("ForDeployment/", "")
                ).ToArray();

            TestUtility.DumpSorted(expectedFiles);
            var allExpectedFiles = string.Join(", ", expectedFiles);
            TestUtility.AssertContains(allExpectedFiles, "PackageInfo.xml");
            TestUtility.AssertContains(allExpectedFiles, ".sql");
            TestUtility.AssertContains(allExpectedFiles, ".rhe");
            TestUtility.AssertNotContains(allExpectedFiles, ".zip");

            ZipFile zipFile = ZipFile.Read(packageFileName);
            var zipFiles = zipFile.Entries
                .Where(entry => !entry.IsDirectory)
                .Select(entry => entry.FileName);
            Assert.AreEqual(TestUtility.DumpSorted(expectedFiles), TestUtility.DumpSorted(zipFiles));
        }
    }
}