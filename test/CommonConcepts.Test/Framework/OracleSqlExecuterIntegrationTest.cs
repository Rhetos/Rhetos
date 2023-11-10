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

using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CommonConcepts.Test.Framework
{
    [TestClass]
    public class OracleSqlExecuterIntegrationTest
    {
        [TestInitialize]
        public void CheckDatabaseIsOracle()
        {
            using var scope = TestScope.Create();
            TestUtility.CheckDatabaseAvailability(scope, "Oracle");
            Assert.AreEqual("OracleSqlExecuter", scope.Resolve<ISqlExecuter>().GetType().Name);
        }

        [ClassCleanup]
        public static void MyTestCleanup()
        {
            using var scope = TestScope.Create();
            if (scope.Resolve<DatabaseSettings>().DatabaseLanguage != "Oracle")
                return;

            scope.Resolve<ISqlExecuter>().ExecuteSql(new[] { @"declare
  c integer;
begin
  select count(*) into c from SYS.user$ where Name = 'RHETOSUNITTEST';
  if c = 1 then
    BEGIN
      EXECUTE IMMEDIATE ('drop user ""RHETOSUNITTEST"" CASCADE');
    END;
  end if;
end;" });
        }

        private string GetRandomTableName()
        {
            using var scope = TestScope.Create();
            scope.Resolve<ISqlExecuter>().ExecuteSql(new[] {
@"declare
  c integer;
begin
  select count(*) into c from SYS.user$ where Name = 'RHETOSUNITTEST';
  if c = 0 then
    BEGIN
      EXECUTE IMMEDIATE ('CREATE USER RHETOSUNITTEST IDENTIFIED BY null
        DEFAULT TABLESPACE ""USERS""
        TEMPORARY TABLESPACE ""TEMP""
        ACCOUNT LOCK');
      EXECUTE IMMEDIATE ('ALTER USER RHETOSUNITTEST QUOTA UNLIMITED ON USERS');
    END;
  end if;
end;" });
            var newTableName = "RHETOSUNITTEST.T" + Guid.NewGuid().ToString().Replace("-", "").Substring(0, 15).ToUpper();
            Console.WriteLine("Generated random table name: " + newTableName);
            return newTableName;
        }

        [TestMethod]
        public void ExecuteSql_SaveLoadTest()
        {
            using var scope = TestScope.Create();
            string table = GetRandomTableName();

            scope.Resolve<ISqlExecuter>().ExecuteSql(new[]
                {
                    "CREATE TABLE " + table + " ( A INTEGER )",
                    "INSERT INTO " + table + " VALUES (123)"
                });
            int actual = 0;
            scope.Resolve<ISqlExecuter>().ExecuteReader("SELECT * FROM " + table, dr => actual = dr.GetInt32(0));
            Assert.AreEqual(123, actual);
        }

        [TestMethod]
        public void ExecuteSql_SimpleSqlError()
        {
            using var scope = TestScope.Create();
            TestUtility.ShouldFail(
                () => scope.Resolve<ISqlExecuter>().ExecuteSql(new[] { @"SELECT 2/0 FROM DUAL" }),
                "divisor is equal to zero");
        }

        [TestMethod]
        public void ExecuteSql_ErrorDescription()
        {
            using var scope = TestScope.Create();
            TestUtility.ShouldFail(
                () => scope.Resolve<ISqlExecuter>().ExecuteSql(new[] { @"SELECT 1/0 FROM DUAL" }),
                     "divisor is equal to zero", "1476");
            /*
                Error starting at line 1 in command:
                SELECT 1/0 FROM DUAL
                Error report:
                SQL Error: ORA-01476: divisor is equal to zero
                01476. 00000 -  "divisor is equal to zero"
                *Cause:    
                *Action:
             */
        }

        [TestMethod]
        public void ExecuteSql_ErrorDescription2()
        {
            using var scope = TestScope.Create();
            TestUtility.ShouldFail(
                () => scope.Resolve<ISqlExecuter>().ExecuteSql(new[] {
@"BEGIN
  RAISE_APPLICATION_ERROR(-20000, 'abc', TRUE);
END;" }),
                    "abc", "20000", "line 2");
            /*
                Error starting at line 1 in command:
                BEGIN
                  RAISE_APPLICATION_ERROR(-20000, 'abc', TRUE);
                END;
                Error report:
                ORA-20000: abc
                ORA-06512: at line 2
                20000. 00000 -  "%s"
                *Cause:    The stored procedure 'raise_application_error'
                           was called which causes this error to be generated.
                *Action:   Correct the problem as described in the error message or contact
                           the application administrator or DBA for more information.
            */
        }

        [TestMethod]
        public void ExecuteSql_LoginError()
        {
            string userId = "U" + Guid.NewGuid().ToString().Replace("-", "");
            string password = "P" + Guid.NewGuid().ToString().Replace("-", "");
            string dataSource = "localhost:1521/xe";
            string connectionString = @"User Id=" + userId + ";Password=" + password + ";Data Source=" + dataSource + ";";

            using var scope = TestScope.Create(builder => builder.RegisterInstance(new ConnectionString(connectionString)));
            var ex = TestUtility.ShouldFail(() => scope.Resolve<ISqlExecuter>().ExecuteSql(new[] { "SELECT 123 FROM DUAL" }), userId, dataSource);
            Assert.IsFalse(ex.ToString().Contains(password));
        }

        [TestMethod]
        public void ExecuteSql_Commit()
        {
            string table = GetRandomTableName();

            using var scope = TestScope.Create();
            scope.Resolve<ISqlExecuter>().ExecuteSql(new[] { "CREATE TABLE " + table + " ( A INTEGER )" });
            AssertRowCount(0, table, "initialization");
            scope.Resolve<ISqlExecuter>().ExecuteSql(new[] { "INSERT INTO " + table + " VALUES (123)" });

            var sw = Stopwatch.StartNew();
            AssertRowCount(1, table, "after insert");
            Console.WriteLine(sw.ElapsedMilliseconds + " ms");
            Assert.IsTrue(sw.ElapsedMilliseconds < 50, "The last query was waiting too long. Possibly the first query was not committed immediately.");
        }

        [TestMethod]
        public void ExecuteSql_Rollback()
        {
            string table = GetRandomTableName();

            using var scope = TestScope.Create();
            scope.Resolve<ISqlExecuter>().ExecuteSql(new[] { "CREATE TABLE " + table + " ( A INTEGER )" });
            AssertRowCount(0, table, "initialization");
            TestUtility.ShouldFail(
                () => scope.Resolve<ISqlExecuter>().ExecuteSql(new[] {
                    "INSERT INTO " + table + " VALUES (123)",
                    "INSERT INTO " + table + " VALUES ('xxx')" }),
                "invalid number");

            var sw = Stopwatch.StartNew();
            AssertRowCount(0, table, "after failed insert");
            Console.WriteLine(sw.ElapsedMilliseconds);
            Assert.IsTrue(sw.ElapsedMilliseconds < 50, "The last query was waiting too long. Possibly the first query was not rollbacked immediately.");
        }

        private static void AssertRowCount(int expectedCount, string table, string msg)
        {
            int cnt = -1;
            using var scope = TestScope.Create();
            scope.Resolve<ISqlExecuter>().ExecuteReader("SELECT COUNT(*) FROM " + table, reader => cnt = reader.GetInt32(0));
            Assert.AreEqual(expectedCount, cnt, msg);
        }

        [TestMethod]
        public void ExecuteSql_StopExecutingScriptsAfterError()
        {
            using var scope = TestScope.Create();
            string table = GetRandomTableName();
            try
            {
                scope.Resolve<ISqlExecuter>().ExecuteSql(new[] {
                    "create table forcederror",
                    "create table " + table + " ( a integer )" });
            }
            catch
            {
            }
            scope.Resolve<ISqlExecuter>().ExecuteSql(new[] { "create table " + table + " ( a integer )" }); // Everything created in the first ExecuteSql call should have been rollbacked.
        }

        [TestMethod]
        public void ExecuteSql_RollbackedTransaction()
        {
            string table = GetRandomTableName();

            using var scope = TestScope.Create();
            scope.Resolve<ISqlExecuter>().ExecuteSql(new[] { "CREATE TABLE " + table + " ( A INTEGER )" });

            AssertRowCount(0, table, "initialization");

            TestUtility.ShouldFail(
                () => scope.Resolve<ISqlExecuter>().ExecuteSql(new[] {
                    "INSERT INTO " + table + " VALUES (123)",
                    "ROLLBACK",
                    "INSERT INTO " + table + " VALUES (456)",
                    "INSERT INTO " + table + " VALUES ('xxx')" }),
                "invalid number");

            AssertRowCount(0, table, "Rollback in the middle of a commands array should not break atomicity of the whole commands array.");
        }

        [TestMethod]
        public void SendUserInfoInSqlContext_NoUser()
        {
            using var scope = TestScope.Create();
            var result = new List<object>();
            scope.Resolve<ISqlExecuter>().ExecuteReader("SELECT SYS_CONTEXT('USERENV','CLIENT_INFO') FROM DUAL", reader => result.Add(reader[0]));
            Console.WriteLine(result.Single());
            Assert.AreEqual(typeof(DBNull), result.Single().GetType());
        }

        class TestUserInfo : IUserInfo
        {
            public bool IsUserRecognized { get; set; }
            public string UserName { get; set; }
            public string Workstation { get; set; }
            public string Report() { return UserName + "," + Workstation; }
        }

        [TestMethod]
        public void SendUserInfoInSqlContext_ReadWithUser()
        {
            var testUser = new TestUserInfo
                               {
                                   IsUserRecognized = true,
                                   UserName = "Bob",
                                   Workstation = "HAL9000"
                               };
            using var scope = TestScope.Create(builder => builder.RegisterInstance<IUserInfo>(testUser));
            var result = new List<string>();

            scope.Resolve<ISqlExecuter>().ExecuteReader(@"SELECT SYS_CONTEXT('USERENV','CLIENT_INFO') FROM DUAL", reader => result.Add(reader[0].ToString()));

            string clientInfo = result.Single();
            TestUtility.AssertContains(clientInfo, testUser.UserName, "CLIENT_INFO should contain username.");
            TestUtility.AssertContains(clientInfo, testUser.Workstation, "CLIENT_INFO should contain client workstation.");
            Assert.AreEqual(SqlUtility.UserContextInfoText(testUser), clientInfo);
        }

        [TestMethod]
        public void SendUserInfoInSqlContext_WriteWithUser()
        {
            var testUser = new TestUserInfo
            {
                IsUserRecognized = true,
                UserName = "Bob",
                Workstation = "HAL9000"
            };
            using var scope = TestScope.Create(builder => builder.RegisterInstance<IUserInfo>(testUser));
            string table = GetRandomTableName();
            var result = new List<string>();

            scope.Resolve<ISqlExecuter>().ExecuteSql(new [] { @"CREATE TABLE " + table + " AS SELECT SYS_CONTEXT('USERENV','CLIENT_INFO') ClientInfo FROM DUAL" });

            scope.Resolve<ISqlExecuter>().ExecuteReader(@"SELECT * FROM " + table, reader => result.Add(reader[0].ToString()));
            var clientInfo = result.Single();
            TestUtility.AssertContains(clientInfo, testUser.UserName, "CLIENT_INFO should contain username.");
            TestUtility.AssertContains(clientInfo, testUser.Workstation, "CLIENT_INFO should contain client workstation.");
            Assert.AreEqual(SqlUtility.UserContextInfoText(testUser), clientInfo);
        }

        [TestMethod]
        public void ExecuteSql_CaseInsenitive()
        {
            using var scope = TestScope.Create();
            Console.WriteLine("NationalLanguage: " + scope.Resolve<DatabaseSettings>().DatabaseNationalLanguage);

            string table = GetRandomTableName();
            var sqlExecuter = scope.Resolve<ISqlExecuter>();

            sqlExecuter.ExecuteSql(new[] {
                "CREATE TABLE " + table + " ( S NVARCHAR2(256) )",
                "INSERT INTO " + table + " VALUES ('a')",
                "INSERT INTO " + table + " VALUES ('A')",
                "INSERT INTO " + table + " VALUES ('b')",
                "INSERT INTO " + table + " VALUES ('B')",
            });

            var result = new List<string>();
            sqlExecuter.ExecuteReader("SELECT * FROM " + table + " WHERE S LIKE 'a%'",
                reader => result.Add(scope.Resolve<ISqlUtility>().EmptyNullString(reader, 0)));

            Assert.AreEqual(
                TestUtility.DumpSorted(new[] { "a", "A" }),
                TestUtility.DumpSorted(result),
                "Comparison will be case insensitive depending on SqlUtility.NationalLanguage" +
                $" (see NLS_SORT), provided in configuration (see OracleSqlUtility.OracleNationalLanguageKey)." +
                " For example Rhetos.Oracle.GENERIC_M_CI or Rhetos.Oracle.XGERMAN_CI.");

            result.Clear();
            sqlExecuter = scope.Resolve<ISqlExecuter>();
            sqlExecuter.ExecuteReader("SELECT * FROM " + table + " WHERE S LIKE 'a%'",
                reader => result.Add(scope.Resolve<ISqlUtility>().EmptyNullString(reader, 0)));

            Assert.AreEqual(
                TestUtility.DumpSorted(new[] { "a", "A" }),
                TestUtility.DumpSorted(result),
                "Using new instance of SqlExecuter.");
        }
    }
}