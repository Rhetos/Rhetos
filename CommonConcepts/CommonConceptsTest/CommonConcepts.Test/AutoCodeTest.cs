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

namespace CommonConcepts.Test
{
    [TestClass]
    public class AutoCodeTest
    {
        private static void DeleteOldData(Common.ExecutionContext executionContext)
        {
            executionContext.SqlExecuter.ExecuteSql(new[]
                {
                    @"DELETE FROM TestAutoCode.ReferenceGroup;
                    DELETE FROM TestAutoCode.ShortReferenceGroup;
                    DELETE FROM TestAutoCode.StringGroup;
                    DELETE FROM TestAutoCode.IntGroup;
                    DELETE FROM TestAutoCode.Simple;"
                });
        }

        private static void TestSimple(Common.ExecutionContext executionContext, Common.DomRepository repository, string format, string expectedCode)
        {
            Guid id = Guid.NewGuid();
            repository.TestAutoCode.Simple.Insert(new[] { new TestAutoCode.Simple { ID = id, Code = format } });

            executionContext.NHibernateSession.Flush();
            executionContext.NHibernateSession.Clear();

            string generatedCode = repository.TestAutoCode.Simple.Query().Where(item => item.ID == id).Select(item => item.Code).Single();
            Console.WriteLine(format + " => " + generatedCode);
            Assert.AreEqual(expectedCode, generatedCode);
        }

        [TestMethod]
        public void Simple()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                DeleteOldData(executionContext);
                var repository = new Common.DomRepository(executionContext);

                TestSimple(executionContext, repository, "+", "1");
                TestSimple(executionContext, repository, "+", "2");
                TestSimple(executionContext, repository, "+", "3");
                TestSimple(executionContext, repository, "9", "9");
                TestSimple(executionContext, repository, "+", "10");
                TestSimple(executionContext, repository, "+", "11");
                TestSimple(executionContext, repository, "AB+", "AB1");
                TestSimple(executionContext, repository, "X", "X");
                TestSimple(executionContext, repository, "X+", "X1");
                TestSimple(executionContext, repository, "AB007", "AB007");
                TestSimple(executionContext, repository, "AB+", "AB008");
                TestSimple(executionContext, repository, "AB999", "AB999");
                TestSimple(executionContext, repository, "AB+", "AB1000");
            }
        }

        private static void TestGroup<TEntity, TGroup>(
            Common.ExecutionContext executionContext, object entityRepository,
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

            executionContext.NHibernateSession.Flush();
            executionContext.NHibernateSession.Clear();

            var filterRepository = (IFilterRepository<IEnumerable<Guid>, TEntity>)entityRepository;
            dynamic loaded = filterRepository.Filter(new[] {id}).Single();
            string generatedCode = loaded.Code;

            Console.WriteLine(format + " => " + generatedCode);
            Assert.AreEqual(expectedCode, generatedCode);
        }

        [TestMethod]
        public void Grouping()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                DeleteOldData(executionContext);
                var repository = new Common.DomRepository(executionContext);

                TestGroup<TestAutoCode.IntGroup, int>(executionContext, repository.TestAutoCode.IntGroup, 500, "+", "1");
                TestGroup<TestAutoCode.IntGroup, int>(executionContext, repository.TestAutoCode.IntGroup, 500, "+", "2");
                TestGroup<TestAutoCode.IntGroup, int>(executionContext, repository.TestAutoCode.IntGroup, 600, "+", "1");
                TestGroup<TestAutoCode.IntGroup, int>(executionContext, repository.TestAutoCode.IntGroup, 600, "A+", "A1");

                TestGroup<TestAutoCode.StringGroup, string>(executionContext, repository.TestAutoCode.StringGroup, "x", "+", "1");
                TestGroup<TestAutoCode.StringGroup, string>(executionContext, repository.TestAutoCode.StringGroup, "x", "+", "2");
                TestGroup<TestAutoCode.StringGroup, string>(executionContext, repository.TestAutoCode.StringGroup, "y", "+", "1");
                TestGroup<TestAutoCode.StringGroup, string>(executionContext, repository.TestAutoCode.StringGroup, "y", "A+", "A1");

                var simple1 = new TestAutoCode.Simple { ID = Guid.NewGuid(), Code = "1" };
                var simple2 = new TestAutoCode.Simple { ID = Guid.NewGuid(), Code = "2" };
                repository.TestAutoCode.Simple.Insert(new[] { simple1, simple2 });
                executionContext.NHibernateSession.Flush();

                TestGroup<TestAutoCode.ReferenceGroup, TestAutoCode.Simple>(executionContext, repository.TestAutoCode.ReferenceGroup, simple1, "+", "1");
                TestGroup<TestAutoCode.ReferenceGroup, TestAutoCode.Simple>(executionContext, repository.TestAutoCode.ReferenceGroup, simple1, "+", "2");
                TestGroup<TestAutoCode.ReferenceGroup, TestAutoCode.Simple>(executionContext, repository.TestAutoCode.ReferenceGroup, simple2, "+", "1");
                TestGroup<TestAutoCode.ReferenceGroup, TestAutoCode.Simple>(executionContext, repository.TestAutoCode.ReferenceGroup, simple2, "A+", "A1");

                var grouping1 = new TestAutoCode.Grouping { ID = Guid.NewGuid(), Code = "1" };
                var grouping2 = new TestAutoCode.Grouping { ID = Guid.NewGuid(), Code = "2" };
                repository.TestAutoCode.Grouping.Insert(new[] { grouping1, grouping2 });
                executionContext.NHibernateSession.Flush();

                TestGroup<TestAutoCode.ShortReferenceGroup, TestAutoCode.Grouping>(executionContext, repository.TestAutoCode.ShortReferenceGroup, grouping1, "+", "1");
                TestGroup<TestAutoCode.ShortReferenceGroup, TestAutoCode.Grouping>(executionContext, repository.TestAutoCode.ShortReferenceGroup, grouping1, "+", "2");
                TestGroup<TestAutoCode.ShortReferenceGroup, TestAutoCode.Grouping>(executionContext, repository.TestAutoCode.ShortReferenceGroup, grouping2, "+", "1");
                TestGroup<TestAutoCode.ShortReferenceGroup, TestAutoCode.Grouping>(executionContext, repository.TestAutoCode.ShortReferenceGroup, grouping2, "A+", "A1");
            }
        }

        [TestMethod]
        public void SqlTriggerHandlesNullValue()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                DeleteOldData(executionContext);

                Guid id = Guid.NewGuid();
                executionContext.SqlExecuter.ExecuteSql(new[] { "INSERT INTO TestAutoCode.Simple (ID, Code) VALUES ('" + id + "', NULL)" });

                var repository = new Common.DomRepository(executionContext);
                var loaded = repository.TestAutoCode.Simple.Query().Where(item => item.ID == id).Single();
                Assert.IsNull(loaded.Code);
            }
        }
    }
}
