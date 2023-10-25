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

using CommonConcepts.Test.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Configuration.Autofac;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.TestCommon;
using System;

namespace CommonConcepts.Test
{
    [TestClass]
    public class DslUtilityIntegrationTest
    {
        [TestMethod]
        public void GetBaseChangesOnDependencyHistory()
        {
            using (var scope = TestScope.Create())
            {
                var dslModel = scope.Resolve<IDslModel>();

                var dependsOnHistory = (SqlQueryableInfo)dslModel.FindByKey("DataStructureInfo TestHistory.SimpleWithLock_History");

                var entityDependencies = DslUtility.GetBaseChangesOnDependency(dependsOnHistory, dslModel);
                Assert.AreEqual(
                    "Entity TestHistory.SimpleWithLock, Entity TestHistory.SimpleWithLock_Changes",
                    TestUtility.DumpSorted(entityDependencies, c => c.GetUserDescription()));
            }
        }
    }
}
