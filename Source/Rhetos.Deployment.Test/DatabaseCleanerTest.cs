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

using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Utilities;
using System.Data;
using System.Text.RegularExpressions;
using Rhetos.TestCommon;

namespace Rhetos.Deployment.Test
{
    [TestClass]
    public class DatabaseCleanerTest
    {
        public DatabaseCleanerTest()
        {
            var configurationProvider = new ConfigurationBuilder()
                .AddRhetosAppConfiguration(AppDomain.CurrentDomain.BaseDirectory)
                .AddConfigurationManagerConfiguration()
                .Build();

            LegacyUtilities.Initialize(configurationProvider);
        }

        private class MockSqlExecuter : ISqlExecuter
        {
            DataTable Columns;
            DataTable Tables;
            DataTable Schemas;

            public MockSqlExecuter(string columns, string tables, string schemas)
            {
                Columns = new DataTable();
                Columns.Columns.Add();
                Columns.Columns.Add();
                Columns.Columns.Add();
                foreach (var c in columns.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)))
                    Columns.Rows.Add(c.Split('.'));

                Tables = new DataTable();
                Tables.Columns.Add();
                Tables.Columns.Add();
                foreach (var t in tables.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)))
                    Tables.Rows.Add(t.Split('.'));

                Schemas = new DataTable();
                Schemas.Columns.Add();
                foreach (var s in schemas.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)))
                {
                    if (!s.StartsWith("_"))
                        throw new Exception("Invalid test data: Migration schema name must start with '_'.");
                    Schemas.Rows.Add(s);
                }
            }

            public void ExecuteReader(string command, Action<System.Data.Common.DbDataReader> action)
            {
                var options = new Dictionary<string, DataTable>
                {
                    { "COLUMNS", Columns },
                    { "TABLES", Tables },
                    { "SCHEMATA", Schemas }
                };

                var dataTable = options.First(o => command.Contains(o.Key)).Value;

                var reader = new DataTableReader(dataTable);
                while (reader.Read())
                    action(reader);
            }

            Regex DropColumn = new Regex(@"^ALTER TABLE \[(\w+)\].\[(\w+)\] DROP COLUMN \[(\w+)\]$");
            Regex DropTable = new Regex(@"^DROP TABLE \[(\w+)\].\[(\w+)\]$");
            Regex DropSchema = new Regex(@"^DROP SCHEMA \[(\w+)\]$");

            public List<string> DroppedColumns = new List<string>();
            public List<string> DroppedTables = new List<string>();
            public List<string> DroppedSchemas = new List<string>();

            public void ExecuteSql(IEnumerable<string> commands, bool useTransaction)
            {
                ExecuteSql(commands, useTransaction, null, null);
            }

            public void ExecuteSql(IEnumerable<string> commands, bool useTransaction, Action<int> beforeExecute, Action<int> afterExecute)
            {
                foreach (var command in commands)
                {
                    Console.WriteLine("[SQL] " + command);

                    var match = DropColumn.Match(command);
                    if (match.Success)
                    {
                        DroppedColumns.Add(match.Groups[1] + "." + match.Groups[2] + "." + match.Groups[3]);
                        continue;
                    }

                    match = DropTable.Match(command);
                    if (match.Success)
                    {
                        DroppedTables.Add(match.Groups[1] + "." + match.Groups[2]);
                        continue;
                    }

                    match = DropSchema.Match(command);
                    if (match.Success)
                    {
                        DroppedSchemas.Add(match.Groups[1].ToString());
                        continue;
                    }

                    throw new Exception("Unexpected SQL command in MockSqlExecuter.");
                }
            }
        }

        public void TestCleanupRedundantOldData(string description,
            string oldColumns, string oldTables, string oldSchemas,
            string expectedDeletedColumns, string expectedDeletedTables, string expectedDeletedSchemas)
        {
            var mockSqlExecuter = new MockSqlExecuter(oldColumns, oldTables, oldSchemas);
            var databaseCleaner = new DatabaseCleaner(new ConsoleLogProvider(), mockSqlExecuter);
            databaseCleaner.RemoveRedundantMigrationColumns();

            Assert.AreEqual(expectedDeletedColumns, TestUtility.DumpSorted(mockSqlExecuter.DroppedColumns), description + ": Deleted columns.");
            Assert.AreEqual(expectedDeletedTables, TestUtility.DumpSorted(mockSqlExecuter.DroppedTables), description + ": Deleted tables.");
            Assert.AreEqual(expectedDeletedSchemas, TestUtility.DumpSorted(mockSqlExecuter.DroppedSchemas), description + ": Deleted schemas.");
        }

        [TestMethod]
        public void CleanupRedundantOldDataTest()
        {
            TestCleanupRedundantOldData("Empty schemas",
                "", "", "_s1, _s2, _s3",
                "", "", "_s1, _s2, _s3");

            TestCleanupRedundantOldData("Empty tables",
                "", "_s1.t1, _s1.t2", "_s1, _s2",
                "", "_s1.t1, _s1.t2", "_s1, _s2");

            TestCleanupRedundantOldData("Used table t1, and empty table t2",
                "_s.t.ID, _s.t.c1, _s.t.c2", "_s.t, _s.t2", "_s",
                "", "_s.t2", "");

            TestCleanupRedundantOldData("Production table exists, ID column redundant",
                "_s.t.ID, s.t.ID", "_s.t", "_s",
                "", "_s.t", "_s");

            TestCleanupRedundantOldData("Production table exists, no columns redundant",
                "_s.t.ID, _s.t.c1, _s.t.c2, s.t.ID", "_s.t", "_s",
                "", "", "");

            TestCleanupRedundantOldData("Redundant column c1",
                "_s.t.ID, _s.t.c1, _s.t.c2, s.t.ID, s.t.c1", "_s.t", "_s",
                "_s.t.c1", "", "");

            TestCleanupRedundantOldData("All columns redundant",
                "_s.t.ID, _s.t.c1, _s.t.c2, s.t.ID, s.t.c1, s.t.c2", "_s.t", "_s",
                "", "_s.t", "_s");
        }

        public void TestCleanupOldData(string description,
            string oldTables, string oldSchemas,
            string expectedDeletedTables, string expectedDeletedSchemas)
        {
            var mockSqlExecuter = new MockSqlExecuter("", oldTables, oldSchemas);
            var databaseCleaner = new DatabaseCleaner(new ConsoleLogProvider(), mockSqlExecuter);
            Console.WriteLine("Report: " + databaseCleaner.DeleteAllMigrationData());

            Assert.AreEqual("", TestUtility.DumpSorted(mockSqlExecuter.DroppedColumns), description + ": Deleted columns.");
            Assert.AreEqual(expectedDeletedTables, TestUtility.DumpSorted(mockSqlExecuter.DroppedTables), description + ": Deleted tables.");
            Assert.AreEqual(expectedDeletedSchemas, TestUtility.DumpSorted(mockSqlExecuter.DroppedSchemas), description + ": Deleted schemas.");
        }

        [TestMethod]
        public void CleanupOldDataTest()
        {
            TestCleanupOldData("Schemas",
                "", "_s1, _s2, _s3",
                "", "_s1, _s2, _s3");

            TestCleanupOldData("Tables and schemas",
                "_s1.t1, _s1.t2", "_s1, _s2",
                "_s1.t1, _s1.t2", "_s1, _s2");
        }
    }
}
