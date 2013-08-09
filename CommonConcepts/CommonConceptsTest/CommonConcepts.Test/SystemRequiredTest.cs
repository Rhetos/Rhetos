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
    public class SystemRequiredTest
    {
        [TestMethod]
        public void InsertSimple()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[]
                    {
                        "DELETE FROM TestSystemRequired.Parent",
                        "DELETE FROM TestSystemRequired.Child"
                    });
                var repository = new Common.DomRepository(executionContext);

                repository.TestSystemRequired.Parent.Insert(new[] { new TestSystemRequired.Parent { Name = "Test" } });

                TestUtility.ShouldFail(
                    () => repository.TestSystemRequired.Parent.Insert(new[] { new TestSystemRequired.Parent { Name = null } }),
                    "Insert null value",
                    "Parent", "Name", "System required");
            }
        }

        [TestMethod]
        public void UpdateSimple()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[]
                    {
                        "DELETE FROM TestSystemRequired.Parent",
                        "DELETE FROM TestSystemRequired.Child"
                    });
                var repository = new Common.DomRepository(executionContext);

                var parent = new TestSystemRequired.Parent { ID = Guid.NewGuid(), Name = "Test" };
                repository.TestSystemRequired.Parent.Insert(new[] { parent });

                parent.Name = null;
                TestUtility.ShouldFail(
                    () => repository.TestSystemRequired.Parent.Update(new[] { parent }),
                    "Update null value",
                    "Parent", "Name", "System required");
            }
        }

        [TestMethod]
        public void InsertReference()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[]
                    {
                        "DELETE FROM TestSystemRequired.Parent",
                        "DELETE FROM TestSystemRequired.Child"
                    });
                var repository = new Common.DomRepository(executionContext);

                var parentID = Guid.NewGuid();
                repository.TestSystemRequired.Parent.Insert(new[] { new TestSystemRequired.Parent { ID = parentID, Name = "Test" } });
                repository.TestSystemRequired.Child.Insert(new[] { new TestSystemRequired.Child { ParentID = parentID, Name = "Test2" } });

                TestUtility.ShouldFail(
                    () => repository.TestSystemRequired.Child.Insert(new[] { new TestSystemRequired.Child { ParentID = null, Name = "Test3" } }),
                    "Insert null reference value",
                    "Child", "System required");
            }
        }
    }
}
