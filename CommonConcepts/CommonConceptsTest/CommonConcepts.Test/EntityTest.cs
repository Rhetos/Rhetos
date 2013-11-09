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
    public class EntityTest
    {
        [TestMethod]
        public void QuerySimple()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[]
                    {
                        "DELETE FROM TestEntity.Claim",
                        "INSERT INTO TestEntity.Claim (ClaimResource, ClaimRight) SELECT 'res1', 'rig1'",
                        "INSERT INTO TestEntity.Claim (ClaimResource, ClaimRight) SELECT 'res2', 'rig2'"
                    });
                var repository = new Common.DomRepository(executionContext);


                var loaded = repository.TestEntity.Claim.Query();
                Assert.AreEqual("res1.rig1, res2.rig2", TestUtility.DumpSorted(loaded, c => c.ClaimResource + "." + c.ClaimRight));
            }
        }

        [TestMethod]
        public void QueryComplex()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);

                executionContext.SqlExecuter.ExecuteSql(new[]
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
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);

                executionContext.SqlExecuter.ExecuteSql(new[]
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
                executionContext.NHibernateSession.Clear();
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
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);
                var claims = repository.TestEntity.Claim;

                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestEntity.Claim" });
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
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);
                var claims = repository.TestEntity.Claim;

                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestEntity.Claim" });
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
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);

                executionContext.SqlExecuter.ExecuteSql(new[]
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
        public void Spike_NHibernateLoadMergeSavePersist()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var pe1id = Guid.NewGuid();
                var pe2id = Guid.NewGuid();
                var pr1id = Guid.NewGuid();
                var pr2id = Guid.NewGuid();
                var cl1id = Guid.NewGuid();
                var cl2id = Guid.NewGuid();

                executionContext.SqlExecuter.ExecuteSql(new[]
                    {
                        "DELETE FROM TestEntity.Permission",
                        "DELETE FROM TestEntity.Principal",
                        "DELETE FROM TestEntity.Claim",
                        "INSERT INTO TestEntity.Claim (ID, ClaimResource, ClaimRight) SELECT '"+cl1id+"', 'res1', 'rig1'",
                        "INSERT INTO TestEntity.Claim (ID, ClaimResource, ClaimRight) SELECT '"+cl2id+"', 'res2', 'rig2'",
                        "INSERT INTO TestEntity.Principal (ID, Name) SELECT '"+pr1id+"', 'pr1'",
                        "INSERT INTO TestEntity.Principal (ID, Name) SELECT '"+pr2id+"', 'pr2'",
                        "INSERT INTO TestEntity.Permission (ID, PrincipalID, ClaimID, IsAuthorized) SELECT '"+pe1id+"', '"+pr1id+"', '"+cl1id+"', 0",
                        "INSERT INTO TestEntity.Permission (ID, PrincipalID, ClaimID, IsAuthorized) SELECT '"+pe2id+"', '"+pr1id+"', '"+cl2id+"', 1"
                    });

                var nhs = executionContext.NHibernateSession;
				
				// NH terminology: "Transient object" - a simple instance that is not bound to other references instances.
				// NH terminology: "Persistent object" - an instance that is bound to its coresponding database record and other referenced instances in NH cache. Allows lazy evaluation of references and navigation by references.
				

                // ============= UPDATE:

                {
                    var pe1 = nhs.Load<TestEntity.Permission>(pe1id); // Lazy load (generates proxy and remembers just the given ID value). First use of the instance will throw an exception if ID doesn't exist.
                    // 'Get' function would instantly load all properties (referenced instances are still lazy).
                    Assert.AreEqual(false, pe1.IsAuthorized);
                    Assert.AreEqual("pr1", pe1.Principal.Name);

                    var pe1transient = new TestEntity.Permission { ID = pe1id, Principal = new TestEntity.Principal { ID = pr2id }, Claim = new TestEntity.Claim { ID = cl1id }, IsAuthorized = true };
                    var newPe1 = (TestEntity.Permission)nhs.Merge(pe1transient); // If the record is already (lazy) loadad from the database, Merge will not load it again.
                    pe1transient.IsAuthorized = false;
                    Assert.AreEqual(true, newPe1.IsAuthorized);
                    // If the record DOES NOT EXIST IN DB when the Merge is called, Flush will INSERT it.
                    Assert.AreEqual("pr2", newPe1.Principal.Name); // Result of the Merge has successfully bound properties, there is NO NEED TO LOAD THEM EXPLICITLY!
                    nhs.Flush();
                    Assert.IsTrue(string.IsNullOrEmpty(pe1transient.Principal.Name)); // The original instance is still not a proxy object.
                    // Update() does not replace Merge(); it seems that it will update the given instance, but not the database record. Update will thrown an error if the object is already loaded.
                }


                // ============= INSERT TRANSIENT (NOT BOUND TO ORM) OBJECT (PERSIST FUNCTION):

                {
                    nhs.Clear();

                    var pe3id = Guid.NewGuid();
                    var pe3transient = new TestEntity.Permission { ID = pe3id, Principal = new TestEntity.Principal { ID = pr1id }, Claim = new TestEntity.Claim { ID = cl1id }, IsAuthorized = true };
                    // It the reference is not an INHibernateProxy, my Save function should sill Load the reference. Do it before calling Merge/Persist, so that Persist whould not need to check the detabase.

                    nhs.Persist(pe3transient); // Save function (instead of Persist) would return ID in case of autoincrement integer which would be instantly loaded from the database. Persist isn't that eager.
                    // If the referenced object is not bound, Save/Persist instantly reads it from db (like Get) to check if is exists, but it does not attach (bind) the loaded instance.
                    // References are not automatically bound after Save/Persist, not even after Flush+Load. Only after Save+Flush+Clear+Load, the reference will be bound.

                    // Save/Persist will instantly fail if that ID already exists in the NH session. Otherwise, Flush will fail if the record exists in the database.
                    nhs.Flush(); // If a reference does not exist in db nor memory, Save/Persist+Flush will first insert a record with NULL reference,
					// expecting the referenced record to be inserted in the same session, and then update the reference. It will fail at that point if the reference is invalid.

                    var loadedPe3 = nhs.Load<TestEntity.Permission>(pe3id);
                    // Save/Persist does not generate a new proxy object!! It will keep in the 1st level cache the given instance (even if it's references are not bound), up until Clear/Refresh.
                    Assert.AreSame(loadedPe3, pe3transient);
                    Assert.IsTrue(string.IsNullOrEmpty(loadedPe3.Principal.Name));

                    nhs.Refresh(loadedPe3); // Refresh before Flush would thrown an exception because the record does not exist in the database.
                    Assert.AreEqual("pr1", loadedPe3.Principal.Name);
                }

                // ============= INSERT WITH MANUALLY CREATED NH PROXY OBJECT (PERSIST FUNCTION):

                {
                    nhs.Clear();

                    var pe4id = Guid.NewGuid();
                    var pr1 = nhs.Load<TestEntity.Principal>(pr1id); // If a reference is Loaded in advance, then Save/Persist will not check them explicitly => One less call to the database!!!
                    var cl1 = nhs.Load<TestEntity.Claim>(cl1id);
                    var pe4 = new TestEntity.Permission { ID = pe4id, Principal = pr1, Claim = cl1, IsAuthorized = true };

                    // MANUALLY CREATING PROXY OBJECT BY EXPLICITLY CALLING Load FOR ALL NAVIGATION PROPERTIES, THEN USING Persist FUNCTION
					// IS FASTER THAN Merge, BECAUSE HN WILL NOT ONCE LOAD THE DATA FROM THE DATABASE.
                    // ON THE OTHER HAND, USING Merge FUNCTION IS MUSH EASIER BECAUSE IT AUTOMATICALLY CREATES THE PROXY OBJECT.

                    nhs.Persist(pe4);

                    nhs.Flush();

                    Assert.AreEqual("pr1", pe4.Principal.Name);
                }

                // ============= INSERT WITH AUTOMATICALLY CREATED NH PROXY OBJECT (MERGE FUNCTION):

                {
                    nhs.Clear();

                    var pe5id = Guid.NewGuid();
                    var pe5transient = new TestEntity.Permission { ID = pe5id, Principal = new TestEntity.Principal { ID = pr2id }, Claim = new TestEntity.Claim { ID = cl2id }, IsAuthorized = true };

                    var pe5 = (TestEntity.Permission)nhs.Merge(pe5transient); // If I have called Save/Persist before Merge, I would get the original (not bound to ORM) instance instead of a new proxy object.
                    // I will use Merge because Save/Persist does not automatically bind navigation properties (does not create a proxy object).
                    // Calling Save/Persist on a transient object creates a confusion because it leaves in the NH session my object with properties that are not bound.
                    Assert.AreEqual("pr2", pe5.Principal.Name);

                    nhs.Flush();
                }
            }
        }

        [TestMethod]
        public void CascadeDelete()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);

                var pid1 = Guid.NewGuid();
                var pid2 = Guid.NewGuid();
                var pid3 = Guid.NewGuid();
                var cid11 = Guid.NewGuid();
                var cid12 = Guid.NewGuid();
                var cid21 = Guid.NewGuid();
                var cid31 = Guid.NewGuid();

                executionContext.SqlExecuter.ExecuteSql(new[]
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
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestEntity.Principal" });
                var repository = new Common.DomRepository(executionContext);

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
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestEntity.Large" });
                var repository = new Common.DomRepository(executionContext);

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
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestTypes.Simple" });
                var repository = new Common.DomRepository(executionContext);

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
    }
}
