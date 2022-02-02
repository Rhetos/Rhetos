﻿/*
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

using CommonConcepts.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Logging;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Rhetos.Persistence.Test
{
    [TestClass]
    public class MsSqlExecuterIntegrationTest
    {
        private static string LogActionName => MethodBase.GetCurrentMethod().DeclaringType.Name;

        /// <summary>
        /// Executes the MsSqlExecuter commands and commits the transaction by default.
        /// </summary>
        private static void InTransaction(Action<MsSqlExecuter> sqlExecuterAction, string connectionString = null, IUserInfo testUser = null, bool commit = true)
        {
            connectionString ??= SqlUtility.ConnectionString;
            testUser ??= new NullUserInfo();

            using (var persistenceTransaction = new PersistenceTransaction(
                new ConsoleLogProvider(),
                connectionString,
                testUser,
                new PersistenceTransactionOptions()))
            {
                var sqlExecuter = new MsSqlExecuter(new ConsoleLogProvider(), persistenceTransaction);
                sqlExecuterAction.Invoke(sqlExecuter);
                if (commit)
                    persistenceTransaction.CommitAndClose();
            }
        }

        /// <summary>
        /// Executes the MsSqlExecuter command and commits the transaction by default.
        /// </summary>
        private static void ExecuteSql(IEnumerable<string> commands, string connectionString = null, IUserInfo testUser = null, bool commit = true)
        {
            InTransaction(sqlExecuter => sqlExecuter.ExecuteSql(commands), connectionString, testUser, commit);
        }

        /// <summary>
        /// Executes the MsSqlExecuter command and commits the transaction by default.
        /// </summary>
        private static void ExecuteReader(string command, Action<DbDataReader> action, string connectionString = null, IUserInfo testUser = null, bool commit = true)
        {
            InTransaction(sqlExecuter => sqlExecuter.ExecuteReader(command, action), connectionString, testUser, commit);
        }

        private string GetRandomTableName()
        {
            ExecuteSql(new[] { "IF SCHEMA_ID('RhetosUnitTest') IS NULL EXEC('CREATE SCHEMA RhetosUnitTest')" });
            var newTableName = "RhetosUnitTest.T" + Guid.NewGuid().ToString().Replace("-", "");
            Console.WriteLine("Generated random table name: " + newTableName);
            return newTableName;
        }

        [TestInitialize]
        public void CheckDatabaseIsMsSql()
        {
            // Creating empty Rhetos DI scope just to initialize static utilities that are required for test in this class.
            using (var scope = TestScope.Create())
                Assert.IsNotNull(SqlUtility.ConnectionString);

            TestUtility.CheckDatabaseAvailability("MsSql");
        }

        [ClassCleanup]
        public static void DropRhetosUnitTestSchema()
        {
            Console.WriteLine("=== ClassCleanup ===");

            using (var scope = TestScope.Create())
            {
                var sqlExecuter = scope.Resolve<ISqlExecuter>();
                sqlExecuter.ExecuteSqlInterpolated(
                    $"DELETE FROM Common.Log WHERE Action = {LogActionName} AND TableName IS NULL");
                scope.CommitAndClose();
            }

            if (SqlUtility.DatabaseLanguage == "MsSql")
                ExecuteSql(new[] {
                    @"DECLARE @sql NVARCHAR(MAX)
                        SET @sql = ''
                        SELECT @sql = @sql + 'DROP TABLE RhetosUnitTest.' + QUOTENAME(name) + ';' + CHAR(13) + CHAR(10)
                            FROM sys.tables WHERE schema_id = SCHEMA_ID('RhetosUnitTest')
                        EXEC (@sql)",
                    "IF SCHEMA_ID('RhetosUnitTest') IS NOT NULL DROP SCHEMA RhetosUnitTest" });
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

            var connectionStringBuilder = new SqlConnectionStringBuilder(SqlUtility.ConnectionString);
            connectionStringBuilder.InitialCatalog = nonexistentDatabase;
            connectionStringBuilder.IntegratedSecurity = true;
            connectionStringBuilder.ConnectTimeout = 1;
            string nonexistentDatabaseConnectionString = connectionStringBuilder.ConnectionString;
            Console.WriteLine(nonexistentDatabaseConnectionString);

            TestUtility.ShouldFail(() => ExecuteSql(new[] { "print 123" }, nonexistentDatabaseConnectionString),
                connectionStringBuilder.DataSource, connectionStringBuilder.InitialCatalog, Environment.UserName);
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
            ExecuteReader("SELECT context_info()", reader => result.Add(reader[0]), SqlUtility.ConnectionString, testUser);

            Console.WriteLine(result.Single());
            Assert.AreEqual(typeof(DBNull), result.Single().GetType());
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

        private static string ReadContextWithSqlExecuter(TestUserInfo testUser)
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
            var tests = new (string Query, string Parameter)[]
            {
                ("WAITFOR DELAY '00:00:01'", ""),
                ("SELECT ClaimRight FROM Common.Claim WHERE ClaimResource = {0} ORDER BY test1", "Common.Claim"),
                ("SELECT ClaimRight FROM Common.Claim WHERE ClaimResource = {0} ORDER BY 1", "Common.Claim"),
            };

            var msSqlExecuterReport = new List<string>();
            var baseSqlExecuterReport = new List<string>();

            foreach (var test in tests)
            {
                string title = $"## Testing [{test.Query}], parameter [{test.Parameter}].";
                Console.WriteLine(title);
                msSqlExecuterReport.Add(Environment.NewLine + title);
                baseSqlExecuterReport.Add(Environment.NewLine + title);

                var log = new List<string>();
                using (var scope = TestScope.Create(builder => builder.ConfigureLogMonitor(log)))
                {
                    Assert.IsNotNull(scope.Resolve<IPersistenceTransaction>().Connection); // Initialize transaction to avoid doing it on first test and not on second.
                    var sqlExecuter = scope.Resolve<ISqlExecuter>();

                    var msSqlExecuterResult = TestSqlReader(
                        read => sqlExecuter.ExecuteReader(string.Format(test.Query, SqlUtility.QuoteText(test.Parameter)), read),
                        log);
                    msSqlExecuterReport.Add(msSqlExecuterResult);

                    var baseSqlExecuterResult = TestSqlReader(
                        read => sqlExecuter.ExecuteReaderRaw(test.Query, new[] { test.Parameter }, read),
                        log);
                    baseSqlExecuterReport.Add(baseSqlExecuterResult);

                    Console.WriteLine(msSqlExecuterResult);
                }
            }

            Assert.AreEqual(
                CleanupSqlExecuterLog(string.Join(Environment.NewLine, msSqlExecuterReport)),
                CleanupSqlExecuterLog(string.Join(Environment.NewLine, baseSqlExecuterReport)),
                $"{nameof(msSqlExecuterReport)} result does not match {nameof(baseSqlExecuterReport)}.");
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

        private static string CleanupSqlExecuterLog(string report)
        {
            // Performance log includes milliseconds that are not reproducible.
            report = subSecondDigitsRegex.Replace(report, "*ms");

            // MsSqlExecuter's method supports multiple SQL commands, hence the additional information in log.
            report = report.Replace("[Trace] MsSqlExecuter: Executing 1 commands.\r\n", "");

            // MsSqlExecuter's method does not support parameters.
            report = sqlStringRegex.Replace(report, "*param");
            report = report.Replace("{0}", "*param");

            return report;
        }

        private static Regex subSecondDigitsRegex = new Regex(@"\b\d{7}\b");

        private static Regex sqlStringRegex = new Regex(@"'.*?'");

        [TestMethod]
        public void ConsistentExecute()
        {
            var tests = new (string Query, string Parameter)[]
            {
                ("WAITFOR DELAY '00:00:01'", ""),
                ("PRINT test1", "test2"),
                ("PRINT {0}", "test3"),
            };

            var msSqlExecuterReport = new List<string>();
            var baseSqlExecuterReport = new List<string>();

            foreach (var test in tests)
            {
                string title = $"## Testing [{test.Query}], parameter [{test.Parameter}].";
                Console.WriteLine(title);
                msSqlExecuterReport.Add(Environment.NewLine + title);
                baseSqlExecuterReport.Add(Environment.NewLine + title);

                var log = new List<string>();
                using (var scope = TestScope.Create(builder => builder.ConfigureLogMonitor(log)))
                {
                    Assert.IsNotNull(scope.Resolve<IPersistenceTransaction>().Connection); // Initialize transaction to avoid doing it on first test and not on second.
                    var sqlExecuter = scope.Resolve<ISqlExecuter>();

                    var msSqlExecuterResult = TestSqlExecuter(
                        read => sqlExecuter.ExecuteSql(string.Format(test.Query, SqlUtility.QuoteText(test.Parameter))),
                        log);
                    msSqlExecuterReport.Add(msSqlExecuterResult);

                    var baseSqlExecuterResult = TestSqlExecuter(
                        read => sqlExecuter.ExecuteSqlRaw(test.Query, new[] { test.Parameter }),
                        log);
                    baseSqlExecuterReport.Add(baseSqlExecuterResult);

                    Console.WriteLine(msSqlExecuterResult);
                }
            }

            Assert.AreEqual(
                CleanupSqlExecuterLog(string.Join(Environment.NewLine, msSqlExecuterReport)),
                CleanupSqlExecuterLog(string.Join(Environment.NewLine, baseSqlExecuterReport)),
                $"{nameof(msSqlExecuterReport)} result does not match {nameof(baseSqlExecuterReport)}.");
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
    }
}