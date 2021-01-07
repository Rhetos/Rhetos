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
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Rhetos.Deployment.Test
{
    // NOTE: There are more tests in the CommonConceptsTest solution, that use a live database.
    [TestClass]
    public class DataMigrationScriptsGeneratorTest
    {
        [TestMethod]
        public void UpDown()
        {
            var files = new[] { "b.sql", "a.down.sql", "a.sql", "b.down.sql" };
            Assert.AreEqual("a.sql:a/a-down, b.sql:b/b-down", GenerateDataMigrationScriptsAssetsFile(files));
        }

        [TestMethod]
        public void Mixed()
        {
            var files = new[] { "b.sql", "a.down.sql", "a.sql" };
            Assert.AreEqual("a.sql:a/a-down, b.sql:b/-", GenerateDataMigrationScriptsAssetsFile(files));
        }

        [TestMethod]
        public void DownWithoutUp()
        {
            var files = new[] { "a.down.sql", "a.sql", "b.down.sql" };
            TestUtility.ShouldFail<FrameworkException>(
                () => GenerateDataMigrationScriptsAssetsFile(files),
                @"There is no matching 'up' data-migration script for the 'down' script 'TestPackage\b.down.sql': Cannot find the same tag 'BBB76531-76AB-43F3-AE07-868434DEAB7F' in the up scripts.");
        }

        [TestMethod]
        public void DuplicateTag1()
        {
            var files = new[] { "b.sql", "b2.sql" };
            TestUtility.ShouldFail<FrameworkException>(
                () => GenerateDataMigrationScriptsAssetsFile(files),
                @"Data migration scripts 'TestPackage\b.sql' and 'TestPackage\b2.sql' have same tag 'BBB76531-76AB-43F3-AE07-868434DEAB7F' in their headers.");
        }

        [TestMethod]
        public void DuplicateTag2()
        {
            var files = new[] { "b.sql", "b.down.sql", "b2.down.sql" };
            TestUtility.ShouldFail<FrameworkException>(
                () => GenerateDataMigrationScriptsAssetsFile(files),
                @"Data migration scripts 'TestPackage\b.down.sql' and 'TestPackage\b2.down.sql' have same tag 'BBB76531-76AB-43F3-AE07-868434DEAB7F' in their headers.");
        }

        [TestMethod]
        public void DuplicateTag3()
        {
            var files = new[] { "b.sql", "b.down.sql", "b2.sql" };
            TestUtility.ShouldFail<FrameworkException>(
                () => GenerateDataMigrationScriptsAssetsFile(files),
                @"Data migration scripts 'TestPackage\b.sql' and 'TestPackage\b2.sql' have same tag 'BBB76531-76AB-43F3-AE07-868434DEAB7F' in their headers.");
        }

        [TestMethod]
        public void DownNameMismatch1()
        {
            var files = new[] { "b.sql", "b2.down.sql" };
            TestUtility.ShouldFail<FrameworkException>(
                () => GenerateDataMigrationScriptsAssetsFile(files),
                @"Data-migration 'down' script 'TestPackage\b2.down.sql' should have same file name as the related 'up' script with added suffix "".down"": TestPackage\b.down.sql.");
        }

        [TestMethod]
        public void DownNameMismatch2()
        {
            var files = new[] { "b.down.sql", "b2.sql" };
            TestUtility.ShouldFail<FrameworkException>(
                () => GenerateDataMigrationScriptsAssetsFile(files),
                @"FrameworkException: Data-migration 'down' script 'TestPackage\b.down.sql' should have same file name as the related 'up' script with added suffix "".down"": TestPackage\b2.down.sql.");
        }

        [TestMethod]
        public void TagLabelIncorrectDown()
        {
            var files = new[] { "i.sql", "i.down.sql" };
            TestUtility.ShouldFail<FrameworkException>(
                () => GenerateDataMigrationScriptsAssetsFile(files),
                @"Data migration scripts 'TestPackage\i.sql' and 'TestPackage\i.down.sql' have same tag '2B676531-76AB-43F3-AE07-868434DEAB7F' in their headers. Note that the 'down' script should have ""DATAMIGRATION-DOWN"" label in the header.");
        }

        [TestMethod]
        public void DownSuffixOnUpScript()
        {
            var files = new[] { "i.down.sql" };
            TestUtility.ShouldFail<FrameworkException>(
                () => GenerateDataMigrationScriptsAssetsFile(files),
                @"Data-migration 'down' script 'TestPackage\i.down.sql' should have ""DATAMIGRATION-DOWN"" label in the header.");
        }

        [TestMethod]
        public void TagLabelIncorrectUp()
        {
            var files = new[] { "i2.sql", "i2.down.sql" };
            TestUtility.ShouldFail<FrameworkException>(
                () => GenerateDataMigrationScriptsAssetsFile(files),
                @"Data migration scripts 'TestPackage\i2.sql' and 'TestPackage\i2.down.sql' have same tag '2B676531-76AB-43F3-AE07-868434DEAB7F' in their headers. Note that the 'down' script should have ""DATAMIGRATION-DOWN"" label in the header.");
        }

        private string GenerateDataMigrationScriptsAssetsFile(string[] files)
        {
            var contentFiles = files.Select(file => new ContentFile { PhysicalPath = $@"TestScripts\{file}", InPackagePath = $@"DataMigration\{file}" }).ToList();
            var installedPackages = new InstalledPackages
            {
                Packages = new List<InstalledPackage>
                {
                    new InstalledPackage("TestPackage", null, null, null, null, null, contentFiles)
                }
            };
            var filesUtility = new FilesUtility(new ConsoleLogProvider());
            var dataMigrationScriptsFile = new DataMigrationScriptsFileMock();
            var dmsGenerator = new DataMigrationScriptsGenerator(installedPackages, filesUtility, dataMigrationScriptsFile);
            dmsGenerator.Generate();
            return TestUtility.Dump(dataMigrationScriptsFile.DataMigrationScripts.Scripts,
                s => $"{Path.GetFileName(s.Path)}:{RemoveHeader(s.Content)}/{RemoveHeader(s.Down)}");
        }

        private string RemoveHeader(string sql)
        {
            if (string.IsNullOrEmpty(sql))
                return "-";
            return sql.Substring(sql.IndexOf("\n") + 1).Trim();
        }

        //=========================================================================================

        [TestMethod]
        public void ParseScriptHeader()
        {
            const string missingHeaderError = "FrameworkException: Data migration script 'TestFile.sql' should start with a header '/*DATAMIGRATION unique_script_identifier*/'.";

            var tests = new (string InputSql, string ExpectedTagAndDown)[]
            {
                ("/*DATAMIGRATION test*/", "test"),
                ("/*DATAMIGRATION test*/\r\nrest", "test"),
                ("/*DATAMIGRATION test*/ rest", "test"),
                ("/*DATAMIGRATION-DOWN test*/", "test-"),

                ("/*DATAMIGRATION-DOWNN test*/", missingHeaderError),
                ("/*DATAMIGRATION */", missingHeaderError),
                ("/*DATAMIGRATION-DOWN*/", missingHeaderError),
                ("/*DATAMIGRATION \r\ntest*/", missingHeaderError),
                ("/*DATAMIGRATION test\r\ntest*/", missingHeaderError),
                ("\r\n/*DATAMIGRATION test*/", missingHeaderError),
            };

            var dmsGenerator = new DataMigrationScriptsGeneratorAccessor();
            foreach (var test in tests)
            {
                string report;
                try
                {
                    var header = dmsGenerator.ParseScriptHeader(test.InputSql, "TestFile.sql");
                    report = $"{header.Tag}{(header.IsDowngradeScript ? "-" : "")}";
                }
                catch (Exception e)
                {
                    report = $"{e.GetType().Name}: {e.Message}";
                }
                Assert.AreEqual(test.ExpectedTagAndDown, report, "Input: " + test.InputSql);
            }
        }
    }
}
