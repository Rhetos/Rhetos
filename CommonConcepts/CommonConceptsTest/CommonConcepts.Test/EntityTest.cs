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
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Logging;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonConcepts.Test
{
    [TestClass]
    public class EntityTest
    {
        [TestMethod]
        public void QuerySimple()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(
                    "DELETE FROM TestEntity.Claim",
                    "INSERT INTO TestEntity.Claim (ClaimResource, ClaimRight) SELECT 'res1', 'rig1'",
                    "INSERT INTO TestEntity.Claim (ClaimResource, ClaimRight) SELECT 'res2', 'rig2'");

                var repository = container.Resolve<Common.DomRepository>();
                var loaded = repository.TestEntity.Claim.Query();
                Assert.AreEqual("res1.rig1, res2.rig2", TestUtility.DumpSorted(loaded, c => c.ClaimResource + "." + c.ClaimRight));
            }
        }

        [TestMethod]
        public void QueryComplex()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                container.Resolve<ISqlExecuter>().ExecuteSql(
                    "DELETE FROM TestEntity.Permission",
                    "DELETE FROM TestEntity.Principal",
                    "DELETE FROM TestEntity.Claim",
                    "INSERT INTO TestEntity.Claim (ID, ClaimResource, ClaimRight) SELECT '4074B807-FA5A-4772-9631-198E89A302DE', 'res1', 'rig1'",
                    "INSERT INTO TestEntity.Claim (ID, ClaimResource, ClaimRight) SELECT 'A45F7194-7288-4B25-BC77-4FCC920A1479', 'res2', 'rig2'",
                    "INSERT INTO TestEntity.Principal (ID, Name) SELECT 'A45F7194-7288-4B25-BC77-4FCC920A1479', 'p1'",
                    "INSERT INTO TestEntity.Permission (ID, PrincipalID, ClaimID, IsAuthorized) SELECT '65D4B68E-B0E7-491C-9405-800F531866CA', 'A45F7194-7288-4B25-BC77-4FCC920A1479', '4074B807-FA5A-4772-9631-198E89A302DE', 0",
                    "INSERT INTO TestEntity.Permission (ID, PrincipalID, ClaimID, IsAuthorized) SELECT 'B7F19BA7-C70F-46ED-BFC7-29A44DFECA9B', 'A45F7194-7288-4B25-BC77-4FCC920A1479', 'A45F7194-7288-4B25-BC77-4FCC920A1479', 1");

                var q1 = repository.TestEntity.Principal.Query();
                var q2 = repository.TestEntity.Permission.Query();
                var loaded =
                    from principal in q1
                    from permission in q2
                    where principal.Name == "p1" && permission.Principal == principal && permission.IsAuthorized.Value
                    select permission.Claim.ClaimResource + "." + permission.Claim.ClaimRight;

                Assert.AreEqual("res2.rig2", loaded.Single());
            }
        }

        [TestMethod]
        public void ReferencedEntity()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                container.Resolve<ISqlExecuter>().ExecuteSql(
                    "DELETE FROM TestEntity.Permission",
                    "DELETE FROM TestEntity.Principal",
                    "DELETE FROM TestEntity.Claim",
                    "INSERT INTO TestEntity.Claim (ID, ClaimResource, ClaimRight) SELECT '4074B807-FA5A-4772-9631-198E89A302DE', 'res1', 'rig1'",
                    "INSERT INTO TestEntity.Claim (ID, ClaimResource, ClaimRight) SELECT 'A45F7194-7288-4B25-BC77-4FCC920A1479', 'res2', 'rig2'",
                    "INSERT INTO TestEntity.Principal (ID, Name) SELECT 'A45F7194-7288-4B25-BC77-4FCC920A1479', 'p1'",
                    "INSERT INTO TestEntity.Permission (ID, PrincipalID, ClaimID, IsAuthorized) SELECT '65D4B68E-B0E7-491C-9405-800F531866CA', 'A45F7194-7288-4B25-BC77-4FCC920A1479', '4074B807-FA5A-4772-9631-198E89A302DE', 0",
                    "INSERT INTO TestEntity.Permission (ID, PrincipalID, ClaimID, IsAuthorized) SELECT 'B7F19BA7-C70F-46ED-BFC7-29A44DFECA9B', 'A45F7194-7288-4B25-BC77-4FCC920A1479', 'A45F7194-7288-4B25-BC77-4FCC920A1479', 1");

                var permission = repository.TestEntity.Permission.Query().Where(perm => perm.IsAuthorized == true).Single();
                Assert.AreEqual(true, permission.IsAuthorized);
                Assert.AreEqual("p1", permission.Principal.Name);

                var permission2 = repository.TestEntity.Permission.Query().Where(perm => perm.IsAuthorized == false).Single();
                Assert.AreEqual(false, permission2.IsAuthorized);
                Assert.AreEqual("p1", permission2.Principal.Name);
            }
        }

        private static string ReportClaims(Common.DomRepository repository)
        {
            var loaded = repository.TestEntity.Claim.Load();
            var report = TestUtility.DumpSorted(loaded, claim => claim.ClaimResource + "-" + claim.ClaimRight);
            Console.WriteLine("Report: " + report);
            return report;
        }

        [TestMethod]
        public void InsertUpdateDelete_TransientInstances()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var claims = repository.TestEntity.Claim;

                container.Resolve<ISqlExecuter>().ExecuteSql("DELETE FROM TestEntity.Claim");
                Assert.AreEqual("", ReportClaims(repository), "initial");

                var newClaims = new[]
                            {
                                new TestEntity.Claim {ClaimResource = "r1", ClaimRight = "cr1"},
                                new TestEntity.Claim {ClaimResource = "r2", ClaimRight = "cr2"}
                            };
                claims.Insert(newClaims);
                Assert.AreEqual("r1-cr1, r2-cr2", ReportClaims(repository), "after insert");

                claims.Update(new TestEntity.Claim { ID = newClaims[1].ID, ClaimResource = "x2", ClaimRight = "xx2" });
                Assert.AreEqual("r1-cr1, x2-xx2", ReportClaims(repository), "after update");

                claims.Delete(new TestEntity.Claim { ID = newClaims[0].ID }, new TestEntity.Claim { ID = newClaims[1].ID });
                Assert.AreEqual("", ReportClaims(repository), "after delete");
            }
        }

        [TestMethod]
        public void UpdateDelete_PersistentInstances()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var claims = repository.TestEntity.Claim;

                container.Resolve<ISqlExecuter>().ExecuteSql("DELETE FROM TestEntity.Claim");
                Assert.AreEqual("", ReportClaims(repository), "initial");

                var newClaims = new[]
                            {
                                new TestEntity.Claim {ClaimResource = "r1", ClaimRight = "cr1"},
                                new TestEntity.Claim {ClaimResource = "r2", ClaimRight = "cr2"}
                            };
                claims.Insert(newClaims);
                Assert.AreEqual("r1-cr1, r2-cr2", ReportClaims(repository), "initial insert");

                var loaded = repository.TestEntity.Claim.Load().OrderBy(c => c.ClaimResource).ToList();
                Assert.AreEqual(2, loaded.Count());

                loaded[1].ClaimResource = "x2";
                loaded[1].ClaimRight = "xx2";
                claims.Update(loaded);
                Assert.AreEqual("r1-cr1, x2-xx2", ReportClaims(repository), "after update");

                claims.Delete(loaded);
                Assert.AreEqual("", ReportClaims(repository), "after delete");
            }
        }

        [TestMethod]
        public void UpdateableExtendedTable()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                {
                    "DELETE FROM TestEntity.Extension",
                    "DELETE FROM TestEntity.BaseEntity",
                    "INSERT INTO TestEntity.BaseEntity (ID, Name) SELECT '5B08EE49-3FC3-47B7-9E1D-4B162E7CFF00', 'a'",
                    "INSERT INTO TestEntity.BaseEntity (ID, Name) SELECT '0BA6DC94-C146-4E81-B80F-4F5A9D2205E5', 'b'",
                    "INSERT INTO TestEntity.Extension (ID, Title) SELECT '5B08EE49-3FC3-47B7-9E1D-4B162E7CFF00', 'aaa'",
                });
                Assert.AreEqual(1, repository.TestEntity.Extension.Query().ToList().Count());

                var extensions = repository.TestEntity.Extension;

                extensions.Delete(repository.TestEntity.Extension.Query().Where(item => item.ID == new Guid("5B08EE49-3FC3-47B7-9E1D-4B162E7CFF00")));
                Assert.AreEqual(0, repository.TestEntity.Extension.Query().ToList().Count());

                extensions.Insert(new TestEntity.Extension { ID = new Guid("0BA6DC94-C146-4E81-B80F-4F5A9D2205E5"), Title = "bbb" });
                Assert.AreEqual(1, repository.TestEntity.Extension.Query().ToList().Count());

                extensions.Update(new TestEntity.Extension { ID = new Guid("0BA6DC94-C146-4E81-B80F-4F5A9D2205E5"), Title = "xxx" });
                Assert.AreEqual(1, repository.TestEntity.Extension.Query().ToList().Count());
                Assert.AreEqual("xxx", repository.TestEntity.Extension.Query().Single().Title);
            }
        }

        [TestMethod]
        public void InsertTransient()
        {
            using (var container = new RhetosTestContainer())
            {
                var context = container.Resolve<Common.ExecutionContext>();
                var repository = container.Resolve<Common.DomRepository>();

                var b = new TestEntity.BaseEntity { ID = Guid.NewGuid(), Name = "b" };
                var c = new TestEntity.Child { Name = "c", ParentID = b.ID };
                repository.TestEntity.BaseEntity.Insert(b);
                repository.TestEntity.Child.Insert(c);

                Assert.AreNotEqual(default(Guid), c.ID);

                var report = repository.TestEntity.Child.Query().Where(item => item.ID == c.ID)
                    .Select(item => item.Name + " " + item.Parent.Name);
                Assert.AreEqual("c b", TestUtility.DumpSorted(report));
            }
        }

        [TestMethod]
        public void UpdateTransient()
        {
            using (var container = new RhetosTestContainer())
            {
                var context = container.Resolve<Common.ExecutionContext>();
                var repository = container.Resolve<Common.DomRepository>();

                var b = new TestEntity.BaseEntity { ID = Guid.NewGuid(), Name = "b" };
                var c = new TestEntity.Child { Name = "c", ParentID = b.ID };
                repository.TestEntity.BaseEntity.Insert(b);
                repository.TestEntity.Child.Insert(c);

                var c2 = new TestEntity.Child { ID = c.ID, Name = "c2", ParentID = b.ID };
                repository.TestEntity.Child.Update(c2);

                var report = repository.TestEntity.Child.Query().Where(item => item.ID == c.ID)
                    .Select(item => item.Name + " " + item.Parent.Name);
                Assert.AreEqual("c2 b", TestUtility.DumpSorted(report));
            }
        }

        [TestMethod]
        public void InsertPersistent()
        {
            using (var container = new RhetosTestContainer())
            {
                var context = container.Resolve<Common.ExecutionContext>();
                var repository = container.Resolve<Common.DomRepository>();

                repository.TestEntity.Child.Delete(repository.TestEntity.Child.Load());
                repository.TestEntity.BaseEntity.Delete(repository.TestEntity.BaseEntity.Load());

                var b = new TestEntity.BaseEntity { ID = Guid.NewGuid(), Name = "b" };
                var c = new TestEntity.Child { Name = "c", ParentID = b.ID };
                repository.TestEntity.BaseEntity.Insert(b);
                repository.TestEntity.Child.Insert(c);

                var c2 = repository.TestEntity.Child.Query()
                    .Select(child => new TestEntity.Child { ID = Guid.NewGuid(), Name = "c2", ParentID = child.ParentID })
                    .ToList();
                repository.TestEntity.Child.Insert(c2);

                var ids = repository.TestEntity.Child.Query().Select(child => child.ID).ToList();
                Assert.AreEqual(2, ids.Count());
                Assert.AreNotEqual(default(Guid), ids[0]);
                Assert.AreNotEqual(default(Guid), ids[1]);
                Assert.AreNotEqual(ids[0], ids[1]);

                var report = repository.TestEntity.Child.Query()
                    .Select(item => item.Name + " " + item.Parent.Name);
                Assert.AreEqual("c b, c2 b", TestUtility.DumpSorted(report));
            }
        }

        [TestMethod]
        public void UpdatePersistent()
        {
            using (var container = new RhetosTestContainer())
            {
                var context = container.Resolve<Common.ExecutionContext>();
                var repository = container.Resolve<Common.DomRepository>();

                var b = new TestEntity.BaseEntity { ID = Guid.NewGuid(), Name = "b" };
                var c = new TestEntity.Child { Name = "c", ParentID = b.ID };
                repository.TestEntity.BaseEntity.Insert(b);
                repository.TestEntity.Child.Insert(c);

                var b2 = repository.TestEntity.BaseEntity.Query().Where(item => item.ID == b.ID).Single();
                b2.Name = "b2"; // Should not be saved when calling Child.Update.

                var c2 = repository.TestEntity.Child.Query().Where(item => item.ID == c.ID).Single();
                Console.WriteLine("c2.GetType().FullName: " + c2.GetType().FullName);
                c2.Name = "c2";
                c2.Parent.Name = "b3"; // Should not be saved when calling Child.Update.
                repository.TestEntity.Child.Update(c2);

                var report = repository.TestEntity.Child.Query().Where(item => item.ID == c.ID)
                    .Select(item => item.Name + " " + item.Parent.Name);
                Assert.AreEqual("c2 b", TestUtility.DumpSorted(report));
            }
        }

        [TestMethod]
        public void DeleteInsertSame()
        {
            using (var container = new RhetosTestContainer())
            {
                var context = container.Resolve<Common.ExecutionContext>();
                var repository = container.Resolve<Common.DomRepository>();
                repository.TestEntity.BaseEntity.Delete(repository.TestEntity.BaseEntity.Load());

                var b = new TestEntity.BaseEntity { ID = Guid.NewGuid(), Name = "b" };
                repository.TestEntity.BaseEntity.Insert(b);

                Assert.AreEqual("b", TestUtility.DumpSorted(repository.TestEntity.BaseEntity.Query(), item => item.Name));

                var b2 = new TestEntity.BaseEntity { ID = b.ID, Name = "b2" };
                repository.TestEntity.BaseEntity.Save(new[] { b2 }, null, new[] { b });

                Assert.AreEqual("b2", TestUtility.DumpSorted(repository.TestEntity.BaseEntity.Query(), item => item.Name));
            }
        }

        [TestMethod]
        public void DeleteInsertSamePersisted()
        {
            using (var container = new RhetosTestContainer())
            {
                var context = container.Resolve<Common.ExecutionContext>();
                var repository = container.Resolve<Common.DomRepository>();
                repository.TestEntity.BaseEntity.Delete(repository.TestEntity.BaseEntity.Load());

                var b = new TestEntity.BaseEntity { ID = Guid.NewGuid(), Name = "b" };
                repository.TestEntity.BaseEntity.Insert(b);

                Assert.AreEqual("b", TestUtility.DumpSorted(repository.TestEntity.BaseEntity.Query(), item => item.Name));

                var b2 = repository.TestEntity.BaseEntity.Query().Where(item => item.ID == b.ID).Single();
                b2.Name = "b2";
                repository.TestEntity.BaseEntity.Save(new[] { b2 }, null, new[] { b });

                Assert.AreEqual("b2", TestUtility.DumpSorted(repository.TestEntity.BaseEntity.Query(), item => item.Name));
            }
        }

        [TestMethod]
        public void CascadeDelete()
        {
            using (var container = new RhetosTestContainer())
            {
                var log = new List<string>();
                container.AddLogMonitor(log, EventType.Info);
                var repository = container.Resolve<Common.DomRepository>();

                var pid1 = Guid.NewGuid();
                var pid2 = Guid.NewGuid();
                var pid3 = Guid.NewGuid();
                var cid11 = Guid.NewGuid();
                var cid12 = Guid.NewGuid();
                var cid21 = Guid.NewGuid();
                var cid31 = Guid.NewGuid();

                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                {
                    "DELETE FROM TestEntity.Child",
                    "DELETE FROM TestEntity.BaseEntity",
                    "INSERT INTO TestEntity.BaseEntity (ID, Name) SELECT '" + pid1 + "', '1'",
                    "INSERT INTO TestEntity.BaseEntity (ID, Name) SELECT '" + pid2+ "', '2'",
                    "INSERT INTO TestEntity.BaseEntity (ID, Name) SELECT '" + pid3 + "', '3'",
                    "INSERT INTO TestEntity.Child (ID, Name, ParentID) SELECT '" + cid11 + "', '1a', '" + pid1 + "'",
                    "INSERT INTO TestEntity.Child (ID, Name, ParentID) SELECT '" + cid12 + "', '1b', '" + pid1 + "'",
                    "INSERT INTO TestEntity.Child (ID, Name, ParentID) SELECT '" + cid21 + "', '2a', '" + pid2 + "'",
                    "INSERT INTO TestEntity.Child (ID, Name, ParentID) SELECT '" + cid31 + "', '3a', '" + pid3 + "'",
                });

                Assert.AreEqual("1a, 1b, 2a, 3a", TestUtility.DumpSorted(repository.TestEntity.Child.Query(), item => item.Name));

                log.Clear();
                repository.TestEntity.BaseEntity.Delete(new [] { new TestEntity.BaseEntity { ID = pid1 }, new TestEntity.BaseEntity { ID = pid2 } });

                Assert.AreEqual("3a", TestUtility.DumpSorted(repository.TestEntity.Child.Query(), item => item.Name));
                Assert.AreEqual("[Info] Child.Deletions: 1a, 1b, 2a.", TestUtility.Dump(log), "Deletion of detail entity should be done through the object model (not just deleted in database by cascade delete).");
            }
        }

        [TestMethod]
        public void ShortStringPropertyBasicRhetosTypeValidation()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql("DELETE FROM TestEntity.Principal");
                var repository = container.Resolve<Common.DomRepository>();

                TestEntity.Principal item = new TestEntity.Principal();
                item.Name = new string('x', 256);
                repository.TestEntity.Principal.Insert(item);

                TestUtility.ShouldFail(
                    () =>
                    {
                        item.Name = new string('x', 257);
                        repository.TestEntity.Principal.Update(item);
                    },
                    "Principal", "Name", "256");
            }
        }

        [TestMethod]
        public void LargeText()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql("DELETE FROM TestEntity.Large");
                var repository = container.Resolve<Common.DomRepository>();

                var item = new TestEntity.Large { Text = new string('x', 1024 * 1024) };
                repository.TestEntity.Large.Insert(item);

                var loaded = repository.TestEntity.Large.Load().Single().Text;
                Assert.AreEqual(item.Text.Length, loaded.Length);
                Assert.AreEqual(item.Text.GetHashCode(), loaded.GetHashCode());
            }
        }

        const double ExpectedDateTimePrecisionSeconds = 0.01;

        static void AssertIsNear(DateTime expected, DateTime actual)
        {
            DateTime start = expected.AddSeconds(-ExpectedDateTimePrecisionSeconds);
            DateTime end = expected.AddSeconds(ExpectedDateTimePrecisionSeconds);

            var failMessage = new Lazy<string>(() => "Given value " + actual.ToString("o") + " is not near " + expected.ToString("o") + " within +/- " + ExpectedDateTimePrecisionSeconds + " seconds.");
            var passMessage = new Lazy<string>(() => "Given value " + actual.ToString("o") + " is near " + expected.ToString("o") + " within +/- " + ExpectedDateTimePrecisionSeconds + " seconds.");

            Assert.IsTrue(actual >= start, failMessage.Value);
            Assert.IsTrue(actual <= end, failMessage.Value);
            Console.WriteLine(passMessage.Value);
        }

        [TestMethod]
        public void DateTimeTest()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql("DELETE FROM TestTypes.Simple");
                var repository = container.Resolve<Common.DomRepository>();

                for (int i = 0; i < 7; i++)
                {
                    DateTime testTime = new DateTime(2013, 10, 27, 10, 45, 54, i * 1000 / 7);

                    var item = new TestTypes.Simple { Start = testTime };
                    repository.TestTypes.Simple.Insert(item);

                    var loaded = repository.TestTypes.Reader.Load().Single();
                    AssertIsNear(testTime, loaded.Start.Value);

                    repository.TestTypes.Simple.Delete(item);
                }
            }
        }

        [TestMethod]
        public void DecimalTest()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql("DELETE FROM TestTypes.Simple");
                var repository = container.Resolve<Common.DomRepository>();

                // Decimal(28,10) allows 18 digits before the decimal point and 10 digits after.
                var s = new TestTypes.Simple { Length = 123456789012345678.0123456789m };
                Assert.AreEqual("123456789012345678.0123456789", s.Length.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                repository.TestTypes.Simple.Insert(s);

                var loaded = repository.TestTypes.Reader.Load().Single();
                Assert.AreEqual(s.Length, loaded.Length);
            }
        }

        [TestMethod]
        public void SaveNonmaterialized()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql("DELETE FROM TestEntity.UniqueEntity");
                var r = container.Resolve<Common.DomRepository>().TestEntity.UniqueEntity;

                var query = new[] { "a", "b", "c" }.Select(name => new TestEntity.UniqueEntity { Name = name });
                Assert.IsFalse(query is ICollection);
                Assert.IsFalse(query is IList);

                r.Insert(query);
                Assert.AreEqual("a, b, c", TestUtility.DumpSorted(r.Query(), item => item.Name));
            }
        }

        [TestMethod]
        public void SaveInvalidRecord_Insert()
        {
            using (var container = new RhetosTestContainer())
            {
                var repos = container.Resolve<Common.DomRepository>().TestEntity.Principal;

                var item1 = new TestEntity.Principal { Name = "a", ID = Guid.NewGuid() };
                repos.Insert(item1);

                var item2 = new TestEntity.Principal { Name = "b", ID = item1.ID };
                TestUtility.ShouldFail<Rhetos.ClientException>(() => repos.Save(new[] { item2 }, null, null, checkUserPermissions: true),
                    "Inserting a record that already exists in database.", item2.ID.ToString());
            }
        }

        [TestMethod]
        public void SaveInvalidRecord_SystemInsert()
        {
            using (var container = new RhetosTestContainer())
            {
                var repos = container.Resolve<Common.DomRepository>().TestEntity.Principal;

                var item1 = new TestEntity.Principal { Name = "a", ID = Guid.NewGuid() };
                repos.Insert(item1);

                var item2 = new TestEntity.Principal { Name = "b", ID = item1.ID };
                var error = TestUtility.ShouldFail<Rhetos.FrameworkException>(() => repos.Save(new[] { item2 }, null, null, checkUserPermissions: false));
                TestUtility.AssertContains(error.ToString(), new[] { "Inserting a record that already exists in database.", item2.ID.ToString(), "PK_Principal" });
            }
        }

        [TestMethod]
        public void SaveInvalidRecord_Update()
        {
            using (var container = new RhetosTestContainer())
            {
                var repos = container.Resolve<Common.DomRepository>().TestEntity.Principal;

                var item1 = new TestEntity.Principal { Name = "1", ID = Guid.NewGuid() };
                var item2 = new TestEntity.Principal { Name = "2", ID = Guid.NewGuid() };
                var item3 = new TestEntity.Principal { Name = "3", ID = Guid.NewGuid() };
                var item4 = new TestEntity.Principal { Name = "4", ID = Guid.NewGuid() };

                repos.Insert(item1, item4);

                var error = TestUtility.ShouldFail<Rhetos.ClientException>(
                    () => repos.Save(null, new[] { item1, item2, item3, item4 }, null, checkUserPermissions: true),
                    "Updating a record that does not exist in database.");

                Assert.IsTrue(error.Message.Contains(item2.ID.ToString()) || error.Message.Contains(item3.ID.ToString()));
            }
        }

        [TestMethod]
        public void SaveInvalidRecord_SystemUpdate()
        {
            using (var container = new RhetosTestContainer())
            {
                var repos = container.Resolve<Common.DomRepository>().TestEntity.Principal;

                var item1 = new TestEntity.Principal { Name = "1", ID = Guid.NewGuid() };
                var item2 = new TestEntity.Principal { Name = "2", ID = Guid.NewGuid() };
                var item3 = new TestEntity.Principal { Name = "3", ID = Guid.NewGuid() };
                var item4 = new TestEntity.Principal { Name = "4", ID = Guid.NewGuid() };

                repos.Insert(item1, item4);

                var error = TestUtility.ShouldFail<Rhetos.FrameworkException>(
                    () => repos.Save(null, new[] { item1, item2, item3, item4 }, null, checkUserPermissions: false),
                    "Updating a record that does not exist in database.");

                Assert.IsTrue(error.ToString().Contains(item2.ID.ToString()) || error.ToString().Contains(item3.ID.ToString()));
            }
        }

        [TestMethod]
        public void SaveInvalidRecord_Delete()
        {
            using (var container = new RhetosTestContainer())
            {
                var repos = container.Resolve<Common.DomRepository>().TestEntity.Principal;

                var item = new TestEntity.Principal { Name = "a", ID = Guid.NewGuid() };
                TestUtility.ShouldFail<Rhetos.ClientException>(() => repos.Save(null, null, new[] { item }, checkUserPermissions: true),
                    "Deleting a record that does not exist in database.", item.ID.ToString());
            }
        }

        [TestMethod]
        public void SaveInvalidRecordMultiple_Insert()
        {
            using (var container = new RhetosTestContainer())
            {
                var repos = container.Resolve<Common.DomRepository>().TestEntity.Principal;

                var item1 = new TestEntity.Principal { Name = "a", ID = Guid.NewGuid() };
                var item2 = new TestEntity.Principal { Name = "b", ID = item1.ID };
                TestUtility.ShouldFail<Rhetos.ClientException>(() => repos.Save(new[] { item1, item2 }, null, null, checkUserPermissions: true),
                    "Inserting a record that already exists in database.", item2.ID.ToString());
            }
        }

        [TestMethod]
        public void SaveInvalidRecordMultiple_InsertDelete()
        {
            using (var container = new RhetosTestContainer())
            {
                var repos = container.Resolve<Common.DomRepository>().TestEntity.Principal;

                var item1 = new TestEntity.Principal { Name = "a", ID = Guid.NewGuid() };
                repos.Insert(item1);
                Assert.AreEqual("a", repos.Query(new[] { item1.ID }).Single().Name);

                var item2 = new TestEntity.Principal { Name = "b", ID = item1.ID };
                repos.Save(new[] { item2 }, null, new[] { item1 }, checkUserPermissions: true);
                Assert.AreEqual("b", repos.Query(new[] { item1.ID }).Single().Name);
            }
        }


        [TestMethod]
        public void DeleteUpdateInsert_ConflictUnique()
        {
            foreach (bool useDatabaseNullSemantics in new[] { false, true })
                using (var container = new RhetosTestContainer())
                {
                    container.SetUseDatabaseNullSemantics(useDatabaseNullSemantics);
                    container.Resolve<ISqlExecuter>().ExecuteSql("DELETE FROM TestEntity.UniqueEntity");
                    var r = container.Resolve<Common.DomRepository>().TestEntity.UniqueEntity;
                    var context = container.Resolve<Common.ExecutionContext>();

                    var ia = new TestEntity.UniqueEntity { Name = "a", ID = Guid.NewGuid() };
                    var ib = new TestEntity.UniqueEntity { Name = "b", ID = Guid.NewGuid() };
                    var ic1 = new TestEntity.UniqueEntity { Name = "c", ID = Guid.NewGuid() };

                    r.Insert(ia, ib, ic1);
                    Assert.AreEqual("a, b, c", TestUtility.DumpSorted(r.Load(), item => item.Name + item.Data));

                    // Deleting old 'c' and inserting new 'c'. Possible conflict on unique constraint for property Name.

                    var ic2 = new TestEntity.UniqueEntity { Name = "c", ID = Guid.NewGuid() };

                    r.Save(new[] { ic2 }, null, new[] { ic1 });
                    Assert.AreEqual("a, b, c", TestUtility.DumpSorted(r.Load(), item => item.Name + item.Data));
                    Guid currentCID = r.Query().Where(item => item.Name == "c").Select(item => item.ID).Single();
                    Assert.AreEqual(ic2.ID, currentCID, "new inserted item 'c'");
                    Assert.AreNotEqual(ic1.ID, currentCID, "old deleted item 'c'");

                    // Deleting old 'c' and inserting new 'c' with same ID. Possible conflict on primary key.

                    var ic3 = new TestEntity.UniqueEntity { Name = "c", Data = "x", ID = ic2.ID };

                    r.Save(new[] { ic3 }, null, new[] { ic2 });
                    Assert.AreEqual("a, b, cx", TestUtility.DumpSorted(r.Load(), item => item.Name + item.Data));

                    // Renaming old 'c' and inserting new 'c'. Possible conflict on unique constraint for property Name.

                    ic3.Name = "oldc";
                    var ic4 = new TestEntity.UniqueEntity { Name = "c", ID = Guid.NewGuid() };

                    r.Save(new[] { ic4 }, new[] { ic3 }, null);
                    Assert.AreEqual("a, b, c, oldcx", TestUtility.DumpSorted(r.Load(), item => item.Name + item.Data));
                }
        }

        [TestMethod]
        public void SimpleReferenceProperty()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql("DELETE FROM TestEntity.BaseEntity");
                var repository = container.Resolve<Common.DomRepository>();
                var context = container.Resolve<Common.ExecutionContext>();

                var b1 = new TestEntity.BaseEntity { Name = "b1" };
                var b2 = new TestEntity.BaseEntity { Name = "b2" };
                repository.TestEntity.BaseEntity.Insert(b1, b2);

                var c1 = new TestEntity.Child { Name = "c1", ParentID = b1.ID };
                repository.TestEntity.Child.Insert(c1);
                Assert.AreEqual("c1b1", TestUtility.DumpSorted(repository.TestEntity.Child.Query(), item => item.Name + item.Parent.Name));

                b1 = repository.TestEntity.BaseEntity.Query().Where(item => item.ID == b1.ID).Single();
                c1 = repository.TestEntity.Child.Query().Where(item => item.ID == c1.ID).Single();

                Assert.AreEqual("c1b1", TestUtility.DumpSorted(repository.TestEntity.Child.Query()
                    .Where(c => c.ParentID == b1.ID), item => item.Name + item.Parent.Name));

                c1.ParentID = b2.ID;
                repository.TestEntity.Child.Update(c1);
                Assert.AreEqual("c1b2", TestUtility.DumpSorted(repository.TestEntity.Child.Query(), item => item.Name + item.Parent.Name));
            }
        }

        [TestMethod]
        public void OptimizedDeleteQuery()
        {
            using (var container = new RhetosTestContainer())
            {
                var sqlExecuter = container.Resolve<ISqlExecuter>();
                var repository = container.Resolve<Common.DomRepository>();

                repository.TestEntity.UniqueEntity.Delete(repository.TestEntity.UniqueEntity.Load());
                var newItem = new TestEntity.UniqueEntity { ID = Guid.NewGuid(), Name = "a", Data = "b" };
                repository.TestEntity.UniqueEntity.Insert(newItem);

                // Temporarily removing a column to detect if the following code will try to read it.
                sqlExecuter.ExecuteSql("ALTER TABLE TestEntity.UniqueEntity DROP COLUMN Data");

                IEnumerable<TestEntity.UniqueEntity> items = repository.TestEntity.UniqueEntity.Query();
                // The following line should not try to read all columns.
                DomHelper.MaterializeItemsToDelete(ref items);
                Assert.AreEqual(1, items.Count());
                Assert.AreEqual(newItem.ID, items.Single().ID);
                Assert.IsNull(items.Single().Name);
                Assert.IsNull(items.Single().Data);
            }
        }
    }
}
