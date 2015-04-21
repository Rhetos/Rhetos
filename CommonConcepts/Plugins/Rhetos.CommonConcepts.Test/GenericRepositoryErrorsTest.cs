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
    public class GenericRepositoryErrorsTest
    {
        public class SimpleEntity : IEntity
        {
            public Guid ID { get; set; }
            public string Name { get; set; }
        }

        class ErrorRepository : IRepository
        {
            public IEnumerable<SimpleEntity> All()
            {
                throw new ApplicationException("fun All");
            }

            public SimpleEntity[] Filter(string parameter)
            {
                throw new ApplicationException("fun Filter string");
            }

            public IQueryable<SimpleEntity> Query()
            {
                throw new ApplicationException("fun Query");
            }

            public IQueryable<SimpleEntity> Filter(IQueryable<SimpleEntity> source, string parameter)
            {
                throw new ApplicationException("fun Filter source string");
            }

            public void Save(IEnumerable<SimpleEntity> insertedNew, IEnumerable<SimpleEntity> updatedNew, IEnumerable<SimpleEntity> deletedIds, bool checkUserPermissions = false)
            {
                throw new ApplicationException("fun Save");
            }
        }

        GenericRepository<SimpleEntity> NewRepos(IRepository repository)
        {
            return new TestGenericRepository<SimpleEntity, SimpleEntity>(repository);
        }

        void TestError(Action action, string errorMessage, string locationFunctionName)
        {
            var ex = TestUtility.ShouldFail<ApplicationException>(action, errorMessage);

            string errorLocation = "at " + typeof(ErrorRepository).FullName.Replace("+", ".") + "." + locationFunctionName + "(";
            TestUtility.AssertContains(ex.ToString(), errorLocation);
        }

        [TestMethod]
        public void GetError()
        {
            var repos = NewRepos(new ErrorRepository());

            TestError(() => repos.Load(), "fun All", "All");
            TestError(() => repos.Load(new FilterAll()), "fun All", "All");
            TestError(() => repos.Load("str"), "fun Filter string", "Filter");
            TestError(() => repos.Query(), "fun Query", "Query");
            TestError(() => repos.Load(new[] { new FilterCriteria { Property = "Name", Operation = "equal", Value = "abc" } }), "fun Query", "Query");
            TestError(() => repos.Filter(new SimpleEntity[] {}.AsQueryable(), "str"), "fun Filter source string", "Filter");
            TestError(() => repos.Save(null, null, null), "fun Save", "Save");
        }
    }
}
