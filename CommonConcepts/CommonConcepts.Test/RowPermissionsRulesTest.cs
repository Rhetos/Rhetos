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
using Rhetos.Extensibility;
using Rhetos.Processing;
using CommonConcepts.Test.Helpers;

namespace CommonConcepts.Test
{
    [TestClass]
    public class RowPermissionsRulesTest
    {
        static string _writeException = "You are not authorized to write";

        [TestMethod]
        public void FilterNoPermissions()
        {
            using (var container = TestContainer.Create())
            {
                var repositories = container.Resolve<Common.DomRepository>();
                var itemsRepository = repositories.TestRowPermissions.RPRulesItem;
                var groupsRepository = repositories.TestRowPermissions.RPRulesGroup;
                itemsRepository.Delete(itemsRepository.Query());
                groupsRepository.Delete(groupsRepository.Query());

                var g1 = new RPRulesGroup { ID = Guid.NewGuid(), Name = "g1" };
                var g2 = new RPRulesGroup { ID = Guid.NewGuid(), Name = "g2" };
                var i1 = new RPRulesItem { ID = Guid.NewGuid(), Name = "i1", GroupID = g1.ID };
                var i2 = new RPRulesItem { ID = Guid.NewGuid(), Name = "i2", GroupID = g1.ID };
                var i3 = new RPRulesItem { ID = Guid.NewGuid(), Name = "i3", GroupID = g2.ID };
                var i4 = new RPRulesItem { ID = Guid.NewGuid(), Name = "i4", GroupID = g2.ID };

                groupsRepository.Insert(new[] { g1, g2 });
                itemsRepository.Insert(new[] { i1, i2, i3, i4 });

                var allowedItems = itemsRepository.Filter(itemsRepository.Query(), new Common.RowPermissionsReadItems());
                Console.WriteLine(itemsRepository.Query().Expression.ToString());
                Console.WriteLine(allowedItems.Expression.ToString());
                Assert.AreEqual("", TestUtility.DumpSorted(allowedItems, item => item.Name));
                Assert.AreEqual("Common.Queryable.TestRowPermissions_RPRulesItem[]", allowedItems.Expression.ToString(), "No need for query, an empty array should be returned.");
            }
        }

        [TestMethod]
        public void FilterWithPermissions()
        {
            using (var container = TestContainer.Create())
            {
                var currentUserName = container.Resolve<IUserInfo>().UserName;
                var repositories = container.Resolve<Common.DomRepository>();
                var itemsRepository = repositories.TestRowPermissions.RPRulesItem;
                var groupsRepository = repositories.TestRowPermissions.RPRulesGroup;
                itemsRepository.Delete(itemsRepository.Query());
                groupsRepository.Delete(groupsRepository.Query());

                var g1 = new RPRulesGroup { ID = Guid.NewGuid(), Name = "g1" };
                var g2 = new RPRulesGroup { ID = Guid.NewGuid(), Name = "g2" };
                var i1 = new RPRulesItem { ID = Guid.NewGuid(), Name = "i1", GroupID = g1.ID };
                var i2 = new RPRulesItem { ID = Guid.NewGuid(), Name = "i2", GroupID = g1.ID };
                var i3 = new RPRulesItem { ID = Guid.NewGuid(), Name = "i3", GroupID = g2.ID };
                var i4 = new RPRulesItem { ID = Guid.NewGuid(), Name = "i4", GroupID = g2.ID };

                groupsRepository.Insert(new[] { g1, g2 });
                itemsRepository.Insert(new[] { i1, i2, i3, i4 });

                repositories.TestRowPermissions.RpRulesAllowGroup.Insert(new[] {
                    new TestRowPermissions.RpRulesAllowGroup { UserName = currentUserName, GroupID = g2.ID } });

                repositories.TestRowPermissions.RpRulesAllowItem.Insert(new[] {
                    new TestRowPermissions.RpRulesAllowItem { UserName = currentUserName, ItemID = i2.ID } });

                repositories.TestRowPermissions.RpRulesDenyItem.Insert(new[] {
                    new TestRowPermissions.RpRulesDenyItem { UserName = currentUserName, ItemID = i3.ID } });

                var allowedItems = itemsRepository.Filter(itemsRepository.Query(), new Common.RowPermissionsReadItems());
                Console.WriteLine(itemsRepository.Query().Expression.ToString());
                Console.WriteLine(allowedItems.Expression.ToString());
                Assert.AreEqual("i2, i4", TestUtility.DumpSorted(allowedItems, item => item.Name));
            }
        }

        [TestMethod]
        public void FilterOptimizeAllowedAll()
        {
            using (var container = TestContainer.Create())
            {
                var currentUserName = container.Resolve<IUserInfo>().UserName;
                var repositories = container.Resolve<Common.DomRepository>();
                var itemsRepository = repositories.TestRowPermissions.RPRulesItem;
                var groupsRepository = repositories.TestRowPermissions.RPRulesGroup;
                itemsRepository.Delete(itemsRepository.Query());
                groupsRepository.Delete(groupsRepository.Query());

                var g1 = new RPRulesGroup { ID = Guid.NewGuid(), Name = "g1" };
                var g2 = new RPRulesGroup { ID = Guid.NewGuid(), Name = "g2" };
                var i1 = new RPRulesItem { ID = Guid.NewGuid(), Name = "i1", GroupID = g1.ID };
                var i2 = new RPRulesItem { ID = Guid.NewGuid(), Name = "i2", GroupID = g1.ID };
                var i3 = new RPRulesItem { ID = Guid.NewGuid(), Name = "i3", GroupID = g2.ID };
                var i4 = new RPRulesItem { ID = Guid.NewGuid(), Name = "i4", GroupID = g2.ID };

                groupsRepository.Insert(new[] { g1, g2 });
                itemsRepository.Insert(new[] { i1, i2, i3, i4 });

                repositories.TestRowPermissions.RpRulesAllowGroup.Insert(new[] {
                    new TestRowPermissions.RpRulesAllowGroup { UserName = currentUserName, GroupID = g1.ID },
                    new TestRowPermissions.RpRulesAllowGroup { UserName = currentUserName, GroupID = g2.ID } });

                repositories.TestRowPermissions.RpRulesAllowItem.Insert(new[] {
                    new TestRowPermissions.RpRulesAllowItem { UserName = currentUserName, ItemID = i1.ID },
                    new TestRowPermissions.RpRulesAllowItem { UserName = currentUserName, ItemID = i2.ID } });

                var allowedItems = itemsRepository.Filter(itemsRepository.Query(), new Common.RowPermissionsReadItems());
                Console.WriteLine(itemsRepository.Query().Expression.ToString());
                Console.WriteLine(allowedItems.Expression.ToString());
                Assert.AreEqual("i1, i2, i3, i4", TestUtility.DumpSorted(allowedItems, item => item.Name));
                Assert.AreEqual(itemsRepository.Query().Expression.ToString(), allowedItems.Expression.ToString(), "'AllowedAllGroups' rule should result with an optimized query without the 'where' part.");
            }
        }

        [TestMethod]
        public void InheritFrom()
        {
            using (var container = TestContainer.Create())
            {
                Level1
                    l1ReadAllow = new Level1() { ID = Guid.NewGuid(), value = 190 },
                    l1ReadDeny = new Level1() { ID = Guid.NewGuid(), value = 90 },
                    l1WriteAllow = new Level1() { ID = Guid.NewGuid(), value = 60 },
                    l1WriteDeny = new Level1() { ID = Guid.NewGuid(), value = 160 };

                Level2
                    l2ParentReadAllow = new Level2() { ID = Guid.NewGuid(), MyParentID = l1ReadAllow.ID, value = 5 },
                    l2cParentReadDeny = new Level2() { ID = Guid.NewGuid(), MyParentID = l1ReadDeny.ID, value = 6 },
                    l2cParentWriteAllow = new Level2() { ID = Guid.NewGuid(), MyParentID = l1WriteAllow.ID, value = 7 },
                    l2cParentWriteDeny = new Level2() { ID = Guid.NewGuid(), MyParentID = l1WriteDeny.ID, value = 8 };

                var repositories = container.Resolve<Common.DomRepository>();
                var l1Repo = repositories.TestRowPermissions.Level1;
                var l2Repo = repositories.TestRowPermissions.Level2;
                var l3Repo = repositories.TestRowPermissions.Level3;
                var browseRepo = repositories.TestRowPermissions.Level1Browse;

                l3Repo.Delete(l3Repo.Query());
                l2Repo.Delete(l2Repo.Query());
                l1Repo.Delete(l1Repo.Query());

                l1Repo.Insert(new Level1[] { l1ReadAllow, l1ReadDeny, l1WriteAllow, l1WriteDeny });
                l2Repo.Insert(new Level2[] { l2ParentReadAllow, l2cParentReadDeny, l2cParentWriteAllow, l2cParentWriteDeny });

                {
                    var l2AllowRead = l2Repo.Filter(l2Repo.Query(), new Common.RowPermissionsReadItems()).ToList();
                    Assert.AreEqual("5, 8", TestUtility.DumpSorted(l2AllowRead, a => a.value.ToString()));
                }

                {
                    var l2AllowWrite = l2Repo.Filter(l2Repo.Query(), new Common.RowPermissionsWriteItems()).ToList();
                    Assert.AreEqual("6, 7", TestUtility.DumpSorted(l2AllowWrite, a => a.value.ToString()));
                }

                // Test combination with rule on level 2
                Level2 cCombo = new Level2() { ID = Guid.NewGuid(), MyParentID = l1ReadAllow.ID, value = 3 };
                l2Repo.Insert(new Level2[] { cCombo });
                {
                    var l2AllowRead = l2Repo.Filter(l2Repo.Query(), new Common.RowPermissionsReadItems()).ToList();
                    Assert.IsTrue(!l2AllowRead.Select(a => a.value).Contains(3));
                }

                // Test double inheritance, only write deny case
                Level3 bDenyWrite = new Level3() { ID = Guid.NewGuid(), MyParentID = l2cParentWriteDeny.ID };
                l3Repo.Insert(new Level3[] { bDenyWrite });
                {
                    Assert.AreEqual(1, l3Repo.Query().Count());
                    var l3DenyWrite = l3Repo.Filter(l3Repo.Query(), new Common.RowPermissionsWriteItems()).ToList();
                    Assert.AreEqual(0, l3DenyWrite.Count);
                }

                // Test inheritance form base data structure
                {
                    var allowedRead = browseRepo.Filter(browseRepo.Query(), new Common.RowPermissionsReadItems()).ToList();
                    Assert.AreEqual("160, 190", TestUtility.DumpSorted(allowedRead, item => item.Value2));

                    var allowedWrite = browseRepo.Filter(browseRepo.Query(), new Common.RowPermissionsWriteItems()).ToList();
                    Assert.AreEqual("60, 90", TestUtility.DumpSorted(allowedWrite, item => item.Value2));
                }
            }
        }

        [TestMethod]
        public void RulesWrite()
        {
            using (var container = TestContainer.Create())
            {
                var repositories = container.Resolve<Common.DomRepository>();
                var emptyRP = repositories.TestRowPermissions.RPWriteRulesEmpty;
                var writeRP = repositories.TestRowPermissions.RPWriteRules;
                var commandImplementations = container.Resolve<IPluginsContainer<ICommandImplementation>>();
                var saveCommand = commandImplementations.GetImplementations(typeof(SaveEntityCommandInfo)).Single();

                {
                    emptyRP.Delete(emptyRP.Query());
                    var saveInfo = new SaveEntityCommandInfo() {Entity = "TestRowPermissions.RPWriteRulesEmpty"};
                    saveInfo.DataToInsert = new[] {new RPWriteRulesEmpty()};
                    TestUtility.ShouldFail(() => saveCommand.Execute(saveInfo), _writeException);
                }
                {
                    writeRP.Delete(writeRP.Query());
                    var saveInfo = new SaveEntityCommandInfo() { Entity = "TestRowPermissions.RPWriteRules" };
                    saveInfo.DataToInsert = (new[] {10}).Select(item => new RPWriteRules() {value = item}).ToArray();
                    TestUtility.ShouldFail(() => saveCommand.Execute(saveInfo), _writeException);
                }

                {
                    writeRP.Delete(writeRP.Query());
                    var saveInfo = new SaveEntityCommandInfo() { Entity = "TestRowPermissions.RPWriteRules" };
                    saveInfo.DataToInsert = (new[] { 5 }).Select(item => new RPWriteRules() { value = item }).ToArray();
                    TestUtility.ShouldFail(() => saveCommand.Execute(saveInfo), _writeException);
                }

                {
                    writeRP.Delete(writeRP.Query());
                    var saveInfo = new SaveEntityCommandInfo() { Entity = "TestRowPermissions.RPWriteRules" };
                    saveInfo.DataToInsert = (new[] { 1, 2, 8 }).Select(item => new RPWriteRules() { value = item }).ToArray();
                    TestUtility.ShouldFail(() => saveCommand.Execute(saveInfo), _writeException);
                }

                {
                    writeRP.Delete(writeRP.Query());
                    var saveInfo = new SaveEntityCommandInfo() { Entity = "TestRowPermissions.RPWriteRules" };
                    saveInfo.DataToDelete = (new[] { 7 }).Select(item => new RPWriteRules() { value = item, ID = Guid.NewGuid()}).ToArray();
                    writeRP.Insert((RPWriteRules[])saveInfo.DataToDelete);

                    TestUtility.ShouldFail(() => saveCommand.Execute(saveInfo), _writeException);
                }

                {
                    writeRP.Delete(writeRP.Query());
                    var saveInfo = new SaveEntityCommandInfo() { Entity = "TestRowPermissions.RPWriteRules" };
                    saveInfo.DataToInsert = (new[] { 1, 2, 3, 4, 6, 9 }).Select(item => new RPWriteRules() { value = item, ID = Guid.NewGuid() }).ToArray();
                    saveCommand.Execute(saveInfo);
                    saveInfo.DataToDelete = saveInfo.DataToInsert;
                    saveInfo.DataToInsert = null;
                    saveCommand.Execute(saveInfo);
                    Assert.AreEqual(0, writeRP.Query().Count());
                }

                // update to legal
                {
                    writeRP.Delete(writeRP.Query());
                    var saveInfo = new SaveEntityCommandInfo() { Entity = "TestRowPermissions.RPWriteRules" };
                    var items = (new[] { 12 }).Select(item => new RPWriteRules() { value = item, ID = Guid.NewGuid() }).ToArray();
                    writeRP.Insert(items);
                    items[0].value = 1;
                    saveInfo.DataToUpdate = items;
                    TestUtility.ShouldFail(() => saveCommand.Execute(saveInfo), _writeException);
                }

                // update from legal
                {
                    writeRP.Delete(writeRP.Query());
                    var saveInfo = new SaveEntityCommandInfo() { Entity = "TestRowPermissions.RPWriteRules" };
                    var items = (new[] { 1 }).Select(item => new RPWriteRules() { value = item, ID = Guid.NewGuid() }).ToArray();
                    writeRP.Insert(items);
                    items[0].value = 12;
                    saveInfo.DataToUpdate = items;
                    TestUtility.ShouldFail(() => saveCommand.Execute(saveInfo), _writeException);
                }

                {
                    writeRP.Delete(writeRP.Query());
                    var saveInfo = new SaveEntityCommandInfo() { Entity = "TestRowPermissions.RPWriteRules" };
                    var items = (new[] { 1 }).Select(item => new RPWriteRules() { value = item, ID = Guid.NewGuid() }).ToArray();
                    writeRP.Insert(items);
                    items[0].value = 2;
                    saveInfo.DataToUpdate = items;
                    saveCommand.Execute(saveInfo);
                }

                {
                    writeRP.Delete(writeRP.Query());
                    var saveInfo = new SaveEntityCommandInfo() { Entity = "TestRowPermissions.RPWriteRules" };
                    saveInfo.DataToInsert = (new[] { 20 }).Select(item => new RPWriteRules() { value = item, ID = Guid.NewGuid() }).ToArray();

                    saveCommand.Execute(saveInfo);
                }
            }
        }

        [TestMethod]
        public void CombinedRules()
        {
            using (var container = TestContainer.Create())
            {
                var settingsRepos = container.Resolve<GenericRepository<RPCombinedRulesSettings>>();
                var itemsRepos = container.Resolve<GenericRepository<RPCombinedRulesItems>>();

                var items = "a a1 a2 ab ab1 ab2 b b1 b2 r w"
                    .Split(' ')
                    .Select(name => new RPCombinedRulesItems { Name = name })
                    .ToList();
                itemsRepos.Save(items, null, itemsRepos.Load());

                {
                    // Test read allow/deny without conditional rules:

                    var settings = new RPCombinedRulesSettings { Settings = "no conditional rules" };
                    settingsRepos.Save(new[] { settings }, null, settingsRepos.Load());

                    var allowRead = itemsRepos.Query<Common.RowPermissionsReadItems>().Select(item => item.Name).ToList();
                    Assert.AreEqual("a, a2, ab, ab2, r", TestUtility.DumpSorted(allowRead));
                }

                {
                    // Test read allow/deny with conditional rules:

                    var settings = new RPCombinedRulesSettings { Settings = "add conditional rules" };
                    settingsRepos.Save(new[] { settings }, null, settingsRepos.Load());

                    var allowRead = itemsRepos.Query<Common.RowPermissionsReadItems>().Select(item => item.Name).ToList();
                    Assert.AreEqual("a, ab, b, r", TestUtility.DumpSorted(allowRead));
                }

                {
                    // Test write allow/deny without conditional rules:

                    var settings = new RPCombinedRulesSettings { Settings = "no conditional rules" };
                    settingsRepos.Save(new[] { settings }, null, settingsRepos.Load());

                    var allowWrite = itemsRepos.Query<Common.RowPermissionsWriteItems>().Select(item => item.Name).ToList();
                    Assert.AreEqual("a, a2, ab, ab2, w", TestUtility.DumpSorted(allowWrite));
                }

                {
                    // Test write allow/deny with conditional rules:

                    var settings = new RPCombinedRulesSettings { Settings = "add conditional rules" };
                    settingsRepos.Save(new[] { settings }, null, settingsRepos.Load());

                    var allowWrite = itemsRepos.Query<Common.RowPermissionsWriteItems>().Select(item => item.Name).ToList();
                    Assert.AreEqual("a, ab, b, w", TestUtility.DumpSorted(allowWrite));
                }
            }
        }

        [TestMethod]
        public void AutoInherit()
        {
            using (var container = TestContainer.Create())
            {
                var names = new[] { "1", "1b", "2", "3", "4" };
                var itemsE4 = names.Select(name => new TestRowPermissions4.E4 { ID = Guid.NewGuid(), Name4 = name }).ToList();
                var itemsE3 = names.Select((name, x) => new TestRowPermissions3.E3 { ID = Guid.NewGuid(), Name3 = name, E4ID = itemsE4[x].ID }).ToList();
                var itemsE2 = names.Select((name, x) => new TestRowPermissions2.E2 { ID = Guid.NewGuid(), Name2 = name, E3ID = itemsE3[x].ID }).ToList();
                var itemsE1 = names.Select((name, x) => new TestRowPermissions1.E1 { ID = Guid.NewGuid(), Name1 = name, E2ID = itemsE2[x].ID }).ToList();

                var reposE1 = container.Resolve<GenericRepository<TestRowPermissions1.E1>>();
                var reposE1Browse = container.Resolve<GenericRepository<TestRowPermissions1.E1Browse>>();
                var reposE1BrowseRP = container.Resolve<GenericRepository<TestRowPermissions1.E1BrowseRP>>();
                var reposE2 = container.Resolve<GenericRepository<TestRowPermissions2.E2>>();
                var reposE3 = container.Resolve<Common.DomRepository>().TestRowPermissions3.E3;
                var reposE4 = container.Resolve<GenericRepository<TestRowPermissions4.E4>>();

                reposE4.Save(itemsE4, null, reposE4.Load());
                reposE3.Save(itemsE3, null, reposE3.Load());
                reposE2.Save(itemsE2, null, reposE2.Load());
                reposE1.Save(itemsE1, null, reposE1.Load());

                Assert.AreEqual("4", TestUtility.DumpSorted(reposE4.Load<Common.RowPermissionsReadItems>(), item => item.Name4));
                Assert.AreEqual("3->3", TestUtility.DumpSorted(reposE3.Query(null, typeof(Common.RowPermissionsReadItems)).ToList(), item => item.Name3 + "->" + item.E4.Name4));
                Assert.AreEqual("2, 3", TestUtility.DumpSorted(reposE2.Load<Common.RowPermissionsReadItems>(), item => item.Name2));
                Assert.AreEqual("1, 2, 3", TestUtility.DumpSorted(reposE1.Load<Common.RowPermissionsReadItems>(), item => item.Name1));
                Assert.AreEqual("1, 2, 3", TestUtility.DumpSorted(reposE1Browse.Load<Common.RowPermissionsReadItems>(), item => item.Name1Browse));
                Assert.AreEqual("1, 1b, 2, 3", TestUtility.DumpSorted(reposE1BrowseRP.Load<Common.RowPermissionsReadItems>(), item => item.Name1Browse));
            }
        }

        [TestMethod]
        public void AutoInheritInternallyVsFull()
        {
            using (var container = TestContainer.Create())
            {
                var repository = container.Resolve<Common.DomRepository>();
                repository.TestRowPermissionsInheritInternally.ExtensionComplex.Delete(repository.TestRowPermissionsInheritInternally.ExtensionComplex.Query());
                repository.TestRowPermissionsInheritInternally.SimpleDetail.Delete(repository.TestRowPermissionsInheritInternally.SimpleDetail.Query());
                repository.TestRowPermissionsInheritFull.ExtensionComplex.Delete(repository.TestRowPermissionsInheritFull.ExtensionComplex.Query());
                repository.TestRowPermissionsInheritFull.SimpleDetail.Delete(repository.TestRowPermissionsInheritFull.SimpleDetail.Query());
                repository.TestRowPermissionsExternal.SimpleParent.Delete(repository.TestRowPermissionsExternal.SimpleParent.Query());
                repository.TestRowPermissionsExternal.SimpleBase.Delete(repository.TestRowPermissionsExternal.SimpleBase.Query());

                var names = new[] { "x", "p", "b", "d", "ec" };
                var itemsParent = names.Select(name => new TestRowPermissionsExternal.SimpleParent { ID = Guid.NewGuid(), Name = name }).ToList();
                var itemsBase = names.Select(name => new TestRowPermissionsExternal.SimpleBase { ID = Guid.NewGuid(), Name = name }).ToList();
                var itemsDetailFull = names.Select((name, x) => new TestRowPermissionsInheritFull.SimpleDetail { ID = Guid.NewGuid(), Name = name, ParentID = itemsParent[x].ID }).ToList();
                var itemsExtensionComplexFull = names.Select((name, x) => new TestRowPermissionsInheritFull.ExtensionComplex { ID = itemsBase[x].ID, Name = name, SimpleDetailID = itemsDetailFull[x].ID }).ToList();
                var itemsDetailInt = names.Select((name, x) => new TestRowPermissionsInheritInternally.SimpleDetail { ID = Guid.NewGuid(), Name = name, ParentID = itemsParent[x].ID }).ToList();
                var itemsExtensionComplexInt = names.Select((name, x) => new TestRowPermissionsInheritInternally.ExtensionComplex { ID = itemsBase[x].ID, Name = name, SimpleDetailID = itemsDetailInt[x].ID }).ToList();

                repository.TestRowPermissionsExternal.SimpleParent.Insert(itemsParent);
                repository.TestRowPermissionsExternal.SimpleBase.Insert(itemsBase);
                repository.TestRowPermissionsInheritFull.SimpleDetail.Insert(itemsDetailFull);
                repository.TestRowPermissionsInheritFull.ExtensionComplex.Insert(itemsExtensionComplexFull);
                repository.TestRowPermissionsInheritInternally.SimpleDetail.Insert(itemsDetailInt);
                repository.TestRowPermissionsInheritInternally.ExtensionComplex.Insert(itemsExtensionComplexInt);

                Assert.AreEqual("p", TestUtility.DumpSorted(repository.TestRowPermissionsExternal.SimpleParent.Query(new Common.RowPermissionsReadItems()), item => item.Name));
                Assert.AreEqual("b", TestUtility.DumpSorted(repository.TestRowPermissionsExternal.SimpleBase.Query(new Common.RowPermissionsReadItems()), item => item.Name));

                Assert.AreEqual("d, p", TestUtility.DumpSorted(repository.TestRowPermissionsInheritFull.SimpleDetail.Query(new Common.RowPermissionsReadItems()), item => item.Name));
                Assert.AreEqual("b, d, ec, p", TestUtility.DumpSorted(repository.TestRowPermissionsInheritFull.ExtensionComplex.Query(new Common.RowPermissionsReadItems()), item => item.Name));

                Assert.AreEqual("d", TestUtility.DumpSorted(repository.TestRowPermissionsInheritInternally.SimpleDetail.Query(new Common.RowPermissionsReadItems()), item => item.Name));
                Assert.AreEqual("d, ec", TestUtility.DumpSorted(repository.TestRowPermissionsInheritInternally.ExtensionComplex.Query(new Common.RowPermissionsReadItems()), item => item.Name));
            }
        }

        [TestMethod]
        public void SelfReference()
        {
            using (var container = TestContainer.Create())
            {
                var repository = container.Resolve<Common.DomRepository>();

                var testData = new[]
                {
                    new { Employee = "E1", Supervisor = "E3" },
                    new { Employee = "E2", Supervisor = "E4" },
                    new { Employee = "E3", Supervisor = "Jane" },
                    new { Employee = "E4", Supervisor = "John" },
                    new { Employee = "Jane", Supervisor = "E5" },
                    new { Employee = "John", Supervisor = "E6" },
                    new { Employee = "E5", Supervisor = "E5" },
                    new { Employee = "E6", Supervisor = "E6" },
                };

                var employees = testData.Select(t => new TestRowPermissions5.Employee
                {
                    ID = Guid.NewGuid(),
                    Name = t.Employee,
                }).ToList();

                foreach (var employee in employees)
                    employee.SupervisorID = employee.ID;

                repository.TestRowPermissions5.Employee.Insert(employees);

                foreach (var t in testData)
                {
                    var employee = employees.Single(e => e.Name == t.Employee);
                    var supervisor = t.Supervisor != null ? employees.Single(s => s.Name == t.Supervisor) : null;
                    employee.SupervisorID = supervisor?.ID;
                }

                repository.TestRowPermissions5.Employee.Update(employees);

                var employeeIds = employees.Select(e => e.ID).ToList();

                var rpEmployees = repository.TestRowPermissions5.Employee.Filter(
                    repository.TestRowPermissions5.Employee.Query(employeeIds),
                    new Common.RowPermissionsReadItems());
                Assert.AreEqual("E3-Jane, E4-John, Jane-E5, John-E6",
                    TestUtility.DumpSorted(rpEmployees, e => e.Name + "-" + e.Supervisor.Name));

                var rpEmployeesBrowse = repository.TestRowPermissions5.EmployeeBrowse.Filter(
                    repository.TestRowPermissions5.EmployeeBrowse.Query(employeeIds),
                    new Common.RowPermissionsReadItems());
                Assert.AreEqual("E3-Jane, E4-John, Jane-E5, John-E6",
                    TestUtility.DumpSorted(rpEmployeesBrowse, e => e.Name + "-" + e.SupervisorName));
            }
        }
    }
}
