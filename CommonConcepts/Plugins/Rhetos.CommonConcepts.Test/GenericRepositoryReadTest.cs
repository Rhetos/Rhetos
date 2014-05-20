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
using Rhetos.Processing.DefaultCommands;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Rhetos.CommonConcepts.Test
{
    static class LegacyExtension
    {
        public static QueryDataSourceCommandResult ExecuteQueryDataSourceCommand<T>(
            this GenericRepository<T> genericRepository,
            QueryDataSourceCommandInfo queryDataSourceCommandInfo)
            where T : class, IEntity
        {
            var commandInfo = queryDataSourceCommandInfo.ToReadCommandInfo();
            var commandResult = genericRepository.ExecuteReadCommand(commandInfo);
            return QueryDataSourceCommandResult.FromReadCommandResult(commandResult);
        }
    }

    [TestClass]
    public class GenericRepositoryReadTest
    {
        interface ISimpleEntity : IEntity
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

        GenericRepository<ISimpleEntity> NewRepos(IRepository repository)
        {
            return new GenericRepository<ISimpleEntity>(
                new DomainObjectModelMock(),
                new Lazy<IIndex<string, IRepository>>(() => new RepositoryIndexMock(typeof(SimpleEntity), repository)),
                new RegisteredInterfaceImplementationsMock(typeof(ISimpleEntity), typeof(SimpleEntity)),
                new ConsoleLogProvider(),
                null,
                new GenericFilterHelper(new DomainObjectModelMock()));
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
                "does not implement",
                typeof(SimpleEntity).FullName,
                typeof(FilterAll).FullName);

            TestUtility.ShouldFail(() => repos.Read(new[] { Guid.NewGuid() }),
                "does not implement",
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
                "does not implement",
                typeof(SimpleEntity).FullName,
                typeof(FilterAll).FullName);

            TestUtility.ShouldFail(() => repos.Read(Guid.NewGuid()),
                "does not implement",
                typeof(SimpleEntity).FullName,
                "System.Guid");

            TestUtility.ShouldFail(() => repos.Read("a"), // Filter with a string parameter is a *private* function.
                "does not implement",
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
                "does not implement",
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
            public IQueryable<SimpleEntity> Query() { return new[] { new SimpleEntity { Name = "Query" } }.AsQueryable(); }
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

        [TestMethod]
        public void ReadFilterAll_PreferQuery()
        {
            var repos = NewRepos(new FilterAllRepository());
            Assert.AreEqual("FilterAll", repos.ReadNonMaterialized(new FilterAll(), preferQuery: false).Single().Name);
            Assert.AreEqual("Query", repos.ReadNonMaterialized(new FilterAll(), preferQuery: true).Single().Name);
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

        //===============================================

        [TestMethod]
        public void ReadFilterGenericFilter()
        {
            var repos = NewRepos(new SimpleQueryRepository());

            var items = repos.Read(new[] { new FilterCriteria { Property = "Name", Operation = "startsWith", Value = "a" } });
            Assert.AreEqual("a1, a2", string.Join(", ", items.Select(item => item.Name)));
            Assert.IsTrue(items is List<SimpleEntity>, "GenericRepository.Read should always return materialized list instead of a query.");
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
        public void ReadGenericFilter2()
        {
            var gf = new[] { new FilterCriteria { Property = "Name", Operation = "StartsWith", Value = "b" } };

            var impRepos = new ImplicitGenericPropertyFilterRepository();

            Assert.AreEqual(0, impRepos.Counter);

            var exp = NewRepos(new ExplicitGenericPropertyFilterRepository()).Read(gf);
            var exp2 = NewRepos(new ExplicitGenericPropertyFilterRepository2()).Read(gf);
            var imp = NewRepos(impRepos).Read(gf);

            Assert.AreEqual(3, impRepos.Counter);

            Assert.AreEqual("exp", TestUtility.Dump(exp));
            Assert.AreEqual("exp2", TestUtility.Dump(exp2));
            Assert.AreEqual("b1, b2", TestUtility.Dump(imp));

            Assert.AreEqual(3, impRepos.Counter);

            Assert.AreEqual("True, True, True", TestUtility.Dump(new object[] { exp, exp2, imp }.Select(o => o is IList)));
            Assert.AreEqual("False, False, False", TestUtility.Dump(new object[] { exp, exp2, imp }.Select(o => o is IQueryable)));

            Assert.AreEqual(3, impRepos.Counter);

            exp = NewRepos(new ExplicitGenericPropertyFilterRepository()).ReadNonMaterialized(gf, false);
            exp2 = NewRepos(new ExplicitGenericPropertyFilterRepository2()).ReadNonMaterialized(gf, false);
            imp = NewRepos(impRepos).ReadNonMaterialized(gf, false);

            Assert.AreEqual(3, impRepos.Counter);

            Assert.AreEqual("exp", TestUtility.Dump(exp));
            Assert.AreEqual("exp2", TestUtility.Dump(exp2));
            Assert.AreEqual("b1, b2", TestUtility.Dump(imp));

            Assert.AreEqual(6, impRepos.Counter);

            Assert.AreEqual("True, True, False", TestUtility.Dump(new object[] { exp, exp2, imp }.Select(o => o is IList)));
            Assert.AreEqual("False, False, True", TestUtility.Dump(new object[] { exp, exp2, imp }.Select(o => o is IQueryable)));

            TestUtility.ShouldFail(() => NewRepos(new NullRepository()).Read(gf), "SimpleEntity", "does not implement", "Expression");
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
        public void ReadGenericFilter_NonMaterialized()
        {
            var gf = new[] { new FilterCriteria { Property = "Name", Operation = "StartsWith", Value = "b" } };
            var entityRepos = new GenericFilterRepository();
            var genericRepos = NewRepos(entityRepos);

            Assert.AreEqual("", entityRepos.Log);

            var items = genericRepos.Read(gf);
            Assert.AreEqual("Q, X", entityRepos.Log);

            Assert.AreEqual("b1, b2", TestUtility.Dump(items));
            Assert.AreEqual("IL", TestIListIQueryable(items));
            Assert.AreEqual("Q, X", entityRepos.Log);

            items = genericRepos.ReadNonMaterialized(gf, true);
            Assert.AreEqual("Q, X, Q", entityRepos.Log);

            Assert.AreEqual("b1, b2", TestUtility.Dump(items));
            Assert.AreEqual("Q, X, Q, X", entityRepos.Log);

            Assert.AreEqual("b1, b2", TestUtility.Dump(items));
            Assert.AreEqual("Q, X, Q, X, X", entityRepos.Log);

            Assert.AreEqual("IQ", TestIListIQueryable(items));
            Assert.AreEqual("Q, X, Q, X, X", entityRepos.Log);
        }

        [TestMethod]
        public void ReadGenericFilter_Empty()
        {
            var gf = new FilterCriteria[] {};
            var entityRepos = new GenericFilterRepository();
            var genericRepos = NewRepos(entityRepos);

            Assert.AreEqual("", entityRepos.Log);

            var items = genericRepos.Read(gf);
            Assert.AreEqual("Q, X", entityRepos.Log);

            Assert.AreEqual("a1, a2, b1, b2", TestUtility.Dump(items));
            Assert.AreEqual("IL", TestIListIQueryable(items));
            Assert.AreEqual("Q, X", entityRepos.Log);

            items = genericRepos.ReadNonMaterialized(gf, true);
            Assert.AreEqual("Q, X, Q", entityRepos.Log);

            Assert.AreEqual("a1, a2, b1, b2", TestUtility.Dump(items));
            Assert.AreEqual("Q, X, Q, X", entityRepos.Log);

            Assert.AreEqual("IQ", TestIListIQueryable(items));
            Assert.AreEqual("Q, X, Q, X", entityRepos.Log);
        }

        [TestMethod]
        public void ReadGenericFilter_NamedFilter()
        {
            var gf = new FilterCriteria[] { new FilterCriteria { Filter = typeof(NamedFilter).FullName } };
            var entityRepos = new GenericFilterRepository();
            var genericRepos = NewRepos(entityRepos);

            Assert.AreEqual("", entityRepos.Log);

            var items = genericRepos.Read(gf);
            Assert.AreEqual("IL", TestIListIQueryable(items));
            Assert.AreEqual("LP", entityRepos.Log);
            Assert.AreEqual("f1, f2", TestUtility.Dump(items));
            Assert.AreEqual("LP", entityRepos.Log);

            items = genericRepos.ReadNonMaterialized(gf, preferQuery: true);
            Assert.AreEqual("IQ", TestIListIQueryable(items));
            Assert.AreEqual("LP, Q, QF", entityRepos.Log);
            Assert.AreEqual("b1, b2", TestUtility.Dump(items));
            Assert.AreEqual("LP, Q, QF, X", entityRepos.Log);
        }

        [TestMethod]
        public void ReadGenericFilter_FilterWithParameters()
        {
            var gf = new FilterCriteria[] { new FilterCriteria { Filter = typeof(ContainsFilter).FullName, Value = new ContainsFilter { Pattern = "a" } } };
            var entityRepos = new GenericFilterRepository();
            var genericRepos = NewRepos(entityRepos);

            Assert.AreEqual("", entityRepos.Log);

            var items = genericRepos.Read(gf);
            Assert.AreEqual("IL", TestIListIQueryable(items));
            Assert.AreEqual("Q, QF, X", entityRepos.Log);
            Assert.AreEqual("a1, a2", TestUtility.Dump(items));
            Assert.AreEqual("Q, QF, X", entityRepos.Log);

            items = genericRepos.ReadNonMaterialized(gf, preferQuery: true);
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
        public void ReadGenericFilter_CombinedFilters()
        {
            var entityRepos = new GenericFilterRepository();
            var genericRepos = NewRepos(entityRepos);

            Assert.AreEqual("IL: a1", TypeAndNames(genericRepos.Read(new[] {
                new FilterCriteria { Filter = typeof(ContainsFilter).FullName, Value = new ContainsFilter { Pattern = "a" } },
                new FilterCriteria { Property = "Name", Operation = "Contains", Value = "1" } })));
            Assert.AreEqual("Q, QF, X", entityRepos.Log); entityRepos._log.Clear();

            Assert.AreEqual("IL: b1", TypeAndNames(genericRepos.Read(new[] {
                new FilterCriteria { Filter = typeof(NamedFilter).FullName },
                new FilterCriteria { Property = "Name", Operation = "Contains", Value = "1" } })));
            Assert.AreEqual("Q, QF, X", entityRepos.Log); entityRepos._log.Clear();

            Assert.AreEqual("IL: f1, f2", TypeAndNames(genericRepos.Read(new[] {
                new FilterCriteria { Filter = typeof(NamedFilter).FullName } })), "Should use enumerable loader (instead of queryable) if only one filter is applied.");
            Assert.AreEqual("LP", entityRepos.Log); entityRepos._log.Clear();

            Assert.AreEqual("IL: b2", TypeAndNames(genericRepos.Read(new[] {
                new FilterCriteria { Filter = typeof(NamedFilter).FullName },
                new FilterCriteria { Filter = typeof(ContainsFilter).FullName, Value = new ContainsFilter { Pattern = "2" } } })));
            Assert.AreEqual("Q, QF, QF, X", entityRepos.Log); entityRepos._log.Clear();

            Assert.AreEqual("IL: b2", TypeAndNames(genericRepos.Read(new[] {
                new FilterCriteria { Filter = typeof(NamedFilter).FullName },
                new FilterCriteria { Filter = typeof(ContainsFilter).FullName, Value = new ContainsFilter { Pattern = "2" } },
                new FilterCriteria { Property = "Name", Operation = "Contains", Value = "2" } })));
            Assert.AreEqual("Q, QF, QF, X", entityRepos.Log); entityRepos._log.Clear();

            Assert.AreEqual("IL: ", TypeAndNames(genericRepos.Read(new[] {
                new FilterCriteria { Filter = typeof(NamedFilter).FullName },
                new FilterCriteria { Filter = typeof(ContainsFilter).FullName, Value = new ContainsFilter { Pattern = "2" } },
                new FilterCriteria { Property = "Name", Operation = "Contains", Value = "1" } })));
            Assert.AreEqual("Q, QF, QF, X", entityRepos.Log); entityRepos._log.Clear();
        }

        [TestMethod]
        public void ReadGenericFilter_QueryVsEnum()
        {
            var entityRepos = new GenericFilterRepository();
            var genericRepos = NewRepos(entityRepos);

            Assert.AreEqual("IQ: ql1, ql2", TypeAndNames(genericRepos.ReadNonMaterialized(new[] {
                new FilterCriteria { Filter = typeof(QueryLoaderFilter).FullName } }, preferQuery: true)));
            Assert.AreEqual("QP", entityRepos.Log); entityRepos._log.Clear();

            // For a single filter, loader is prefered:
            Assert.AreEqual("IL: ql1, ql2", TypeAndNames(genericRepos.Read(new[] {
                new FilterCriteria { Filter = typeof(QueryLoaderFilter).FullName } })));
            Assert.AreEqual("LP", entityRepos.Log); entityRepos._log.Clear();

            // For multiple filters, queryable loader (QP) is prefered:
            Assert.AreEqual("IL: ql1", TypeAndNames(genericRepos.Read(new[] {
                new FilterCriteria { Filter = typeof(QueryLoaderFilter).FullName },
                new FilterCriteria { Property = "Name", Operation = "Contains", Value = "1" } })));
            Assert.AreEqual("QP", entityRepos.Log); entityRepos._log.Clear();

            // Using given order of filters. There is no loader for property filter (expression).
            Assert.AreEqual("IL: a1_qf, b1_qf", TypeAndNames(genericRepos.Read(new[] {
                new FilterCriteria { Property = "Name", Operation = "Contains", Value = "1" },
                new FilterCriteria { Filter = typeof(QueryLoaderFilter).FullName }, })));
            Assert.AreEqual("Q, QF, X", entityRepos.Log); entityRepos._log.Clear();

            // When only enumerable loader is available, use enumerable filter:
            Assert.AreEqual("IL: lp1_ef, lp2_ef", TypeAndNames(genericRepos.Read(new[] {
                new FilterCriteria { Filter = typeof(LoaderParameter).FullName },
                new FilterCriteria { Filter = typeof(QueryLoaderFilter).FullName }, })));
            Assert.AreEqual("LP, EF", entityRepos.Log); entityRepos._log.Clear();

            // Trying to use queryable filter (property filter) on enumerable filtered items:
            Assert.AreEqual("IL: lp1_ef", TypeAndNames(genericRepos.Read(new[] {
                new FilterCriteria { Filter = typeof(LoaderParameter).FullName },
                new FilterCriteria { Filter = typeof(QueryLoaderFilter).FullName },
                new FilterCriteria { Property = "Name", Operation = "Contains", Value = "1" } })), "This is not a required feature. Property filter (as navigable queryable filter) does not need to work (and maybe it shouldn't) on materialized lists!");
            Assert.AreEqual("LP, EF", entityRepos.Log); entityRepos._log.Clear();

            // Trying to use a loader after the first position in generic filter should fail.
            // REMOVE THIS TEST after (if ever) automatic FilterCriteria reordering is implemented.
            TestUtility.ShouldFail(() => TypeAndNames(genericRepos.Read(new[] {
                new FilterCriteria { Filter = typeof(QueryLoaderFilter).FullName },
                new FilterCriteria { Filter = typeof(LoaderParameter).FullName }, })),
                "SimpleEntity", "does not implement a filter", "LoaderParameter");
            entityRepos._log.Clear();

            // Enumerable filter after enumerable loader
            Assert.AreEqual("IL: lp1_ef, lp2_ef", TypeAndNames(genericRepos.Read(new[] {
                new FilterCriteria { Filter = typeof(LoaderParameter).FullName },
                new FilterCriteria { Filter = typeof(EnumerableFilter).FullName }, })));
            Assert.AreEqual("LP, EF", entityRepos.Log); entityRepos._log.Clear();

            // Enumerable filter without a parametrized loader (inefficient)
            Assert.AreEqual("IL: a1_ef, a2_ef, b1_ef, b2_ef", TypeAndNames(genericRepos.Read(new[] {
                new FilterCriteria { Filter = typeof(EnumerableFilter).FullName }, })));
            Assert.AreEqual("Q, X, EF", entityRepos.Log); entityRepos._log.Clear();

            // Enumerable filter on a query (inefficient)
            Assert.AreEqual("IL: ql1_ef, ql2_ef", TypeAndNames(genericRepos.Read(new[] {
                new FilterCriteria { Filter = typeof(QueryLoaderFilter).FullName },
                new FilterCriteria { Filter = typeof(EnumerableFilter).FullName }, })));
            Assert.AreEqual("QP, EF", entityRepos.Log); entityRepos._log.Clear();
        }

        [TestMethod]
        public void ReadGenericFilter_NotMatches()
        {
            Expression<Func<SimpleEntity, bool>> predicate = item => item.Name.StartsWith("a");

            var entityRepos = new GenericFilterRepository();
            var genericRepos = NewRepos(entityRepos);

            Assert.AreEqual("IQ: a1, a2", TypeAndNames(genericRepos.ReadNonMaterialized(
                new[] { new FilterCriteria { Filter = predicate.GetType().AssemblyQualifiedName, Operation = "Matches", Value = predicate } },
                preferQuery: false)));
            Assert.AreEqual("Q, X", entityRepos.Log); entityRepos._log.Clear();

            Assert.AreEqual("IQ: b1, b2", TypeAndNames(genericRepos.ReadNonMaterialized(
                new[] { new FilterCriteria { Filter = predicate.GetType().AssemblyQualifiedName, Operation = "NotMatches", Value = predicate } },
                preferQuery: false)));
            Assert.AreEqual("Q, X", entityRepos.Log); entityRepos._log.Clear();
        }

        [TestMethod]
        public void ReadGenericFilter_NotLimitedToInterface()
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

            Assert.AreEqual("IQ: 2, 12", TypeAndNames(genericRepos.ReadNonMaterialized(genericFilter, preferQuery: true)));
        }

        class StringFilterRepository : IRepository
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
                return null;
            }

            public IEnumerable<SimpleEntity> Filter(IEnumerable<SimpleEntity> items, Guid parameter)
            {
                return null;
            }
        }

        [TestMethod]
        public void ReadGenericFilter_Errors()
        {
            var entityRepos = new StringFilterRepository();
            var genericRepos = NewRepos(entityRepos);

            TestUtility.ShouldFail(() => genericRepos.Read(new[] { new FilterCriteria { Operation = "x" } }), "both property filter and predefined filter are null");
            TestUtility.ShouldFail(() => genericRepos.Read(new[] { new FilterCriteria { Value = "x" } }), "both property filter and predefined filter are null");
            TestUtility.ShouldFail(() => genericRepos.Read(new[] { new FilterCriteria { } }), "both property filter and predefined filter are null");
            TestUtility.ShouldFail(() => genericRepos.Read(new[] { new FilterCriteria { Filter = "xx", Property = "yy" } }), "both property filter and predefined filter are set", "xx", "yy");

            TestUtility.ShouldFail(() => genericRepos.Read(new[] { new FilterCriteria { Filter = "System.String", Operation = "xxx" } }), "Filter", "System.String", "Operation", "xxx", "Matches", "NotMatches");
            TestUtility.ShouldFail(() => genericRepos.Read(new[] { new FilterCriteria { Filter = "System.String", Value = 123 } }), "System.String", "System.Int32");
            TestUtility.ShouldFail(() => genericRepos.Read(new[] { new FilterCriteria { Property = "Name", Operation = "xxx" } }), "Operation", "xxx");
            TestUtility.ShouldFail(() => genericRepos.Read(new[] { new FilterCriteria { Property = "Name", Value = "xxx" } }), "Property", "Name", "Operation");

            TestUtility.ShouldFail(() => genericRepos.Read(new[] { new FilterCriteria { Filter = "System.DateTime" } }), "SimpleEntity", "System.DateTime", "null");
            TestUtility.ShouldFail(() => genericRepos.Read(new[] { new FilterCriteria { Filter = "System.Guid" } }), "SimpleEntity", "System.Guid", "null");
        }

        //=======================================================

        class ImplicitQueryDataSourceCommandRepository : IRepository
        {
            public int QueryCount = 0;
            public int DropQueryCount() { int t = QueryCount; QueryCount = 0; return t; }

            public IQueryable<SimpleEntity> Query()
            {
                return new[] { 1 }
                    .SelectMany(x =>
                    {
                        QueryCount++;
                        return new SimpleEntityList { "a1", "b1", "b2", "b3", "b4", "b5" };
                    }).AsQueryable();
            }

            public IEnumerable<SimpleEntity> Filter(IEnumerable<SimpleEntity> source, string pattern)
            {
                return source.Where(item => item.Name.Contains(pattern));
            }
        }

        class ExplicitQueryDataSourceCommandRepository : IRepository
        {
            public ReadCommandResult ReadCommand(ReadCommandInfo commandInfo)
            {
                return new ReadCommandResult
                {
                    Records = new SimpleEntityList { "a1", "b1", "b2" }.ToArray<object>(),
                    TotalCount = 10
                };
            }
        }

        string Dump(QueryDataSourceCommandResult commandResult)
        {
            return TestUtility.Dump(commandResult.Records) + " / " + commandResult.TotalRecords;
        }

        [TestMethod]
        public void QueryDataSourceCommand()
        {
            var entityRepos = new ImplicitQueryDataSourceCommandRepository();
            var genericRepos = NewRepos(entityRepos);

            var command = new QueryDataSourceCommandInfo
            {
                GenericFilter = new[] { new FilterCriteria { Property = "Name", Operation = "StartsWith", Value = "b" } },
                OrderByProperty = "Name",
                RecordsPerPage = 3,
                PageNumber = 2
            };
            Assert.AreEqual("a1, b1, b2 / 10", Dump(NewRepos(new ExplicitQueryDataSourceCommandRepository()).ExecuteQueryDataSourceCommand(command)));
            Assert.AreEqual(0, entityRepos.DropQueryCount());
            Assert.AreEqual("b4, b5 / 5", Dump(genericRepos.ExecuteQueryDataSourceCommand(command)));
            Assert.AreEqual(2, entityRepos.DropQueryCount()); // Paging should result with two queries: selecting items and count.

            command = new QueryDataSourceCommandInfo();
            Assert.AreEqual("a1, b1, b2, b3, b4, b5 / 6", Dump(genericRepos.ExecuteQueryDataSourceCommand(command)));
            Assert.AreEqual(1, entityRepos.DropQueryCount()); // Without paging, there is no need for two queries.

            command = new QueryDataSourceCommandInfo { Filter = "1" };
            Assert.AreEqual("a1, b1 / 2", Dump(genericRepos.ExecuteQueryDataSourceCommand(command)));
            Assert.AreEqual(1, entityRepos.DropQueryCount()); // Without paging, there is no need for two queries.

            command = new QueryDataSourceCommandInfo { Filter = "1", GenericFilter = new[] { new FilterCriteria { Property = "Name", Operation = "StartsWith", Value = "b" } } };
            Assert.AreEqual("b1 / 1", Dump(genericRepos.ExecuteQueryDataSourceCommand(command)));
            Assert.AreEqual(1, entityRepos.DropQueryCount()); // Without paging, there is no need for two queries.

            command = new QueryDataSourceCommandInfo { Filter = "1", GenericFilter = new[] { new FilterCriteria { Filter = "System.String", Value = "b" } } };
            Assert.AreEqual("b1 / 1", Dump(genericRepos.ExecuteQueryDataSourceCommand(command)));
            Assert.AreEqual(1, entityRepos.DropQueryCount()); // Without paging, there is no need for two queries.

            command = new QueryDataSourceCommandInfo { Filter = "b", PageNumber = 2, RecordsPerPage = 2, OrderByProperty = "Name" };
            Assert.AreEqual("b3, b4 / 5", Dump(genericRepos.ExecuteQueryDataSourceCommand(command)));
            Assert.AreEqual(1, entityRepos.DropQueryCount()); // Enumerable filter will cause GenericRepository to materialize of the query, so it will be executed only once even though the paging is used.
        }

        [TestMethod]
        public void QueryDataSourceCommand_OutsideInterface()
        {
            var entityRepos = new ImplicitQueryDataSourceCommandRepository();
            var genericRepos = NewRepos(entityRepos);

            var command = new QueryDataSourceCommandInfo
            {
                GenericFilter = new[] { new FilterCriteria { Property = "Name", Operation = "StartsWith", Value = "b" } },
                OrderByProperty = "Data",
                RecordsPerPage = 3,
                PageNumber = 2
            };
            Assert.AreEqual("b4, b5 / 5", Dump(genericRepos.ExecuteQueryDataSourceCommand(command)));

            command = new QueryDataSourceCommandInfo
            {
                GenericFilter = new[] { new FilterCriteria { Property = "Data", Operation = "Equal", Value = "xxx" } },
                OrderByProperty = "Name",
                RecordsPerPage = 3,
                PageNumber = 2
            };
            Assert.AreEqual(" / 0", Dump(genericRepos.ExecuteQueryDataSourceCommand(command)));
        }

        [TestMethod]
        public void QueryDataSourceCommand_Null()
        {
            var entityRepos = new ImplicitQueryDataSourceCommandRepository();
            var genericRepos = NewRepos(entityRepos);

            var command = new QueryDataSourceCommandInfo
            {
                GenericFilter = new[] { new FilterCriteria { Property = "Data", Operation = "Equal", Value = null } },
                OrderByProperty = "Name",
                RecordsPerPage = 3,
                PageNumber = 2
            };
            Assert.AreEqual("b3, b4, b5 / 6", Dump(genericRepos.ExecuteQueryDataSourceCommand(command)));
        }
    }
}
