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

using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Rhetos.TestCommon;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Data;

namespace Rhetos.Utilities.Test
{
    [TestClass]
    [DeploymentItem("ConnectionStrings.config")]
    public class SqlUtilityTest
    {
        [TestMethod]
        public void OracleLimitIdentifierLength()
        {
            var names = new[] {
                "123456789012345678901234567890",
                "123456789012345678901234567890a",
                "123456789012345678901234567890b" };
            Assert.AreEqual("30, 31, 31", TestUtility.Dump(names, name => name.Length));

            var limited = names.Select(name => OracleSqlUtility.LimitIdentifierLength(name)).ToArray();
            TestUtility.Dump(limited);

            Assert.AreEqual(names[0], limited[0]);
            Assert.AreEqual(30, limited[1].Length);
            Assert.AreEqual(30, limited[2].Length);
            Assert.IsFalse(limited[1].Equals(limited[2], StringComparison.OrdinalIgnoreCase));
        }

        [TestMethod]
        public void SingleQuote_SimpleTest()
        {
            Assert.AreEqual("'abc'", SqlUtility.QuoteText("abc"));
        }

        [TestMethod]
        public void SingleQuote_EscapeSequenceTest()
        {
            Assert.AreEqual("'ab''c'", SqlUtility.QuoteText("ab'c"));
        }

        [TestMethod]
        public void SqlObjectName()
        {
            TestUtility.CheckDatabaseAvailability();

            Assert.AreEqual("someschema", SqlUtility.GetSchemaName("someschema.someview"));
            Assert.AreEqual("someview", SqlUtility.GetShortName("someschema.someview"));

            TestUtility.ShouldFail(() => SqlUtility.GetShortName("a.b.c"), "Invalid database object name");
            TestUtility.ShouldFail(() => SqlUtility.GetShortName("a."), "Invalid database object name");
        }

        [TestMethod]
        public void MsSqlObjectName()
        {
            TestUtility.CheckDatabaseAvailability("MsSql");

            Assert.AreEqual("dbo", SqlUtility.GetSchemaName("someview"));
            Assert.AreEqual("someview", SqlUtility.GetShortName("someview"));
        }

        [TestMethod]
        public void OracleObjectName()
        {
            TestUtility.CheckDatabaseAvailability("Oracle");

            TestUtility.ShouldFail(() => SqlUtility.GetSchemaName("someview"), "Missing schema");
            Assert.AreEqual("someview", SqlUtility.GetShortName("someview"));
        }

        [TestMethod]
        public void MaskPasswordTest()
        {
            var tests = new[,]
            {
                { "Data Source=myOracleDB;User Id=SYS;Password=SYS;DBA Privilege=SYSDBA;", "Data Source=myOracleDB;User Id=SYS;Password=*;DBA Privilege=SYSDBA;" },
                { "password=asd;", "password=*;" },
                { "PASSWORD=asd;", "PASSWORD=*;" },
                { "pwd=asd;", "pwd=*;" },
                { "PWD=asd;", "PWD=*;" },
                { "pwd=", "pwd=*" },
                { "pwd=;", "pwd=*;" },

                { "pwd=asdf asdf asddfas fasd asdf asdfas dfas ", "pwd=*" },
                { "pwd=asdf asdf asddfas fasd asdf asdfas dfas; ", "pwd=*; " },

                { "user = password asdf asdf ; pwd = 123; asdf = pwd; password=pwd;", "user = password asdf asdf ; pwd =*; asdf = pwd; password=*;" },

                { "mypwd=pwd;a=2", "mypwd=pwd;a=2" },

                { "Data Source=username/password@//myserver:1521/my.service.com;", "Data Source=username/*@//myserver:1521/my.service.com;" },
                { "Data Source=username/xx+-*@//myserver:1521/my.service.com;", "Data Source=username/*@//myserver:1521/my.service.com;" },
                { "Data Source=username/@//myserver:1521/my.service.com;", "Data Source=username/*@//myserver:1521/my.service.com;" }
            };

            for (int i = 0; i < tests.GetLength(0); i++)
            {
                Assert.AreEqual(tests[i, 1], SqlUtility.MaskPassword(tests[i, 0]));
                Console.WriteLine("OK: " + tests[i, 1]);
            }
        }

        class MockSqlExecuter : ISqlExecuter
        {
            public void ExecuteReader(string command, Action<System.Data.Common.DbDataReader> action)
            {
                if (command == "SELECT GETDATE()")
                {
                    var dataTable = new DataTable("mocktable");
                    dataTable.Columns.Add("column0", typeof(DateTime));
                    dataTable.Rows.Add(new DateTime(2001, 2, 3, 4, 5, 6, 7));

                    var dataReader = new DataTableReader(dataTable);
                    while (dataReader.Read())
                        action(dataReader);
                    dataReader.Close();
                }
                else
                    throw new NotImplementedException();
            }

            public void ExecuteSql(IEnumerable<string> commands, bool useTransaction)
            {
                throw new NotImplementedException();
            }

            public void ExecuteSql(IEnumerable<string> commands, bool useTransaction, Action<int> beforeExecute, Action<int> afterExecute)
            {
                throw new NotImplementedException();
            }
        }

        [TestMethod]
        public void GetDatabaseTimeTest()
        {
            var sqlExecuter = new MockSqlExecuter();

            SqlUtility.GetDatabaseTime(sqlExecuter); // First run, might not be cached.

            var getNonCachedTime = typeof(SqlUtility).GetMethod("GetDatabaseTimeFromDatabase", BindingFlags.NonPublic | BindingFlags.Static );
            Assert.IsNotNull(getNonCachedTime);
            var notCachedDatabaseTime = (DateTime)getNonCachedTime.Invoke(null, new[] { sqlExecuter });
            var cachedTime = SqlUtility.GetDatabaseTime(sqlExecuter);

            Console.WriteLine(notCachedDatabaseTime.ToString("o"));
            Console.WriteLine(cachedTime.ToString("o"));

            Assert.IsTrue(notCachedDatabaseTime - cachedTime <= TimeSpan.FromSeconds(0.01));
            Assert.IsTrue(cachedTime - notCachedDatabaseTime <= TimeSpan.FromSeconds(0.01));
        }

        [TestMethod]
        public void ExtractUserInfoTestFormat()
        {
            var initialUserInfo = new TestUserInfo("os\ab", "cd.ef", true);
            var processedUserInfo = SqlUtility.ExtractUserInfo(SqlUtility.UserContextInfoText(initialUserInfo));
            Assert.AreEqual(
                initialUserInfo.UserName + "|" + initialUserInfo.Workstation,
                processedUserInfo.UserName + "|" + processedUserInfo.Workstation);
        }

        [TestMethod]
        public void ExtractUserInfoTest()
        {
            var tests = new Dictionary<string, string>
            {
                { @"Alpha:OS\aa,bb-cc.hr", @"OS\aa|bb-cc.hr" },
                { @"Rhetos:Bob,Some workstation", @"Bob|Some workstation" },
                { @"Rhetos:OS\aa,192.168.113.108 port 49271", @"OS\aa|192.168.113.108 port 49271" },
                { @"Rhetos:aa,b.c", @"aa|b.c" },
                { @"Rhetos:verylongdomainname\extremelylongusernamenotcompl", @"verylongdomainname\extremelylongusernamenotcompl|null" },
                { @"Rhetos:asdf", @"asdf|null" },
                { @"Rhetos:1,2,3", @"1|2,3" },
                { @"Rhetos:1 1 , 2 2 ", @"1 1|2 2" },
                { "<null>", @"null|null" },
                { @"", @"null|null" },
                { @"Rhetos:", @"null|null" },
                { @"Rhetos:   ", @"null|null" },
                { @"Rhetos:  , ", @"null|null" },
                { @"1:2,3", @"null|null" }
            };

            foreach (var test in tests)
            {
                var result = SqlUtility.ExtractUserInfo(test.Key == "<null>" ? null : test.Key);
                Assert.AreEqual(test.Value, (result.UserName ?? "null") + "|" + (result.Workstation ?? "null"), "Input: " + test.Key);
            }
        }

        [TestMethod]
        public void QuoteIdentifier()
        {
            var tests = new Dictionary<string, string>
            {
                { "abc", "[abc]" },
                { "abc[", "[abc[]" },
                { "abc]", "[abc]]]" },
                { "[][][]", "[[]][]][]]]" },
                { " '\" ", "[ '\" ]" }
            };

            foreach (var test in tests)
                Assert.AreEqual(test.Value, SqlUtility.QuoteIdentifier(test.Key));
        }

        [TestMethod]
        public void SplitBatches()
        {
            var tests = new Dictionary<string, string[]>
            {
                { "111\r\nGO\r\n222", new[] { "111", "222" } }, // Simple
                { "111\nGO\n222", new[] { "111", "222" } }, // UNIX EOL
                { "111\r\ngo\r\n222", new[] { "111", "222" } }, // Case insensitive
                { "111\r\n    GO\t\t\t\r\n222", new[] { "111", "222" } }, // Spaces and tabs
                { "GO\r\n111\r\nGO\r\n222\r\nGO", new[] { "111", "222" } }, // Beginning and ending with GO
                { "111\r\nGO\r\n222\r\nGO   ", new[] { "111", "222" } }, // Beginning and ending with GO
                { "\r\n  GO  \r\n\r\n111\r\nGO\r\n222\r\nGO   \r\nGO   \r\n\r\n", new[] { "111", "222" } }, // Beginning and ending with GO
                { "111\r\n222\r\nGO\r\n333\r\n444", new[] { "111\r\n222", "333\r\n444" } }, // Multi-line batches
                { "111\n222\nGO\n333\n444", new[] { "111\n222", "333\n444" } }, // Multi-line batches, UNIX EOL
                { "111\r\nGO GO\r\n		GO   \r\n		go           \r\n222\r\ngoo\r\ngo", new[] { "111\r\nGO GO", "222\r\ngoo" } }, // Complex
                { "", new string[] { } }, // Empty batches
                { "GO", new string[] { } }, // Empty batches
                { "\r\n  \r\n  GO  \r\n  \r\n", new string[] { } } // Empty batches
            };
            foreach (var test in tests)
                Assert.AreEqual(TestUtility.Dump(test.Value), TestUtility.Dump(SqlUtility.SplitBatches(test.Key)), "Input: " + test.Key);
        }

        [TestMethod]
        public void Comment()
        {
            Assert.AreEqual("/*abc\r\ndef*/", SqlUtility.Comment("abc\r\ndef"));

            var tests = new Dictionary<string, string>
            {
                { "", "" },
                { "abc", "abc" },
                { "/*", "/*" },
                { "/**/", "/*" },
                { "*/", "" },
                { "abc*/", "abc" },
                { "*/abc", "abc" },
                { "*/*/*/", "" },
                { "***///", "" },
                { "/*/*/*", "" },
                { "///***", "" },
                { "/**//**/", "" },
                { "/", "" },
            };

            foreach (var test in tests)
            {
                var comment = SqlUtility.Comment(test.Key);
                var testInfo = $"{test.Key}=>{comment}";

                Assert.IsTrue(comment.StartsWith("/*"), testInfo);
                Assert.IsTrue(comment.EndsWith("*/"), testInfo);

                TestUtility.AssertNotContains(comment.Substring(2, comment.Length - 4), "*/", testInfo);
                TestUtility.AssertNotContains(comment.Substring(2), "/*", testInfo);

                if (!string.IsNullOrEmpty(test.Value))
                    TestUtility.AssertContains(comment, test.Value, testInfo);
            }
        }
    }
}
