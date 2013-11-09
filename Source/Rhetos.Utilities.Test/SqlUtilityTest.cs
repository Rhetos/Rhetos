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
using System.Diagnostics;
using Rhetos.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Rhetos.TestCommon;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Rhetos.Utilities.Test
{
    [TestClass()]
    public class SqlUtilityTest
    {
        [TestMethod()]
        public void ValidateNameTest()
        {
            string[] validNames = new[] {
                "abc", "ABC", "i",
                "a12300", "a1a",
                "_abc", "_123", "_", "a_a_"
            };

            string[] invalidNames = new[] {
                "0", "2asdasd", "123", "1_",
                null, "",
                " abc", "abc ", " ",
                "!", "@", "#", "a!", "a@", "a#",
                "ač", "č",
            };

            foreach (string name in validNames)
                SqlUtility.CheckIdentifier(name);

            foreach (string name in invalidNames)
            {
                Console.WriteLine("Testing invalid name '" + name + "'.");
                TestUtility.ShouldFail(() => SqlUtility.CheckIdentifier(name), "database object name", name != null ? "'" + name + "'"  : "null");
            }
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

        [TestMethod()]
        public void SingleQuote_SimpleTest()
        {
            Assert.AreEqual("'abc'", SqlUtility.QuoteText("abc"));
        }

        [TestMethod()]
        public void SingleQuote_EscapeSequenceTest()
        {
            Assert.AreEqual("'ab''c'", SqlUtility.QuoteText("ab'c"));
        }

        [TestMethod]
        [DeploymentItem("ConnectionStrings.config")]
        public void SqlObjectName()
        {
            TestUtility.CheckDatabaseAvailability();

            Assert.AreEqual("someschema", SqlUtility.GetSchemaName("someschema.someview"));
            Assert.AreEqual("someview", SqlUtility.GetShortName("someschema.someview"));

            TestUtility.ShouldFail(() => SqlUtility.GetShortName("a.b.c"), "Invalid database object name");
            TestUtility.ShouldFail(() => SqlUtility.GetShortName("a."), "Invalid database object name");
        }

        [TestMethod]
        [DeploymentItem("ConnectionStrings.config")]
        public void MsSqlObjectName()
        {
            TestUtility.CheckDatabaseAvailability("MsSql");

            Assert.AreEqual("dbo", SqlUtility.GetSchemaName("someview"));
            Assert.AreEqual("someview", SqlUtility.GetShortName("someview"));
        }

        [TestMethod]
        [DeploymentItem("ConnectionStrings.config")]
        public void OracleObjectName()
        {
            TestUtility.CheckDatabaseAvailability("Oracle");

            TestUtility.ShouldFail(() => SqlUtility.GetSchemaName("someview"), "Missing schema");
            Assert.AreEqual("someview", SqlUtility.GetShortName("someview"));
        }

        [TestMethod()]
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

        [TestMethod]
        [DeploymentItem("ConnectionStrings.config")]
        public void GetDatabaseTimeTest()
        {
            TestUtility.CheckDatabaseAvailability("MsSql");

            var sqlExecuter = new MsSqlExecuter(SqlUtility.ConnectionString, new ConsoleLogProvider(), new NullUserInfo());

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
    }
}
