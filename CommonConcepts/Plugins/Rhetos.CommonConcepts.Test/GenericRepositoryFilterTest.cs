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
    public class GenericRepositoryFilterTest
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

        class SimpleEntityList : List<SimpleEntity>
        {
            static int idCounter = 0;
            public void Add(string name) { Add(new SimpleEntity { Name = name, ID = new Guid(idCounter++, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0) }); }
        }

        static readonly SimpleEntity[] EmptyArray = new SimpleEntity[] { };
        static readonly SimpleEntity[] AbcArray = new SimpleEntity[] { new SimpleEntity { Name = "abc" } };

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
        public void FilterNoFunction()
        {
            var repos = NewRepos(new NullRepository());

            TestUtility.ShouldFail(() => repos.Filter(EmptyArray, "abc"),
                "no suitable functions",
                typeof(SimpleEntity).FullName,
                "string");
        }

        //===============================================

        class FilterQueryRepository : IRepository
        {
            public IQueryable<SimpleEntity> Filter(IQueryable<SimpleEntity> source, string parameter)
            {
                return new[] { new SimpleEntity { Name = "filter query" } }
                .AsQueryable().Where(item => true).Select(item => item);
            }
        }

        [TestMethod]
        public void FilterQuery()
        {
            var repos = NewRepos(new FilterQueryRepository());

            TestUtility.ShouldFail(() => repos.Filter(EmptyArray, Guid.NewGuid()),
                "no suitable functions",
                typeof(SimpleEntity).FullName,
                "System.Guid");

            Assert.AreEqual("filter query", repos.Filter(EmptyArray, "a").Single().Name);

            Assert.IsTrue(repos.Filter(EmptyArray, "a") is List<SimpleEntity>, "GenericRepository.Filter should always return materialized list instead of a query.");
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

        class QueryFilterAllRepository : IRepository
        {
            public IEnumerable<SimpleEntity> Filter(IQueryable<SimpleEntity> source, FilterAll parameter) { return new[] { new SimpleEntity { Name = "QueryFilterAll" } }; }
        }

        [TestMethod]
        public void FilterAll()
        {
            Assert.AreEqual("abc", NewRepos(new FilterAllRepository()).Filter(AbcArray, new FilterAll()).Single().Name);
            Assert.AreEqual("abc", NewRepos(new AllRepository()).Filter(AbcArray, new FilterAll()).Single().Name);
            Assert.AreEqual("QueryFilterAll", NewRepos(new QueryFilterAllRepository()).Filter(AbcArray, new FilterAll()).Single().Name);
        }

        //===============================================

        [TestMethod]
        public void FilterGenericFilter()
        {
            var repos = NewRepos(new NullRepository());

            var items = repos.Filter(
                new SimpleEntityList { "a1", "a2", "b1" },
                new[] { new FilterCriteria { Property = "Name", Operation = "startsWith", Value = "a" } });
            Assert.AreEqual("a1, a2", string.Join(", ", items.Select(item => item.Name)));
            Assert.IsTrue(items is List<SimpleEntity>, "GenericRepository.Filter should always return materialized list instead of a query.");
        }

        [TestMethod]
        public void FilterExpression()
        {
            var repos = NewRepos(new NullRepository());

            var items = repos.Filter(
                new SimpleEntityList { "a1", "a2", "b1" },
                (Expression<Func<ISimpleEntity, bool>>)(item => item.Name.StartsWith("a")));
            Assert.AreEqual("a1, a2", string.Join(", ", items.Select(item => item.Name)));
            Assert.IsTrue(items is List<SimpleEntity>, "GenericRepository.Filter should always return materialized list instead of a query.");

            items = repos.Filter(
                new SimpleEntityList { "a1", "a2", "b1" }.Where(item => true).Select(item => item),
                (Expression<Func<ISimpleEntity, bool>>)(item => item.Name.StartsWith("a")));
            Assert.AreEqual("a1, a2", string.Join(", ", items.Select(item => item.Name)));
            Assert.IsTrue(items is List<SimpleEntity>, "GenericRepository.Filter should always return materialized list instead of a query.");
        }

        [TestMethod]
        public void FilterFunction()
        {
            var repos = NewRepos(new NullRepository());

            var items = repos.Filter(
                new SimpleEntityList { "a1", "a2", "b1" },
                (Func<ISimpleEntity, bool>)(item => item.Name.StartsWith("a")));
            Assert.AreEqual("a1, a2", string.Join(", ", items.Select(item => item.Name)));
            Assert.IsTrue(items is List<SimpleEntity>, "GenericRepository.Filter should always return materialized list instead of a query.");

            items = repos.Filter(
                new SimpleEntityList { "a1", "a2", "b1" }.Where(item => true).Select(item => item),
                (Func<ISimpleEntity, bool>)(item => item.Name.StartsWith("a")));
            Assert.AreEqual("a1, a2", string.Join(", ", items.Select(item => item.Name)));
            Assert.IsTrue(items is List<SimpleEntity>, "GenericRepository.Filter should always return materialized list instead of a query.");
        }

        //===============================================

        class FilterObjectRepository : IRepository
        {
            public IEnumerable<SimpleEntity> Filter(IQueryable<SimpleEntity> source, object o) { return new[] { new SimpleEntity { Name = "o" } }; }
            public IEnumerable<SimpleEntity> Filter(IQueryable<SimpleEntity> source, string s) { return new[] { new SimpleEntity { Name = "s" } }; }

            public IEnumerable<SimpleEntity> Filter(IQueryable<SimpleEntity> source, IEnumerable<int> p) { return new[] { new SimpleEntity { Name = "ei" } }; }
            public IEnumerable<SimpleEntity> Filter(IQueryable<SimpleEntity> source, int[] p) { return new[] { new SimpleEntity { Name = "ai" } }; }
        }

        [TestMethod]
        public void FilterObject()
        {
            var repos = NewRepos(new FilterObjectRepository());

            Assert.AreEqual("o", repos.Filter(EmptyArray, new object()).Single().Name);
            Assert.AreEqual("o", repos.Filter<object>(EmptyArray, null).Single().Name);
            Assert.AreEqual("s", repos.Filter(EmptyArray, "abc").Single().Name);
            Assert.AreEqual("s", repos.Filter<string>(EmptyArray, null).Single().Name);
            Assert.AreEqual("o", repos.Filter<object>(EmptyArray, "abc").Single().Name);
            Assert.AreEqual("o", repos.Filter(EmptyArray, "abc", typeof(object)).Single().Name);
            Assert.AreEqual("s", repos.Filter(EmptyArray, "abc", "abc".GetType()).Single().Name);
            Assert.AreEqual("o", repos.Filter(EmptyArray, null, typeof(object)).Single().Name);
            Assert.AreEqual("s", repos.Filter(EmptyArray, null, typeof(string)).Single().Name);

            object o = new[] { 1, 2, 3 };
            Assert.AreEqual("ai", repos.Filter(EmptyArray, o, o.GetType()).Single().Name);
            Assert.AreEqual("ei", repos.Filter(EmptyArray, o, typeof(IEnumerable<int>)).Single().Name);
        }
    }
}
