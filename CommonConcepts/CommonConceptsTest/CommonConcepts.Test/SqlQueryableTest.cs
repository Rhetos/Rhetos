﻿/*
    Copyright (C) 2013 Omega software d.o.o.

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

namespace CommonConcepts.Test
{
    [TestClass]
    public class SqlQueryableTest
    {
        [TestMethod]
        public void QueryableFromRepository()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);

                var secondString = repository.TestDataStructure.SqlQueryable1.Query().Where(item => item.i == 2).Select(item => item.s).Single();
                Assert.AreEqual("b", secondString);
            }
        }

        private static string ReportCachingTestView(Common.DomRepository repository)
        {
            return string.Join(", ", repository.TestDataStructure.CachingTestView.All().Select(item => item.S).OrderBy(x => x));
        }

        [TestMethod]
        public void NotCached()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);

                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestDataStructure.CachingTestEntity;" });
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
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] {"DELETE FROM TestSqlQueryable.Document;"});
                var documentRepository = new Common.DomRepository(executionContext).TestSqlQueryable.Document;

                var doc = new TestSqlQueryable.Document { ID = Guid.NewGuid(), Name = "abc" };
                documentRepository.Insert(new[] { doc });
                Assert.AreEqual(3, documentRepository.All().Single().Extension_DocumentInfo.NameLen);

                doc.Name = "abcd";
                documentRepository.Update(new[] { doc });

                Assert.AreEqual(4, documentRepository.All().Single().Extension_DocumentInfo.NameLen);
            }
        }
    }
}
