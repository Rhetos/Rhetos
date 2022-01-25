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

using CommonConcepts.Test.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos;
using Rhetos.Configuration.Autofac;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CommonConcepts.Test
{
    [TestClass]
    public class AutoCodeCachedTest
    {
        private static void DeleteOldData(UnitOfWorkScope scope)
        {
            scope.Resolve<ISqlExecuter>().ExecuteSql(new[]
                {
                    @"DELETE FROM Common.AutoCodeCache WHERE Entity LIKE 'TestAutoCodeCached.%';
                    DELETE FROM TestAutoCodeCached.ReferenceGroup;
                    DELETE FROM TestAutoCodeCached.ShortReferenceGroup;
                    DELETE FROM TestAutoCodeCached.Grouping;
                    DELETE FROM TestAutoCodeCached.StringGroup;
                    DELETE FROM TestAutoCodeCached.IntGroup;
                    DELETE FROM TestAutoCodeCached.Simple;
                    DELETE FROM TestAutoCodeCached.DoubleAutoCode;
                    DELETE FROM TestAutoCodeCached.DoubleAutoCodeWithGroup;"
                });
        }

        private static void TestSimple(Common.DomRepository repository, string format, string expectedCode)
        {
            Guid id = Guid.NewGuid();
            repository.TestAutoCodeCached.Simple.Insert(new[] { new TestAutoCodeCached.Simple { ID = id, Code = format } });

            string generatedCode = repository.TestAutoCodeCached.Simple.Query().Where(item => item.ID == id).Select(item => item.Code).Single();
            Console.WriteLine(format + " => " + generatedCode);
            Assert.AreEqual(expectedCode, generatedCode);
        }
        
        private static void TestDoubleAutoCode(Common.DomRepository repository, string formatA, string formatB, string expectedCodes)
        {
            Guid id = Guid.NewGuid();
            repository.TestAutoCodeCached.DoubleAutoCode.Insert(new[] { new TestAutoCodeCached.DoubleAutoCode { ID = id, CodeA = formatA, CodeB = formatB } });

            string generatedCodes = repository.TestAutoCodeCached.DoubleAutoCode.Query()
				.Where(item => item.ID == id)
				.Select(item => item.CodeA + "," + item.CodeB).Single();
            Console.WriteLine(formatA + "," + formatB + " => " + generatedCodes);
            Assert.AreEqual(expectedCodes, generatedCodes);
        }

        private static void TestDoubleAutoCodeWithGroup(Common.DomRepository repository, string group, string formatA, string formatB, string expectedCodes)
        {
            Guid id = Guid.NewGuid();
            repository.TestAutoCodeCached.DoubleAutoCodeWithGroup.Insert(new[] { new TestAutoCodeCached.DoubleAutoCodeWithGroup { ID = id, Grouping = group, CodeA = formatA, CodeB = formatB } });

            string generatedCodes = repository.TestAutoCodeCached.DoubleAutoCodeWithGroup.Query()
                .Where(item => item.ID == id)
                .Select(item => item.CodeA + "," + item.CodeB).Single();
            Console.WriteLine(formatA + "," + formatB + " => " + generatedCodes);
            Assert.AreEqual(expectedCodes, generatedCodes);
        }

        [TestMethod]
        public void Simple()
        {
            using (var scope = TestScope.Create())
            {
                DeleteOldData(scope);
                var repository = scope.Resolve<Common.DomRepository>();

                TestSimple(repository, "+", "1");
                TestSimple(repository, "+", "2");
                TestSimple(repository, "+", "3");
                TestSimple(repository, "9", "9");
                TestSimple(repository, "+", "10");
                TestSimple(repository, "+", "11");
                TestSimple(repository, "AB+", "AB1");
                TestSimple(repository, "X", "X");
                TestSimple(repository, "X+", "X1");
                TestSimple(repository, "AB007", "AB007");
                TestSimple(repository, "AB+", "AB008");
                TestSimple(repository, "AB999", "AB999");
                TestSimple(repository, "AB+", "AB1000");
            }
        }

        [TestMethod]
        public void InsertMultipleItems()
        {
            using (var scope = TestScope.Create())
            {
                DeleteOldData(scope);
                var repository = scope.Resolve<Common.DomRepository>();

                var tests = new ListOfTuples<string, string>
                {
                    { "+", "10" }, // Exactly specified values are considered before generated values, therefore this item is handled after core "9".
                    { "+", "11" },
                    { "+", "12" },
                    { "9", "9" },
                    { "+", "13" },
                    { "+", "14" },
                    { "AB+", "AB1000" },
                    { "X", "X" },
                    { "X+", "X1" },
                    { "AB007", "AB007" },
                    { "AB+", "AB1001" },
                    { "AB999", "AB999" },
                    { "AB+", "AB1002" },
                };

                repository.TestAutoCodeCached.Simple.Insert(
                    tests.Select((test, index) => new TestAutoCodeCached.Simple { ID = new Guid(index + 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0), Code = test.Item1 }));

                IEnumerable<string> generatedCodes = repository.TestAutoCodeCached.Simple.Load()
                    .OrderBy(item => item.ID)
                    .Select(item => item.Code);

                IEnumerable<string> expectedCodes = tests.Select(test => test.Item2);

                Assert.AreEqual(TestUtility.Dump(expectedCodes), TestUtility.Dump(generatedCodes));
            }
        }

        [TestMethod]
        public void SimpleFromHelp()
        {
            using (var scope = TestScope.Create())
            {
                DeleteOldData(scope);
                var repository = scope.Resolve<Common.DomRepository>();

                TestSimple(repository, "ab+", "ab1");
                TestSimple(repository, "ab+", "ab2");
                TestSimple(repository, "ab+", "ab3");
                TestSimple(repository, "ab++++", "ab0004");
                TestSimple(repository, "c+", "c1");
                TestSimple(repository, "+", "1");
                TestSimple(repository, "+", "2");
                TestSimple(repository, "ab+", "ab0005");
            }
        }

        [TestMethod]
        public void DoubleAutoCode()
        {
            using (var scope = TestScope.Create())
            {
                DeleteOldData(scope);
                var repository = scope.Resolve<Common.DomRepository>();

                TestDoubleAutoCode(repository, "+", "+", "1,1");
                TestDoubleAutoCode(repository, "+", "4", "2,4");
                TestDoubleAutoCode(repository, "+", "+", "3,5");
                TestDoubleAutoCode(repository, "9", "+", "9,6");
                TestDoubleAutoCode(repository, "+", "11", "10,11");
                TestDoubleAutoCode(repository, "+", "+", "11,12");
                TestDoubleAutoCode(repository, "AB+", "+", "AB1,13");
                TestDoubleAutoCode(repository, "AB+", "X", "AB2,X");
                TestDoubleAutoCode(repository, "AB+", "X+", "AB3,X1");
                TestDoubleAutoCode(repository, "AB008", "X+", "AB008,X2");
                TestDoubleAutoCode(repository, "AB+", "+", "AB009,14");
                TestDoubleAutoCode(repository, "+", "AB9999", "12,AB9999");
                TestDoubleAutoCode(repository, "AB+", "AB+", "AB010,AB10000");
            }
        }

        [TestMethod]
        public void DoubleAutoCodeWithGroup()
        {
            using (var scope = TestScope.Create())
            {
                DeleteOldData(scope);
                var repository = scope.Resolve<Common.DomRepository>();

                TestDoubleAutoCodeWithGroup(repository, "1", "+", "+", "1,1");
                TestDoubleAutoCodeWithGroup(repository, "1", "+", "4", "2,4");
                TestDoubleAutoCodeWithGroup(repository, "2", "+", "+", "3,1");
                TestDoubleAutoCodeWithGroup(repository, "1", "9", "+", "9,5");
                TestDoubleAutoCodeWithGroup(repository, "2", "+", "11", "10,11");
                TestDoubleAutoCodeWithGroup(repository, "1", "+", "+", "11,6");
                TestDoubleAutoCodeWithGroup(repository, "1", "AB+", "+", "AB1,7");
                TestDoubleAutoCodeWithGroup(repository, "1", "AB+", "X", "AB2,X");
                TestDoubleAutoCodeWithGroup(repository, "2", "AB+", "X09", "AB3,X09");
                TestDoubleAutoCodeWithGroup(repository, "2", "AB+", "X+", "AB4,X10");
                TestDoubleAutoCodeWithGroup(repository, "1", "AB+", "X+", "AB5,X1");
                TestDoubleAutoCodeWithGroup(repository, "1", "AB008", "X+", "AB008,X2");
                TestDoubleAutoCodeWithGroup(repository, "1", "AB+", "+", "AB009,8");
                TestDoubleAutoCodeWithGroup(repository, "1", "+", "AB9999", "12,AB9999");
                TestDoubleAutoCodeWithGroup(repository, "1", "AB+", "AB+", "AB010,AB10000");
            }
        }

        private static void TestGroup<TEntity, TGroup>(
            IQueryableRepository<IEntity> entityRepository,
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
            using (var scope = TestScope.Create())
            {
                DeleteOldData(scope);
                var repository = scope.Resolve<Common.DomRepository>();

                TestGroup<TestAutoCodeCached.IntGroup, int>(repository.TestAutoCodeCached.IntGroup, 500, "+", "1");
                TestGroup<TestAutoCodeCached.IntGroup, int>(repository.TestAutoCodeCached.IntGroup, 500, "+", "2");
                TestGroup<TestAutoCodeCached.IntGroup, int>(repository.TestAutoCodeCached.IntGroup, 600, "+", "1");
                TestGroup<TestAutoCodeCached.IntGroup, int>(repository.TestAutoCodeCached.IntGroup, 600, "A+", "A1");

                TestGroup<TestAutoCodeCached.StringGroup, string>(repository.TestAutoCodeCached.StringGroup, "x", "+", "1");
                TestGroup<TestAutoCodeCached.StringGroup, string>(repository.TestAutoCodeCached.StringGroup, "x", "+", "2");
                TestGroup<TestAutoCodeCached.StringGroup, string>(repository.TestAutoCodeCached.StringGroup, "y", "+", "1");
                TestGroup<TestAutoCodeCached.StringGroup, string>(repository.TestAutoCodeCached.StringGroup, "y", "A+", "A1");

                var simple1 = new TestAutoCodeCached.Simple { ID = Guid.NewGuid(), Code = "1" };
                var simple2 = new TestAutoCodeCached.Simple { ID = Guid.NewGuid(), Code = "2" };
                repository.TestAutoCodeCached.Simple.Insert(new[] { simple1, simple2 });

                TestGroup<TestAutoCodeCached.ReferenceGroup, TestAutoCodeCached.Simple>(repository.TestAutoCodeCached.ReferenceGroup, simple1, "+", "1");
                TestGroup<TestAutoCodeCached.ReferenceGroup, TestAutoCodeCached.Simple>(repository.TestAutoCodeCached.ReferenceGroup, simple1, "+", "2");
                TestGroup<TestAutoCodeCached.ReferenceGroup, TestAutoCodeCached.Simple>(repository.TestAutoCodeCached.ReferenceGroup, simple2, "+", "1");
                TestGroup<TestAutoCodeCached.ReferenceGroup, TestAutoCodeCached.Simple>(repository.TestAutoCodeCached.ReferenceGroup, simple2, "A+", "A1");

                var grouping1 = new TestAutoCodeCached.Grouping { ID = Guid.NewGuid(), Code = "1" };
                var grouping2 = new TestAutoCodeCached.Grouping { ID = Guid.NewGuid(), Code = "2" };
                repository.TestAutoCodeCached.Grouping.Insert(new[] { grouping1, grouping2 });

                TestGroup<TestAutoCodeCached.ShortReferenceGroup, TestAutoCodeCached.Grouping>(repository.TestAutoCodeCached.ShortReferenceGroup, grouping1, "+", "1");
                TestGroup<TestAutoCodeCached.ShortReferenceGroup, TestAutoCodeCached.Grouping>(repository.TestAutoCodeCached.ShortReferenceGroup, grouping1, "+", "2");
                TestGroup<TestAutoCodeCached.ShortReferenceGroup, TestAutoCodeCached.Grouping>(repository.TestAutoCodeCached.ShortReferenceGroup, grouping2, "+", "1");
                TestGroup<TestAutoCodeCached.ShortReferenceGroup, TestAutoCodeCached.Grouping>(repository.TestAutoCodeCached.ShortReferenceGroup, grouping2, "A+", "A1");
            }
        }

        [TestMethod]
        public void AllowedNullValueInternally()
        {
            using (var scope = TestScope.Create())
            {
                DeleteOldData(scope);

                var context = scope.Resolve<Common.ExecutionContext>();

                var s1 = new TestAutoCodeCached.Simple { ID = Guid.NewGuid(), Code = null };

                AutoCodeHelper.UpdateCodesWithCache(
                    context.SqlExecuter, "TestAutoCodeCached.Simple", "Code",
                    new[] { AutoCodeItem.Create(s1, s1.Code) },
                    (item, newCode) => item.Code = newCode);

                Assert.AreEqual("1", s1.Code);
            }
        }

        [TestMethod]
        public void AutocodeStringNull()
        {
            using (var scope = TestScope.Create())
            {
                DeleteOldData(scope);

                var repository = scope.Resolve<Common.DomRepository>();
                var item1 = new TestAutoCodeCached.Simple { ID = Guid.NewGuid() };
                var item2 = new TestAutoCodeCached.Simple { ID = Guid.NewGuid() };
                repository.TestAutoCodeCached.Simple.Insert(item1);
                repository.TestAutoCodeCached.Simple.Insert(item2);
                Assert.AreEqual("1", repository.TestAutoCodeCached.Simple.Load(new[] { item1.ID }).Single().Code);
                Assert.AreEqual("2", repository.TestAutoCodeCached.Simple.Load(new[] { item2.ID }).Single().Code);
            }
        }

        [TestMethod]
        public void AutocodeStringEmpty()
        {
            using (var scope = TestScope.Create())
            {
                DeleteOldData(scope);

                var repository = scope.Resolve<Common.DomRepository>();
                var item1 = new TestAutoCodeCached.Simple { ID = Guid.NewGuid(), Code = "" };
                repository.TestAutoCodeCached.Simple.Insert(item1);
                Assert.AreEqual("", repository.TestAutoCodeCached.Simple.Load(new[] { item1.ID }).Single().Code);
            }
        }

        [TestMethod]
        public void SimpleWithPredefinedSuffixLength()
        {
            using (var scope = TestScope.Create())
            {
                DeleteOldData(scope);
                var repository = scope.Resolve<Common.DomRepository>();

                TestSimple(repository, "+", "1");
                TestSimple(repository, "+", "2");
                TestSimple(repository, "++++", "0003");
                TestSimple(repository, "+", "0004");
                TestSimple(repository, "++", "0005");
                TestSimple(repository, "AB+", "AB1");
                TestSimple(repository, "X", "X");
                TestSimple(repository, "X+", "X1");
                TestSimple(repository, "AB007", "AB007");
                TestSimple(repository, "AB+", "AB008");
                TestSimple(repository, "AB999", "AB999");
                TestSimple(repository, "AB+", "AB1000");
                TestSimple(repository, "AB++++++", "AB001001");
            }
        }

        [TestMethod]
        public void SimpleWithPredefinedSuffixLength2()
        {
            using (var scope = TestScope.Create())
            {
                DeleteOldData(scope);
                var repository = scope.Resolve<Common.DomRepository>();

                TestSimple(repository, "+++", "001");
                TestSimple(repository, "+", "002");

                TestSimple(repository, "AB99", "AB99");
                TestSimple(repository, "AB++", "AB100");

                TestSimple(repository, "AB999", "AB999");
                TestSimple(repository, "AB++++++", "AB001000");

                TestSimple(repository, "B999", "B999");
                TestSimple(repository, "B++", "B1000");

                TestSimple(repository, "C500", "C500");
                TestSimple(repository, "C++", "C501");
            }
        }

        [TestMethod]
        public void DifferentLengths()
        {
            using (var scope = TestScope.Create())
            {
                DeleteOldData(scope);
                var repository = scope.Resolve<Common.DomRepository>();

                TestSimple(repository, "002", "002");
                TestSimple(repository, "55", "55");
                TestSimple(repository, "+", "56");

                TestSimple(repository, "A002", "A002");
                TestSimple(repository, "A55", "A55");
                TestSimple(repository, "A++", "A56");

                TestSimple(repository, "C100", "C100");
                TestSimple(repository, "C99", "C99");
                TestSimple(repository, "C+", "C101");
            }
        }

        [TestMethod]
        public void InvalidFormat()
        {
            foreach (var test in new[] {"a+a", "a++a", "+a", "++a", "+a+", "++a+", "+a++", "++a++"})
            {
                Console.WriteLine("Test: " + test);
                using (var scope = TestScope.Create())
                {
                    DeleteOldData(scope);
                    var repository = scope.Resolve<Common.DomRepository>();

                    TestUtility.ShouldFail(
                        () => repository.TestAutoCodeCached.Simple.Insert(new[] {
                            new TestAutoCodeCached.Simple { ID = Guid.NewGuid(), Code = test } }),
                        "invalid code");
                }
            }
        }

        [TestMethod]
        public void ParallelInsertsSmokeTestSamePrefix()
        {
            // Each thread inserts 10*2 records with an empty AutoCode cache:
            for (int i = 0; i < 10; i++)
                Execute2ParallelInserts(1, (process, repository) =>
                {
                    repository.Insert(new[] { new TestAutoCodeCached.Simple { Code = "+", Data = process.ToString() } });
                    repository.Insert(new[] { new TestAutoCodeCached.Simple { Code = "+", Data = process.ToString() } });
                });

            // Each thread inserts 50*2 records, reusing the existing AutoCode cache:
            Execute2ParallelInserts(50, (process, repository) =>
            {
                repository.Insert(new[] { new TestAutoCodeCached.Simple { Code = "+", Data = process.ToString() } });
                repository.Insert(new[] { new TestAutoCodeCached.Simple { Code = "+", Data = process.ToString() } });
            });

            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>().TestAutoCodeCached.Simple;
                var generatedCodes = repository.Query().Select(item => item.Code).ToList();
                var expected = Enumerable.Range(1, 50 * 2 * 2);
                Assert.AreEqual(TestUtility.DumpSorted(expected), TestUtility.DumpSorted(generatedCodes));
            }
        }

        [TestMethod]
        public void ParallelInsertsSmokeTestDifferentPrefix()
        {
            // Each thread inserts 10*2 records with an empty AutoCode cache:
            for (int i = 0; i < 10; i++)
                Execute2ParallelInserts(1, (process, repository) =>
                {
                    repository.Insert(new[] { new TestAutoCodeCached.Simple { Code = (char)('a' + process) + "+", Data = process.ToString() } });
                    repository.Insert(new[] { new TestAutoCodeCached.Simple { Code = (char)('a' + process) + "+", Data = process.ToString() } });
                });

            // Each thread inserts 50*2 records, reusing the existing AutoCode cache:
            Execute2ParallelInserts(50, (process, repository) =>
            {
                repository.Insert(new[] { new TestAutoCodeCached.Simple { Code = (char)('a' + process) + "+", Data = process.ToString() } });
                repository.Insert(new[] { new TestAutoCodeCached.Simple { Code = (char)('a' + process) + "+", Data = process.ToString() } });
            });

            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>().TestAutoCodeCached.Simple;
                var generatedCodes = repository.Query().Select(item => item.Code).ToList();
                var expected = Enumerable.Range(1, 50 * 2).SelectMany(x => new[] { "a" + x, "b" + x });
                Assert.AreEqual(TestUtility.DumpSorted(expected), TestUtility.DumpSorted(generatedCodes));
            }
        }

        [TestMethod]
        public void ParallelInsertsLockingTestSamePrefix()
        {
            // One process must wait for another, since they use the same code prefix and code group:

            var endTimes = new DateTime[2];

            Execute2ParallelInserts(1, (process, repository) =>
            {
                repository.Insert(new[] { new TestAutoCodeCached.Simple { Code = "+", Data = process.ToString() } });
                System.Threading.Thread.Sleep(200);
                endTimes[process] = DateTime.Now;
            });

            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>().TestAutoCodeCached.Simple;
                var generatedCodes = repository.Query().Select(item => item.Code).ToList();
                var expected = Enumerable.Range(1, 2);
                Assert.AreEqual(TestUtility.DumpSorted(expected), TestUtility.DumpSorted(generatedCodes));

                var codeByProcessReport = TestUtility.Dump(repository.Query()
                    .OrderBy(item => item.Code)
                    .Take(4)
                    .Select(item => item.Data + ":" + item.Code));
                Assert.IsTrue(codeByProcessReport == "0:1, 1:2" || codeByProcessReport == "1:1, 0:2");

                TestUtility.DumpSorted(endTimes);
                var delay = Math.Abs(endTimes[0].Subtract(endTimes[1]).TotalMilliseconds);
                Console.WriteLine(delay);
                if (delay < 200)
                    Assert.Fail("One process did not wait for another.");
                if (delay > 300)
                    Assert.Inconclusive("System too slow. Delay should be a little above 200.");
            }
        }

        [TestMethod]
        public void ParallelInsertsLockingTestDifferentPrefix()
        {
            // Executing this test multiple time, to reduce system warm-up effects and performance instability.

            for (int retries = 4; retries >= 0; retries--)
            {
                try
                {
                    // One process may be executed in parallel with another, since they use different prefixes:

                    var endTimes = new DateTime[2];

                    Execute2ParallelInserts(1, (process, repository) =>
                    {
                        repository.Insert(new[] { new TestAutoCodeCached.Simple { Code = (char)('a' + process) + "+", Data = process.ToString() } });
                        System.Threading.Thread.Sleep(200);
                        endTimes[process] = DateTime.Now;
                    });

                    using (var scope = TestScope.Create())
                    {
                        var repository = scope.Resolve<Common.DomRepository>().TestAutoCodeCached.Simple;
                        var generatedCodes = repository.Query().Select(item => item.Code).ToList();
                        var expected = new[] { "a1", "b1" };
                        Assert.AreEqual(TestUtility.DumpSorted(expected), TestUtility.DumpSorted(generatedCodes));

                        var codeByProcessReport = TestUtility.Dump(repository.Query()
                            .OrderBy(item => item.Code)
                            .Take(4)
                            .Select(item => item.Data + ":" + item.Code));
                        Assert.AreEqual("0:a1, 1:b1", codeByProcessReport);

                        TestUtility.DumpSorted(endTimes, item => item.ToString("o"));
                        var delay = Math.Abs(endTimes[0].Subtract(endTimes[1]).TotalMilliseconds);
                        Console.WriteLine(delay);
                        if (delay > 200)
                            Assert.Fail("One process waited for another, or system too slow for this unit test.");
                        if (delay > 100)
                            Assert.Inconclusive("System too slow. Delay should be a little above 200.");
                    }
                }
                catch (Exception ex)
                {
                    if (retries > 0)
                    {
                        Console.WriteLine(ex);
                        Console.WriteLine("Retrying " + retries + " more times.");
                    }
                    else
                        throw;
                }
            }
        }

        private void Execute2ParallelInserts(int testCount, Action<int, TestAutoCodeCached.Repositories.Simple_Repository> action)
        {
            const int threadCount = 2;
            using (var scope = TestScope.Create())
            {
                DeleteOldData(scope);
                ConcurrencyUtility.CheckForParallelism(scope.Resolve<ISqlExecuter>(), threadCount);
                scope.CommitAndClose();
            }

            for (int test = 1; test <= testCount; test++)
            {
                Console.WriteLine("Test: " + test);

                var scopes = new[] { TestScope.Create(), TestScope.Create() };
                var repositories = scopes.Select(s => s.Resolve<Common.DomRepository>().TestAutoCodeCached.Simple).ToList();
                foreach (var r in repositories)
                    Assert.IsTrue(r.Query().Count() >= 0); // Cold start.

                try
                {
                    Parallel.For(0, threadCount, process =>
                    {
                        action(process, repositories[process]);
                        scopes[process].CommitAndClose();
                        scopes[process].Dispose();
                        scopes[process] = null;
                    });
                }
                finally
                {
                    foreach (var c in scopes)
                        if (c != null)
                            c.Dispose();
                }
            }
        }
    }
}
