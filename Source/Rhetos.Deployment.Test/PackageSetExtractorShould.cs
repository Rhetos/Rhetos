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
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Deployment;
using System.Linq;
using Rhetos.TestCommon;
namespace Rhetos.Deployment.Test
{
    [TestClass]
    public class PackageSetExtractorShould
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        [DeploymentItem("TestData")]
        public void ReadFileList()
        {
            string source = Path.Combine(TestContext.TestDeploymentDir, "Packages");
            var result = PackageSetExtractor.ReadPackageList(Path.Combine(source, "PackageSet.txt"));

            TestUtility.DumpSorted(result);

            Assert.AreEqual(3, result.Count());
            foreach (var file in result)
                Assert.IsTrue(File.Exists(file));
        }

        [TestMethod]
        [DeploymentItem("TestData")]
        public void ExtractPackages()
        {
            string source = Path.Combine(TestContext.TestDeploymentDir, "Packages");
            var packages = new[] { "TestPackage1_1_2_3_4.zip", "TestPackage2_1_0_0_0.zip" }
                .Select(file => Path.Combine(source, file)).ToArray();

            string target = Path.Combine(TestContext.TestDeploymentDir, "ExtractPackages_" + Guid.NewGuid().ToString().Replace("-", ""));
            target = Path.GetFullPath(target);
            Console.WriteLine(target);

            PackageSetExtractor.ExtractAndCombinePackages(packages, target);

            var extractedFiles = Directory.GetFiles(target, "*", SearchOption.AllDirectories)
                .Select(file => file.Substring(target.Length + 1)).ToArray();

            var expected = new[]
            {
                @"DataMigration\TestPackage1\1.0.1\01 insert data.sql",
                @"DataMigration\TestPackage1\1.0.1\02 transform data.sql",
                @"DslScripts\TestPackage1\Group1\Subgroup1\1 Security.rhe",
                @"DslScripts\TestPackage1\Group1\Subgroup1\9999 Menu.rhe",
                @"DslScripts\TestPackage2\Group1\Subgroup1\1 Security.rhe",
                @"DslScripts\TestPackage2\Group1\Subgroup1\9999 Menu.rhe",
                @"bin\Plugins\file1.bin",
                @"bin\Plugins\file2.bin",
                @"PackageInfo\TestPackage1.xml",
                @"PackageInfo\TestPackage2.xml",
                @"Resources\TestPackage1\Group1\Report1.docx"
            };

            Assert.AreEqual(TestUtility.DumpSorted(expected), TestUtility.DumpSorted(extractedFiles));
        }

        [TestMethod]
        [DeploymentItem("TestData")]
        public void CreateAllDirectories()
        {
            string source = Path.Combine(TestContext.TestDeploymentDir, "Packages");
            var packages = new[] { "TestPackage3_1_0_0_0.zip" }
                .Select(file => Path.Combine(source, file)).ToArray();

            string target = Path.Combine(TestContext.TestDeploymentDir, "ExtractPackages_" + Guid.NewGuid().ToString().Replace("-", ""));
            target = Path.GetFullPath(target);
            Console.WriteLine(target);

            PackageSetExtractor.ExtractAndCombinePackages(packages, target);

            var extractedFiles = Directory.GetDirectories(target, "*", SearchOption.AllDirectories)
                .Select(file => file.Substring(target.Length + 1)).ToArray();
            Assert.AreEqual(@"bin, bin\Plugins, DataMigration, DslScripts, PackageInfo, Resources", TestUtility.DumpSorted(extractedFiles));
        }

        [TestMethod]
        [DeploymentItem("TestData")]
        public void FailOnConflictingFiles()
        {
            string source = Path.Combine(TestContext.TestDeploymentDir, "Packages");
            var packages = new[] { "TestPackage1_1_2_3_4.zip", "TestPackage3_1_0_0_0.zip" }
                .Select(file => Path.Combine(source, file)).ToArray();

            string target = Path.Combine(TestContext.TestDeploymentDir, "ExtractPackages_" + Guid.NewGuid().ToString().Replace("-", ""));
            target = Path.GetFullPath(target);
            Console.WriteLine(target);

            TestUtility.ShouldFail(() => PackageSetExtractor.ExtractAndCombinePackages(packages, target),
                "same file plugin file in two packages", "file2.bin");
        }

        [TestMethod]
        [DeploymentItem("TestData")]
        public void FailOnNonExistingReference()
        {
            string source = Path.Combine(TestContext.TestDeploymentDir, "Packages");
            var packages = new[] { "TestPackage4_1_0_0_0.zip" }
                .Select(file => Path.Combine(source, file)).ToArray();

            string target = Path.Combine(TestContext.TestDeploymentDir, "ExtractPackages_" + Guid.NewGuid().ToString().Replace("-", ""));
            target = Path.GetFullPath(target);
            Console.WriteLine(target);

            TestUtility.ShouldFail(() => PackageSetExtractor.ExtractAndCombinePackages(packages, target),
                "PackageInfo.xml contains reference to nonexisting package", "NonExistingPackage");
        }
    }
}