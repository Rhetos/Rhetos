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
using Rhetos;
using Rhetos.Logging;
using Rhetos.Persistence;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace CommonConcepts.Test.Framework
{
    [TestClass]
    public class MsSqlExecuterIntegrationTest
    {
        private string LogActionName => GetType().Name;

        [TestInitialize]
        public void CheckDatabaseIsMsSql()
        {
            using var scope = TestScope.Create();
            TestUtility.CheckDatabaseAvailability(scope, "MsSql");
            Assert.AreEqual("MsSqlExecuter", scope.Resolve<ISqlExecuter>().GetType().Name);
        }

        [ClassCleanup]
        public static void DropRhetosUnitTestSchema()
        {
            Console.WriteLine("=== ClassCleanup ===");

            var test = new MsSqlExecuterIntegrationTest();

            using (var scope = TestScope.Create())
            {
                if (scope.Resolve<DatabaseSettings>().DatabaseLanguage != "MsSql")
                    return;
                var sqlExecuter = scope.Resolve<ISqlExecuter>();
                sqlExecuter.ExecuteSqlInterpolated(
                    $"DELETE FROM Common.Log WHERE Action = {test.LogActionName} AND TableName IS NULL");
                scope.CommitAndClose();
            }

            test.ExecuteSql(new[] {
                @"DECLARE @sql NVARCHAR(MAX)
                    SET @sql = ''
                    SELECT @sql = @sql + 'DROP TABLE RhetosUnitTest.' + QUOTENAME(name) + ';' + CHAR(13) + CHAR(10)
                        FROM sys.tables WHERE schema_id = SCHEMA_ID('RhetosUnitTest')
                    EXEC (@sql)",
                "IF SCHEMA_ID('RhetosUnitTest') IS NOT NULL DROP SCHEMA RhetosUnitTest" });
        }

        /// <summary>
        /// Executes the MsSqlExecuter commands and commits the transaction by default.
        /// </summary>
        private void InTransaction(Action<ISqlExecuter> sqlExecuterAction, IUserInfo testUser = null, bool commit = true)
        {
            using var scope = TestScope.Create(builder =>
                {
                    builder.RegisterInstance(testUser ?? new NullUserInfo()).As<IUserInfo>();
                });

            var sqlExecuter = scope.Resolve<ISqlExecuter>();
            sqlExecuterAction.Invoke(sqlExecuter);
            if (commit)
                scope.CommitAndClose();
        }

        /// <summary>
        /// Executes the MsSqlExecuter command and commits the transaction by default.
        /// </summary>
        private void ExecuteSql(IEnumerable<string> commands, IUserInfo testUser = null, bool commit = true)
        {
            InTransaction(sqlExecuter => sqlExecuter.ExecuteSql(commands), testUser, commit);
        }

        /// <summary>
        /// Executes the MsSqlExecuter command and commits the transaction by default.
        /// </summary>
        private void ExecuteReader(string command, Action<DbDataReader> action, IUserInfo testUser = null, bool commit = true)
        {
            InTransaction(sqlExecuter => sqlExecuter.ExecuteReader(command, action), testUser, commit);
        }

        private string GetRandomTableName()
        {
            ExecuteSql(new[] { "IF SCHEMA_ID('RhetosUnitTest') IS NULL EXEC('CREATE SCHEMA RhetosUnitTest')" });
            var newTableName = "RhetosUnitTest.T" + Guid.NewGuid().ToString().Replace("-", "");
            Console.WriteLine("Generated random table name: " + newTableName);
            return newTableName;
        }

        [TestMethod]
        public void ExecuteSql_SaveLoadTest()
        {
            string table = GetRandomTableName();

            IEnumerable<string> commands = new[]
                {
                    "CREATE TABLE " + table + " ( A INTEGER )",
                    "INSERT INTO " + table + " SELECT 123"
                };
            ExecuteSql(commands);
            int actual = 0;
            ExecuteReader("SELECT * FROM " + table, dr => actual = dr.GetInt32(0));
            Assert.AreEqual(123, actual);
        }

        [TestMethod]
        public void ExecuteSql_SimpleSqlError()
        {
            TestUtility.ShouldFail(() => ExecuteSql(new[] { "raiserror('aaa', 16, 100)" }),
                "aaa", "16", "100");
        }

        [TestMethod]
        public void ExecuteSql_InfoMessageIsNotError()
        {
            ExecuteSql(new[] { "raiserror('aaa', 0, 100)" }); // Exception not expected here.
        }

        [TestMethod]
        public void ExecuteSql_ErrorDescriptions()
        {
            IEnumerable<string> commands = new[]
                {
@"print 'xxx'
raiserror('aaa', 0, 100)
raiserror('bbb', 1, 101)
raiserror('ccc', 10, 110)
raiserror('ddd', 16, 116)
raiserror('eee', 17, 117)
raiserror('fff', 18, 118)"
                };

            var expectedStrings = new[] { "aaa", "bbb", "ccc", "ddd", "eee", "fff", "101", "116", "117", "118" }; // Error of severity 0 and 10 (states "100" and "110") do not require detailed error message.

            TestUtility.ShouldFail(() => ExecuteSql(commands), expectedStrings);
        }

        [TestMethod]
        public void ExecuteSql_LoginError()
        {
            string nonexistentDatabase = "db" + Guid.NewGuid().ToString().Replace("-", "");

            DbConnectionStringBuilder connectionStringBuilder;
            using (var scope = TestScope.Create())
            {
                string initialConnectionString = scope.Resolve<ConnectionString>().ToString();
                var dbProvider = scope.Resolve<DbProviderFactory>();
                connectionStringBuilder = dbProvider.CreateConnectionStringBuilder();
                connectionStringBuilder.ConnectionString = initialConnectionString;
                connectionStringBuilder["Initial Catalog"] = nonexistentDatabase;
                connectionStringBuilder["Integrated Security"] = true;
                connectionStringBuilder["Connect Timeout"] = 1;
            }

            string nonexistentDatabaseConnectionString = connectionStringBuilder.ConnectionString;
            Console.WriteLine(nonexistentDatabaseConnectionString);

            using (var scope = TestScope.Create(builder => builder.RegisterInstance(new ConnectionString(nonexistentDatabaseConnectionString))))
            {
                TestUtility.ShouldFail(
                    () => scope.Resolve<ISqlExecuter>().ExecuteSql("print 123"),
                    connectionStringBuilder["Data Source"].ToString(), connectionStringBuilder["Initial Catalog"].ToString(), Environment.UserName);
            }
        }

        [TestMethod]
        public void ExecuteSql_CommitImmediately()
        {
            string table = GetRandomTableName();
            ExecuteSql(new[] {
                "create table " + table + " ( a integer )" });
            ExecuteSql(new[] {
                "set lock_timeout 0",
                "select * from " + table }); // Exception not expected here.
        }

        [TestMethod]
        public void ExecuteSql_RollbackImmediately()
        {
            string table = GetRandomTableName();
            try
            {
                ExecuteSql(new[] {
                    "create table " + table + " ( a integer )",
                    "create table forcederror" });
            }
            catch
            {
            }
            ExecuteSql(new[] {
                "set lock_timeout 0",
                "create table " + table + " ( a integer )" }); // Lock timeout exception not expected here.
        }

        [TestMethod]
        public void ExecuteSql_StopExecutingScriptsAfterError()
        {
            string table = GetRandomTableName();
            try
            {
                ExecuteSql(new[] {
                    "create table forcederror",
                    "create table " + table + " ( a integer )" });
            }
            catch
            {
            }
            ExecuteSql(new[] {
                "create table " + table + " ( a integer )" }); // Exception not expected here.
        }

        [TestMethod]
        public void ExecuteSql_RollbackedTransaction()
        {
            // Since Rhetos v5 there is no explicit check for transaction level in MsSqlExecuter (only in SqlTransactionBatches),
            // but the unit of work should still fail on transaction commit at the end of the scope, because it is no longer open.

            Guid id = Guid.NewGuid();
            TestUtility.ShouldFail<InvalidOperationException>(
                () => ExecuteSql(
                    new[] {
                        $"INSERT INTO Common.Log (Action, ItemId, Description) SELECT '{LogActionName}', '{id}', '1'",
                        $"ROLLBACK",
                        $"INSERT INTO Common.Log (Action, ItemId, Description) SELECT '{LogActionName}', '{id}', '2'" }),
                "This SqlTransaction has completed; it is no longer usable.");

            // Record '2' was unintentionally inserted out of transaction, since MsSqlExecuter does not check the transaction state
            // after each command, for performance reasons.
            var result = new List<string>();
            ExecuteReader(
                $"SELECT Description FROM Common.Log WHERE ItemId = '{id}'",
                reader => result.Add(reader.GetString(0)));
            Assert.AreEqual("2", TestUtility.DumpSorted(result));
        }

        [TestMethod]
        public void ExecuteSql_RollbackedTransactionCanceled()
        {
            // Since Rhetos v5 there is no explicit check for transaction level in MsSqlExecuter (only in SqlTransactionBatches),
            // but the unit of work should still fail on transaction commit at the end of the scope, because it is no longer open.

            Guid id = Guid.NewGuid();
            ExecuteSql(
                new[] {
                    $"INSERT INTO Common.Log (Action, ItemId, Description) SELECT '{LogActionName}', '{id}', '1'",
                    $"ROLLBACK",
                    $"INSERT INTO Common.Log (Action, ItemId, Description) SELECT '{LogActionName}', '{id}', '2'" },
                commit: false);

            // Record '2' was unintentionally inserted out of transaction, since MsSqlExecuter does not check the transaction state
            // after each command, for performance reasons.
            var result = new List<string>();
            ExecuteReader(
                $"SELECT Description FROM Common.Log WHERE ItemId = '{id}'",
                reader => result.Add(reader.GetString(0)));
            Assert.AreEqual("2", TestUtility.DumpSorted(result));
        }

        [TestMethod]
        public void ExecuteSql_TransactionNotClosed()
        {
            Guid id = Guid.NewGuid();
            ExecuteSql(
                new[] {
                    $"INSERT INTO Common.Log (Action, ItemId, Description) SELECT '{LogActionName}', '{id}', '1'",
                    $"BEGIN TRAN",
                    $"INSERT INTO Common.Log (Action, ItemId, Description) SELECT '{LogActionName}', '{id}', '2'" });

            // No records have been committed (even though there was no error), since MsSqlExecuter does not check the transaction state
            // after each command, for performance reasons.
            // After "BEGIN TRAN", the trancount have been increased to 2, PersistenceTransaction's commit
            // just reduced the trancount to 1, and the SqlTransaction/SqlConnection disposal rolled back the transaction.
            var result = new List<string>();
            ExecuteReader(
                $"SELECT Description FROM Common.Log WHERE ItemId = '{id}'",
                reader => result.Add(reader.GetString(0)));
            Assert.AreEqual("", TestUtility.DumpSorted(result));
        }

        [TestMethod]
        public void SendUserInfoInSqlContext_NoUser()
        {
            var testUser = new TestUserInfo(null, null, false);
            var result = new List<object>();
            ExecuteReader($"SELECT {contextInfoToText}", reader => result.Add(reader[0]), testUser);

            Console.WriteLine(result.Single());
            Assert.AreEqual("Rhetos:", (string)result.Single());
        }

        private const string contextInfoToText = "(CONVERT([varchar](128),left(context_info(),isnull(nullif(charindex(0x00,context_info())-(1),(-1)),(128))),(0)))";

        [TestMethod]
        public void SendUserInfoInSqlContext_ReadWithUser()
        {
            var testUser = new TestUserInfo("Bob", "HAL9000");

            Assert.AreEqual("Rhetos:Bob,HAL9000", ReadContextWithSqlExecuter(testUser));
        }

        [TestMethod]
        public void SendUserInfoInSqlContext_WriteWithUser()
        {
            var testUser = new TestUserInfo("Bob", "HAL9000");
            string table = GetRandomTableName();

            var result = new List<string>();
            ExecuteSql(new[] { $"SELECT Context = {contextInfoToText} INTO {table}" }, testUser: testUser);
            ExecuteReader(
                @"SELECT * FROM " + table,
                reader => result.Add(reader[0].ToString()), testUser: testUser);

            Assert.AreEqual("Rhetos:Bob,HAL9000", TestUtility.Dump(result));
        }

        [TestMethod]
        public void SendUserInfoInSqlContext_NonAscii()
        {
            var testUser = new TestUserInfo("BobČ", "HAL9000ž");

            Assert.AreEqual("Rhetos:Bob?,HAL9000?", ReadContextWithSqlExecuter(testUser));
        }

        [TestMethod]
        public void SendUserInfoInSqlContext_Long()
        {
            string userName = "abc" + new string('x', 100);
            string workstationName = "def" + new string('x', 100);
            var testUser = new TestUserInfo(userName, workstationName);
            string expected = $"Rhetos:{userName},{workstationName}".Limit(128);

            Assert.AreEqual(expected, ReadContextWithSqlExecuter(testUser));
        }

        private string ReadContextWithSqlExecuter(TestUserInfo testUser)
        {
            var result = new List<string>();
            ExecuteReader(
                $"SELECT {contextInfoToText}",
                reader => result.Add(reader[0].ToString()),
                testUser: testUser);
            return result.Single();
        }

        [TestMethod]
        public void ConsistentRead()
        {
            var tests = new (string Query, string Parameter, string ExpectedReport)[]
            {
                ("WAITFOR DELAY '00:00:01'", "",
                    @"Result: 
                    Log:
                    [Trace] MsSqlExecuter: Executing reader: WAITFOR DELAY '00:00:01'
                    [Info] Performance.MsSqlExecuter: 00:00:01.*ms WAITFOR DELAY '00:00:01'"),

                ("SELECT ClaimRight FROM Common.Claim WHERE ClaimResource = {0} ORDER BY test1", "Common.Claim",
                    @"Result: Rhetos.FrameworkException: SqlException has occurred: Msg 207, Level 16, State 1, Line 1: Invalid column name 'test1'.
                    Log:
                    [Trace] MsSqlExecuter: Executing reader: SELECT ClaimRight FROM Common.Claim WHERE ClaimResource = {0} ORDER BY test1
                    [Error] MsSqlExecuter: Unable to execute SQL query:
                    SELECT ClaimRight FROM Common.Claim WHERE ClaimResource = @__p0 ORDER BY test1"),

                ("SELECT ClaimRight FROM Common.Claim WHERE ClaimResource = {0} ORDER BY 1", "Common.Claim",
                    @"Result: Edit, New, Read, Remove
                    Log:
                    [Trace] MsSqlExecuter: Executing reader: SELECT ClaimRight FROM Common.Claim WHERE ClaimResource = {0} ORDER BY 1"),
            };

            foreach (var test in tests)
            {
                Console.WriteLine($"## Testing [{test.Query}], parameter [{test.Parameter}].");

                var log = new List<string>();
                using (var scope = TestScope.Create(builder => builder.ConfigureLogMonitor(log)))
                {
                    Assert.IsNotNull(scope.Resolve<IPersistenceTransaction>().Connection); // Initialize transaction to avoid doing it on first test and not on second.
                    var sqlExecuter = scope.Resolve<ISqlExecuter>();

                    string simpleSqlExecuterReport = TestSqlReader(
                        read => sqlExecuter.ExecuteReader(string.Format(test.Query, scope.Resolve<ISqlUtility>().QuoteText(test.Parameter)), read),
                        log);

                    string parametrizedSqlExecuterReport = TestSqlReader(
                        read => sqlExecuter.ExecuteReaderRaw(test.Query, new[] { test.Parameter }, read),
                        log);

                    Assert.AreEqual(
                        CleanupIndent(test.ExpectedReport),
                        CleanupMilliseconds(parametrizedSqlExecuterReport),
                        $"Testing expected report: '{test.Query}'");

                    Assert.AreEqual(
                        CleanupSqlExecuterLog(simpleSqlExecuterReport),
                        CleanupSqlExecuterLog(parametrizedSqlExecuterReport),
                        $"Testing same report: '{test.Query}'");
                }
            }
        }

        private static string TestSqlReader(Action<Action<DbDataReader>> executeReader, List<string> log)
        {
            log.Clear();
            int? lastSizeAfterReader = null;
            
            var oldSlowEventLimit = LoggerHelper.SlowEvent;
            string queryResult = null;
            try
            {
                List<string> records = new();
                LoggerHelper.SlowEvent = TimeSpan.FromSeconds(0.5);
                executeReader(reader => records.Add(reader.GetString(0)));
                lastSizeAfterReader = log.Count;
                queryResult = TestUtility.DumpSorted(records);
            }
            catch (Exception e)
            {
                queryResult = $"{e.GetType()}: {e.Message}";
            }
            finally
            {
                LoggerHelper.SlowEvent = oldSlowEventLimit;
            }

            lastSizeAfterReader = lastSizeAfterReader ?? log.Count;
            return "Result: " + queryResult + Environment.NewLine + "Log:" + Environment.NewLine + string.Join(Environment.NewLine, log.Take(lastSizeAfterReader.Value));
        }

        private static string CleanupIndent(string report) => indentRegex.Replace(report, "");

        private static Regex indentRegex = new Regex(@"^\s+", RegexOptions.Multiline);

        private static string CleanupMilliseconds(string report) => subSecondDigitsRegex.Replace(report, "*ms");

        private static string CleanupSqlExecuterLog(string report)
        {
            // Performance log includes milliseconds that are not reproducible.
            report = subSecondDigitsRegex.Replace(report, "*ms");

            // MsSqlExecuter's method does not support parameters.
            report = sqlStringRegex.Replace(report, "*param"); // Parameters inserted as literals.
            report = report.Replace("{0}", "*param"); // Input parameters provided to ISqlExecuer.
            report = report.Replace("@__p0", "*param"); // DbParameters prepared by BaseSqlExecuter.

            return report;
        }

        private static Regex subSecondDigitsRegex = new Regex(@"\b\d{7}\b");

        private static Regex sqlStringRegex = new Regex(@"N?'.*?'");

        [TestMethod]
        public void ConsistentExecute()
        {
            var tests = new (string Query, string Parameter, string ExpectedReport)[]
            {
                ("WAITFOR DELAY '00:00:01'", "",
                    @"Result: 
                    Log:
                    [Trace] MsSqlExecuter: Executing command: WAITFOR DELAY '00:00:01'
                    [Info] Performance.MsSqlExecuter: 00:00:01.*ms WAITFOR DELAY '00:00:01'"),

                ("PRINT test1", "test2",
                    @"Result: Rhetos.FrameworkException: SqlException has occurred: Msg 128, Level 15, State 1, Line 1: The name ""test1"" is not permitted in this context. Valid expressions are constants, constant expressions, and (in some contexts) variables. Column names are not permitted.
                        Log:
                        [Trace] MsSqlExecuter: Executing command: PRINT test1
                        [Error] MsSqlExecuter: Unable to execute SQL query:
                        PRINT test1"),

                ("PRINT {0}", "test3",
                    @"Result: 
                    Log:
                    [Trace] MsSqlExecuter: Executing command: PRINT {0}"),
            };

            foreach (var test in tests)
            {
                Console.WriteLine($"## Testing [{test.Query}], parameter [{test.Parameter}].");

                var log = new List<string>();
                using (var scope = TestScope.Create(builder => builder.ConfigureLogMonitor(log)))
                {
                    Assert.IsNotNull(scope.Resolve<IPersistenceTransaction>().Connection); // Initialize transaction to avoid doing it on first test and not on second.
                    var sqlExecuter = scope.Resolve<ISqlExecuter>();

                    var simpleSqlExecuterReport = TestSqlExecuter(
                        read => sqlExecuter.ExecuteSql(string.Format(test.Query, scope.Resolve<ISqlUtility>().QuoteText(test.Parameter))),
                        log);

                    var parametrizedSqlExecuterReport = TestSqlExecuter(
                        read => sqlExecuter.ExecuteSqlRaw(test.Query, new[] { test.Parameter }),
                        log);

                    Assert.AreEqual(
                        CleanupIndent(test.ExpectedReport),
                        CleanupMilliseconds(parametrizedSqlExecuterReport),
                        $"Testing expected report: '{test.Query}'");

                    Assert.AreEqual(
                        CleanupSqlExecuterLog(simpleSqlExecuterReport),
                        CleanupSqlExecuterLog(parametrizedSqlExecuterReport),
                        $"Testing same report: '{test.Query}'");
                }
            }
        }

        private static string TestSqlExecuter(Action<Action<DbDataReader>> executeReader, List<string> log)
        {
            log.Clear();
            int? lastSizeAfterReader = null;

            var oldSlowEventLimit = LoggerHelper.SlowEvent;
            string queryResult = null;
            try
            {
                List<string> records = new();
                LoggerHelper.SlowEvent = TimeSpan.FromSeconds(0.5);
                executeReader(reader => records.Add(reader.GetString(0)));
                lastSizeAfterReader = log.Count;
                queryResult = TestUtility.DumpSorted(records);
            }
            catch (Exception e)
            {
                queryResult = $"{e.GetType()}: {e.Message}";
            }
            finally
            {
                LoggerHelper.SlowEvent = oldSlowEventLimit;
            }

            lastSizeAfterReader = lastSizeAfterReader ?? log.Count;
            return "Result: " + queryResult + Environment.NewLine + "Log:" + Environment.NewLine + string.Join(Environment.NewLine, log.Take(lastSizeAfterReader.Value));
        }

        [TestMethod]
        public void LockWait()
        {
            using var scope1 = TestScope.Create();
            using var scope2 = TestScope.Create();

            var sqlExecuter1 = scope1.Resolve<ISqlExecuter>();
            var sqlExecuter2 = scope2.Resolve<ISqlExecuter>();

            sqlExecuter1.GetDbLock("a");
            sqlExecuter2.GetDbLock("b");

            sqlExecuter2.ExecuteSql("SET LOCK_TIMEOUT 1000");

            var stopwatch = Stopwatch.StartNew();
            TestUtility.ShouldFail<UserException>(() => sqlExecuter2.GetDbLock("A"), "The resource you are trying to access is currently unavailable");
            stopwatch.Stop();
            Assert.IsTrue(stopwatch.Elapsed.TotalSeconds > 0.95 && stopwatch.Elapsed.TotalSeconds < 2, stopwatch.Elapsed.TotalSeconds.ToString());
        }

        [TestMethod]
        public void LockNoWait()
        {
            using var scope1 = TestScope.Create();
            using var scope2 = TestScope.Create();

            var sqlExecuter1 = scope1.Resolve<ISqlExecuter>();
            var sqlExecuter2 = scope2.Resolve<ISqlExecuter>();

            sqlExecuter1.GetDbLock("a");
            sqlExecuter2.GetDbLock("b");

            var stopwatch = Stopwatch.StartNew();
            TestUtility.ShouldFail<UserException>(() => sqlExecuter2.GetDbLock("A", wait: false), "The resource you are trying to access is currently unavailable");
            stopwatch.Stop();
            Assert.IsTrue(stopwatch.Elapsed.TotalSeconds < 0.5, stopwatch.Elapsed.TotalSeconds.ToString());
        }

        [TestMethod]
        public void LockRelease()
        {
            using var scope1 = TestScope.Create();
            using var scope2 = TestScope.Create();

            var sqlExecuter1 = scope1.Resolve<ISqlExecuter>();
            var sqlExecuter2 = scope2.Resolve<ISqlExecuter>();

            sqlExecuter1.GetDbLock("a");
            sqlExecuter1.GetDbLock("b");
            TestUtility.ShouldFail<UserException>(() => sqlExecuter2.GetDbLock("A", wait: false), "The resource you are trying to access is currently unavailable");
            sqlExecuter1.ReleaseDbLock("a");
            sqlExecuter2.GetDbLock("A");
            TestUtility.ShouldFail<UserException>(() => sqlExecuter2.GetDbLock("B", wait: false), "The resource you are trying to access is currently unavailable");
        }

        [TestMethod]
        public void LockMany()
        {
            using var scope1 = TestScope.Create();
            using var scope2 = TestScope.Create();

            var sqlExecuter1 = scope1.Resolve<ISqlExecuter>();
            var sqlExecuter2 = scope2.Resolve<ISqlExecuter>();

            sqlExecuter1.GetDbLock(new[] { "a", "b", "c" });
            sqlExecuter1.ReleaseDbLock("c");

            sqlExecuter2.GetDbLock(new[] { "c", "d" });
            TestUtility.ShouldFail<UserException>(() => sqlExecuter2.GetDbLock(new[] { "a", "b", "c" }, wait: false), "The resource you are trying to access is currently unavailable");
        }

        [TestMethod]
        public void LockMany2()
        {
            using var scope1 = TestScope.Create();
            using var scope2 = TestScope.Create();

            var sqlExecuter1 = scope1.Resolve<ISqlExecuter>();
            var sqlExecuter2 = scope2.Resolve<ISqlExecuter>();

            string[] resources = new[] { "a", "b", "c" };
            sqlExecuter1.GetDbLock(resources);
            sqlExecuter1.ReleaseDbLock("c");

            var report = new List<string>();
            foreach (var resource in resources)
            {
                try
                {
                    sqlExecuter2.GetDbLock(resource, wait: false);
                    report.Add(resource + " not locked");
                }
                catch
                {
                    report.Add(resource + " locked");
                }
            }
            Assert.AreEqual("a locked, b locked, c not locked", string.Join(", ", report));

            var stopwatch = Stopwatch.StartNew();
            TestUtility.ShouldFail<UserException>(() => sqlExecuter2.GetDbLock(resources, wait: false), "The resource you are trying to access is currently unavailable");
            stopwatch.Stop();
            Assert.IsTrue(stopwatch.Elapsed.TotalSeconds < 0.5, stopwatch.Elapsed.TotalSeconds.ToString());
        }

        [TestMethod]
        public void LockManyWait()
        {
            using var scope1 = TestScope.Create();
            using var scope2 = TestScope.Create();

            var sqlExecuter1 = scope1.Resolve<ISqlExecuter>();
            var sqlExecuter2 = scope2.Resolve<ISqlExecuter>();

            sqlExecuter1.GetDbLock(new[] { "b", "c" });
            sqlExecuter1.ReleaseDbLock("c");

            sqlExecuter2.ExecuteSql("SET LOCK_TIMEOUT 1000");

            var stopwatch = Stopwatch.StartNew();
            TestUtility.ShouldFail<UserException>(() => sqlExecuter2.GetDbLock(new[] { "a", "b", "c" }), "The resource you are trying to access is currently unavailable");
            stopwatch.Stop();
            Assert.IsTrue(stopwatch.Elapsed.TotalSeconds > 0.95 && stopwatch.Elapsed.TotalSeconds < 2, stopwatch.Elapsed.TotalSeconds.ToString());
        }
    }
}