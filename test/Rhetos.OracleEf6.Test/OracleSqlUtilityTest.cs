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
using Oracle.ManagedDataAccess.Client;
using Rhetos.TestCommon;
using System;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace Rhetos.Utilities.Test
{
    [TestClass]
    public class OracleSqlUtilityTest
    {
        internal static OracleSqlUtility NewSqlUtility() => new OracleSqlUtility(new DatabaseSettings { DatabaseLanguage = "Oracle" });

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
        public void ConnectionStringAddAppName()
        {
            string initialConnectionString = "User Id=jjj;Password=jjj;Data Source=localhost:1521/xe;";

            IConfiguration configuration = new ConfigurationBuilder(new ConsoleLogProvider())
                .AddKeyValue(ConnectionString.ConnectionStringConfigurationKey, initialConnectionString)
                .Build();
            ISqlUtility sqlUtility = NewSqlUtility();
            string rhetosConnectionString = new ConnectionString(configuration, sqlUtility).ToString();

            Assert.AreEqual(
                initialConnectionString, // OracleSqlUtility does not implement the application name in CS yet.
                rhetosConnectionString,
                ignoreCase: true);

            var s = new OracleConnectionStringBuilder(rhetosConnectionString);
            Assert.AreEqual("localhost:1521/xe", s.DataSource); // Testing if the connection string is still valid.
        }

        [TestMethod]
        public void SingleQuote_SimpleTest()
        {
            Assert.AreEqual("'abc'", NewSqlUtility().QuoteText("abc"));
        }

        [TestMethod]
        public void SingleQuote_EscapeSequenceTest()
        {
            Assert.AreEqual("'ab''c'", NewSqlUtility().QuoteText("ab'c"));
        }

        [TestMethod]
        public void GetShortName()
        {
            Assert.AreEqual("someview", NewSqlUtility().GetShortName("someschema.someview"));
            Assert.AreEqual("someview", NewSqlUtility().GetShortName("someview"));

            TestUtility.ShouldFail(() => NewSqlUtility().GetShortName("a.b.c"), "Invalid database object name");
            TestUtility.ShouldFail(() => NewSqlUtility().GetShortName("a."), "Invalid database object name");
        }

        [TestMethod]
        public void OracleGetSchemaName()
        {
            Assert.AreEqual("someschema", NewSqlUtility().GetSchemaName("someschema.someview"));
            TestUtility.ShouldFail(() => NewSqlUtility().GetSchemaName("someview"), "Missing schema");
        }

        [TestMethod]
        public void MaskPasswordTest()
        {
            var tests = new ListOfTuples<string, string[]>
            {
                // Format: connection string, expected content.
                { "User Id=jjj;Password=jjj;Data Source=localhost:1521/xe;", new[] { "localhost:1521/xe" } },
                { "User Id=jjj;Password='jjj;jjj=jjj';Data Source=localhost:1521/xe;", new[] { "localhost:1521/xe" } },
                { "User Id=jjj;Password=\"jjj;jjj=jjj\";Data Source=localhost:1521/xe;", new[] { "localhost:1521/xe" } },
                { "';[]=-", Array.Empty<string>() },
            };

            foreach (var test in tests)
            {
                Console.WriteLine("CS: " + test.Item1);
                string report = NewSqlUtility().SqlConnectionInfo(test.Item1);
                Console.WriteLine("Report: " + report);

                TestUtility.AssertNotContains(report, "j", "Username or password leaked.");
                if (test.Item2.Any())
                    TestUtility.AssertContains(report, test.Item2);
            }
        }
    }
}
