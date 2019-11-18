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
using System;
using Rhetos.TestCommon;
using System.Collections.Generic;
using System.Linq;
using System.Data;

namespace Rhetos.Utilities.Test
{
    [TestClass]
    [DeploymentItem("ConnectionStrings.config")]
    public class SqlUtilityTest
    {
        public SqlUtilityTest()
        {
            var configurationProvider = new ConfigurationBuilder()
                .AddRhetosAppConfiguration(AppDomain.CurrentDomain.BaseDirectory)
                .AddConfigurationManagerConfiguration()
                .Build();

            LegacyUtilities.Initialize(configurationProvider);
        }

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
            var tests = new ListOfTuples<string, string[]>
            {
                // Format: connection string, expected content.
                { "Server=tcp:name.database.windows.net,1433;Initial Catalog=RhetosAzureDB;Persist Security Info=False;User ID=jjj;Password=jjj;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;",
                    new[] { "tcp:name.database.windows.net,1433", "RhetosAzureDB" } },
                { "Data Source=localhost;Initial Catalog=Rhetos;Integrated Security=SSPI;",
                    new[] { "localhost", "Rhetos" } },
                { "User Id=jjj;Password=jjj;Data Source=localhost:1521/xe;",
                    new[] { "localhost:1521/xe" } },
                { "User Id=jjj;Password='jjj;jjj=jjj';Data Source=localhost:1521/xe;",
                    new[] { "localhost:1521/xe" } },
                { "User Id=jjj;Password=\"jjj;jjj=jjj\";Data Source=localhost:1521/xe;",
                    new[] { "localhost:1521/xe" } },
                { "';[]=-",
                    new string[] { } },
            };

            foreach (var test in tests)
            {
                Console.WriteLine(test.Item1);
                string report = SqlUtility.SqlConnectionInfo(test.Item1);
                Console.WriteLine("=> " + report);

                TestUtility.AssertNotContains(report, "j", "Username or password leaked.");
                if (test.Item2.Any())
                    TestUtility.AssertContains(report, test.Item2);
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
            // More detailed tests are implemented in the DatabaseTimeCacheTest class.
            // This is only a smoke test for SqlUtility.

            var sqlExecuter = new MockSqlExecuter();

            Enumerable.Range(0, 4).Select(x => SqlUtility.GetDatabaseTime(sqlExecuter)); // Caching initialization.

            var notCachedDatabaseTime = MsSqlUtility.GetDatabaseTime(sqlExecuter);
            var cachedTime = SqlUtility.GetDatabaseTime(sqlExecuter);

            Console.WriteLine(notCachedDatabaseTime.ToString("o"));
            Console.WriteLine(cachedTime.ToString("o"));

            Assert.IsTrue(notCachedDatabaseTime - cachedTime <= TimeSpan.FromSeconds(0.01));
            Assert.IsTrue(cachedTime - notCachedDatabaseTime <= TimeSpan.FromSeconds(0.01));
        }

        [TestMethod]
        public void ExtractUserInfoTestFormat()
        {
            var initialUserInfo = new TestUserInfo(@"os\ab", "cd.ef", true);
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
    }
}
