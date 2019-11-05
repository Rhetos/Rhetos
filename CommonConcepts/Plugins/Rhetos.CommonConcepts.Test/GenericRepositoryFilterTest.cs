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
using Rhetos.CommonConcepts.Test.Mocks;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.TestCommon;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Rhetos.CommonConcepts.Test
{
    [TestClass]
    public class GenericRepositoryFilterTest
    {
        public interface ISimpleEntity : IEntity
        {
            string Name { get; set; }
        }

        public class SimpleEntity : ISimpleEntity
        {
            public Guid ID { get; set; }
            public string Name { get; set; }
            public string Data { get; set; }

            public override string ToString()
            {
                return Name != null ? Name : "<null>";
            }
        }

        class SimpleEntityList : List<SimpleEntity>
        {
            int idCounter = 0;
            public void Add(string name) { Add(new SimpleEntity { Name = name, ID = new Guid(idCounter++, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0) }); }
        }

        static readonly SimpleEntity[] EmptyArray = new SimpleEntity[] { };
        static readonly SimpleEntity[] AbcArray = new SimpleEntity[] { new SimpleEntity { Name = "abc" } };

        GenericRepository<ISimpleEntity> NewRepos(IRepository repository)
        {
            return new TestGenericRepository<ISimpleEntity, SimpleEntity>(repository);
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
                "does not implement",
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
                "does not implement",
                typeof(SimpleEntity).FullName,
                "System.Guid");

            Assert.AreEqual("filter query", repos.Filter(EmptyArray, "a").Single().Name);

            Assert.IsTrue(repos.Filter(EmptyArray, "a") is List<SimpleEntity>, "GenericRepository.Filter should always return materialized list instead of a query.");
        }

        class FilterEnumQueryRepository : IRepository
        {
            public IEnumerable<SimpleEntity> Filter(IEnumerable<SimpleEntity> source, string parameter)
            {
                return new[] { new SimpleEntity { Name = "filter enumerable " + ((source is IList) ? "on materialized source" : "") } }
                .AsQueryable().Where(item => true).Select(item => item);
            }

            public IQueryable<SimpleEntity> Filter(IQueryable<SimpleEntity> source, string parameter)
            {
                return new[] { new SimpleEntity { Name = "filter query " + ((source is IQueryable) ? "on queryable source" : "") } }
                .AsQueryable().Where(item => true).Select(item => item);
            }

            public IEnumerable<SimpleEntity> Filter(IEnumerable<SimpleEntity> source, int parameter)
            {
                return new[] { new SimpleEntity { Name = "filter enumerable " + ((source is IList) ? "on materialized source" : "") } }
                .AsQueryable().Where(item => true).Select(item => item);
            }

            public IQueryable<SimpleEntity> Filter(IQueryable<SimpleEntity> source, DateTime parameter)
            {
                return new[] { new SimpleEntity { Name = "filter query " + ((source is IQueryable) ? "on queryable source" : "") } }
                .AsQueryable().Where(item => true).Select(item => item);
            }
        }

        [TestMethod]
        public void FilterEnumQuery()
        {
            var repos = NewRepos(new FilterEnumQueryRepository());

            var enumSource = new SimpleEntity[] { };
            var querySource = new SimpleEntity[] { }.AsQueryable();

            // Both queryable and enumerable filters exist:
            Assert.AreEqual("filter enumerable on materialized source", repos.FilterOrQuery(enumSource, "a").Single().Name);
            Assert.AreEqual("filter query on queryable source", repos.FilterOrQuery(querySource, "a").Single().Name);

            // Only enumerable filter exists:
            Assert.AreEqual("filter enumerable on materialized source", repos.FilterOrQuery(enumSource, 1).Single().Name);
            Assert.AreEqual("filter enumerable on materialized source", repos.FilterOrQuery(querySource, 1).Single().Name);

            // Only queryable filter exists:
            Assert.AreEqual("filter query on queryable source", repos.FilterOrQuery(enumSource, DateTime.MinValue).Single().Name);
            Assert.AreEqual("filter query on queryable source", repos.FilterOrQuery(querySource, DateTime.MinValue).Single().Name);
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
            Assert.AreEqual("s", repos.Filter<object>(EmptyArray, "abc").Single().Name);
            Assert.AreEqual("o", repos.Filter(EmptyArray, "abc", typeof(object)).Single().Name);
            Assert.AreEqual("s", repos.Filter(EmptyArray, "abc", "abc".GetType()).Single().Name);
            Assert.AreEqual("o", repos.Filter(EmptyArray, null, typeof(object)).Single().Name);
            Assert.AreEqual("s", repos.Filter(EmptyArray, null, typeof(string)).Single().Name);

            object o = new[] { 1, 2, 3 };
            Assert.AreEqual("ai", repos.Filter(EmptyArray, o, o.GetType()).Single().Name);
            Assert.AreEqual("ei", repos.Filter(EmptyArray, o, typeof(IEnumerable<int>)).Single().Name);
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

        class ExplicitGenericPropertyFilterRepository : IRepository
        {
            public IEnumerable<SimpleEntity> Filter(IEnumerable<FilterCriteria> p) { return new[] { new SimpleEntity { Name = "exp" } }; }
        }

        class ExplicitGenericPropertyFilterRepository2 : IRepository
        {
            public IEnumerable<SimpleEntity> Filter(IQueryable<SimpleEntity> source, IEnumerable<FilterCriteria> p) { return new[] { new SimpleEntity { Name = "exp2" } }; }

            public IQueryable<SimpleEntity> Query()
            {
                return new[] { "a1", "b1", "b2" }.Select(s => new SimpleEntity { Name = s }).AsQueryable();
            }
        }

        class ImplicitGenericPropertyFilterRepository : IRepository
        {
            public int Counter = 0;

            public IQueryable<SimpleEntity> Query()
            {
                return new[] { "a1", "b1", "b2" }.Select(s => { Counter++; return new SimpleEntity { Name = s }; }).AsQueryable();
            }
        }

        [TestMethod]
        public void FilterGenericFilter2()
        {
            int sourceCounter = 0;
            var sourceQuery = new[] { "aa1", "bb1", "bb2" }.Select(s => { sourceCounter++; return new SimpleEntity { Name = s }; }).AsQueryable();
            var gf = new[] { new FilterCriteria { Property = "Name", Operation = "StartsWith", Value = "b" } };

            var impRepos = new ImplicitGenericPropertyFilterRepository();

            Assert.AreEqual(0, impRepos.Counter);
            Assert.AreEqual(0, sourceCounter);

            var exp = NewRepos(new ExplicitGenericPropertyFilterRepository()).Filter(sourceQuery, gf);
            var exp2 = NewRepos(new ExplicitGenericPropertyFilterRepository2()).Filter(sourceQuery, gf);
            var imp = NewRepos(impRepos).Filter(sourceQuery, gf);

            Assert.AreEqual(0, impRepos.Counter);
            Assert.AreEqual(6, sourceCounter);

            Assert.AreEqual("bb1, bb2", TestUtility.Dump(exp));
            Assert.AreEqual("exp2", TestUtility.Dump(exp2));
            Assert.AreEqual("bb1, bb2", TestUtility.Dump(imp));

            Assert.AreEqual(0, impRepos.Counter);
            Assert.AreEqual(6, sourceCounter);

            Assert.AreEqual("True, True, True", TestUtility.Dump(new object[] { exp, exp2, imp }.Select(o => o is IList)));
            Assert.AreEqual("False, False, False", TestUtility.Dump(new object[] { exp, exp2, imp }.Select(o => o is IQueryable)));

            Assert.AreEqual(0, impRepos.Counter);
            Assert.AreEqual(6, sourceCounter);

            exp = NewRepos(new ExplicitGenericPropertyFilterRepository()).FilterOrQuery(sourceQuery, gf);
            exp2 = NewRepos(new ExplicitGenericPropertyFilterRepository2()).FilterOrQuery(sourceQuery, gf);
            imp = NewRepos(impRepos).FilterOrQuery(sourceQuery, gf);

            Assert.AreEqual(0, impRepos.Counter);
            Assert.AreEqual(6, sourceCounter);

            Assert.AreEqual("bb1, bb2", TestUtility.Dump(exp));
            Assert.AreEqual("exp2", TestUtility.Dump(exp2));
            Assert.AreEqual("bb1, bb2", TestUtility.Dump(imp));

            Assert.AreEqual(0, impRepos.Counter);
            Assert.AreEqual(12, sourceCounter);

            Assert.AreEqual("False, True, False", TestUtility.Dump(new object[] { exp, exp2, imp }.Select(o => o is IList)));
            Assert.AreEqual("True, False, True", TestUtility.Dump(new object[] { exp, exp2, imp }.Select(o => o is IQueryable)));
        }

        public class NamedFilter { }

        public class ContainsFilter { public string Pattern { get; set; } }

        public class QueryLoaderFilter { }

        public class LoaderParameter { }

        public class EnumerableFilter { }

        public class GenericFilterRepository : IRepository
        {
            public GenericFilterRepository()
            {
            }
            public GenericFilterRepository(IEnumerable<SimpleEntity> items)
            {
                _items = items;
            }
            IEnumerable<SimpleEntity> _items = null;

            public List<string> _log = new List<string>();
            public string Log { get { return string.Join(", ", _log); } }

            void AddLog(string message)
            {
                _log.Add(message);
                Console.WriteLine(message);
            }

            public IQueryable<SimpleEntity> Query()
            {
                if (_items != null)
                    return _items.AsQueryable();

                AddLog("Q"); // Using query ...
                return new[] { 1 }.SelectMany(x =>
                {
                    AddLog("X"); // Query executed!
                    return new SimpleEntityList { "a1", "a2", "b1", "b2" };
                }).AsQueryable();
            }

            public IQueryable<SimpleEntity> Filter(IQueryable<SimpleEntity> items, NamedFilter parameter)
            {
                AddLog("QF"); // Using queryable filter ...
                return items.Where(i => i.Name.StartsWith("b"));
            }

            public IEnumerable<SimpleEntity> Filter(NamedFilter parameter)
            {
                AddLog("LP"); // Using loader with parameters ...
                return new SimpleEntityList { "f1", "f2" };
            }

            public IQueryable<SimpleEntity> Filter(IQueryable<SimpleEntity> items, ContainsFilter parameter)
            {
                AddLog("QF"); // Using queryable filter ...
                return items.Where(i => i.Name.Contains(parameter.Pattern));
            }

            public IEnumerable<SimpleEntity> Filter(QueryLoaderFilter parameter)
            {
                AddLog("LP"); // Using loader with parameters ...
                return new SimpleEntityList { "ql1", "ql2" };
            }

            public IQueryable<SimpleEntity> Query(QueryLoaderFilter parameter)
            {
                AddLog("QP"); // Using query with parameters ...
                return new SimpleEntityList { "ql1", "ql2" }.AsQueryable();
            }

            public IEnumerable<SimpleEntity> Filter(IEnumerable<SimpleEntity> items, QueryLoaderFilter parameter)
            {
                AddLog("EF"); // Using enumerable filter ...
                return items.Select(item => new SimpleEntity { Name = item.Name + "_ef" });
            }

            public IQueryable<SimpleEntity> Filter(IQueryable<SimpleEntity> items, QueryLoaderFilter parameter)
            {
                AddLog("QF"); // Using queryable filter ...
                return items.Select(item => new SimpleEntity { Name = item.Name + "_qf" });
            }

            public IEnumerable<SimpleEntity> Filter(LoaderParameter parameter)
            {
                AddLog("LP"); // Using loader with parameters ...
                return new SimpleEntityList { "lp1", "lp2" };
            }

            public IEnumerable<SimpleEntity> Filter(IEnumerable<SimpleEntity> items, EnumerableFilter parameter)
            {
                AddLog("EF"); // Using enumerable filter ...
                return items.Select(item => new SimpleEntity { Name = item.Name + "_ef" });
            }
        }

        string TestIListIQueryable(IEnumerable items)
        {
            var s = new List<string>();
            if (items is IList) s.Add("IL");
            if (items is IQueryable) s.Add("IQ");
            return string.Join("&", s);
        }

        [TestMethod]
        public void FilterGenericFilter_NonMaterialized()
        {
            var gf = new[] { new FilterCriteria { Property = "Name", Operation = "StartsWith", Value = "b" } };
            var entityRepos = new GenericFilterRepository();
            var genericRepos = NewRepos(entityRepos);

            Assert.AreEqual("", entityRepos.Log);

            var items = genericRepos.Filter(genericRepos.Query(), gf);
            Assert.AreEqual("Q, X", entityRepos.Log);

            Assert.AreEqual("b1, b2", TestUtility.Dump(items));
            Assert.AreEqual("IL", TestIListIQueryable(items));
            Assert.AreEqual("Q, X", entityRepos.Log);

            items = genericRepos.FilterOrQuery(genericRepos.Query(), gf);
            Assert.AreEqual("Q, X, Q", entityRepos.Log);

            Assert.AreEqual("b1, b2", TestUtility.Dump(items));
            Assert.AreEqual("Q, X, Q, X", entityRepos.Log);

            Assert.AreEqual("b1, b2", TestUtility.Dump(items));
            Assert.AreEqual("Q, X, Q, X, X", entityRepos.Log);

            Assert.AreEqual("IQ", TestIListIQueryable(items));
            Assert.AreEqual("Q, X, Q, X, X", entityRepos.Log);
        }

        [TestMethod]
        public void FilterGenericFilter_Empty()
        {
            var gf = new FilterCriteria[] { };
            var entityRepos = new GenericFilterRepository();
            var genericRepos = NewRepos(entityRepos);

            Assert.AreEqual("", entityRepos.Log);

            var items = genericRepos.Filter(genericRepos.Query(), gf);
            Assert.AreEqual("Q, X", entityRepos.Log);

            Assert.AreEqual("a1, a2, b1, b2", TestUtility.Dump(items));
            Assert.AreEqual("IL", TestIListIQueryable(items));
            Assert.AreEqual("Q, X", entityRepos.Log);

            items = genericRepos.FilterOrQuery(genericRepos.Query(), gf);
            Assert.AreEqual("Q, X, Q", entityRepos.Log);

            Assert.AreEqual("a1, a2, b1, b2", TestUtility.Dump(items));
            Assert.AreEqual("Q, X, Q, X", entityRepos.Log);

            Assert.AreEqual("IQ", TestIListIQueryable(items));
            Assert.AreEqual("Q, X, Q, X", entityRepos.Log);
        }

        [TestMethod]
        public void FilterGenericFilter_NamedFilter()
        {
            var gf = new FilterCriteria[] { new FilterCriteria { Filter = typeof(NamedFilter).FullName } };
            var entityRepos = new GenericFilterRepository();
            var genericRepos = NewRepos(entityRepos);

            Assert.AreEqual("", entityRepos.Log);

            var items = genericRepos.Filter(genericRepos.Query(), gf);
            Assert.AreEqual("IL", TestIListIQueryable(items));
            Assert.AreEqual("Q, QF, X", entityRepos.Log);
            Assert.AreEqual("b1, b2", TestUtility.Dump(items));
            Assert.AreEqual("Q, QF, X", entityRepos.Log);

            items = genericRepos.FilterOrQuery(genericRepos.Query(), gf);
            Assert.AreEqual("IQ", TestIListIQueryable(items));
            Assert.AreEqual("Q, QF, X, Q, QF", entityRepos.Log);
            Assert.AreEqual("b1, b2", TestUtility.Dump(items));
            Assert.AreEqual("Q, QF, X, Q, QF, X", entityRepos.Log);
        }

        [TestMethod]
        public void FilterGenericFilter_FilterWithParameters()
        {
            var gf = new FilterCriteria[] { new FilterCriteria { Filter = typeof(ContainsFilter).FullName, Value = new ContainsFilter { Pattern = "a" } } };
            var entityRepos = new GenericFilterRepository();
            var genericRepos = NewRepos(entityRepos);

            Assert.AreEqual("", entityRepos.Log);

            var items = genericRepos.Filter(genericRepos.Query(), gf);
            Assert.AreEqual("IL", TestIListIQueryable(items));
            Assert.AreEqual("Q, QF, X", entityRepos.Log);
            Assert.AreEqual("a1, a2", TestUtility.Dump(items));
            Assert.AreEqual("Q, QF, X", entityRepos.Log);

            items = genericRepos.FilterOrQuery(genericRepos.Query(), gf);
            Assert.AreEqual("IQ", TestIListIQueryable(items));
            Assert.AreEqual("Q, QF, X, Q, QF", entityRepos.Log);
            Assert.AreEqual("a1, a2", TestUtility.Dump(items));
            Assert.AreEqual("Q, QF, X, Q, QF, X", entityRepos.Log);
        }

        string TypeAndNames(IEnumerable<ISimpleEntity> items)
        {
            return TestIListIQueryable(items) + ": " + TestUtility.Dump(items);
        }

        [TestMethod]
        public void FilterGenericFilter_CombinedFilters()
        {
            var entityRepos = new GenericFilterRepository();
            var genericRepos = NewRepos(entityRepos);

            Assert.AreEqual("IL: a1", TypeAndNames(genericRepos.Filter(genericRepos.Query(), new[] {
                new FilterCriteria { Filter = typeof(ContainsFilter).FullName, Value = new ContainsFilter { Pattern = "a" } },
                new FilterCriteria { Property = "Name", Operation = "Contains", Value = "1" } })));
            Assert.AreEqual("Q, QF, X", entityRepos.Log); entityRepos._log.Clear();

            Assert.AreEqual("IL: b1", TypeAndNames(genericRepos.Filter(genericRepos.Query(), new[] {
                new FilterCriteria { Filter = typeof(NamedFilter).FullName },
                new FilterCriteria { Property = "Name", Operation = "Contains", Value = "1" } })));
            Assert.AreEqual("Q, QF, X", entityRepos.Log); entityRepos._log.Clear();

            Assert.AreEqual("IL: b1, b2", TypeAndNames(genericRepos.Filter(genericRepos.Query().ToList(), new[] {
                new FilterCriteria { Filter = typeof(NamedFilter).FullName } })));
            Assert.AreEqual("Q, X, QF", entityRepos.Log); entityRepos._log.Clear();

            Assert.AreEqual("IL: b2", TypeAndNames(genericRepos.Filter(genericRepos.Query(), new[] {
                new FilterCriteria { Filter = typeof(NamedFilter).FullName },
                new FilterCriteria { Filter = typeof(ContainsFilter).FullName, Value = new ContainsFilter { Pattern = "2" } } })));
            Assert.AreEqual("Q, QF, QF, X", entityRepos.Log); entityRepos._log.Clear();

            Assert.AreEqual("IL: b2", TypeAndNames(genericRepos.Filter(genericRepos.Query(), new[] {
                new FilterCriteria { Filter = typeof(NamedFilter).FullName },
                new FilterCriteria { Filter = typeof(ContainsFilter).FullName, Value = new ContainsFilter { Pattern = "2" } },
                new FilterCriteria { Property = "Name", Operation = "Contains", Value = "2" } })));
            Assert.AreEqual("Q, QF, QF, X", entityRepos.Log); entityRepos._log.Clear();

            Assert.AreEqual("IL: ", TypeAndNames(genericRepos.Filter(genericRepos.Query(), new[] {
                new FilterCriteria { Filter = typeof(NamedFilter).FullName },
                new FilterCriteria { Filter = typeof(ContainsFilter).FullName, Value = new ContainsFilter { Pattern = "2" } },
                new FilterCriteria { Property = "Name", Operation = "Contains", Value = "1" } })));
            Assert.AreEqual("Q, QF, QF, X", entityRepos.Log); entityRepos._log.Clear();
        }

        [TestMethod]
        public void FilterGenericFilter_QueryVsEnum()
        {
            var entityRepos = new GenericFilterRepository();
            var genericRepos = NewRepos(entityRepos);

            Assert.AreEqual("IQ: a1_qf, a2_qf, b1_qf, b2_qf", TypeAndNames(genericRepos.FilterOrQuery(genericRepos.Query(), new[] {
                new FilterCriteria { Filter = typeof(QueryLoaderFilter).FullName } })));
            Assert.AreEqual("Q, QF, X", entityRepos.Log); entityRepos._log.Clear();

            Assert.AreEqual("IL: a1_qf, a2_qf, b1_qf, b2_qf", TypeAndNames(genericRepos.Filter(genericRepos.Query(), new[] {
                new FilterCriteria { Filter = typeof(QueryLoaderFilter).FullName } })));
            Assert.AreEqual("Q, QF, X", entityRepos.Log); entityRepos._log.Clear();

            // Prefere queryable filter version of QueryLoaderFilter on queryable
            Assert.AreEqual("IL: a1_qf, b1_qf", TypeAndNames(genericRepos.Filter(genericRepos.Query(), new[] {
                new FilterCriteria { Filter = typeof(QueryLoaderFilter).FullName },
                new FilterCriteria { Property = "Name", Operation = "Contains", Value = "1" } })));
            Assert.AreEqual("Q, QF, X", entityRepos.Log); entityRepos._log.Clear();

            Assert.AreEqual("IL: a1_qf, b1_qf", TypeAndNames(genericRepos.Filter(genericRepos.Query(), new[] {
                new FilterCriteria { Property = "Name", Operation = "Contains", Value = "1" },
                new FilterCriteria { Filter = typeof(QueryLoaderFilter).FullName }, })));
            Assert.AreEqual("Q, QF, X", entityRepos.Log); entityRepos._log.Clear();

            // Enumerable filter on queryable:
            // Prefere enumerable filter version of QueryLoaderFilter on queryable
            Assert.AreEqual("IL: a1_ef_ef, a2_ef_ef, b1_ef_ef, b2_ef_ef", TypeAndNames(genericRepos.Filter(genericRepos.Query(), new[] {
                new FilterCriteria { Filter = typeof(EnumerableFilter).FullName },
                new FilterCriteria { Filter = typeof(QueryLoaderFilter).FullName }, })));
            Assert.AreEqual("Q, X, EF, EF", entityRepos.Log); entityRepos._log.Clear();

            // Queryable filter (property filter) on enumerable filtered items:
            Assert.AreEqual("IL: a1_ef_ef, b1_ef_ef", TypeAndNames(genericRepos.Filter(genericRepos.Query(), new[] {
                new FilterCriteria { Filter = typeof(EnumerableFilter).FullName },
                new FilterCriteria { Filter = typeof(QueryLoaderFilter).FullName },
                new FilterCriteria { Property = "Name", Operation = "Contains", Value = "1" } })));
            Assert.AreEqual("Q, X, EF, EF", entityRepos.Log); entityRepos._log.Clear();

            // Trying to use a loader for filtering
            TestUtility.ShouldFail(() => genericRepos.Filter(genericRepos.Query(), new[] {
                new FilterCriteria { Filter = typeof(LoaderParameter).FullName },
                new FilterCriteria { Filter = typeof(QueryLoaderFilter).FullName } }),
                "SimpleEntity", "does not implement a filter", "LoaderParameter");
            entityRepos._log.Clear();

            Assert.AreEqual("IL: a1_ef, a2_ef, b1_ef, b2_ef", TypeAndNames(genericRepos.Filter(genericRepos.Query(), new[] {
                new FilterCriteria { Filter = typeof(EnumerableFilter).FullName }, })));
            Assert.AreEqual("Q, X, EF", entityRepos.Log); entityRepos._log.Clear();

            Assert.AreEqual("IL: a1_qf_ef, a2_qf_ef, b1_qf_ef, b2_qf_ef", TypeAndNames(genericRepos.Filter(genericRepos.Query(), new[] {
                new FilterCriteria { Filter = typeof(QueryLoaderFilter).FullName },
                new FilterCriteria { Filter = typeof(EnumerableFilter).FullName }, })));
            Assert.AreEqual("Q, QF, X, EF", entityRepos.Log); entityRepos._log.Clear();
        }

        [TestMethod]
        public void FilterGenericFilter_NotMatches()
        {
            Expression<Func<SimpleEntity, bool>> predicate = item => item.Name.StartsWith("a");

            var entityRepos = new GenericFilterRepository();
            var genericRepos = NewRepos(entityRepos);

            Assert.AreEqual("IQ: a1, a2", TypeAndNames(genericRepos.FilterOrQuery(genericRepos.Query(),
                new[] { new FilterCriteria { Filter = predicate.GetType().AssemblyQualifiedName, Operation = "Matches", Value = predicate } })));
            Assert.AreEqual("Q, X", entityRepos.Log); entityRepos._log.Clear();

            Assert.AreEqual("IQ: b1, b2", TypeAndNames(genericRepos.FilterOrQuery(genericRepos.Query(),
                new[] { new FilterCriteria { Filter = predicate.GetType().AssemblyQualifiedName, Operation = "NotMatches", Value = predicate } })));
            Assert.AreEqual("Q, X", entityRepos.Log); entityRepos._log.Clear();
        }

        [TestMethod]
        public void FilterGenericFilter_NotLimitedToInterface()
        {
            var items = new[] {
                new SimpleEntity { Name = "1", Data = "ax" },
                new SimpleEntity { Name = "2", Data = "ay" },
                new SimpleEntity { Name = "3", Data = "bx" },
                new SimpleEntity { Name = "4", Data = "by" },
                new SimpleEntity { Name = "11", Data = "ax0" },
                new SimpleEntity { Name = "12", Data = "ay0" },
                new SimpleEntity { Name = "13", Data = "bx0" },
                new SimpleEntity { Name = "14", Data = "by0" },
            };
            var entityRepos = new GenericFilterRepository(items);
            var genericRepos = NewRepos(entityRepos);

            Expression<Func<SimpleEntity, bool>> expressionFilter = item => item.Data.Contains("y");
            var propertyFilter = new FilterCriteria { Property = "Data", Operation = "Contains", Value = "a" };

            var genericFilter = new[] { propertyFilter, new FilterCriteria { Filter = expressionFilter.GetType().AssemblyQualifiedName, Value = expressionFilter } };

            Assert.AreEqual("IQ: 2, 12", TypeAndNames(genericRepos.FilterOrQuery(genericRepos.Query(), genericFilter)));
        }

        class SystemFilterRepository : IRepository
        {
            public IQueryable<SimpleEntity> Query()
            {
                return new SimpleEntityList { "a", "b" }.AsQueryable();
            }

            public IQueryable<SimpleEntity> Filter(IQueryable<SimpleEntity> items, string parameter)
            {
                return items;
            }

            public IQueryable<SimpleEntity> Filter(IQueryable<SimpleEntity> items, DateTime parameter)
            {
                return new[] { new SimpleEntity { Name = parameter.ToString("s") } }.AsQueryable();
            }

            public IEnumerable<SimpleEntity> Filter(IEnumerable<SimpleEntity> items, Guid parameter)
            {
                return new[] { new SimpleEntity { Name = parameter.ToString() } }.AsQueryable();
            }
        }

        [TestMethod]
        public void FilterGenericFilter_Errors()
        {
            var entityRepos = new SystemFilterRepository();
            var genericRepos = NewRepos(entityRepos);

            TestUtility.ShouldFail(() => genericRepos.Filter(genericRepos.Query(), new[] { new FilterCriteria { Operation = "x" } }), "both property filter and predefined filter are null");
            TestUtility.ShouldFail(() => genericRepos.Filter(genericRepos.Query(), new[] { new FilterCriteria { Value = "x" } }), "both property filter and predefined filter are null");
            TestUtility.ShouldFail(() => genericRepos.Filter(genericRepos.Query(), new[] { new FilterCriteria { } }), "both property filter and predefined filter are null");
            TestUtility.ShouldFail(() => genericRepos.Filter(genericRepos.Query(), new[] { new FilterCriteria { Filter = "xx", Property = "yy" } }), "both property filter and predefined filter are set", "xx", "yy");

            TestUtility.ShouldFail(() => genericRepos.Filter(genericRepos.Query(), new[] { new FilterCriteria { Filter = "System.String", Operation = "xxx" } }), "Filter", "System.String", "Operation", "xxx", "Matches", "NotMatches");
            TestUtility.ShouldFail(() => genericRepos.Filter(genericRepos.Query(), new[] { new FilterCriteria { Filter = "System.String", Value = 123 } }), "System.String", "System.Int32");
            TestUtility.ShouldFail(() => genericRepos.Filter(genericRepos.Query(), new[] { new FilterCriteria { Property = "Name", Operation = "xxx" } }), "Operation", "xxx");
            TestUtility.ShouldFail(() => genericRepos.Filter(genericRepos.Query(), new[] { new FilterCriteria { Property = "Name", Value = "xxx" } }), "Property", "Name", "Operation");
        }

        [TestMethod]
        public void FilterGenericFilter_Defaults()
        {
            var entityRepos = new SystemFilterRepository();
            var genericRepos = NewRepos(entityRepos);

            Assert.AreEqual("0001-01-01T00:00:00", genericRepos
                .Filter(genericRepos.Query(), new[] { new FilterCriteria { Filter = "System.DateTime" } })
                .Single().Name);
            Assert.AreEqual("00000000-0000-0000-0000-000000000000", genericRepos
                .Filter(genericRepos.Query(), new[] { new FilterCriteria { Filter = "System.Guid" } })
                .Single().Name);
        }

        [TestMethod]
        public void FilterOperationWithID()
        {
            List<SimpleEntity> temp = new List<SimpleEntity>();
            temp.Add(new SimpleEntity { ID = new Guid("DA04CA2A-7AA0-4827-8F71-05047B0FB201"), Data = "a", Name = "1" });
            temp.Add(new SimpleEntity { ID = new Guid("85ECBC94-2A9D-457C-934B-1117A7D72834"), Data = "b", Name = "2" });
            temp.Add(new SimpleEntity { ID = new Guid("F440440A-993F-40B8-BE27-2ECA04FBE191"), Data = "c", Name = "3" });
            temp.Add(new SimpleEntity { ID = new Guid("723FB608-052F-4696-A1D1-8A12BC19E69F"), Data = "d", Name = "4" });

            var entityRepos = new GenericFilterRepository(temp);
            var genericRepos = NewRepos(entityRepos);
            
            var gf_Greater = new[] { new FilterCriteria { Property = "ID", Operation = "greater", Value = "85ECBC94-2A9D-457C-934B-1117A7D72834" } };

            var gf_GreaterEqual = new[] { new FilterCriteria { Property = "ID", Operation = "greaterequal", Value = "85ECBC94-2A9D-457C-934B-1117A7D72834" } };

            var gf_Less = new[] { new FilterCriteria { Property = "ID", Operation = "less", Value = "85ECBC94-2A9D-457C-934B-1117A7D72834" } };

            var gf_LessEqual = new[] { new FilterCriteria { Property = "ID", Operation = "lessequal", Value = "85ECBC94-2A9D-457C-934B-1117A7D72834" } };

            string resultGreater = TypeAndNames(genericRepos.Filter(genericRepos.Query(), gf_Greater));
            string resultGreaterEqual = TypeAndNames(genericRepos.Filter(genericRepos.Query(), gf_GreaterEqual));
            string resultLess = TypeAndNames(genericRepos.Filter(genericRepos.Query(), gf_Less));
            string resultLessEqual = TypeAndNames(genericRepos.Filter(genericRepos.Query(), gf_LessEqual));

            Assert.AreEqual("IL: 1, 3", resultGreater);
            Assert.AreEqual("IL: 1, 2, 3", resultGreaterEqual);
            Assert.AreEqual("IL: 4", resultLess);
            Assert.AreEqual("IL: 2, 4", resultLessEqual);
        }
    }
}
