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
using Rhetos.Configuration.Autofac;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using Rhetos.Dom.DefaultConcepts;
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
                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        "DELETE FROM TestEntity.Claim",
                        "INSERT INTO TestEntity.Claim (ClaimResource, ClaimRight) SELECT 'res1', 'rig1'",
                        "INSERT INTO TestEntity.Claim (ClaimResource, ClaimRight) SELECT 'res2', 'rig2'"
                    });

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

                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        "DELETE FROM TestEntity.Permission",
                        "DELETE FROM TestEntity.Principal",
                        "DELETE FROM TestEntity.Claim",
                        "INSERT INTO TestEntity.Claim (ID, ClaimResource, ClaimRight) SELECT '4074B807-FA5A-4772-9631-198E89A302DE', 'res1', 'rig1'",
                        "INSERT INTO TestEntity.Claim (ID, ClaimResource, ClaimRight) SELECT 'A45F7194-7288-4B25-BC77-4FCC920A1479', 'res2', 'rig2'",
                        "INSERT INTO TestEntity.Principal (ID, Name) SELECT 'A45F7194-7288-4B25-BC77-4FCC920A1479', 'p1'",
                        "INSERT INTO TestEntity.Permission (ID, PrincipalID, ClaimID, IsAuthorized) SELECT '65D4B68E-B0E7-491C-9405-800F531866CA', 'A45F7194-7288-4B25-BC77-4FCC920A1479', '4074B807-FA5A-4772-9631-198E89A302DE', 0",
                        "INSERT INTO TestEntity.Permission (ID, PrincipalID, ClaimID, IsAuthorized) SELECT 'B7F19BA7-C70F-46ED-BFC7-29A44DFECA9B', 'A45F7194-7288-4B25-BC77-4FCC920A1479', 'A45F7194-7288-4B25-BC77-4FCC920A1479', 1"
                    });

                var loaded =
                    from principal in repository.TestEntity.Principal.Query()
                    from permission in repository.TestEntity.Permission.Query()
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

                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        "DELETE FROM TestEntity.Permission",
                        "DELETE FROM TestEntity.Principal",
                        "DELETE FROM TestEntity.Claim",
                        "INSERT INTO TestEntity.Claim (ID, ClaimResource, ClaimRight) SELECT '4074B807-FA5A-4772-9631-198E89A302DE', 'res1', 'rig1'",
                        "INSERT INTO TestEntity.Claim (ID, ClaimResource, ClaimRight) SELECT 'A45F7194-7288-4B25-BC77-4FCC920A1479', 'res2', 'rig2'",
                        "INSERT INTO TestEntity.Principal (ID, Name) SELECT 'A45F7194-7288-4B25-BC77-4FCC920A1479', 'p1'",
                        "INSERT INTO TestEntity.Permission (ID, PrincipalID, ClaimID, IsAuthorized) SELECT '65D4B68E-B0E7-491C-9405-800F531866CA', 'A45F7194-7288-4B25-BC77-4FCC920A1479', '4074B807-FA5A-4772-9631-198E89A302DE', 0",
                        "INSERT INTO TestEntity.Permission (ID, PrincipalID, ClaimID, IsAuthorized) SELECT 'B7F19BA7-C70F-46ED-BFC7-29A44DFECA9B', 'A45F7194-7288-4B25-BC77-4FCC920A1479', 'A45F7194-7288-4B25-BC77-4FCC920A1479', 1"
                    });

                var permission = repository.TestEntity.Permission.Query().Where(perm => perm.IsAuthorized == true).Single();
                Assert.AreEqual(true, permission.IsAuthorized);
                Assert.AreEqual("p1", permission.Principal.Name);

                var permission2 = repository.TestEntity.Permission.Query().Where(perm => perm.IsAuthorized == false).Single();
                Assert.AreEqual(false, permission2.IsAuthorized);
                container.Resolve<Common.ExecutionContext>().NHibernateSession.Clear();
                Assert.AreEqual("p1", permission2.Principal.Name, "after NHibernateSession.Clear");
            }
        }

        private static string ReportClaims(Common.DomRepository repository)
        {
            var loaded = repository.TestEntity.Claim.All();
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

                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestEntity.Claim" });
                Assert.AreEqual("", ReportClaims(repository), "initial");

                var newClaims = new[]
                            {
                                new TestEntity.Claim {ClaimResource = "r1", ClaimRight = "cr1"},
                                new TestEntity.Claim {ClaimResource = "r2", ClaimRight = "cr2"}
                            };
                claims.Insert(newClaims);
                Assert.AreEqual("r1-cr1, r2-cr2", ReportClaims(repository), "after insert");

                claims.Update(new[] { new TestEntity.Claim { ID = newClaims[1].ID, ClaimResource = "x2", ClaimRight = "xx2" } });
                Assert.AreEqual("r1-cr1, x2-xx2", ReportClaims(repository), "after update");

                claims.Delete(new[] { new TestEntity.Claim { ID = newClaims[0].ID }, new TestEntity.Claim { ID = newClaims[1].ID } });
                Assert.AreEqual("", ReportClaims(repository), "after delete");
            }
        }

        [TestMethod]
        public void UpdateDelete_PersistendInstances()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var claims = repository.TestEntity.Claim;

                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestEntity.Claim" });
                Assert.AreEqual("", ReportClaims(repository), "initial");

                var newClaims = new[]
                            {
                                new TestEntity.Claim {ClaimResource = "r1", ClaimRight = "cr1"},
                                new TestEntity.Claim {ClaimResource = "r2", ClaimRight = "cr2"}
                            };
                claims.Insert(newClaims);
                Assert.AreEqual("r1-cr1, r2-cr2", ReportClaims(repository), "initial insert");

                var loaded = repository.TestEntity.Claim.All();
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

                extensions.Delete(repository.TestEntity.Extension.Query().Where(item => item.ID == Guid.Parse("5B08EE49-3FC3-47B7-9E1D-4B162E7CFF00")));
                Assert.AreEqual(0, repository.TestEntity.Extension.Query().ToList().Count());

                extensions.Insert(new[] { new TestEntity.Extension { ID = Guid.Parse("0BA6DC94-C146-4E81-B80F-4F5A9D2205E5"), Title = "bbb" } });
                Assert.AreEqual(1, repository.TestEntity.Extension.Query().ToList().Count());

                extensions.Update(new[] { new TestEntity.Extension { ID = Guid.Parse("0BA6DC94-C146-4E81-B80F-4F5A9D2205E5"), Title = "xxx" } });
                Assert.AreEqual(1, repository.TestEntity.Extension.Query().ToList().Count());
                Assert.AreEqual("xxx", repository.TestEntity.Extension.Query().Single().Title);
            }
        }

        [TestMethod]
        public void InsertTransient()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void UpdateTransient()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void InsertPersistent()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void UpdatePersistent()
        {
            throw new NotImplementedException();
        }

        [TestMethod]
        public void UpdatePersistentButDontSave()
        {
            // Update a persistent entity (with lazy evaluation), then save another entity. The first one should not be saved.
            throw new NotImplementedException();
        }
        
        [TestMethod]
        public void CascadeDelete()
        {
            using (var container = new RhetosTestContainer())
            {
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
                    "INSERT INTO TestEntity.Child (ID, Name, ParentID) SELECT '" + cid11 + "', '11', '" + pid1 + "'",
                    "INSERT INTO TestEntity.Child (ID, Name, ParentID) SELECT '" + cid12 + "', '12', '" + pid1 + "'",
                    "INSERT INTO TestEntity.Child (ID, Name, ParentID) SELECT '" + cid21 + "', '21', '" + pid2 + "'",
                    "INSERT INTO TestEntity.Child (ID, Name, ParentID) SELECT '" + cid31 + "', '31', '" + pid3 + "'",
                });

                Assert.AreEqual("11, 12, 21, 31", TestUtility.DumpSorted(repository.TestEntity.Child.All(), item => item.Name));

                repository.TestEntity.BaseEntity.Delete(new [] { new TestEntity.BaseEntity { ID = pid1 }, new TestEntity.BaseEntity { ID = pid2 } });

                Assert.AreEqual("31", TestUtility.DumpSorted(repository.TestEntity.Child.All(), item => item.Name));
            }
        }

        [TestMethod]
        public void ShortStringPropertyBasicRhetosTypeValidation()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestEntity.Principal" });
                var repository = container.Resolve<Common.DomRepository>();

                TestEntity.Principal item = new TestEntity.Principal();
                item.Name = new string('x', 256);
                repository.TestEntity.Principal.Insert(new[] { item });

                TestUtility.ShouldFail(
                    () =>
                    {
                        item.Name = new string('x', 257);
                        repository.TestEntity.Principal.Update(new[] { item });
                    },
                    "Principal", "Name", "256");
            }
        }

        [TestMethod]
        public void LargeText()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestEntity.Large" });
                var repository = container.Resolve<Common.DomRepository>();

                var item = new TestEntity.Large { Text = new string('x', 1024 * 1024) };
                repository.TestEntity.Large.Insert(new[] { item });

                var loaded = repository.TestEntity.Large.All().Single().Text;
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
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestTypes.Simple" });
                var repository = container.Resolve<Common.DomRepository>();

                for (int i = 0; i < 7; i++)
                {
                    DateTime testTime = new DateTime(2013, 10, 27, 10, 45, 54, i * 1000 / 7);

                    var item = new TestTypes.Simple { Start = testTime };
                    repository.TestTypes.Simple.Insert(new[] { item });

                    var loaded = repository.TestTypes.Reader.All().Single();
                    AssertIsNear(testTime, loaded.Start.Value);

                    repository.TestTypes.Simple.Delete(new[] { item });
                }
            }
        }

        [TestMethod]
        public void DecimalSizeTest()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestTypes.Simple" });
                var repository = container.Resolve<Common.DomRepository>();

                var s = new TestTypes.Simple { Length = 1234567890123456789012345678m };
                Assert.AreEqual("1234567890123456789012345678", s.Length.Value.ToString());
                Assert.Inconclusive("NHibernate's limit");
                repository.TestTypes.Simple.Insert(new[] { s });

                var loaded = repository.TestTypes.Reader.All().Single();
                Assert.AreEqual(s.Length, loaded.Length);
            }
        }

        [TestMethod]
        public void DecimalPrecisionTest()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestTypes.Simple" });
                var repository = container.Resolve<Common.DomRepository>();

                var s = new TestTypes.Simple { Length = 0.0123456789m };
                Assert.AreEqual("0.0123456789", s.Length.Value.ToString("F10", System.Globalization.CultureInfo.InvariantCulture));
                repository.TestTypes.Simple.Insert(new[] { s });

                var loaded = repository.TestTypes.Reader.All().Single();
                Assert.AreEqual(s.Length, loaded.Length);
            }
        }

        [TestMethod]
        public void SaveNonmaterialized()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestEntity.UniqueEntity" });
                var r = container.Resolve<Common.DomRepository>().TestEntity.UniqueEntity;

                var query = new[] { "a", "b", "c" }.Select(name => new TestEntity.UniqueEntity { Name = name });
                Assert.IsFalse(query is ICollection);
                Assert.IsFalse(query is IList);

                r.Insert(query);
                Assert.AreEqual("a, b, c", TestUtility.DumpSorted(r.All(), item => item.Name));
            }
        }

        [TestMethod]
        public void SaveDuplicate()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestEntity.Principal" });
                var r = container.Resolve<Common.DomRepository>().TestEntity.Principal;

                var i1 = new TestEntity.Principal { Name = "a", ID = Guid.NewGuid() };
                var i2 = new TestEntity.Principal { Name = "b", ID = i1.ID };

                r.Insert(new[] { i1 });
                TestUtility.ShouldFail(() => r.Insert(new[] { i2 }), "Inserting a record that already exists");
            }
        }

        [TestMethod]
        public void DeleteUpdateInsert_ConflictUnique()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestEntity.UniqueEntity" });
                var r = container.Resolve<Common.DomRepository>().TestEntity.UniqueEntity;
                var nhSession = container.Resolve<Common.ExecutionContext>().NHibernateSession;

                var ia = new TestEntity.UniqueEntity { Name = "a", ID = Guid.NewGuid() };
                var ib = new TestEntity.UniqueEntity { Name = "b", ID = Guid.NewGuid() };
                var ic1 = new TestEntity.UniqueEntity { Name = "c", ID = Guid.NewGuid() };

                r.Insert(new[] { ia, ib, ic1 });
                Assert.AreEqual("a, b, c", TestUtility.DumpSorted(r.All(), item => item.Name + item.Data));

                // Deleting old 'c' and inserting new 'c'. Possible conflict on unique constraint for property Name.

                var ic2 = new TestEntity.UniqueEntity { Name = "c", ID = Guid.NewGuid() };

                r.Save(new[] { ic2 }, null, new[] { ic1 });
                nhSession.Clear();
                Assert.AreEqual("a, b, c", TestUtility.DumpSorted(r.All(), item => item.Name + item.Data));
                Guid currentCID = r.Query().Where(item => item.Name == "c").Select(item => item.ID).Single();
                Assert.AreEqual(ic2.ID, currentCID, "new inserted item 'c'");
                Assert.AreNotEqual(ic1.ID, currentCID, "old deleted item 'c'");

                // Deleting old 'c' and inserting new 'c' with same ID. Possible conflict on primary key.

                var ic3 = new TestEntity.UniqueEntity { Name = "c", Data = "x", ID = ic2.ID };

                r.Save(new[] { ic3 }, null, new[] { ic2 });
                nhSession.Clear();
                Assert.AreEqual("a, b, cx", TestUtility.DumpSorted(r.All(), item => item.Name + item.Data));

                // Renaming old 'c' and inserting new 'c'. Possible conflict on unique constraint for property Name.

                ic3.Name = "oldc";
                var ic4 = new TestEntity.UniqueEntity { Name = "c", ID = Guid.NewGuid() };

                r.Save(new[] { ic4 }, new[] { ic3 }, null);
                nhSession.Clear();
                Assert.AreEqual("a, b, c, oldcx", TestUtility.DumpSorted(r.All(), item => item.Name + item.Data));
            }
        }
    }
}
