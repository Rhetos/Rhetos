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
using Rhetos.Configuration.Autofac;
using Rhetos.Utilities;
using Rhetos.Dom.DefaultConcepts;

namespace CommonConcepts.Test
{
    [TestClass]
    public class SqlQueryableTest
    {
        [TestMethod]
        public void QueryableFromRepository()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                var secondString = repository.TestDataStructure.SqlQueryable1.Query().Where(item => item.i == 2).Select(item => item.s).Single();
                Assert.AreEqual("b", secondString);
            }
        }

        private static string ReportCachingTestView(Common.DomRepository repository)
        {
            return string.Join(", ", repository.TestDataStructure.CachingTestView.Query().Select(item => item.S).OrderBy(x => x));
        }

        [TestMethod]
        public void NotCached()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestDataStructure.CachingTestEntity;" });
                Assert.AreEqual("", ReportCachingTestView(repository), "initial");

                var entities = repository.TestDataStructure.CachingTestEntity;
                var id = Guid.NewGuid();
                entities.Insert(new[] { new TestDataStructure.CachingTestEntity { ID = id, S = "v1" } });
                Assert.AreEqual("v1", ReportCachingTestView(repository), "after insert");

                entities.Update(new[] { new TestDataStructure.CachingTestEntity { ID = id, S = "v2" } });
                Assert.AreEqual("v2", ReportCachingTestView(repository), "after update");

                entities.Delete(new[] { new TestDataStructure.CachingTestEntity { ID = id } });
                Assert.AreEqual("", ReportCachingTestView(repository), "after delete");
            }
        }

        [TestMethod]
        public void NotCachedReference()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] {"DELETE FROM TestSqlQueryable.Document;"});
                var documentRepository = container.Resolve<Common.DomRepository>().TestSqlQueryable.Document;

                var doc = new TestSqlQueryable.Document { ID = Guid.NewGuid(), Name = "abc" };
                documentRepository.Insert(new[] { doc });
                Assert.AreEqual(3, documentRepository.Query().Single().Extension_DocumentInfo.NameLen);

                doc.Name = "abcd";
                documentRepository.Update(new[] { doc });

                Assert.AreEqual(4, documentRepository.Query().ToList().Single().Extension_DocumentInfo.NameLen);
            }
        }

        [TestMethod]
        public void ReferenceToView()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestDataStructure.ReferenceView;" });

                var entityRepository = container.Resolve<Common.DomRepository>().TestDataStructure.ReferenceView;

                var newItem = new TestDataStructure.ReferenceView { SqlQueryable1ID = new Guid("DB97EA5F-FB8C-408F-B35B-AD6642C593D7") };
                entityRepository.Insert(new[] { newItem });

                Assert.AreEqual("b", entityRepository.Query().Select(item => item.SqlQueryable1.s).Single());
            }
        }
    }
}
