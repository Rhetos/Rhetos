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
    [TestClass]
    public class GenericRepositoryQueryTest
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
            return new TestGenericRepository<ISimpleEntity, SimpleEntity>(repository);
        }

        //=======================================================

        class NullRepository : IRepository
        {
        }

        [TestMethod]
        public void NoQueryFunction()
        {
            var repos = NewRepos(new NullRepository());

            TestUtility.ShouldFail(() => repos.Query(),
                "does not implement", "query",
                typeof(SimpleEntity).FullName);

            TestUtility.ShouldFail(() => repos.Query("abc"),
                "does not implement a loader, a query or a filter",
                "string",
                typeof(SimpleEntity).FullName);
        }

        //===============================================

        class SimpleRepository : IRepository
        {
            public IQueryable<SimpleEntity> Query()
            {
                return new SimpleEntityList { "qa", "qb", "qc" }.AsQueryable();
            }

            public IEnumerable<SimpleEntity> Filter(Parameter2 parameter)
            {
                if (parameter != null) throw new Exception("Parameter null is expected.");
                return new SimpleEntityList { "l2" };
            }

            public IEnumerable<SimpleEntity> Filter(IEnumerable<SimpleEntity> items, Parameter3 parameter)
            {
                if (parameter != null) throw new Exception("Parameter null is expected.");
                return new SimpleEntityList { "ef3" };
            }

            public IQueryable<SimpleEntity> Query(Parameter4 parameter)
            {
                if (parameter != null) throw new Exception("Parameter null is expected.");
                return new SimpleEntityList { "q4" }.AsQueryable();
            }

            public IQueryable<SimpleEntity> Filter(IQueryable<SimpleEntity> items, Parameter5 parameter)
            {
                if (parameter != null) throw new Exception("Parameter null is expected.");
                return new SimpleEntityList { "qf5" }.AsQueryable();
            }

            public IQueryable<SimpleEntity> Filter(IQueryable<SimpleEntity> items, Parameter6 parameter)
            {
                return items.Where(item => item.Name.Contains(parameter.Pattern));
            }
        }

        class Parameter1 { }
        class Parameter2 { }
        class Parameter3 { }
        class Parameter4 { }
        class Parameter5 { }
        class Parameter6 { public string Pattern; }

        [TestMethod]
        public void QueryWithParameters()
        {
            var repos = NewRepos(new SimpleRepository());

            Assert.AreEqual("qb", repos.Query(item => item.Name.Contains("b")).Single().Name);

            TestUtility.ShouldFail(() => repos.Query(null, typeof(Parameter1)),
                "does not implement a loader, a query or a filter with parameter",
                typeof(SimpleEntity).FullName,
                typeof(Parameter1).FullName);

            TestUtility.ShouldFail(() => repos.Query(null, typeof(Parameter2)),
                "does not implement a query method or a filter with parameter",
                "IQueryable",
                "Try using the Load function instead",
                typeof(SimpleEntity).FullName,
                typeof(Parameter2).FullName);

            TestUtility.ShouldFail(() => repos.Query(null, typeof(Parameter3)),
                "does not implement a query method or a filter with parameter",
                "IQueryable",
                "Try using the Load function instead",
                typeof(SimpleEntity).FullName,
                typeof(Parameter3).FullName);

            Assert.AreEqual("q4", repos.Query(null, typeof(Parameter4)).Single().Name);

            Assert.AreEqual("qf5", repos.Query(null, typeof(Parameter5)).Single().Name);

            Assert.AreEqual("qb", repos.Query(new Parameter6 { Pattern = "b" }).Single().Name);
        }

        [TestMethod]
        public void GenericFilterByIds()
        {
            var repos = NewRepos(new SimpleRepository());

            var guids = new[] { new Guid(1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0) };
            Assert.AreEqual("qb", repos.Load(guids).Single().Name);
            Assert.AreEqual("qb", repos.Load(guids.ToList()).Single().Name);
            Assert.AreEqual("qb", repos.Load(guids.AsQueryable()).Single().Name);
            Assert.AreEqual("qb", repos.Query(guids).Single().Name);
            Assert.AreEqual("qb", repos.Query(guids.ToList()).Single().Name);
            Assert.AreEqual("qb", repos.Query(guids.AsQueryable()).Single().Name);

            var q = repos.Query();
            Assert.AreEqual("qb", repos.Filter(q, guids).Single().Name);
            Assert.AreEqual("qb", repos.Filter(q, guids.ToList()).Single().Name);
            Assert.AreEqual("qb", repos.Filter(q, guids.AsQueryable()).Single().Name);

            var a = repos.Load();
            Assert.AreEqual("qb", repos.Filter(a, guids).Single().Name);
            Assert.AreEqual("qb", repos.Filter(a, guids.ToList()).Single().Name);
            Assert.AreEqual("qb", repos.Filter(a, guids.AsQueryable()).Single().Name);
        }
    }
}
