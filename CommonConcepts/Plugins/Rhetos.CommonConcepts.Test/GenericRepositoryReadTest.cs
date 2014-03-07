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

using Autofac.Features.Indexed;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.CommonConcepts.Test.Mocks;
using Rhetos.Dom;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Rhetos.CommonConcepts.Test
{
    [TestClass]
    public class GenericRepositoryReadTest
    {
        interface ISimpleEntity : IEntity
        {
            string Name { get; set; }
        }

        class SimpleEntity : ISimpleEntity
        {
            public Guid ID { get; set; }
            public string Name { get; set; }

            public override string ToString()
            {
                return Name != null ? Name : "<null>";
            }
        }

        GenericRepository<ISimpleEntity> NewRepos(IRepository repository)
        {
            return new GenericRepository<ISimpleEntity>(
                new DomainObjectModelMock(),
                new Lazy<IIndex<string, IRepository>>(() => new RepositoryIndexMock(typeof(SimpleEntity), repository)),
                new RegisteredInterfaceImplementationsMock(typeof(ISimpleEntity), typeof(SimpleEntity)),
                new ConsoleLogProvider(),
                null);
        }

        //=======================================================

        class NullRepository : IRepository
        {
        }

        [TestMethod]
        public void ReadNoFunction()
        {
            var repos = NewRepos(new NullRepository());

            TestUtility.ShouldFail(() => repos.Read(),
                "no suitable functions",
                typeof(SimpleEntity).FullName,
                typeof(FilterAll).FullName);

            TestUtility.ShouldFail(() => repos.Read(new[] { Guid.NewGuid() }),
                "no suitable functions",
                typeof(SimpleEntity).FullName,
                "System.Guid[]");
        }

        //===============================================

        class FilterParameterRepository : IRepository
        {
            public SimpleEntity[] Filter(IEnumerable<Guid> parameter) { return new[] { new SimpleEntity { Name = "IE Guid" } }; }
            private SimpleEntity[] Filter(string parameter) { return new[] { new SimpleEntity { Name = "private string" } }; }
        }

        [TestMethod]
        public void ReadFilterParameter()
        {
            var repos = NewRepos(new FilterParameterRepository());

            TestUtility.ShouldFail(() => repos.Read(),
                "no suitable functions",
                typeof(SimpleEntity).FullName,
                typeof(FilterAll).FullName);

            TestUtility.ShouldFail(() => repos.Read(Guid.NewGuid()),
                "no suitable functions",
                typeof(SimpleEntity).FullName,
                "System.Guid");

            TestUtility.ShouldFail(() => repos.Read("a"), // Filter with a string parameter is a *private* function.
                "no suitable functions",
                typeof(SimpleEntity).FullName,
                "System.string");

            Assert.AreEqual("IE Guid", repos.Read(new[] { Guid.NewGuid() }).Single().Name, "Sending derived type as an argument.");
        }

        //===============================================

        class FilterQueryRepository : IRepository
        {
            public IQueryable<SimpleEntity> Filter(IQueryable<SimpleEntity> source, string parameter)
            {
                return new[] { new SimpleEntity { Name = "filter query" } }
                .AsQueryable().Where(item => true).Select(item => item);
            }
            public IQueryable<SimpleEntity> Query()
            {
                return new[] { new SimpleEntity { Name = "query" } }
                .AsQueryable().Where(item => true).Select(item => item);
            }
        }

        [TestMethod]
        public void ReadFilterQuery()
        {
            var repos = NewRepos(new FilterQueryRepository());

            TestUtility.ShouldFail(() => repos.Read(Guid.NewGuid()),
                "no suitable functions",
                typeof(SimpleEntity).FullName,
                "System.Guid");

            Assert.AreEqual("filter query", repos.Read("a").Single().Name);
            Assert.AreEqual("query", repos.Read().Single().Name);

            Assert.IsTrue(repos.Read("a") is List<SimpleEntity>, "GenericRepository.Read should always return materialized list instead of a query.");
            Assert.IsTrue(repos.Read() is List<SimpleEntity>, "GenericRepository.Read should always return materialized list instead of a query.");
        }

        //===============================================

        class FilterAllRepository : IRepository
        {
            public IEnumerable<SimpleEntity> Filter(FilterAll parameter) { return new[] { new SimpleEntity { Name = "FilterAll" } }; }
        }

        class AllRepository : IRepository
        {
            public IEnumerable<SimpleEntity> All() { return new[] { new SimpleEntity { Name = "All" } }; }
        }

        [TestMethod]
        public void ReadFilterAll()
        {
            {
                var repos = NewRepos(new FilterAllRepository());
                Assert.AreEqual("FilterAll", repos.Read().Single().Name);
                Assert.AreEqual("FilterAll", repos.Read(new FilterAll()).Single().Name);
            }

            {
                var repos = NewRepos(new AllRepository());
                Assert.AreEqual("All", repos.Read().Single().Name);
                Assert.AreEqual("All", repos.Read(new FilterAll()).Single().Name);
            }
        }

        //===============================================

        class SimpleQueryRepository : IRepository
        {
            public IQueryable<SimpleEntity> Query()
            {
                return new[] {
                    new SimpleEntity { Name = "a1" },
                    new SimpleEntity { Name = "a2", ID = Guid.NewGuid() },
                    new SimpleEntity { Name = "b1" },
                }.AsQueryable();
            }
        }

        [TestMethod]
        public void ReadFilterGenericFilter()
        {
            var repos = NewRepos(new SimpleQueryRepository());

            var items = repos.Read(new[] { new FilterCriteria { Property = "Name", Operation = "startsWith", Value = "a" } });
            Assert.AreEqual("a1, a2", string.Join(", ", items.Select(item => item.Name)));
            Assert.IsTrue(items is List<SimpleEntity>, "GenericRepository.Read should always return materialized list instead of a query.");
        }

        [TestMethod]
        public void ReadFilterExpression()
        {
            var repos = NewRepos(new SimpleQueryRepository());

            var items = repos.Read(item => item.Name.StartsWith("a"));
            Assert.AreEqual("a1, a2", string.Join(", ", items.Select(item => item.Name)));
            Assert.IsTrue(items is List<SimpleEntity>, "GenericRepository.Read should always return materialized list instead of a query.");
        }

        //===============================================

        class FilterObjectRepository : IRepository
        {
            public IEnumerable<SimpleEntity> Filter(object o) { return new[] { new SimpleEntity { Name = "o" } }; }
            public IEnumerable<SimpleEntity> Filter(string s) { return new[] { new SimpleEntity { Name = "s" } }; }

            public IEnumerable<SimpleEntity> Filter(IEnumerable<int> p) { return new[] { new SimpleEntity { Name = "ei" } }; }
            public IEnumerable<SimpleEntity> Filter(int[] p) { return new[] { new SimpleEntity { Name = "ai" } }; }
        }

        [TestMethod]
        public void ReadFilterObject()
        {
            var repos = NewRepos(new FilterObjectRepository());

            Assert.AreEqual("o", repos.Read(new object()).Single().Name);
            Assert.AreEqual("o", repos.Read<object>(null).Single().Name);
            Assert.AreEqual("s", repos.Read("abc").Single().Name);
            Assert.AreEqual("s", repos.Read<string>(null).Single().Name);
            Assert.AreEqual("o", repos.Read<object>("abc").Single().Name);
            Assert.AreEqual("o", repos.Read("abc", typeof(object)).Single().Name);
            Assert.AreEqual("s", repos.Read("abc", "abc".GetType()).Single().Name);
            Assert.AreEqual("o", repos.Read(null, typeof(object)).Single().Name);
            Assert.AreEqual("s", repos.Read(null, typeof(string)).Single().Name);

            object o = new[] { 1, 2, 3 };
            Assert.AreEqual("ai", repos.Read(o, o.GetType()).Single().Name);
            Assert.AreEqual("ei", repos.Read(o, typeof(IEnumerable<int>)).Single().Name);
        }
    }
}
