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
                    DELETE FROM TestAutoCode.IntegerAutoCode;"
                });
        }

        private static void TestSimple(RhetosTestContainer container, Common.DomRepository repository, string format, string expectedCode)
        {
            Guid id = Guid.NewGuid();
            repository.TestAutoCode.Simple.Insert(new[] { new TestAutoCode.Simple { ID = id, Code = format } });

            container.Resolve<Common.ExecutionContext>().NHibernateSession.Flush();
            container.Resolve<Common.ExecutionContext>().NHibernateSession.Clear();

            string generatedCode = repository.TestAutoCode.Simple.Query().Where(item => item.ID == id).Select(item => item.Code).Single();
            Console.WriteLine(format + " => " + generatedCode);
            Assert.AreEqual(expectedCode, generatedCode);
        }

        private static void TestIntAutoCode(RhetosTestContainer container, Common.DomRepository repository, int? input, int expectedCode)
        {
            Guid id = Guid.NewGuid();
            repository.TestAutoCode.IntegerAutoCode.Insert(new[] { new TestAutoCode.IntegerAutoCode { ID = id, Code = input } });

            container.Resolve<Common.ExecutionContext>().NHibernateSession.Flush();
            container.Resolve<Common.ExecutionContext>().NHibernateSession.Clear();

            int? generatedCode = repository.TestAutoCode.IntegerAutoCode.Query().Where(item => item.ID == id).Select(item => item.Code).Single();
            Console.WriteLine(input.ToString() + " => " + generatedCode.ToString());
            Assert.AreEqual(expectedCode, generatedCode);
        }

        private static void TestDoubleAutoCode(RhetosTestContainer container, Common.DomRepository repository, string formatA, string formatB, string expectedCodes)
        {
            Guid id = Guid.NewGuid();
            repository.TestAutoCode.DoubleAutoCode.Insert(new[] { new TestAutoCode.DoubleAutoCode { ID = id, CodeA = formatA, CodeB = formatB } });

            container.Resolve<Common.ExecutionContext>().NHibernateSession.Flush();
            container.Resolve<Common.ExecutionContext>().NHibernateSession.Clear();

            string generatedCodes = repository.TestAutoCode.DoubleAutoCode.Query().Where(item => item.ID == id).Select(item => item.CodeA + "," + item.CodeB).Single();
            Console.WriteLine(formatA + "," + formatB + " => " + generatedCodes);
            Assert.AreEqual(expectedCodes, generatedCodes);
        }

        private static void TestDoubleAutoCodeWithGroup(RhetosTestContainer container, Common.DomRepository repository, string group, string formatA, string formatB, string expectedCodes)
        {
            Guid id = Guid.NewGuid();
            repository.TestAutoCode.DoubleAutoCodeWithGroup.Insert(new[] { new TestAutoCode.DoubleAutoCodeWithGroup { ID = id, Grouping = group, CodeA = formatA, CodeB = formatB } });

            container.Resolve<Common.ExecutionContext>().NHibernateSession.Flush();
            container.Resolve<Common.ExecutionContext>().NHibernateSession.Clear();

            string generatedCodes = repository.TestAutoCode.DoubleAutoCodeWithGroup.Query().Where(item => item.ID == id).Select(item => item.CodeA + "," + item.CodeB).Single();
            Console.WriteLine(formatA + "," + formatB + " => " + generatedCodes);
            Assert.AreEqual(expectedCodes, generatedCodes);
        }

        private static void TestDoubleIntegerAutoCodeWithGroup(RhetosTestContainer container, Common.DomRepository repository, int group, int codeA, int codeB, string expectedCodes)
        {
            Guid id = Guid.NewGuid();
            repository.TestAutoCode.IntegerAutoCodeForEach.Insert(new[] { new TestAutoCode.IntegerAutoCodeForEach { ID = id, Grouping = group, CodeA = codeA, CodeB = codeB } });

            container.Resolve<Common.ExecutionContext>().NHibernateSession.Flush();
            container.Resolve<Common.ExecutionContext>().NHibernateSession.Clear();

            string generatedCodes = repository.TestAutoCode.IntegerAutoCodeForEach.Query().Where(item => item.ID == id).Select(item => item.CodeA.ToString() + "," + item.CodeB.ToString()).Single();
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
                // Null is not allowed since AutoCode generates Required concept for targeted property
                TestUtility.ShouldFail(() => repository.TestAutoCode.IntegerAutoCode.Insert(new[] { new TestAutoCode.IntegerAutoCode { Code = null } }), "required", "Code");
                TestIntAutoCode(container, repository, 99, 99);
                TestIntAutoCode(container, repository, 0, 100);
                TestIntAutoCode(container, repository, 0, 101);
                TestIntAutoCode(container, repository, 0, 102);
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
                DeleteOldData(container);
                var repository = container.Resolve<Common.DomRepository>();

                TestDoubleIntegerAutoCodeWithGroup(container, repository, 1, 0, 0, "1,1");
                TestDoubleIntegerAutoCodeWithGroup(container, repository, 1, 5, 0, "5,2");
                TestDoubleIntegerAutoCodeWithGroup(container, repository, 1, 0, 0, "6,3");
                TestDoubleIntegerAutoCodeWithGroup(container, repository, 2, 8, 0, "8,1");
                TestDoubleIntegerAutoCodeWithGroup(container, repository, 2, 0, 0, "9,2");
                TestDoubleIntegerAutoCodeWithGroup(container, repository, 1, 0, 0, "10,4");
            }
        }

        private static void TestGroup<TEntity, TGroup>(
            RhetosTestContainer container, object entityRepository,
            TGroup group, string format, string expectedCode)
                where TEntity : new()
        {
            var writeableRepository = (IWritableRepository) entityRepository;

            Guid id = Guid.NewGuid();
            dynamic entity = new TEntity();
            entity.ID = id;
            entity.Code = format;
            entity.Grouping = group;
            writeableRepository.Save(new[] { entity }, null, null);

            container.Resolve<Common.ExecutionContext>().NHibernateSession.Flush();
            container.Resolve<Common.ExecutionContext>().NHibernateSession.Clear();

            var filterRepository = (IFilterRepository<IEnumerable<Guid>, TEntity>)entityRepository;
            dynamic loaded = filterRepository.Filter(new[] {id}).Single();
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
                container.Resolve<Common.ExecutionContext>().NHibernateSession.Flush();

                TestGroup<TestAutoCode.ReferenceGroup, TestAutoCode.Simple>(container, repository.TestAutoCode.ReferenceGroup, simple1, "+", "1");
                TestGroup<TestAutoCode.ReferenceGroup, TestAutoCode.Simple>(container, repository.TestAutoCode.ReferenceGroup, simple1, "+", "2");
                TestGroup<TestAutoCode.ReferenceGroup, TestAutoCode.Simple>(container, repository.TestAutoCode.ReferenceGroup, simple2, "+", "1");
                TestGroup<TestAutoCode.ReferenceGroup, TestAutoCode.Simple>(container, repository.TestAutoCode.ReferenceGroup, simple2, "A+", "A1");

                var grouping1 = new TestAutoCode.Grouping { ID = Guid.NewGuid(), Code = "1" };
                var grouping2 = new TestAutoCode.Grouping { ID = Guid.NewGuid(), Code = "2" };
                repository.TestAutoCode.Grouping.Insert(new[] { grouping1, grouping2 });
                container.Resolve<Common.ExecutionContext>().NHibernateSession.Flush();

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
                var loaded = repository.TestAutoCode.Simple.Query().Where(item => item.ID == id).Single();
                Assert.IsNull(loaded.Code);
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
                TestSimple(container, repository, "+", "0005");
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
    }
}
