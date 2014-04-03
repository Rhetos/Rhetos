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
                    DELETE FROM TestAutoCode.Simple;"
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
    }
}
