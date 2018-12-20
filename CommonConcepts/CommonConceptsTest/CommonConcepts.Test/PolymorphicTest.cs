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
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
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
    public class PolymorphicTest
    {
        [TestMethod]
        public void Simple()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                // Initialize data:

                repository.TestPolymorphic.Simple1.Delete(repository.TestPolymorphic.Simple1.Query());
                repository.TestPolymorphic.Simple2.Delete(repository.TestPolymorphic.Simple2.Query());
                Assert.AreEqual(0, repository.TestPolymorphic.SimpleBase.Query().Count());

                repository.TestPolymorphic.Simple1.Insert(new[] {
                    new TestPolymorphic.Simple1 { Name = "a", Days = 1 },
                    new TestPolymorphic.Simple1 { Name = "b", Days = 2 },
                    new TestPolymorphic.Simple1 { Name = "b3", Days = 2 },
                    new TestPolymorphic.Simple1 { Name = "b7", Days = 3 },
                });
                repository.TestPolymorphic.Simple2.Insert(new[] {
                    new TestPolymorphic.Simple2 { Name1 = "aa", Name2 = 11, Finish = new DateTime(2000, 1, 1) },
                    new TestPolymorphic.Simple2 { Name1 = "bb", Name2 = 22, Finish = new DateTime(2000, 1, 2) },
                    new TestPolymorphic.Simple2 { Name1 = "cc", Name2 = 33, Finish = new DateTime(2000, 1, 3) },
                });

                // Tests:

                var all = repository.TestPolymorphic.SimpleBase.Query();
                Assert.AreEqual(
                    "a/1, aa-11/1, b/2, b3/2, b7/3, bb-22/2, cc-33/3",
                    TestUtility.DumpSorted(all, item => item.Name + "/" + item.Days),
                    "Property implementations");

                var filterBySubtype = repository.TestPolymorphic.SimpleBase.Query()
                    .Where(item => item.Subtype == "TestPolymorphic.Simple1")
                    .Select(item => item.Name);
                Assert.AreEqual("a, b, b3, b7", TestUtility.DumpSorted(filterBySubtype), "filterBySubtype");

                var filterBySubtypeReference = repository.TestPolymorphic.SimpleBase.Query()
                    .Where(item => item.Simple2 != null)
                    .Select(item => item.Name);
                Assert.AreEqual("aa-11, bb-22, cc-33", TestUtility.DumpSorted(filterBySubtypeReference), "filterBySubtypeReference");

                var filterByProperty = repository.TestPolymorphic.SimpleBase.Query()
                    .Where(item => item.Days == 2)
                    .Select(item => item.Name);
                Assert.AreEqual("b, b3, bb-22", TestUtility.DumpSorted(filterByProperty), "filterByProperty");

                Guid aId = repository.TestPolymorphic.SimpleBase.Query().Where(item => item.Name == "a").Single().ID;
                var filterByID = repository.TestPolymorphic.SimpleBase.Query()
                    .Where(item => item.ID == aId)
                    .Select(item => item.Name);
                Assert.AreEqual("a", TestUtility.DumpSorted(filterByID), "filterByID");

                var filterBySubtypeID = repository.TestPolymorphic.SimpleBase.Query()
                    .Where(item => item.Simple1.ID == aId)
                    .Select(item => item.Name);
                Assert.AreEqual("a", TestUtility.DumpSorted(filterBySubtypeID), "filterBySubtypeID");

                var filterByOtherSubtypeID = repository.TestPolymorphic.SimpleBase.Query()
                    .Where(item => item.Simple2.ID == aId)
                    .Select(item => item.Name);
                Assert.AreEqual("", TestUtility.DumpSorted(filterByOtherSubtypeID), "filterByOtherSubtypeID");
            }
        }

        [TestMethod]
        public void Simple_NoExcessSql()
        {
            using (var container = new RhetosTestContainer())
            {
                CheckColumns(container, "ID, Days, Name", "TestPolymorphic", "Simple1_As_SimpleBase");
                CheckColumns(container, "ID, Days, Name", "TestPolymorphic", "Simple2_As_SimpleBase");
                CheckColumns(container, "ID, Days, Name, Subtype, Simple1ID, Simple2ID", "TestPolymorphic", "SimpleBase");
            }
        }

        private static void CheckColumns(RhetosTestContainer container, string expectedColumns, string schema, string table)
        {
            var sqlExecuter = container.Resolve<ISqlExecuter>();
            var actualColumns = new List<string>();
            sqlExecuter.ExecuteReader(
                "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = '" + schema + "' AND TABLE_NAME = '" + table + "'",
                reader => actualColumns.Add(reader[0].ToString()));
            Assert.AreEqual(
                TestUtility.DumpSorted(expectedColumns.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)),
                TestUtility.DumpSorted(actualColumns), schema + "." + table);
        }

        [TestMethod]
        public void Simple_NoExcessCs()
        {
            List<string> baseProperties = typeof(TestPolymorphic.SimpleBase).GetProperties().Select(p => p.Name).ToList();

            string expectedProperties = "ID, Days, Name, Subtype, Simple1ID, Simple2ID";

            Assert.AreEqual(
                TestUtility.DumpSorted(expectedProperties.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)),
                TestUtility.DumpSorted(baseProperties.Where(p => !p.Contains("_"))));
        }

        [TestMethod]
        public void Simple_Browse()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                // Initialize data:

                repository.TestPolymorphic.Simple1.Delete(repository.TestPolymorphic.Simple1.Query());
                repository.TestPolymorphic.Simple2.Delete(repository.TestPolymorphic.Simple2.Query());
                Assert.AreEqual(0, repository.TestPolymorphic.SimpleBase.Query().Count());

                repository.TestPolymorphic.Simple1.Insert(new[] {
                    new TestPolymorphic.Simple1 { Name = "a", Days = 1 },
                    new TestPolymorphic.Simple1 { Name = "b", Days = 2 },
                    new TestPolymorphic.Simple1 { Name = "b3", Days = 2 },
                    new TestPolymorphic.Simple1 { Name = "b7", Days = 3 },
                });
                repository.TestPolymorphic.Simple2.Insert(new[] {
                    new TestPolymorphic.Simple2 { Name1 = "aa", Name2 = 11, Finish = new DateTime(2000, 1, 1) },
                    new TestPolymorphic.Simple2 { Name1 = "bb", Name2 = 22, Finish = new DateTime(2000, 1, 2) },
                    new TestPolymorphic.Simple2 { Name1 = "cc", Name2 = 33, Finish = new DateTime(2000, 1, 3) },
                });

                // Tests:

                var report = repository.TestPolymorphic.SimpleBrowse.Query()
                    .Where(item => item.Days == 2)
                    .Select(item => item.Name + "/" + item.Days + "(" + item.Simple1Name + "/" + item.Simple2Name1 + "/" + item.Simple2.Name2 + ")")
                    .ToList();

                Assert.AreEqual(
                    "b/2(b//), b3/2(b3//), bb-22/2(/bb/22)",
                    TestUtility.DumpSorted(report));
            }
        }

        [TestMethod]
        public void Empty()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                var loaded = repository.TestPolymorphic.Empty.Load();

                Assert.AreEqual("", TestUtility.DumpSorted(loaded, item => item.ID + "-" + item.Subtype));
            }
        }

        [TestMethod]
        public void SecondBase()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                // Initialize data:

                repository.TestPolymorphic.Simple1.Delete(repository.TestPolymorphic.Simple1.Query());
                repository.TestPolymorphic.Simple2.Delete(repository.TestPolymorphic.Simple2.Query());
                repository.TestPolymorphic.Second1.Delete(repository.TestPolymorphic.Second1.Query());
                Assert.AreEqual(0, repository.TestPolymorphic.SecondBase.Query().Count());

                repository.TestPolymorphic.Simple1.Insert(new[] {
                    new TestPolymorphic.Simple1 { Name = "a", Days = 1 },
                });
                repository.TestPolymorphic.Simple2.Insert(new[] {
                    new TestPolymorphic.Simple2 { Name1 = "b", Name2 = 2, Finish = new DateTime(2000, 1, 22) },
                });
                repository.TestPolymorphic.Second1.Insert(new[] {
                    new TestPolymorphic.Second1 { Info = "c" },
                });

                // Tests:

                var all = repository.TestPolymorphic.SecondBase.Query();
                Assert.AreEqual(
                    "a/1, b/2/2000-01-22T00:00:00, c",
                    TestUtility.DumpSorted(all, item => item.Info));
            }
        }

        [TestMethod]
        public void Dependant_FKConstraintDelete()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                var s1 = new TestPolymorphic.Simple1 { ID = Guid.NewGuid(), Name = "a", Days = 1 };
                repository.TestPolymorphic.Simple1.Insert(new[] { s1 });

                var dep = new TestPolymorphic.Dependant { ID = Guid.NewGuid(), Name = "dep", SimpleBaseID = s1.ID };
                repository.TestPolymorphic.Dependant.Insert(new[] { dep });
                Assert.AreEqual("dep-a", TestUtility.DumpSorted(
                    repository.TestPolymorphic.DependantBrowse.Query(new[] { dep.ID }),
                    item => item.Name + "-" + item.SimpleBaseName));

                var ex = TestUtility.ShouldFail<Rhetos.UserException>(
                    () => repository.TestPolymorphic.Simple1.Delete(new[] { s1 }),
                    "It is not allowed to delete");
                TestUtility.AssertContains(ex.ToString(), new[] { "Dependant", "REFERENCE", "SimpleBase" }, "Expected inner SQL exception");
            }
        }

        [TestMethod]
        public void Dependant_FKConstraintInsert()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                var s1 = new TestPolymorphic.Simple1 { ID = Guid.NewGuid(), Name = "a", Days = 1 };
                repository.TestPolymorphic.Simple1.Insert(new[] { s1 });

                var dep = new TestPolymorphic.Dependant { ID = Guid.NewGuid(), Name = "dep", SimpleBaseID = s1.ID };
                repository.TestPolymorphic.Dependant.Insert(new[] { dep });

                var depInvalidReference = new TestPolymorphic.Dependant { ID = Guid.NewGuid(), Name = "depInvalidReference", SimpleBaseID = Guid.NewGuid() };
                var ex = TestUtility.ShouldFail<Rhetos.UserException>(
                    () => repository.TestPolymorphic.Dependant.Insert(new[] { depInvalidReference }),
                    "It is not allowed to enter the record.");
                TestUtility.AssertContains(ex.ToString(), new[] { "Dependant", "FOREIGN KEY", "SimpleBase" }, "Expected inner SQL exception");
            }
        }

        [TestMethod]
        public void Dependant_FKConstraintUpdate()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                var s1 = new TestPolymorphic.Simple1 { ID = Guid.NewGuid(), Name = "a", Days = 1 };
                repository.TestPolymorphic.Simple1.Insert(new[] { s1 });

                var dep = new TestPolymorphic.Dependant { ID = Guid.NewGuid(), Name = "dep", SimpleBaseID = s1.ID };
                repository.TestPolymorphic.Dependant.Insert(new[] { dep });

                dep.SimpleBaseID = Guid.NewGuid();

                var ex = TestUtility.ShouldFail<Rhetos.UserException>(
                    () => repository.TestPolymorphic.Dependant.Update(new[] { dep }),
                    "It is not allowed to edit the record.");
                TestUtility.AssertContains(ex.ToString(), new[] { "Dependant", "FOREIGN KEY", "SimpleBase" }, "Expected inner SQL exception");
            }
        }

        [TestMethod]
        public void Disjunctive()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                repository.TestPolymorphic.Disjunctive1.Delete(repository.TestPolymorphic.Disjunctive1.Load());
                repository.TestPolymorphic.Disjunctive2.Delete(repository.TestPolymorphic.Disjunctive2.Load());
                Assert.AreEqual(0, repository.TestPolymorphic.Disjunctive.Load().Count());

                var d1 = new TestPolymorphic.Disjunctive1 { ID = Guid.NewGuid(), Name = "abc" };
                repository.TestPolymorphic.Disjunctive1.Insert(new[] { d1 });

                var d2 = new TestPolymorphic.Disjunctive2 { ID = Guid.NewGuid(), Days = 123 };
                repository.TestPolymorphic.Disjunctive2.Insert(new[] { d2 });

                var all = repository.TestPolymorphic.Disjunctive.Query();
                Assert.AreEqual(
                    TestUtility.DumpSorted(new[] { d1.ID, d2.ID }),
                    TestUtility.DumpSorted(all, item => item.ID));

                var browseReport = repository.TestPolymorphic.DisjunctiveBrowse.Query()
                    .Select(item => item.Subtype + "-" + item.Disjunctive1.Name + "-" + item.Disjunctive2Days)
                    .ToList();
                Assert.AreEqual(
                    "TestPolymorphic.Disjunctive1-abc-, TestPolymorphic.Disjunctive2--123",
                    TestUtility.DumpSorted(browseReport));
            }
        }

        [TestMethod]
        public void MultipleImplementations()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                // Initialize data:

                repository.TestPolymorphic.MultipleImplementations.Delete(repository.TestPolymorphic.MultipleImplementations.Query());

                var mi1 = new TestPolymorphic.MultipleImplementations { Name1 = "abc", Name2 = "123" };
                var mi2 = new TestPolymorphic.MultipleImplementations { Name1 = "def", Name2 = "456" };

                repository.TestPolymorphic.MultipleImplementations.Insert(new[] { mi1, mi2 });

                // Testing unions:

                var base1 = repository.TestPolymorphic.Base1.Load();
                Assert.AreEqual("abc, cba, def, fed", TestUtility.DumpSorted(base1, item => item.Name1));

                var base2 = repository.TestPolymorphic.Base2.Load();
                Assert.AreEqual("123, 321, 456, 654", TestUtility.DumpSorted(base2, item => item.Name2));

                var base3 = repository.TestPolymorphic.Base3.Load();
                Assert.AreEqual("abc-3, def-3", TestUtility.DumpSorted(base3, item => item.Name1));

                // Testing specific implementation ID uniqueness:

                var base1IDs = base1.Select(item => item.ID).ToList();
                Assert.AreEqual(base1IDs.Count, base1IDs.Distinct().Count());

                // Testing specific implementation ID stability:

                var secondRead = repository.TestPolymorphic.Base1.Query();
                Assert.AreEqual(
                    TestUtility.DumpSorted(base1IDs),
                    TestUtility.DumpSorted(secondRead, item => item.ID));

                // Testing querying by specific implementation subtype:

                Assert.AreEqual(
                    "abc-, cba-abc, def-, fed-def",
                    TestUtility.DumpSorted(repository.TestPolymorphic.Base1.Query()
                        .Select(item => item.Name1 + "-" + item.MultipleImplementationsReverse.Name1)));

                // Testing C# implementation:

                int implementationHash = DomUtility.GetSubtypeImplementationHash("Reverse");

                var expected = new[] {
                    new TestPolymorphic.Base1 {
                        ID = DomUtility.GetSubtypeImplementationId(mi1.ID, implementationHash),
                        MultipleImplementationsReverseID = mi1.ID },
                    new TestPolymorphic.Base1 {
                        ID = DomUtility.GetSubtypeImplementationId(mi2.ID, implementationHash),
                        MultipleImplementationsReverseID = mi2.ID },
                };
                var actual = base1.Where(item => item.MultipleImplementationsReverseID != null);
                Assert.AreEqual(
                    TestUtility.DumpSorted(expected, item => item.MultipleImplementationsReverseID.ToString() + "/" + item.ID.ToString()),
                    TestUtility.DumpSorted(actual, item => item.MultipleImplementationsReverseID.ToString() + "/" + item.ID.ToString()));

                // Testing persisted IDs for specific implementation subtype:

                Assert.AreEqual(
                    TestUtility.DumpSorted(base1IDs),
                    TestUtility.DumpSorted(repository.TestPolymorphic.Base1_Materialized.Query().Select(item => item.ID)));
            }
        }

        [TestMethod]
        public void GetSubtypeImplementationId()
        {
            var testGuids = new Guid[] {
                new Guid("60CFE21A-1C36-45A0-9D57-DD635551B33B"),
                new Guid("12345678-2345-3456-4567-567890123456"),
                new Guid("FEDCBFED-CBFE-DCBF-EDCB-FEDCBFEDCBFE"),
                new Guid("00000000-0000-0000-0000-000000000000"),
                new Guid("11111111-1111-1111-1111-111111111111"),
                new Guid("FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF"),
            };

            var testHashes = new int[] {
                0,
                1,
                -1,
                123456,
                -123456,
                123456789,
                -123456789,
                1234567890,
                -1234567890,
                2147483647,
                -2147483647,
                -2147483648
            };

            using (var container = new RhetosTestContainer())
            {
                var sqlExecuter = container.Resolve<ISqlExecuter>();
                
                foreach (var guid in testGuids)
                    foreach (var hash in testHashes)
                    {
                        Guid csId = DomUtility.GetSubtypeImplementationId(guid, hash);

                        var sql = string.Format(
                            @"SELECT ID = CONVERT(UNIQUEIDENTIFIER, CONVERT(BINARY(4), CONVERT(INT, CONVERT(BINARY(4), {1})) ^ {0}) + SUBSTRING(CONVERT(BINARY(16), {1}), 5, 12))",
                            hash,
                            "CONVERT(UNIQUEIDENTIFIER, '" + guid.ToString().ToUpper() + "')");

                        Guid sqlId = Guid.Empty;
                        sqlExecuter.ExecuteReader(sql, reader => { sqlId = reader.GetGuid(0); });

                        Assert.AreEqual(csId, sqlId);
                    }
            }
        }

        [TestMethod]
        public void SubtypeInDifferentModule()
        {
            using (var container = new RhetosTestContainer())
            {
                var repositories = container.Resolve<GenericRepositories>();

                var subtype1 = new TestPolymorphic.DifferentModule { ID = Guid.NewGuid(), Code = "a" };
                var subtype2 = new TestPolymorphic2.DifferentModule { ID = Guid.NewGuid(), Code = "b" };

                repositories.Save(new[] { subtype1 }, null, repositories.Load<TestPolymorphic.DifferentModule>());
                repositories.Save(new[] { subtype2 }, null, repositories.Load<TestPolymorphic2.DifferentModule>());

                Assert.AreEqual(
                    "TestPolymorphic.DifferentModule a1, TestPolymorphic2.DifferentModule b2",
                    TestUtility.DumpSorted(repositories.Query<TestPolymorphic.DifferentModuleBase>(), item => item.Subtype + " " + item.Name));
            }
        }

        [TestMethod]
        public void SpecificComplexSubtype()
        {
            using (var container = new RhetosTestContainer())
            {
                var repositories = container.Resolve<GenericRepositories>();

                var subtype1 = new TestPolymorphic.ComplexImplementationData { ID = Guid.NewGuid(), a = "a" };
                repositories.Save(new[] { subtype1 }, null, repositories.Load<TestPolymorphic.ComplexImplementationData>());

                var subtype2 = new TestPolymorphic.ComplexImplementationSql { ID = Guid.NewGuid(), AlternativeId = Guid.NewGuid(), s = "s" };
                repositories.Save(new[] { subtype2 }, null, repositories.Load<TestPolymorphic.ComplexImplementationSql>());

                var all = repositories.Query<Common.Queryable.TestPolymorphic_ComplexBase>()
                    .OrderBy(item => item.Subtype).ThenBy(item => item.Name1);

                var expectedAll = new[]
                {
                    "TestPolymorphic.ComplexImplementationQuery abc1",
                    "TestPolymorphic.ComplexImplementationQuery q2 abc2",
                    "TestPolymorphic.ComplexImplementationSql s3",
                    "TestPolymorphic.ComplexImplementationSql sql2 s4",
                };

                Assert.AreEqual(
                    TestUtility.Dump(expectedAll),
                    TestUtility.Dump(all, item => item.Subtype + " " + item.Name1));

                var references = all.Select(item => new
                {
                    q = item.ComplexImplementationQuery.a,
                    q2 = item.ComplexImplementationQueryq2.a,
                    s = item.ComplexImplementationSql.s,
                    s2 = item.ComplexImplementationSqlsql2.s,
                }).ToList();

                var expectedReferences = new[]
                {
                    "abc---",
                    "-abc--",
                    "--s-",
                    "---s",
                };
                Assert.AreEqual(
                    TestUtility.Dump(expectedReferences),
                    TestUtility.Dump(references, item => (item.q ?? "-") + (item.q2 ?? "-") + (item.s ?? "-") + (item.s2 ?? "-")));
            }
        }

        [TestMethod]
        public void ReferenceTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var repositories = container.Resolve<GenericRepositories>();
                repositories.Delete(repositories.Load<TestPolymorphic.ChildA>());
                repositories.Delete(repositories.Load<TestPolymorphic.ChildB>());
                repositories.Delete(repositories.Load<TestPolymorphic.Parent>());

                var p1 = new TestPolymorphic.Parent { ID = Guid.NewGuid(), Name = "p1" };
                var ca1 = new TestPolymorphic.ChildA { ID = Guid.NewGuid(), Name = "ca1", ParentID = p1.ID };
                var cb1 = new TestPolymorphic.ChildB { ID = Guid.NewGuid(), Name = "cb1", ManualParentID = p1.ID };

                repositories.Insert(p1);
                repositories.Insert(ca1);
                repositories.Insert(cb1);

                Assert.AreEqual("ca1 p1, cb1 p1", TestUtility.DumpSorted(
                    repositories.Query<Common.Queryable.TestPolymorphic_Child>().Select(c => c.Name + " " + c.Parent.Name)));
            }
        }

        [TestMethod]
        public void WhereTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                repository.TestPolymorphic.DeactivatableEntity.Delete(repository.TestPolymorphic.DeactivatableEntity.Query());
                Assert.AreEqual("", TestUtility.DumpSorted(repository.TestPolymorphic.ActiveRecords.Query(), item => item.Name));

                var d1 = new TestPolymorphic.DeactivatableEntity { Active = true, Name = "d1" };
                var d2 = new TestPolymorphic.DeactivatableEntity { Active = true, Name = "d2" };
                var d3 = new TestPolymorphic.DeactivatableEntity { Active = false, Name = "d3" };
                var d5 = new TestPolymorphic.DeactivatableEntity { Active = true, Name = "xd5" };
                var d6 = new TestPolymorphic.DeactivatableEntity { Active = true, Name = "xd6" };
                var d7 = new TestPolymorphic.DeactivatableEntity { Active = true, Name = "d7" };
                repository.TestPolymorphic.DeactivatableEntity.Insert(new[] { d1, d2, d3, d5, d6, d7 });
                Assert.AreEqual("d1, d2, d7", TestUtility.DumpSorted(repository.TestPolymorphic.ActiveRecords.Query(), item => item.Name));
                Assert.AreEqual(
                    TestUtility.DumpSorted(repository.TestPolymorphic.ActiveRecords.Query().Select(item => item.ID)),
                    TestUtility.DumpSorted(repository.TestPolymorphic.ActiveRecords_Materialized.Query().Select(item => item.ID)));

                d2.Active = false;
                d3.Active = true;
                var d4 = new TestPolymorphic.DeactivatableEntity { Active = true, Name = "d4" };
                d6.Name = "d6";
                d7.Name = "xd7";
                repository.TestPolymorphic.DeactivatableEntity.Save(new[] { d4 }, new[] { d2, d3, d6, d7 }, new[] { d1 });
                Assert.AreEqual("d3, d4, d6", TestUtility.DumpSorted(repository.TestPolymorphic.ActiveRecords.Query(), item => item.Name));
                Assert.AreEqual(
                    TestUtility.DumpSorted(repository.TestPolymorphic.ActiveRecords.Query().Select(item => item.ID)),
                    TestUtility.DumpSorted(repository.TestPolymorphic.ActiveRecords_Materialized.Query().Select(item => item.ID)));
            }
        }

        [TestMethod]
        public void FilterSubtype()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                repository.TestPolymorphic.ComplexImplementationData.Delete(repository.TestPolymorphic.ComplexImplementationData.Load());
                repository.TestPolymorphic.ComplexImplementationSql.Delete(repository.TestPolymorphic.ComplexImplementationSql.Load());

                Func<Guid, string, Guid> hash = (id, implementation) =>
                    DomUtility.GetSubtypeImplementationId(id,
                        DomUtility.GetSubtypeImplementationHash(implementation));

                var d = new TestPolymorphic.ComplexImplementationData { a = "d", ID = Guid.NewGuid() };
                var sx = new TestPolymorphic.ComplexImplementationSql { s = "sx", ID = Guid.NewGuid() };
                var sy = new TestPolymorphic.ComplexImplementationSql { s = "sy", ID = Guid.NewGuid() };
                // Automatic update of _Materialized entity will not work if AlternativeId does not match ID hashed with implementation name.
                sx.AlternativeId = hash(sx.ID, "sql2");
                sy.AlternativeId = hash(sy.ID, "sql2");

                repository.TestPolymorphic.ComplexImplementationData.Insert(new[] { d });
                repository.TestPolymorphic.ComplexImplementationSql.Insert(new[] { sx, sy });

                Assert.AreEqual("abc1, abc2, sx3, sx4, sy3, sy4",
                    TestUtility.DumpSorted(repository.TestPolymorphic.ComplexBase.Query(), item => item.Name1));

                Action<string, IEnumerable<Guid>, string, string> test =
                    (expected, ids, subtype, implementationName) => Assert.AreEqual(expected,
                        TestUtility.DumpSorted(
                            repository.TestPolymorphic.ComplexBase.Filter(new FilterSubtype { Ids = ids, Subtype = subtype, ImplementationName = implementationName }),
                            item => item.Name1));

                // Testing filter:

                test("abc1", new[] { d.ID }, "TestPolymorphic.ComplexImplementationQuery", null);
                test("abc1", new[] { d.ID }, "TestPolymorphic.ComplexImplementationQuery", "");
                test("abc2", new[] { hash(d.ID, "q2") }, "TestPolymorphic.ComplexImplementationQuery", "q2");
                test("", new[] { d.ID }, "TestPolymorphic.ComplexImplementationSql", null);
                test("sx3", new[] { sx.ID }, "TestPolymorphic.ComplexImplementationSql", null);
                test("sx4", new[] { sx.AlternativeId.Value }, "TestPolymorphic.ComplexImplementationSql", "sql2");

                TestUtility.ShouldFail<Rhetos.ClientException>(() => repository.TestPolymorphic.ComplexBase.Filter(new FilterSubtype { Ids = new Guid[] {},
                    Subtype = "nonexisting", ImplementationName = "" }), "nonexisting");

                // Testing update of materialized data (it uses the Subtype filter):

                Assert.AreEqual(
                    TestUtility.DumpSorted(repository.TestPolymorphic.ComplexBase.Query(), item => item.ID),
                    TestUtility.DumpSorted(repository.TestPolymorphic.ComplexBase_Materialized.Query(), item => item.ID));

                sx.s = "sxx";
                repository.TestPolymorphic.ComplexImplementationSql.Update(new[] { sx });
                Assert.AreEqual("abc1, abc2, sxx3, sxx4, sy3, sy4",
                    TestUtility.DumpSorted(repository.TestPolymorphic.ComplexBase.Query(), item => item.Name1));
                Assert.AreEqual(
                    TestUtility.DumpSorted(repository.TestPolymorphic.ComplexBase.Query(), item => item.ID),
                    TestUtility.DumpSorted(repository.TestPolymorphic.ComplexBase_Materialized.Query(), item => item.ID));

                repository.TestPolymorphic.ComplexImplementationSql.Delete(new[] { sx });
                Assert.AreEqual("abc1, abc2, sy3, sy4",
                    TestUtility.DumpSorted(repository.TestPolymorphic.ComplexBase.Query(), item => item.Name1));
                Assert.AreEqual(
                    TestUtility.DumpSorted(repository.TestPolymorphic.ComplexBase.Query(), item => item.ID),
                    TestUtility.DumpSorted(repository.TestPolymorphic.ComplexBase_Materialized.Query(), item => item.ID));

                repository.TestPolymorphic.ComplexImplementationData.Delete(new[] { d });
                Assert.AreEqual("sy3, sy4",
                    TestUtility.DumpSorted(repository.TestPolymorphic.ComplexBase.Query(), item => item.Name1));
                Assert.AreEqual(
                    TestUtility.DumpSorted(repository.TestPolymorphic.ComplexBase.Query(), item => item.ID),
                    TestUtility.DumpSorted(repository.TestPolymorphic.ComplexBase_Materialized.Query(), item => item.ID));

                repository.TestPolymorphic.ComplexImplementationSql.Delete(new[] { sy });
                Assert.AreEqual("",
                    TestUtility.DumpSorted(repository.TestPolymorphic.ComplexBase.Query(), item => item.Name1));
                Assert.AreEqual(
                    TestUtility.DumpSorted(repository.TestPolymorphic.ComplexBase.Query(), item => item.ID),
                    TestUtility.DumpSorted(repository.TestPolymorphic.ComplexBase_Materialized.Query(), item => item.ID));
            }
        }

        [TestMethod]
        public void AutomaticPropertySqlDependency()
        {
            using (var container = new RhetosTestContainer())
            {
                var dslModel = container.Resolve<IDslModel>();

                //var simpleBase = dslModel.FindByKey("DataStructureInfo TestPolymorphic.SimpleBase");
                var simple1SqlView = dslModel.FindByKey("SqlViewInfo TestPolymorphic.Simple1_As_SimpleBase");
                var sqlViewDependsOnProperty = dslModel.FindByReference<SqlDependsOnPropertyInfo>(p => p.Dependent, simple1SqlView)
                    .Select(dep => dep.DependsOn.GetKeyProperties()).ToList();

                TestUtility.AssertContains(
                    TestUtility.DumpSorted(sqlViewDependsOnProperty),
                    new[] { "TestPolymorphic.Simple1.Days", "TestPolymorphic.Simple1.Name" });
            }
        }

        [TestMethod]
        public void NamedSubtypeWithAutocode()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                repository.TestPolymorphic.OtherFeatures.Delete(repository.TestPolymorphic.OtherFeatures.Load());

                repository.TestPolymorphic.OtherFeatures.Insert(new TestPolymorphic.OtherFeatures { Name = "a" });
                repository.TestPolymorphic.OtherFeatures.Insert(new TestPolymorphic.OtherFeatures { Name = "b" });

                Assert.AreEqual("a, b", TestUtility.DumpSorted(repository.TestPolymorphic.OtherFeaturesBase.Load(), item => item.Name ));
                Assert.AreEqual("a-1, b-2", TestUtility.DumpSorted(repository.TestPolymorphic.OtherFeatures.Load(), item => item.Name + "-" + item.Code));
            }
        }

        [TestMethod]
        public void Dependencies()
        {
            using (var container = new RhetosTestContainer())
            {
                int? dependencies = null;
                string checkDependencies =
                    @"SELECT
                        COUNT(*)
                    FROM
                        Rhetos.AppliedConceptDependsOn dependency
                        INNER JOIN Rhetos.AppliedConcept dependent ON dependent.ID = dependency.DependentID
                        INNER JOIN Rhetos.AppliedConcept dependsOn ON dependsOn.ID = dependency.DependsOnID
                    WHERE
                        dependent.CreateQuery LIKE '%VIEW TestPolymorphic.DependentQuery%'
                        AND dependsOn.CreateQuery LIKE '%VIEW TestPolymorphic.PolyWithDependencies%'";

                var sqlExecuter = container.Resolve<ISqlExecuter>();
                sqlExecuter.ExecuteReader(checkDependencies, reader => { dependencies = reader.GetInt32(0); });

                Assert.AreEqual(1, dependencies);
            }
        }
    }
}
