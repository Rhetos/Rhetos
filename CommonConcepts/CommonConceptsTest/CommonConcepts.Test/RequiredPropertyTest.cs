/*
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
using Rhetos.TestCommon;

namespace CommonConcepts.Test
{
    [TestClass]
    public class RequiredPropertyTest
    {
        [TestMethod]
        public void ValidData()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestRequired.Simple" });
                var repository = new Common.DomRepository(executionContext);

                repository.TestRequired.Simple.Insert(new[] { new TestRequired.Simple { Count = 1, Name = "test1" } });
                repository.TestRequired.Simple.Insert(new[] { new TestRequired.Simple { Count = 0, Name = "test2" } });
                Assert.AreEqual("0-test2, 1-test1", TestUtility.DumpSorted(repository.TestRequired.Simple.All(), item => item.Count + "-" + item.Name));
            }
        }

        [TestMethod]
        public void NullInteger()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestRequired.Simple" });
                var repository = new Common.DomRepository(executionContext);

                TestUtility.ShouldFail(() => repository.TestRequired.Simple.Insert(new[] { new TestRequired.Simple { Count = null, Name = "test3" } }), "required", "Count");
            }
        }

        [TestMethod]
        public void NullString()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestRequired.Simple" });
                var repository = new Common.DomRepository(executionContext);

                TestUtility.ShouldFail(() => repository.TestRequired.Simple.Insert(new[] { new TestRequired.Simple { Count = 4, Name = null } }), "required", "Name");
            }
        }

        [TestMethod]
        public void EmptyString()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestRequired.Simple" });
                var repository = new Common.DomRepository(executionContext);

                TestUtility.ShouldFail(() => repository.TestRequired.Simple.Insert(new[] { new TestRequired.Simple { Count = 5, Name = "" } }), "required", "Name");
            }
        }
    }
}
