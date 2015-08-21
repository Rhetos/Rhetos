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
using Rhetos.Configuration.Autofac;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonConcepts.Test
{
    [TestClass]
    public class QueryableRepositoryTest
    {
        [TestMethod]
        public void QueryWithParameterSimple()
        {
            using (var container = new RhetosTestContainer())
            {
                var testData = new[] { "a1", "a2", "b1" }
                    .Select(name => new TestQueryable.Simple { Name = name })
                    .ToList();

                var simpleRepository = container.Resolve<Common.DomRepository>().TestQueryable.Simple;
                simpleRepository.Delete(simpleRepository.Load());
                simpleRepository.Insert(testData);

                var parameter = new TestQueryable.StartsWith { Prefix = "a" };
                Assert.AreEqual("a1, a2", TestUtility.DumpSorted(simpleRepository.Query(parameter), item => item.Name));

                var genericRepository = container.Resolve<GenericRepository<TestQueryable.Simple>>();
                Assert.AreEqual("a1, a2", TestUtility.DumpSorted(genericRepository.Query(parameter), item => item.Name));
                Assert.AreEqual("a1, a2", TestUtility.DumpSorted(genericRepository.Load(parameter), item => item.Name));
                TestUtility.ShouldFail(() => genericRepository.Filter(genericRepository.Query(), parameter), "does not implement", "TestQueryable.StartsWith");
            }
        }

        [TestMethod]
        public void QueryCovariance()
        {
            using (var container = new RhetosTestContainer())
            {
                var testData = new[] { "a1", "a2", "b1" }
                    .Select((name, x) => new TestQueryable.Simple { Name = name, ID = Guid.NewGuid() })
                    .ToList();

                var simpleRepository = container.Resolve<Common.DomRepository>().TestQueryable.Simple;
                simpleRepository.Delete(simpleRepository.Load());
                simpleRepository.Insert(testData);

                Assert.AreEqual("a2", ((TestQueryable.Simple)QueryCovarianceIEntity(simpleRepository, testData[1].ID)).Name);
                Assert.AreEqual("a2", ((TestQueryable.Simple)QueryCovarianceGeneric(simpleRepository, testData[1].ID)).Name);
            }
        }

        private object QueryCovarianceIEntity(IQueryableRepository<IEntity> repository, Guid id)
        {
            return repository.Query().Where(item => item.ID == id).FirstOrDefault();
        }

        private object QueryCovarianceGeneric<TEntity>(IQueryableRepository<TEntity> repository, Guid id) where TEntity : class, IEntity
        {
            return repository.Query().Where(item => item.ID == id).FirstOrDefault();
        }
    }
}
