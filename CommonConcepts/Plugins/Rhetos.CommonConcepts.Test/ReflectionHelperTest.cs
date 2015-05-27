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
using Rhetos.Dom.DefaultConcepts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.CommonConcepts.Test
{
    [TestClass]
    public class ReflectionHelperTest
    {
        class SimpleEntity : IEntity
        {
            public Guid ID { get; set; }
        }

        class DerivedEntity : SimpleEntity
        {
            public Guid ID { get; set; }
            public string Data { get; set; }
        }

        [TestMethod]
        public void AsQueryable()
        {
            var simpleEntityReflection = new ReflectionHelper<IEntity>(typeof(SimpleEntity).FullName, new Mocks.DomainObjectModelMock(), null);

            {
                var entityList = new List<IEntity>() { new DerivedEntity { Data = "d" } };
                var q = simpleEntityReflection.AsQueryable(entityList);
                Console.WriteLine(q.GetType());
                Assert.IsTrue(q is IQueryable<SimpleEntity>);
                Assert.AreEqual("d", ((DerivedEntity)q.Single()).Data);
                Assert.IsFalse(object.ReferenceEquals(entityList, q));
            }
            {
                var derivedList = new List<DerivedEntity>() { new DerivedEntity { Data = "d" } };
                var q = simpleEntityReflection.AsQueryable(derivedList);
                Console.WriteLine(q.GetType());
                Assert.IsTrue(q is IQueryable<SimpleEntity>);
                Assert.AreEqual("d", ((DerivedEntity)q.Single()).Data);
                Assert.IsFalse(object.ReferenceEquals(derivedList, q));
            }
            {
                var simpleQueryable = new List<SimpleEntity>() { new DerivedEntity { Data = "d" } }.AsQueryable();
                var q = simpleEntityReflection.AsQueryable(simpleQueryable);
                Console.WriteLine(q.GetType());
                Assert.IsTrue(q is IQueryable<SimpleEntity>);
                Assert.AreEqual("d", ((DerivedEntity)q.Single()).Data);
                Assert.IsTrue(object.ReferenceEquals(simpleQueryable, q), "Optimized.");
            }
            {
                var derivedQueryable = new List<DerivedEntity>() { new DerivedEntity { Data = "d" } }.AsQueryable();
                var q = simpleEntityReflection.AsQueryable(derivedQueryable);
                Console.WriteLine(q.GetType());
                Assert.IsTrue(q is IQueryable<SimpleEntity>);
                Assert.AreEqual("d", ((DerivedEntity)q.Single()).Data);
                Assert.IsTrue(object.ReferenceEquals(derivedQueryable, q), "Optimized.");
            }
        }
    }
}
