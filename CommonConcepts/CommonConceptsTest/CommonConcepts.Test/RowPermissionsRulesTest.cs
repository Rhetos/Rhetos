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
using Rhetos;
using Rhetos.Configuration.Autofac;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Processing.DefaultCommands;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Linq;
using TestRowPermissions;

namespace CommonConcepts.Test
{
    [TestClass]
    public class RowPermissionsRulesTest
    {
        [TestMethod]
        public void FilterNoPermissions()
        {
            using (var container = new RhetosTestContainer())
            {
                var repositories = container.Resolve<Common.DomRepository>();
                var itemsRepository = repositories.TestRowPermissions.RPRulesItem;
                var groupsRepository = repositories.TestRowPermissions.RPRulesGroup;
                itemsRepository.Delete(itemsRepository.All());
                groupsRepository.Delete(groupsRepository.All());

                var g1 = new RPRulesGroup { ID = Guid.NewGuid(), Name = "g1" };
                var g2 = new RPRulesGroup { ID = Guid.NewGuid(), Name = "g2" };
                var i1 = new RPRulesItem { ID = Guid.NewGuid(), Name = "i1", Group = g1 };
                var i2 = new RPRulesItem { ID = Guid.NewGuid(), Name = "i2", Group = g1 };
                var i3 = new RPRulesItem { ID = Guid.NewGuid(), Name = "i3", Group = g2 };
                var i4 = new RPRulesItem { ID = Guid.NewGuid(), Name = "i4", Group = g2 };

                groupsRepository.Insert(new[] { g1, g2 });
                itemsRepository.Insert(new[] { i1, i2, i3, i4 });

                var allowedItems = itemsRepository.Filter(itemsRepository.Query(), new Common.RowPermissionsAllowedItems());
                Console.WriteLine(itemsRepository.Query().Expression.ToString());
                Console.WriteLine(allowedItems.Expression.ToString());
                Assert.AreEqual("", TestUtility.DumpSorted(allowedItems, item => item.Name));
                Assert.AreEqual("TestRowPermissions.RPRulesItem[]", allowedItems.Expression.ToString(), "No need for query, an empty array should be returned.");
            }
        }

        [TestMethod]
        public void FilterWithPermissions()
        {
            using (var container = new RhetosTestContainer())
            {
                var currentUserName = container.Resolve<IUserInfo>().UserName;
                var repositories = container.Resolve<Common.DomRepository>();
                var itemsRepository = repositories.TestRowPermissions.RPRulesItem;
                var groupsRepository = repositories.TestRowPermissions.RPRulesGroup;
                itemsRepository.Delete(itemsRepository.All());
                groupsRepository.Delete(groupsRepository.All());

                var g1 = new RPRulesGroup { ID = Guid.NewGuid(), Name = "g1" };
                var g2 = new RPRulesGroup { ID = Guid.NewGuid(), Name = "g2" };
                var i1 = new RPRulesItem { ID = Guid.NewGuid(), Name = "i1", Group = g1 };
                var i2 = new RPRulesItem { ID = Guid.NewGuid(), Name = "i2", Group = g1 };
                var i3 = new RPRulesItem { ID = Guid.NewGuid(), Name = "i3", Group = g2 };
                var i4 = new RPRulesItem { ID = Guid.NewGuid(), Name = "i4", Group = g2 };

                groupsRepository.Insert(new[] { g1, g2 });
                itemsRepository.Insert(new[] { i1, i2, i3, i4 });

                repositories.TestRowPermissions.RpRulesAllowGroup.Insert(new[] {
                    new TestRowPermissions.RpRulesAllowGroup { UserName = currentUserName, Group = g2 } });

                repositories.TestRowPermissions.RpRulesAllowItem.Insert(new[] {
                    new TestRowPermissions.RpRulesAllowItem { UserName = currentUserName, Item = i2 } });

                repositories.TestRowPermissions.RpRulesDenyItem.Insert(new[] {
                    new TestRowPermissions.RpRulesDenyItem { UserName = currentUserName, Item = i3 } });

                var allowedItems = itemsRepository.Filter(itemsRepository.Query(), new Common.RowPermissionsAllowedItems());
                Console.WriteLine(itemsRepository.Query().Expression.ToString());
                Console.WriteLine(allowedItems.Expression.ToString());
                Assert.AreEqual("i2, i4", TestUtility.DumpSorted(allowedItems, item => item.Name));
            }
        }

        [TestMethod]
        public void FilterOptimizeAllowedAll()
        {
            using (var container = new RhetosTestContainer())
            {
                var currentUserName = container.Resolve<IUserInfo>().UserName;
                var repositories = container.Resolve<Common.DomRepository>();
                var itemsRepository = repositories.TestRowPermissions.RPRulesItem;
                var groupsRepository = repositories.TestRowPermissions.RPRulesGroup;
                itemsRepository.Delete(itemsRepository.All());
                groupsRepository.Delete(groupsRepository.All());

                var g1 = new RPRulesGroup { ID = Guid.NewGuid(), Name = "g1" };
                var g2 = new RPRulesGroup { ID = Guid.NewGuid(), Name = "g2" };
                var i1 = new RPRulesItem { ID = Guid.NewGuid(), Name = "i1", Group = g1 };
                var i2 = new RPRulesItem { ID = Guid.NewGuid(), Name = "i2", Group = g1 };
                var i3 = new RPRulesItem { ID = Guid.NewGuid(), Name = "i3", Group = g2 };
                var i4 = new RPRulesItem { ID = Guid.NewGuid(), Name = "i4", Group = g2 };

                groupsRepository.Insert(new[] { g1, g2 });
                itemsRepository.Insert(new[] { i1, i2, i3, i4 });

                repositories.TestRowPermissions.RpRulesAllowGroup.Insert(new[] {
                    new TestRowPermissions.RpRulesAllowGroup { UserName = currentUserName, Group = g1 },
                    new TestRowPermissions.RpRulesAllowGroup { UserName = currentUserName, Group = g2 } });

                repositories.TestRowPermissions.RpRulesAllowItem.Insert(new[] {
                    new TestRowPermissions.RpRulesAllowItem { UserName = currentUserName, Item = i1 },
                    new TestRowPermissions.RpRulesAllowItem { UserName = currentUserName, Item = i2 } });

                var allowedItems = itemsRepository.Filter(itemsRepository.Query(), new Common.RowPermissionsAllowedItems());
                Console.WriteLine(itemsRepository.Query().Expression.ToString());
                Console.WriteLine(allowedItems.Expression.ToString());
                Assert.AreEqual("i1, i2, i3, i4", TestUtility.DumpSorted(allowedItems, item => item.Name));
                Assert.AreEqual(itemsRepository.Query().Expression.ToString(), allowedItems.Expression.ToString(), "'AllowedAllGroups' rule should result with an optimized query without the 'where' part.");
            }
        }
    }
}
