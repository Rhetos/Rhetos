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
using TestDeactivatable;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommonConcepts.Test
{
    [TestClass]
    public class DeactivatableTest
    {
        [TestMethod]
        [ExpectedException(typeof(Rhetos.UserException))]
        public void ActivePropertyValueHasToBeDefined()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);
                var entity = new BasicEnt { Name = "ttt" };
                repository.TestDeactivatable.BasicEnt.Insert(new[] { entity });
            }
        }

        [TestMethod]
        public void ItIsOkToInsertInactiveOrActiveItem()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);
                var entity = new BasicEnt { Name = "ttt", Active = false };
                var entity2 = new BasicEnt { Name = "ttt2", Active = true };
                repository.TestDeactivatable.BasicEnt.Insert(new[] { entity, entity2 });
            }
        }

        [TestMethod]
        public void TestForActiveItemsFilter()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { 
                    "DELETE FROM TestDeactivatable.BasicEnt;",
                });
                var repository = new Common.DomRepository(executionContext);
                var entity = new BasicEnt { Name = "ttt", Active = false };
                var entity2 = new BasicEnt { Name = "ttt2", Active = true };
                
                repository.TestDeactivatable.BasicEnt.Insert(new[] { entity, entity2 });
                Assert.AreEqual(1, repository.TestDeactivatable.BasicEnt.Filter(new ActiveItems()).Count());
            }
        }

        [TestMethod]
        public void TestForThisAndActiveItemsFilter()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { 
                    "DELETE FROM TestDeactivatable.BasicEnt;",
                });
                var e1ID = Guid.NewGuid();
                var repository = new Common.DomRepository(executionContext);
                var entity = new BasicEnt { ID = e1ID, Name = "ttt", Active = false };
                var entity2 = new BasicEnt { Name = "ttt2", Active = true };
                var entity3 = new BasicEnt { Name = "ttt3", Active = false };

                repository.TestDeactivatable.BasicEnt.Insert(new[] { entity, entity2, entity3 });
                Assert.AreEqual(1, repository.TestDeactivatable.BasicEnt.Filter(new ActiveItems()).Count());
                Assert.AreEqual(2, repository.TestDeactivatable.BasicEnt.Filter(new BasicEnt_ThisAndActiveItems { ItemID = e1ID }).Count());
            }
        }
    }
}
