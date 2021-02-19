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

using Autofac.Core;
using CommonConcepts.Test.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos;
using Rhetos.Configuration.Autofac;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CommonConcepts.Test
{
    [TestClass]
    public class AutoCodeTest
    {
        private static void DeleteOldData(TransactionScopeContainer scope)
        {
            scope.Resolve<ISqlExecuter>().ExecuteSql(new[]
                {
                    @"DELETE FROM TestAutoCode.ReferenceGroup;
                    DELETE FROM TestAutoCode.ShortReferenceGroup;
                    DELETE FROM TestAutoCode.Grouping;
                    DELETE FROM TestAutoCode.StringGroup;
                    DELETE FROM TestAutoCode.IntGroup;
                    DELETE FROM TestAutoCode.BoolGroup;
                    DELETE FROM TestAutoCode.Simple;
                    DELETE FROM TestAutoCode.DoubleAutoCode;
                    DELETE FROM TestAutoCode.DoubleAutoCodeWithGroup;
                    DELETE FROM TestAutoCode.IntegerAutoCode;
                    DELETE FROM TestAutoCode.MultipleGroups;"
                });
        }

        private static void TestSimple(Common.DomRepository repository, string format, string expectedCode)
        {
            Guid id = Guid.NewGuid();
            repository.TestAutoCode.Simple.Insert(new[] { new TestAutoCode.Simple { ID = id, Code = format } });

            string generatedCode = repository.TestAutoCode.Simple.Query().Where(item => item.ID == id).Select(item => item.Code).Single();
            Console.WriteLine(format + " => " + generatedCode);
            Assert.AreEqual(expectedCode, generatedCode);
        }

        private static void TestIntAutoCode(Common.DomRepository repository, int? input, int expectedCode)
        {
            Guid id = Guid.NewGuid();
            repository.TestAutoCode.IntegerAutoCode.Insert(new[] { new TestAutoCode.IntegerAutoCode { ID = id, Code = input } });

            int? generatedCode = repository.TestAutoCode.IntegerAutoCode.Query().Where(item => item.ID == id).Select(item => item.Code).Single();
            Console.WriteLine(input.ToString() + " => " + generatedCode.ToString());
            Assert.AreEqual(expectedCode, generatedCode);
        }

        private static void TestDoubleAutoCode(Common.DomRepository repository, string formatA, string formatB, string expectedCodes)
        {
            Guid id = Guid.NewGuid();
            repository.TestAutoCode.DoubleAutoCode.Insert(new[] { new TestAutoCode.DoubleAutoCode { ID = id, CodeA = formatA, CodeB = formatB } });

            string generatedCodes = repository.TestAutoCode.DoubleAutoCode.Query()
                .Where(item => item.ID == id)
                .Select(item => item.CodeA + "," + item.CodeB).Single();
            Console.WriteLine(formatA + "," + formatB + " => " + generatedCodes);
            Assert.AreEqual(expectedCodes, generatedCodes);
        }

        private static void TestDoubleAutoCodeWithGroup(Common.DomRepository repository, string group, string formatA, string formatB, string expectedCodes)
        {
            Guid id = Guid.NewGuid();
            repository.TestAutoCode.DoubleAutoCodeWithGroup.Insert(new[] { new TestAutoCode.DoubleAutoCodeWithGroup { ID = id, Grouping = group, CodeA = formatA, CodeB = formatB } });

            string generatedCodes = repository.TestAutoCode.DoubleAutoCodeWithGroup.Query()
                .Where(item => item.ID == id)
                .Select(item => item.CodeA + "," + item.CodeB).Single();
            Console.WriteLine(formatA + "," + formatB + " => " + generatedCodes);
            Assert.AreEqual(expectedCodes, generatedCodes);
        }

        private static void TestDoubleIntegerAutoCodeWithGroup(Common.DomRepository repository, int group, int? codeA, int? codeB, string expectedCodes)
        {
            Guid id = Guid.NewGuid();
            repository.TestAutoCode.IntegerAutoCodeForEach.Insert(new[] {
                new TestAutoCode.IntegerAutoCodeForEach { ID = id, Grouping = group, CodeA = codeA, CodeB = codeB } });

            string generatedCodes = repository.TestAutoCode.IntegerAutoCodeForEach.Query()
                .Where(item => item.ID == id)
                .Select(item => item.CodeA.ToString() + "," + item.CodeB.ToString()).Single();
            Console.WriteLine(codeA.ToString() + "," + codeB.ToString() + " => " + generatedCodes);
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

                repository.TestAutoCode.Simple.Insert(
                    tests.Select((test, index) => new TestAutoCode.Simple { ID = new Guid(index + 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0), Code = test.Item1 }));

                IEnumerable<string> generatedCodes = repository.TestAutoCode.Simple.Load()
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
        public void IntAutoCode()
        {
            using (var scope = TestScope.Create())
            {
                DeleteOldData(scope);
                var repository = scope.Resolve<Common.DomRepository>();

                TestIntAutoCode(repository, 0, 1);
                TestIntAutoCode(repository, 10, 10);
                TestIntAutoCode(repository, 0, 11);
                TestIntAutoCode(repository, 99, 99);
                TestIntAutoCode(repository, 0, 100);
                TestIntAutoCode(repository, 0, 101);
                TestIntAutoCode(repository, 0, 102);
            }
        }

        [TestMethod]
        public void IntAutoCodeNull()
        {
            using (var scope = TestScope.Create())
            {
                DeleteOldData(scope);
                var repository = scope.Resolve<Common.DomRepository>();

                TestIntAutoCode(repository, null, 1);
                TestIntAutoCode(repository, null, 2);
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

        [TestMethod]
        public void DoubleIntegerAutoCodeWithGroup()
        {
            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();
                repository.TestAutoCode.IntegerAutoCodeForEach.Delete(repository.TestAutoCode.IntegerAutoCodeForEach.Query());

                TestDoubleIntegerAutoCodeWithGroup(repository, 1, 0, 0, "1,1");
                TestDoubleIntegerAutoCodeWithGroup(repository, 1, 5, 0, "5,2");
                TestDoubleIntegerAutoCodeWithGroup(repository, 1, 0, 0, "6,3");
                TestDoubleIntegerAutoCodeWithGroup(repository, 2, 8, 0, "8,1");
                TestDoubleIntegerAutoCodeWithGroup(repository, 2, 0, 0, "9,2");
                TestDoubleIntegerAutoCodeWithGroup(repository, 1, 0, 0, "10,4");
            }
        }

        [TestMethod]
        public void DoubleIntegerAutoCodeWithGroupNull()
        {
            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();
                repository.TestAutoCode.IntegerAutoCodeForEach.Delete(repository.TestAutoCode.IntegerAutoCodeForEach.Query());

                TestDoubleIntegerAutoCodeWithGroup(repository, 1, null, null, "1,1");
                TestDoubleIntegerAutoCodeWithGroup(repository, 1, 5, null, "5,2");
                TestDoubleIntegerAutoCodeWithGroup(repository, 1, null, null, "6,3");
                TestDoubleIntegerAutoCodeWithGroup(repository, 2, 8, null, "8,1");
                TestDoubleIntegerAutoCodeWithGroup(repository, 2, null, null, "9,2");
                TestDoubleIntegerAutoCodeWithGroup(repository, 1, null, null, "10,4");
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

                TestGroup<TestAutoCode.IntGroup, int>(repository.TestAutoCode.IntGroup, 500, "+", "1");
                TestGroup<TestAutoCode.IntGroup, int>(repository.TestAutoCode.IntGroup, 500, "+", "2");
                TestGroup<TestAutoCode.IntGroup, int>(repository.TestAutoCode.IntGroup, 600, "+", "1");
                TestGroup<TestAutoCode.IntGroup, int>(repository.TestAutoCode.IntGroup, 600, "A+", "A1");
                TestGroup<TestAutoCode.IntGroup, int>(repository.TestAutoCode.IntGroup, 600, "A+", "A2");

                TestGroup<TestAutoCode.BoolGroup, bool>(repository.TestAutoCode.BoolGroup, false, "+", "1");
                TestGroup<TestAutoCode.BoolGroup, bool>(repository.TestAutoCode.BoolGroup, false, "+", "2");
                TestGroup<TestAutoCode.BoolGroup, bool>(repository.TestAutoCode.BoolGroup, true, "+", "1");
                TestGroup<TestAutoCode.BoolGroup, bool>(repository.TestAutoCode.BoolGroup, true, "A+", "A1");
                TestGroup<TestAutoCode.BoolGroup, bool>(repository.TestAutoCode.BoolGroup, true, "A+", "A2");

                TestGroup<TestAutoCode.StringGroup, string>(repository.TestAutoCode.StringGroup, "x", "+", "1");
                TestGroup<TestAutoCode.StringGroup, string>(repository.TestAutoCode.StringGroup, "x", "+", "2");
                TestGroup<TestAutoCode.StringGroup, string>(repository.TestAutoCode.StringGroup, "y", "+", "1");
                TestGroup<TestAutoCode.StringGroup, string>(repository.TestAutoCode.StringGroup, "y", "A+", "A1");
                TestGroup<TestAutoCode.StringGroup, string>(repository.TestAutoCode.StringGroup, "y", "A+", "A2");

                var simple1 = new TestAutoCode.Simple { ID = Guid.NewGuid(), Code = "1" };
                var simple2 = new TestAutoCode.Simple { ID = Guid.NewGuid(), Code = "2" };
                repository.TestAutoCode.Simple.Insert(new[] { simple1, simple2 });

                TestGroup<TestAutoCode.ReferenceGroup, TestAutoCode.Simple>(repository.TestAutoCode.ReferenceGroup, simple1, "+", "1");
                TestGroup<TestAutoCode.ReferenceGroup, TestAutoCode.Simple>(repository.TestAutoCode.ReferenceGroup, simple1, "+", "2");
                TestGroup<TestAutoCode.ReferenceGroup, TestAutoCode.Simple>(repository.TestAutoCode.ReferenceGroup, simple2, "+", "1");
                TestGroup<TestAutoCode.ReferenceGroup, TestAutoCode.Simple>(repository.TestAutoCode.ReferenceGroup, simple2, "A+", "A1");
                TestGroup<TestAutoCode.ReferenceGroup, TestAutoCode.Simple>(repository.TestAutoCode.ReferenceGroup, simple2, "A+", "A2");

                var grouping1 = new TestAutoCode.Grouping { ID = Guid.NewGuid(), Code = "1" };
                var grouping2 = new TestAutoCode.Grouping { ID = Guid.NewGuid(), Code = "2" };
                repository.TestAutoCode.Grouping.Insert(new[] { grouping1, grouping2 });

                TestGroup<TestAutoCode.ShortReferenceGroup, TestAutoCode.Grouping>(repository.TestAutoCode.ShortReferenceGroup, grouping1, "+", "1");
                TestGroup<TestAutoCode.ShortReferenceGroup, TestAutoCode.Grouping>(repository.TestAutoCode.ShortReferenceGroup, grouping1, "+", "2");
                TestGroup<TestAutoCode.ShortReferenceGroup, TestAutoCode.Grouping>(repository.TestAutoCode.ShortReferenceGroup, grouping2, "+", "1");
                TestGroup<TestAutoCode.ShortReferenceGroup, TestAutoCode.Grouping>(repository.TestAutoCode.ShortReferenceGroup, grouping2, "A+", "A1");
                TestGroup<TestAutoCode.ShortReferenceGroup, TestAutoCode.Grouping>(repository.TestAutoCode.ShortReferenceGroup, grouping2, "A+", "A2");
            }
        }

        [TestMethod]
        public void AllowedNullValueInternally()
        {
            using (var scope = TestScope.Create())
            {
                DeleteOldData(scope);

                var context = scope.Resolve<Common.ExecutionContext>();

                var s1 = new TestAutoCode.Simple { ID = Guid.NewGuid(), Code = null };

                AutoCodeHelper.UpdateCodesWithoutCache(
                    context.SqlExecuter, "TestAutoCode.Simple", "Code",
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
            using (var scope = TestScope.Create())
            {
                DeleteOldData(scope);

                var repository = scope.Resolve<Common.DomRepository>();
                var item1 = new TestAutoCode.Simple { ID = Guid.NewGuid(), Code = "" };
                repository.TestAutoCode.Simple.Insert(item1);
                Assert.AreEqual("", repository.TestAutoCode.Simple.Load(new[] { item1.ID }).Single().Code);
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
                        () => repository.TestAutoCode.Simple.Insert(new[] {
                            new TestAutoCode.Simple { ID = Guid.NewGuid(), Code = test } }),
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
                    repository.Insert(new[] { new TestAutoCode.Simple { Code = "+", Data = process.ToString() } });
                    repository.Insert(new[] { new TestAutoCode.Simple { Code = "+", Data = process.ToString() } });
                });

            // Each thread inserts 50*2 records, reusing the existing AutoCode cache:
            Execute2ParallelInserts(50, (process, repository) =>
            {
                repository.Insert(new[] { new TestAutoCode.Simple { Code = "+", Data = process.ToString() } });
                repository.Insert(new[] { new TestAutoCode.Simple { Code = "+", Data = process.ToString() } });
            });

            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>().TestAutoCode.Simple;
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
                    repository.Insert(new[] { new TestAutoCode.Simple { Code = (char)('a' + process) + "+", Data = process.ToString() } });
                    repository.Insert(new[] { new TestAutoCode.Simple { Code = (char)('a' + process) + "+", Data = process.ToString() } });
                });

            // Each thread inserts 50*2 records, reusing the existing AutoCode cache:
            Execute2ParallelInserts(50, (process, repository) =>
            {
                repository.Insert(new[] { new TestAutoCode.Simple { Code = (char)('a' + process) + "+", Data = process.ToString() } });
                repository.Insert(new[] { new TestAutoCode.Simple { Code = (char)('a' + process) + "+", Data = process.ToString() } });
            });

            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>().TestAutoCode.Simple;
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
                repository.Insert(new[] { new TestAutoCode.Simple { Code = "+", Data = process.ToString() } });
                System.Threading.Thread.Sleep(200);
                endTimes[process] = DateTime.Now;
            });

            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>().TestAutoCode.Simple;
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

            for (int retries = 0; retries < 4; retries++)
            {
                try
                {
                    // One process may be executed in parallel with another, since they use different prefixes:

                    var endTimes = new DateTime[2];

                    Execute2ParallelInserts(1, (process, repository) =>
                    {
                        repository.Insert(new[] { new TestAutoCode.Simple { Code = (char)('a' + process) + "+", Data = process.ToString() } });
                        System.Threading.Thread.Sleep(200);
                        endTimes[process] = DateTime.Now;
                    });

                    using (var scope = TestScope.Create())
                    {
                        var repository = scope.Resolve<Common.DomRepository>().TestAutoCode.Simple;
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
                        if (delay < 200)
                            Assert.Fail("One process did not wait for another.");
                        if (delay > 300)
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

        private void Execute2ParallelInserts(int testCount, Action<int, TestAutoCode._Helper.Simple_Repository> action)
        {
            const int threadCount = 2;

            using (var scope = TestScope.Create())
            {
                ConcurrencyUtility.CheckForParallelism(scope.Resolve<ISqlExecuter>(), threadCount);
                DeleteOldData(scope);
                scope.CommitChanges();
            }

            for (int test = 1; test <= testCount; test++)
            {
                Console.WriteLine("Test: " + test);
                var containers = Enumerable.Range(0, threadCount).Select(t => TestScope.Create()).ToArray();

                try
                {
                    var repositories = containers.Select(c => c.Resolve<Common.DomRepository>().TestAutoCode.Simple).ToArray();
                    foreach (var r in repositories)
                        Assert.IsTrue(r.Query().Count() >= 0); // Cold start.

                    Parallel.For(0, threadCount, process =>
                    {
                        action(process, repositories[process]);
                        containers[process].CommitChanges();
                        containers[process].Dispose();
                        containers[process] = null;
                    });
                } 
                finally
                {
                    foreach (var c in containers)
                        if (c != null)
                            c.Dispose();
                }
            }
        }

        [TestMethod]
        public void ParallelInsertsLockErrorHandling()
        {
            var actions = new Action<Common.ExecutionContext>[]
            {
                // Starts at 0 ms, ends at 400ms.
                context =>
                {
                    Thread.Sleep(0);
                    context.SqlExecuter.ExecuteSql("SET LOCK_TIMEOUT 0");
                    context.Repository.TestAutoCode.Simple.Insert(new TestAutoCode.Simple { Code = "+" });
                    Thread.Sleep(400);
                },
                // Starts at 100 ms, lock timeout at 300ms.
                context =>
                {
                    Thread.Sleep(100);
                    context.SqlExecuter.ExecuteSql("SET LOCK_TIMEOUT 200");
                    context.Repository.TestAutoCode.Simple.Insert(new TestAutoCode.Simple { Code = "+" });
                },

                // Starts at 200 ms, lock timeout at 200ms.
                context =>
                {
                    Thread.Sleep(200);
                    context.SqlExecuter.ExecuteSql("SET LOCK_TIMEOUT 0");
                    context.Repository.TestAutoCode.Simple.Insert(new TestAutoCode.Simple { Code = "+" });
                },

                // Starts at 200 ms, ends at 200ms.
                context =>
                {
                    Thread.Sleep(200);
                    context.SqlExecuter.ExecuteSql("SET LOCK_TIMEOUT 0");
                    context.Repository.TestAutoCode.Simple.Load(item => item.Code == "1");
                }
            };

            var exceptions = ExecuteParallel(actions,
                context => context.Repository.TestAutoCode.Simple.Insert(new TestAutoCode.Simple { Code = "1" }),
                context => Assert.AreEqual(1, context.Repository.TestAutoCode.Simple.Query().Count()));

            Assert.IsNull(exceptions[0]);
            Assert.IsNotNull(exceptions[1]);
            Assert.IsNotNull(exceptions[2]);
            Assert.IsNull(exceptions[3]); // sql3 should be allowed to read the record with code '1'. sql0 has exclusive lock on code '2'. autocode should not put exclusive lock on other records.

            // Query sql1 may generate next autocode, but it should wait for the entity's table exclusive lock to be released (from sql0).
            TestUtility.AssertContains(exceptions[1].ToString(), new[] { "Cannot insert", "another user's insert command is still running", "TestAutoCode.Simple" });

            // Query sql2 may not generate next autocode until sql1 releases the lock.
            TestUtility.AssertContains(exceptions[2].ToString(), new[] { "Cannot insert", "another user's insert command is still running", "TestAutoCode.Simple" });
        }

        private Exception[] ExecuteParallel(Action<Common.ExecutionContext>[] actions, Action<Common.ExecutionContext> coldStartInsert, Action<Common.ExecutionContext> coldStartQuery)
        {
            int threadCount = actions.Count();

            using (var scope = TestScope.Create())
            {
                var sqlExecuter = scope.Resolve<ISqlExecuter>();
                ConcurrencyUtility.CheckForParallelism(sqlExecuter, threadCount);
                DeleteOldData(scope);

                coldStartInsert(scope.Resolve<Common.ExecutionContext>());
                
                scope.CommitChanges();
            }

            var containers = Enumerable.Range(0, threadCount).Select(t => TestScope.Create()).ToArray();

            var exceptions = new Exception[threadCount];

            try
            {
                var contexts = containers.Select(c => c.Resolve<Common.ExecutionContext>()).ToArray();
                foreach (var context in contexts)
                    coldStartQuery(context);

                Parallel.For(0, threadCount, thread =>
                {
                    try
                    {
                        actions[thread].Invoke(contexts[thread]);
                    }
                    catch (Exception ex)
                    {
                        exceptions[thread] = ex;
                        contexts[thread].PersistenceTransaction.DiscardChanges();
                    }
                    finally
                    {
                        containers[thread].CommitChanges();
                        containers[thread].Dispose();
                        containers[thread] = null;
                    }
                });
            }
            finally
            {
                foreach (var c in containers)
                    if (c != null)
                        c.Dispose();
            }

            for (int x = 0; x < threadCount; x++)
                Console.WriteLine("Exception " + x + ": " + exceptions[x] + ".");
            return exceptions;
        }

        /// <summary>
        /// There is no need to lock all inserts. It should be allowed to insert different groups in parallel.
        /// </summary>
        [TestMethod]
        public void OptimizeParallelInsertsForDifferentGroups()
        {
            var tests = new (string, string, bool ExpectedParallel)[]
            // Format:
            // 1. Records to insert (Grouping1-Grouping2) to entity MultipleGroups, with parallel requests.
            // 2. Expected generated codes (Code1-Code2) for each record.
            // 3. Whether the inserts should be executed in parallel.
            {
                ( "A-B, A-B", "1-1, 2-2", false ), // Same Grouping1 and Grouping2: codes should be generated sequentially.
                ( "A-B, A-C", "1-1, 2-1", false ), // Same Grouping1: Code1 should be generated sequentially.
                ( "A-B, C-B", "1-1, 1-2", false ), // Same Grouping2: Code2 should be generated sequentially.
                ( "A-B, C-D", "1-1, 1-1", true ),
                ( "A-B, B-A", "1-1, 1-1", true ),
            };

            var results = new List<(string, string, bool ExecutedInParallel)>();
            var report = new List<string>();

            const int testPause = 100;
            const int retries = 20;

            foreach (var test in tests)
            {
                var items = test.Item1.Split(',').Select(item => item.Trim()).Select(item => item.Split('-'))
                    .Select(item => new TestAutoCode.MultipleGroups { Grouping1 = item[0], Grouping2 = item[1] })
                    .ToArray();

                var insertDurations = new double[items.Length];

                var parallelInsertRequests = items.Select((item, x) => (Action<Common.ExecutionContext>)
                    (context =>
                    {
                        var sw = Stopwatch.StartNew();
                        context.Repository.TestAutoCode.MultipleGroups.Insert(item);
                        insertDurations[x] = sw.Elapsed.TotalMilliseconds;
                        Thread.Sleep(testPause);
                    }))
                    .ToArray();

                string generatedCodes = null;
                bool startedImmediately = false;
                bool executedInParallel = false;

                for (int retry = 0; retry <= retries; retry++)
                {
                    // Execute in parallel inserts that generate AutoCode values:

                    var exceptions = ExecuteParallel(parallelInsertRequests,
                        context => context.Repository.TestAutoCode.MultipleGroups.Insert(new TestAutoCode.MultipleGroups { }),
                        context => Assert.AreEqual(1, context.Repository.TestAutoCode.MultipleGroups.Query().Count(), $"({test.Item1}) Test initialization failed."));

                    Assert.IsTrue(exceptions.All(e => e == null), $"({test.Item1}) Test initialization threw exception. See the test output for details");

                    // Check the generated codes:

                    using (var scope = TestScope.Create())
                    {
                        var repository = scope.Resolve<Common.DomRepository>();
                        generatedCodes = TestUtility.DumpSorted(
                            repository.TestAutoCode.MultipleGroups.Load(items.Select(item => item.ID)),
                            x => $"{x.Code1}-{x.Code2}");
                    }

                    // Check if the inserts were executed in parallel:

                    startedImmediately = insertDurations.Any(t => t < testPause);
                    executedInParallel = insertDurations.All(t => t < testPause);

                    // It the parallelism check did not pass, try again to reduce false negatives when the test machine is under load.
                    if (!startedImmediately && retry < retries)
                        Console.WriteLine("Retrying, did not start immediately.");
                    else if (executedInParallel != test.ExpectedParallel && retry < retries)
                        Console.WriteLine("Retrying, did not achieve parallelism.");
                    else
                        break;
                }

                Assert.IsTrue(startedImmediately, $"({test.Item1}) At lease one item should be inserted without waiting. The test machine was probably under load during the parallelism test.");

                report.Add($"Test '{test.Item1}' insert durations: '{TestUtility.Dump(insertDurations)}'.");
                results.Add((test.Item1, generatedCodes, executedInParallel));
            }

            Assert.AreEqual(
                string.Concat(tests.Select(t => $"{t.Item1} => {t.Item2} {(t.ExpectedParallel ? "parallel" : "sequential")}\r\n")),
                string.Concat(results.Select(r => $"{r.Item1} => {r.Item2} {(r.ExecutedInParallel ? "parallel" : "sequential")}\r\n")),
                "Report: " + string.Concat(report.Select(line => "\r\n" + line)));
        }
    }
}
