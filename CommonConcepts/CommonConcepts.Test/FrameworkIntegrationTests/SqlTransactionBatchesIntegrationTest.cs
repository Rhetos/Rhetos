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
using CommonConcepts.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Rhetos.Persistence.Test
{
    [TestClass]
    public class SqlTransactionBatchesIntegrationTest
    {
        private static string LogActionName => MethodBase.GetCurrentMethod().DeclaringType.Name;

        [TestMethod]
        public void SqlTransactionBatches_TransactionLevelIncreased()
        {
            var log = new List<string>();
            using (var scope = TestScope.Create(builder => builder.ConfigureLogMonitor(log)))
            {
                var scripts = new SqlBatchScript[]
                {
                    new SqlBatchScript { Sql = "print '1'" },
                    new SqlBatchScript { Sql = "begin tran" },
                    new SqlBatchScript { Sql = "print '2'", Name = "C:\\script2.sql" },
                };

                var stb = scope.Resolve<ISqlTransactionBatches>();

                TestUtility.ShouldFail<FrameworkException>(
                    () => stb.Execute(scripts),
                    "FrameworkException: Database transaction state has been unexpectedly modified in SQL commands."
                        + " Transaction count is 2, expected value is 1."
                        + " See error log for more information.");

                string logReport = string.Join(Environment.NewLine, log);

                TestUtility.AssertContains(logReport, new[]
                {
                    "Database transaction state has been unexpectedly modified in SQL commands."
                        + " Transaction count is 2, expected value is 1."
                        + " Executed 3 commands:",
                    "0: print '1'",
                    "1: begin tran",
                    "2: C:\\script2.sql",
                });
            }
        }

        [TestMethod]
        public void SqlTransactionBatches_TransactionLevelDecreased()
        {
            var log = new List<string>();
            using (var scope = TestScope.Create(builder => builder.ConfigureLogMonitor(log)))
            {
                var scripts = new SqlBatchScript[]
                {
                    new SqlBatchScript { Sql = "print '1'" },
                    new SqlBatchScript { Sql = "rollback" },
                    new SqlBatchScript { Sql = "print '2'", Name = "C:\\script2.sql" },
                };

                var stb = scope.Resolve<ISqlTransactionBatches>();

                TestUtility.ShouldFail<FrameworkException>(
                    () => stb.Execute(scripts),
                    "FrameworkException: Database transaction state has been unexpectedly modified in SQL commands."
                        + " Transaction count is 0, expected value is 1."
                        + " See error log for more information.");

                string logReport = string.Join(Environment.NewLine, log);

                TestUtility.AssertContains(logReport, new[]
                {
                    "Database transaction state has been unexpectedly modified in SQL commands."
                        + " Transaction count is 0, expected value is 1."
                        + " Executed 3 commands:",
                    "0: print '1'",
                    "1: rollback",
                    "2: C:\\script2.sql",
                });
            }
        }

        /// <summary>
        /// Inserts a record with ISqlTransactionBatches, and cancels the main unit-of-work scope.
        /// Returns @@TRANCOUNT value at insert.
        /// </summary>
        public string ReportTransactionState(Action<ContainerBuilder> registerCustomComponents = null)
        {
            Guid id = Guid.NewGuid();

            using (var scope = TestScope.Create(registerCustomComponents))
            {
                string sql =
                    @$"IF @@TRANCOUNT > 0
                        INSERT INTO Common.Log (Action, ItemId, Description) SELECT '{LogActionName}', '{id}', 'transaction'
                    ELSE
                        INSERT INTO Common.Log (Action, ItemId, Description) SELECT '{LogActionName}', '{id}', 'no transaction'";

                var stb = scope.Resolve<ISqlTransactionBatches>();
                stb.Execute(new[] { new SqlBatchScript { Sql = sql } });
                // Omitted `scope.CommitAndClose()` may affect the script execution by ISqlTransactionBatches,
                // depending on configuration from `registerCustomComponents`.
            }

            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();
                var inserted = repository.Common.LogReader
                    .Query(log => log.ItemId == id)
                    .Select(log => log.Description)
                    .SingleOrDefault();
                return inserted ?? "rollback";
            }
        }

        [TestMethod]
        public void SqlTransactionBatches_SeparateTransaction()
        {
            Assert.AreEqual("transaction", ReportTransactionState());
            // Omitted `scope.CommitAndClose()` should not affect the script executed by ISqlTransactionBatches
            // since new scope uses new database connection by default.
        }

        [TestMethod]
        public void SqlTransactionBatches_SameTransaction()
        {
            Assert.AreEqual("rollback", ReportTransactionState(builder => builder
                .ConfigureOptions<SqlTransactionBatchesOptions>(o => o.ExecuteOnNewConnection = false)));
            // Omitted `scope.CommitAndClose()` should affect the script executed by ISqlTransactionBatches
            // since it uses same database connection as this scope.
        }

        [TestMethod]
        public void SqlTransactionBatches_OverridesTransaction()
        {
            Assert.AreEqual("transaction", ReportTransactionState(builder => builder
                .ConfigureOptions<PersistenceTransactionOptions>(o => o.UseDatabaseTransaction = false)));
            // Omitted `scope.CommitAndClose()` should not affect the script executed by ISqlTransactionBatches
            // since it uses new database connection by default.
        }

        [TestMethod]
        public void SqlTransactionBatches_InheritedNoTransaction()
        {
            // Configuring SqlTransactionBatches to use parent scope connection,
            // which is configured to not created transactions.
            Assert.AreEqual("no transaction", ReportTransactionState(builder => builder
                .ConfigureOptions<SqlTransactionBatchesOptions>(o => o.ExecuteOnNewConnection = false)
                .ConfigureOptions<PersistenceTransactionOptions>(o => o.UseDatabaseTransaction = false)));
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            using (var scope = TestScope.Create())
            {
                var sqlExecuter = scope.Resolve<ISqlExecuter>();
                sqlExecuter.ExecuteSqlInterpolated(
                    $"DELETE FROM Common.Log WHERE Action = {LogActionName} AND TableName IS NULL");
                scope.CommitAndClose();
            }
        }
    }
}