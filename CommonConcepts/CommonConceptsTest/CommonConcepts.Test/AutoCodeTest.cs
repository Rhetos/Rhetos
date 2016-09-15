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
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Configuration.Autofac;
using Rhetos.Utilities;
using Rhetos.TestCommon;
using System.Threading.Tasks;
using System.Diagnostics;

namespace CommonConcepts.Test
{
    [TestClass]
    public class AutoCodeTest
    {
        private static void DeleteOldData(RhetosTestContainer container)
        {
            container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                {
                    @"DELETE FROM TestAutoCode.ReferenceGroup;
                    DELETE FROM TestAutoCode.ShortReferenceGroup;
                    DELETE FROM TestAutoCode.StringGroup;
                    DELETE FROM TestAutoCode.IntGroup;
                    DELETE FROM TestAutoCode.Simple;
                    DELETE FROM TestAutoCode.DoubleAutoCode;
                    DELETE FROM TestAutoCode.DoubleAutoCodeWithGroup;
                    DELETE FROM TestAutoCode.IntegerAutoCode;
                    DELETE FROM TestAutoCode.Grouping"
                });
        }

        private static void TestSimple(RhetosTestContainer container, Common.DomRepository repository, string format, string expectedCode)
        {
            Guid id = Guid.NewGuid();
            repository.TestAutoCode.Simple.Insert(new[] { new TestAutoCode.Simple { ID = id, Code = format } });

            container.Resolve<Common.ExecutionContext>().EntityFrameworkContext.ClearCache();

            string generatedCode = repository.TestAutoCode.Simple.Query().Where(item => item.ID == id).Select(item => item.Code).Single();
            Console.WriteLine(format + " => " + generatedCode);
            Assert.AreEqual(expectedCode, generatedCode);
        }

        private static void TestIntAutoCode(RhetosTestContainer container, Common.DomRepository repository, int? input, int expectedCode)
        {
            Guid id = Guid.NewGuid();
            repository.TestAutoCode.IntegerAutoCode.Insert(new[] { new TestAutoCode.IntegerAutoCode { ID = id, Code = input } });

            container.Resolve<Common.ExecutionContext>().EntityFrameworkContext.ClearCache();

            int? generatedCode = repository.TestAutoCode.IntegerAutoCode.Query().Where(item => item.ID == id).Select(item => item.Code).Single();
            Console.WriteLine(input.ToString() + " => " + generatedCode.ToString());
            Assert.AreEqual(expectedCode, generatedCode);
        }

        private static void TestDoubleAutoCode(RhetosTestContainer container, Common.DomRepository repository, string formatA, string formatB, string expectedCodes)
        {
            Guid id = Guid.NewGuid();
            repository.TestAutoCode.DoubleAutoCode.Insert(new[] { new TestAutoCode.DoubleAutoCode { ID = id, CodeA = formatA, CodeB = formatB } });

            container.Resolve<Common.ExecutionContext>().EntityFrameworkContext.ClearCache();

            string generatedCodes = repository.TestAutoCode.DoubleAutoCode.Query()
                .Where(item => item.ID == id)
                .Select(item => item.CodeA + "," + item.CodeB).Single();
            Console.WriteLine(formatA + "," + formatB + " => " + generatedCodes);
            Assert.AreEqual(expectedCodes, generatedCodes);
        }

        private static void TestDoubleAutoCodeWithGroup(RhetosTestContainer container, Common.DomRepository repository, string group, string formatA, string formatB, string expectedCodes)
        {
            Guid id = Guid.NewGuid();
            repository.TestAutoCode.DoubleAutoCodeWithGroup.Insert(new[] { new TestAutoCode.DoubleAutoCodeWithGroup { ID = id, Grouping = group, CodeA = formatA, CodeB = formatB } });

            container.Resolve<Common.ExecutionContext>().EntityFrameworkContext.ClearCache();

            string generatedCodes = repository.TestAutoCode.DoubleAutoCodeWithGroup.Query()
                .Where(item => item.ID == id)
                .Select(item => item.CodeA + "," + item.CodeB).Single();
            Console.WriteLine(formatA + "," + formatB + " => " + generatedCodes);
            Assert.AreEqual(expectedCodes, generatedCodes);
        }

        private static void TestDoubleIntegerAutoCodeWithGroup(RhetosTestContainer container, Common.DomRepository repository, int group, int? codeA, int? codeB, string expectedCodes)
        {
            Guid id = Guid.NewGuid();
            repository.TestAutoCode.IntegerAutoCodeForEach.Insert(new[] {
                new TestAutoCode.IntegerAutoCodeForEach { ID = id, Grouping = group, CodeA = codeA, CodeB = codeB } });

            container.Resolve<Common.ExecutionContext>().EntityFrameworkContext.ClearCache();

            string generatedCodes = repository.TestAutoCode.IntegerAutoCodeForEach.Query()
                .Where(item => item.ID == id)
                .Select(item => item.CodeA.ToString() + "," + item.CodeB.ToString()).Single();
            Console.WriteLine(codeA.ToString() + "," + codeB.ToString() + " => " + generatedCodes);
            Assert.AreEqual(expectedCodes, generatedCodes);
        }
        
        [TestMethod]
        public void Simple()
        {
            using (var container = new RhetosTestContainer())
            {
                DeleteOldData(container);
                var repository = container.Resolve<Common.DomRepository>();

                TestSimple(container, repository, "+", "1");
                TestSimple(container, repository, "+", "2");
                TestSimple(container, repository, "+", "3");
                TestSimple(container, repository, "9", "9");
                TestSimple(container, repository, "+", "10");
                TestSimple(container, repository, "+", "11");
                TestSimple(container, repository, "AB+", "AB1");
                TestSimple(container, repository, "X", "X");
                TestSimple(container, repository, "X+", "X1");
                TestSimple(container, repository, "AB007", "AB007");
                TestSimple(container, repository, "AB+", "AB008");
                TestSimple(container, repository, "AB999", "AB999");
                TestSimple(container, repository, "AB+", "AB1000");
            }
        }

        [TestMethod]
        public void SimpleFromHelp()
        {
            using (var container = new RhetosTestContainer())
            {
                DeleteOldData(container);
                var repository = container.Resolve<Common.DomRepository>();

                TestSimple(container, repository, "ab+", "ab1");
                TestSimple(container, repository, "ab+", "ab2");
                TestSimple(container, repository, "ab+", "ab3");
                TestSimple(container, repository, "ab++++", "ab0004");
                TestSimple(container, repository, "c+", "c1");
                TestSimple(container, repository, "+", "1");
                TestSimple(container, repository, "+", "2");
                TestSimple(container, repository, "ab+", "ab0005");
            }
        }

        [TestMethod]
        public void DoubleAutoCode()
        {
            using (var container = new RhetosTestContainer())
            {
                DeleteOldData(container);
                var repository = container.Resolve<Common.DomRepository>();

                TestDoubleAutoCode(container, repository, "+", "+", "1,1");
                TestDoubleAutoCode(container, repository, "+", "4", "2,4");
                TestDoubleAutoCode(container, repository, "+", "+", "3,5");
                TestDoubleAutoCode(container, repository, "9", "+", "9,6");
                TestDoubleAutoCode(container, repository, "+", "11", "10,11");
                TestDoubleAutoCode(container, repository, "+", "+", "11,12");
                TestDoubleAutoCode(container, repository, "AB+", "+", "AB1,13");
                TestDoubleAutoCode(container, repository, "AB+", "X", "AB2,X");
                TestDoubleAutoCode(container, repository, "AB+", "X+", "AB3,X1");
                TestDoubleAutoCode(container, repository, "AB008", "X+", "AB008,X2");
                TestDoubleAutoCode(container, repository, "AB+", "+", "AB009,14");
                TestDoubleAutoCode(container, repository, "+", "AB9999", "12,AB9999");
                TestDoubleAutoCode(container, repository, "AB+", "AB+", "AB010,AB10000");
            }
        }

        [TestMethod]
        public void IntAutoCode()
        {
            using (var container = new RhetosTestContainer())
            {
                DeleteOldData(container);
                var repository = container.Resolve<Common.DomRepository>();

                TestIntAutoCode(container, repository, 0, 1);
                TestIntAutoCode(container, repository, 10, 10);
                TestIntAutoCode(container, repository, 0, 11);
                TestIntAutoCode(container, repository, 99, 99);
                TestIntAutoCode(container, repository, 0, 100);
                TestIntAutoCode(container, repository, 0, 101);
                TestIntAutoCode(container, repository, 0, 102);
            }
        }

        [TestMethod]
        public void IntAutoCodeNull()
        {
            using (var container = new RhetosTestContainer())
            {
                DeleteOldData(container);
                var repository = container.Resolve<Common.DomRepository>();

                TestIntAutoCode(container, repository, null, 1);
                TestIntAutoCode(container, repository, null, 2);
            }
        }

        [TestMethod]
        public void DoubleAutoCodeWithGroup()
        {
            using (var container = new RhetosTestContainer())
            {
                DeleteOldData(container);
                var repository = container.Resolve<Common.DomRepository>();

                TestDoubleAutoCodeWithGroup(container, repository, "1", "+", "+", "1,1");
                TestDoubleAutoCodeWithGroup(container, repository, "1", "+", "4", "2,4");
                TestDoubleAutoCodeWithGroup(container, repository, "2", "+", "+", "3,1");
                TestDoubleAutoCodeWithGroup(container, repository, "1", "9", "+", "9,5");
                TestDoubleAutoCodeWithGroup(container, repository, "2", "+", "11", "10,11");
                TestDoubleAutoCodeWithGroup(container, repository, "1", "+", "+", "11,6");
                TestDoubleAutoCodeWithGroup(container, repository, "1", "AB+", "+", "AB1,7");
                TestDoubleAutoCodeWithGroup(container, repository, "1", "AB+", "X", "AB2,X");
                TestDoubleAutoCodeWithGroup(container, repository, "2", "AB+", "X09", "AB3,X09");
                TestDoubleAutoCodeWithGroup(container, repository, "2", "AB+", "X+", "AB4,X10");
                TestDoubleAutoCodeWithGroup(container, repository, "1", "AB+", "X+", "AB5,X1");
                TestDoubleAutoCodeWithGroup(container, repository, "1", "AB008", "X+", "AB008,X2");
                TestDoubleAutoCodeWithGroup(container, repository, "1", "AB+", "+", "AB009,8");
                TestDoubleAutoCodeWithGroup(container, repository, "1", "+", "AB9999", "12,AB9999");
                TestDoubleAutoCodeWithGroup(container, repository, "1", "AB+", "AB+", "AB010,AB10000");
            }
        }

        [TestMethod]
        public void DoubleIntegerAutoCodeWithGroup()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                repository.TestAutoCode.IntegerAutoCodeForEach.Delete(repository.TestAutoCode.IntegerAutoCodeForEach.Query());

                TestDoubleIntegerAutoCodeWithGroup(container, repository, 1, 0, 0, "1,1");
                TestDoubleIntegerAutoCodeWithGroup(container, repository, 1, 5, 0, "5,2");
                TestDoubleIntegerAutoCodeWithGroup(container, repository, 1, 0, 0, "6,3");
                TestDoubleIntegerAutoCodeWithGroup(container, repository, 2, 8, 0, "8,1");
                TestDoubleIntegerAutoCodeWithGroup(container, repository, 2, 0, 0, "9,2");
                TestDoubleIntegerAutoCodeWithGroup(container, repository, 1, 0, 0, "10,4");
            }
        }

        [TestMethod]
        public void DoubleIntegerAutoCodeWithGroupNull()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                repository.TestAutoCode.IntegerAutoCodeForEach.Delete(repository.TestAutoCode.IntegerAutoCodeForEach.Query());

                TestDoubleIntegerAutoCodeWithGroup(container, repository, 1, null, null, "1,1");
                TestDoubleIntegerAutoCodeWithGroup(container, repository, 1, 5, null, "5,2");
                TestDoubleIntegerAutoCodeWithGroup(container, repository, 1, null, null, "6,3");
                TestDoubleIntegerAutoCodeWithGroup(container, repository, 2, 8, null, "8,1");
                TestDoubleIntegerAutoCodeWithGroup(container, repository, 2, null, null, "9,2");
                TestDoubleIntegerAutoCodeWithGroup(container, repository, 1, null, null, "10,4");
            }
        }

        private static void TestGroup<TEntity, TGroup>(
            RhetosTestContainer container, IQueryableRepository<IEntity> entityRepository,
            TGroup group, string format, string expectedCode)
                where TEntity : class, IEntity, new()
        {
            var writeableRepository = (IWritableRepository<TEntity>) entityRepository;

            Guid id = Guid.NewGuid();
            dynamic entity = new TEntity();
            entity.ID = id;
            entity.Code = format;
            try
            {
                entity.Grouping = group;
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
            {
                entity.GroupingID = ((dynamic)group).ID;
            }
            
            writeableRepository.Insert((TEntity)entity);

            container.Resolve<Common.ExecutionContext>().EntityFrameworkContext.ClearCache();

            var query = entityRepository.Query().Where(e => e.ID == id);
            Console.WriteLine(query.GetType().FullName);
            Console.WriteLine(query.Expression.ToString());
            Console.WriteLine(query.ToString());
            
            dynamic loaded = query.Single();
            string generatedCode = loaded.Code;

            Console.WriteLine(format + " => " + generatedCode);
            Assert.AreEqual(expectedCode, generatedCode);
        }

        [TestMethod]
        public void Grouping()
        {
            using (var container = new RhetosTestContainer())
            {
                DeleteOldData(container);
                var repository = container.Resolve<Common.DomRepository>();

                TestGroup<TestAutoCode.IntGroup, int>(container, repository.TestAutoCode.IntGroup, 500, "+", "1");
                TestGroup<TestAutoCode.IntGroup, int>(container, repository.TestAutoCode.IntGroup, 500, "+", "2");
                TestGroup<TestAutoCode.IntGroup, int>(container, repository.TestAutoCode.IntGroup, 600, "+", "1");
                TestGroup<TestAutoCode.IntGroup, int>(container, repository.TestAutoCode.IntGroup, 600, "A+", "A1");

                TestGroup<TestAutoCode.StringGroup, string>(container, repository.TestAutoCode.StringGroup, "x", "+", "1");
                TestGroup<TestAutoCode.StringGroup, string>(container, repository.TestAutoCode.StringGroup, "x", "+", "2");
                TestGroup<TestAutoCode.StringGroup, string>(container, repository.TestAutoCode.StringGroup, "y", "+", "1");
                TestGroup<TestAutoCode.StringGroup, string>(container, repository.TestAutoCode.StringGroup, "y", "A+", "A1");

                var simple1 = new TestAutoCode.Simple { ID = Guid.NewGuid(), Code = "1" };
                var simple2 = new TestAutoCode.Simple { ID = Guid.NewGuid(), Code = "2" };
                repository.TestAutoCode.Simple.Insert(new[] { simple1, simple2 });

                TestGroup<TestAutoCode.ReferenceGroup, TestAutoCode.Simple>(container, repository.TestAutoCode.ReferenceGroup, simple1, "+", "1");
                TestGroup<TestAutoCode.ReferenceGroup, TestAutoCode.Simple>(container, repository.TestAutoCode.ReferenceGroup, simple1, "+", "2");
                TestGroup<TestAutoCode.ReferenceGroup, TestAutoCode.Simple>(container, repository.TestAutoCode.ReferenceGroup, simple2, "+", "1");
                TestGroup<TestAutoCode.ReferenceGroup, TestAutoCode.Simple>(container, repository.TestAutoCode.ReferenceGroup, simple2, "A+", "A1");

                var grouping1 = new TestAutoCode.Grouping { ID = Guid.NewGuid(), Code = "1" };
                var grouping2 = new TestAutoCode.Grouping { ID = Guid.NewGuid(), Code = "2" };
                repository.TestAutoCode.Grouping.Insert(new[] { grouping1, grouping2 });

                TestGroup<TestAutoCode.ShortReferenceGroup, TestAutoCode.Grouping>(container, repository.TestAutoCode.ShortReferenceGroup, grouping1, "+", "1");
                TestGroup<TestAutoCode.ShortReferenceGroup, TestAutoCode.Grouping>(container, repository.TestAutoCode.ShortReferenceGroup, grouping1, "+", "2");
                TestGroup<TestAutoCode.ShortReferenceGroup, TestAutoCode.Grouping>(container, repository.TestAutoCode.ShortReferenceGroup, grouping2, "+", "1");
                TestGroup<TestAutoCode.ShortReferenceGroup, TestAutoCode.Grouping>(container, repository.TestAutoCode.ShortReferenceGroup, grouping2, "A+", "A1");
            }
        }

        [TestMethod]
        public void SqlTriggerHandlesNullValue()
        {
            using (var container = new RhetosTestContainer())
            {
                DeleteOldData(container);

                Guid id = Guid.NewGuid();
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "INSERT INTO TestAutoCode.Simple (ID, Code) VALUES ('" + id + "', NULL)" });

                var repository = container.Resolve<Common.DomRepository>();
                var loaded = repository.TestAutoCode.Simple.Load(item => item.ID == id).Single();
                Assert.AreEqual("1", loaded.Code);
            }
        }

        [TestMethod]
        public void AutocodeStringNull()
        {
            using (var container = new RhetosTestContainer())
            {
                DeleteOldData(container);

                var repository = container.Resolve<Common.DomRepository>();
                var item1 = new TestAutoCode.Simple { ID = Guid.NewGuid() };
                var item2 = new TestAutoCode.Simple { ID = Guid.NewGuid() };
                repository.TestAutoCode.Simple.Insert(item1);
                repository.TestAutoCode.Simple.Insert(item2);
                Assert.AreEqual("1", repository.TestAutoCode.Simple.Load(new[] { item1.ID }).Single().Code);
                Assert.AreEqual("2", repository.TestAutoCode.Simple.Load(new[] { item2.ID }).Single().Code);
            }
        }

        [TestMethod]
        public void AutocodeStringEmpty()
        {
            using (var container = new RhetosTestContainer())
            {
                DeleteOldData(container);

                var repository = container.Resolve<Common.DomRepository>();
                var item1 = new TestAutoCode.Simple { ID = Guid.NewGuid(), Code = "" };
                repository.TestAutoCode.Simple.Insert(item1);
                Assert.AreEqual("", repository.TestAutoCode.Simple.Load(new[] { item1.ID }).Single().Code);
            }
        }

        [TestMethod]
        public void SimpleWithPredefinedSuffixLength()
        {
            using (var container = new RhetosTestContainer())
            {
                DeleteOldData(container);
                var repository = container.Resolve<Common.DomRepository>();

                TestSimple(container, repository, "+", "1");
                TestSimple(container, repository, "+", "2");
                TestSimple(container, repository, "++++", "0003");
                TestSimple(container, repository, "+", "0004");
                TestSimple(container, repository, "++", "0005");
                TestSimple(container, repository, "AB+", "AB1");
                TestSimple(container, repository, "X", "X");
                TestSimple(container, repository, "X+", "X1");
                TestSimple(container, repository, "AB007", "AB007");
                TestSimple(container, repository, "AB+", "AB008");
                TestSimple(container, repository, "AB999", "AB999");
                TestSimple(container, repository, "AB+", "AB1000");
                TestSimple(container, repository, "AB++++++", "AB001001");
            }
        }

        [TestMethod]
        public void SimpleWithPredefinedSuffixLength2()
        {
            using (var container = new RhetosTestContainer())
            {
                DeleteOldData(container);
                var repository = container.Resolve<Common.DomRepository>();

                TestSimple(container, repository, "+++", "001");
                TestSimple(container, repository, "+", "002");

                TestSimple(container, repository, "AB99", "AB99");
                TestSimple(container, repository, "AB++", "AB100");

                TestSimple(container, repository, "AB999", "AB999");
                TestSimple(container, repository, "AB++++++", "AB001000");

                TestSimple(container, repository, "B999", "B999");
                TestSimple(container, repository, "B++", "B1000");

                TestSimple(container, repository, "C500", "C500");
                TestSimple(container, repository, "C++", "C501");
            }
        }

        [TestMethod]
        public void DifferentLengths()
        {
            using (var container = new RhetosTestContainer())
            {
                DeleteOldData(container);
                var repository = container.Resolve<Common.DomRepository>();

                TestSimple(container, repository, "002", "002");
                TestSimple(container, repository, "55", "55");
                TestSimple(container, repository, "+", "56");

                TestSimple(container, repository, "A002", "A002");
                TestSimple(container, repository, "A55", "A55");
                TestSimple(container, repository, "A++", "A56");

                TestSimple(container, repository, "C100", "C100");
                TestSimple(container, repository, "C99", "C99");
                TestSimple(container, repository, "C+", "C101");
            }
        }

        [TestMethod]
        public void InvalidFormat()
        {
            foreach (var test in new[] {"a+a", "a++a", "+a", "++a", "+a+", "++a+", "+a++", "++a++"})
            {
                Console.WriteLine("Test: " + test);
                using (var container = new RhetosTestContainer())
                {
                    DeleteOldData(container);
                    var repository = container.Resolve<Common.DomRepository>();

                    TestUtility.ShouldFail(
                        () => repository.TestAutoCode.Simple.Insert(new[] {
                            new TestAutoCode.Simple { ID = Guid.NewGuid(), Code = test } }),
                        "invalid code");
                }
            }
        }

        [TestMethod]
        public void ParallelInserts()
        {
            using (var container = new RhetosTestContainer(true))
            {
                var sqlExecuter = container.Resolve<ISqlExecuter>();
                sqlExecuter.ExecuteSql(new[] { "DELETE FROM TestAutoCode.Simple" });
            }

            using (var container = new RhetosTestContainer())
            {
                var sqlExecuter = container.Resolve<ISqlExecuter>();
                var sqlInsert = "INSERT INTO TestAutoCode.Simple (ID, Code) SELECT '{0}', '+'";
                var insertedIds = new List<Guid>();

                for (int test = 1; test <= 50; test++)
                {
                    Console.WriteLine("Test: " + test);

                    string[] sqlQueries = Enumerable.Range(0, 2).Select(x =>
                    {
                        var id = Guid.NewGuid();
                        insertedIds.Add(id);
                        return string.Format(sqlInsert, id);
                    }).ToArray();

                    Parallel.For(0, 2, process => sqlExecuter.ExecuteSql(new[] { sqlQueries[process] }, useTransaction: false));
                }

                var simpleRepository = container.Resolve<GenericRepository<TestAutoCode.Simple>>();

                var generatedCodes = simpleRepository.Load(insertedIds).Select(item => Int32.Parse(item.Code));
                Console.WriteLine("generatedCodes: " + string.Join(", ", generatedCodes.Select(x => x.ToString())));

                Assert.AreEqual(insertedIds.Count(), generatedCodes.Count());
                Assert.AreEqual(insertedIds.Count(), generatedCodes.Max() - generatedCodes.Min() + 1);
            }
        }

        [TestMethod]
        public void ParallelInsertsLockErrorHandling()
        {
            const int testProcessCount = 4;

            using (var container = new RhetosTestContainer(true))
            {
                var sqlExecuter = container.Resolve<ISqlExecuter>();
                sqlExecuter.ExecuteSql(new[] { "DELETE FROM TestAutoCode.Simple", "INSERT INTO TestAutoCode.Simple (Code) SELECT '1'" });
            }

            using (RhetosTestContainer container0 = new RhetosTestContainer(),
                container1 = new RhetosTestContainer(),
                container2 = new RhetosTestContainer(),
                container3 = new RhetosTestContainer())
            {
                // Starts at 0 ms, ends at 400ms.
                var sql0 = new[] { "WAITFOR DELAY '00:00:00.000'", "SET LOCK_TIMEOUT 0", "INSERT INTO TestAutoCode.Simple (Code) SELECT '+'", "WAITFOR DELAY '00:00:00.400'" };

                // Starts at 100 ms, lock timeout at 300ms.
                var sql1 = new[] { "WAITFOR DELAY '00:00:00.100'", "SET LOCK_TIMEOUT 200", "INSERT INTO TestAutoCode.Simple (Code) SELECT '+'" };

                // Starts at 200 ms, lock timeout at 200ms.
                var sql2 = new[] { "WAITFOR DELAY '00:00:00.200'", "SET LOCK_TIMEOUT 0", "INSERT INTO TestAutoCode.Simple (Code) SELECT '+'" };

                // Starts at 200 ms, ends at 200ms.
                var sql3 = new[] { "WAITFOR DELAY '00:00:00.200'", "SET LOCK_TIMEOUT 0", "SELECT * FROM TestAutoCode.Simple WHERE Code = '1'" };

                var sqls = new [] { sql0, sql1, sql2, sql3 };
                Assert.AreEqual(testProcessCount, sqls.Count());

                var containers = new[] { container0, container1, container2, container3 };
                var sqlExecuters = containers.Select(c => c.Resolve<ISqlExecuter>()).ToArray();
                CheckForParallelism(sqlExecuters);

                Exception[] exceptions = new Exception[testProcessCount];

                Parallel.For(0, testProcessCount, process =>
                    {
                        try
                        {
                            sqlExecuters[process].ExecuteSql(sqls[process]);
                        }
                        catch (Exception ex)
                        {
                            exceptions[process] = ex;
                        }
                    });

                for (int x = 0; x < testProcessCount; x++)
                    Console.WriteLine("Exception " + x + ": " + exceptions[x] + ".");

                Assert.IsNull(exceptions[0]);
                Assert.IsNotNull(exceptions[1]);
                Assert.IsNotNull(exceptions[2]);
                Assert.IsNull(exceptions[3]); // sql3 should be allowed to read the record with code '1'. sql0 has exclusive lock on code '2'. autocode should not put exclusive lock on other records.

                // Query sql1 may generate next autocode, but it should wait for the entity's table exclusive lock to be released (from sql0).
                Assert.IsTrue(
                    exceptions[1].ToString().Contains("lock request time out")
                    || exceptions[1].ToString().Contains("another user's insert command is still running"), // When READ_COMMITTED_SNAPSHOT is ON.
                    "See 'Exception 1' in output log.");

                // Query sql2 may not generate next autocode until sql1 releases the lock.
                TestUtility.AssertContains(exceptions[2].ToString(), new[] { "Cannot insert", "TestAutoCode.Simple", "another user" });
            }
        }

        private void CheckForParallelism(ISqlExecuter[] sqlExecuters)
        {
 	        string sqlDelay01 = "WAITFOR DELAY '00:00:00.100'";
            var sqls = new[] { sqlDelay01 };
            // Cold start.
            Parallel.ForEach(sqlExecuters, sqlExecuter => { sqlExecuter.ExecuteSql(sqls); });

            var sw = Stopwatch.StartNew();
            Parallel.ForEach(sqlExecuters, sqlExecuter => { sqlExecuter.ExecuteSql(sqls); });
            sw.Stop();

            Console.WriteLine("CheckForParallelism: " + sw.ElapsedMilliseconds + " ms.");

            if (sw.ElapsedMilliseconds < 50)
                Assert.Fail("Delay is unexpectedly short: " + sw.ElapsedMilliseconds);

            if (sw.Elapsed.TotalMilliseconds > 190)
                Assert.Inconclusive(string.Format(
                    "This test requires {0} parallel SQL queries. {0} parallel delays for 100 ms are executed in {1} ms.",
                    sqlExecuters.Count(),
                    sw.ElapsedMilliseconds));
        }
    }
}
