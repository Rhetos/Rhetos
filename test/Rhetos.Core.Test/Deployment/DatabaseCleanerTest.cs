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
using Rhetos.DatabaseGenerator.Test;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;

namespace Rhetos.Deployment.Test
{
    [TestClass]
    public class DatabaseCleanerTest
    {
        public void TestCleanupRedundantOldData(string description,
            string oldColumns, string oldTables, string oldSchemas,
            string expectedDeletedColumns, string expectedDeletedTables, string expectedDeletedSchemas)
        {
            var mockSqlExecuter = new MockSqlExecuter(oldColumns, oldTables, oldSchemas);
            var fakeSqlTransactionBatches = new FakeSqlTransactionBatches(mockSqlExecuter);
            var databaseCleaner = new DatabaseCleaner(new ConsoleLogProvider(), mockSqlExecuter, fakeSqlTransactionBatches, new FakeSqlUtility());
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
            var fakeSqlTransactionBatches = new FakeSqlTransactionBatches(mockSqlExecuter);
            var databaseCleaner = new DatabaseCleaner(new ConsoleLogProvider(), mockSqlExecuter, fakeSqlTransactionBatches, new FakeSqlUtility());
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
