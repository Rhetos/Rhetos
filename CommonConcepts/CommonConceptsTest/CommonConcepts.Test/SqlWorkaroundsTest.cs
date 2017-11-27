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

using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Utilities;
using Rhetos.TestCommon;
using Rhetos;
using Rhetos.Configuration.Autofac;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Dsl;

namespace CommonConcepts.Test
{
    [TestClass]
    public class SqlWorkaroundsTest
    {
        private static string ReportSqlQueryResult(ISqlExecuter sqlExecuter, string sql)
        {
            var rows = new List<string>();
            sqlExecuter.ExecuteReader(sql,
                reader =>
                {
                    var fields = new List<string>();
                    for (int c = 0; c < reader.FieldCount; c++)
                        fields.Add(reader[c].ToString());
                    rows.Add(string.Join(", ", fields));
                });
            return string.Join("\r\n", rows);
        }

        [TestMethod]
        public void SqlFunction()
        {
            using (var container = new RhetosTestContainer())
            {
                Assert.AreEqual("11", ReportSqlQueryResult(container.Resolve<ISqlExecuter>(), "SELECT * FROM TestSqlWorkarounds.Fun2(10)"));
            }
        }

        [TestMethod]
        public void SqlObject()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                {
                    "DELETE FROM TestSqlWorkarounds.E",
                    "INSERT INTO TestSqlWorkarounds.E (I) VALUES (100)"
                });

                string report = "";
                container.Resolve<ISqlExecuter>().ExecuteReader(
                    @"SELECT E.I, V1.I1, V2.I2
                        FROM TestSqlWorkarounds.E
                        INNER JOIN TestSqlWorkarounds.V1 ON V1.ID = E.ID
                        INNER JOIN TestSqlWorkarounds.V2 ON V2.ID = E.ID",
                    reader => report += reader.GetInt32(0) + ", " + reader.GetInt32(1) + ", " + reader.GetInt32(2) + ".");
                Assert.AreEqual("100, 101, 102.", report);

                report = "";
                container.Resolve<ISqlExecuter>().ExecuteReader(
                    @"SELECT X FROM TestSqlWorkarounds.V3 ORDER BY X",
                    reader => report += reader.GetInt32(0) + ".");
                Assert.AreEqual("101.102.", report);
            }
        }

        [TestMethod]
        public void ExecuteSqlProcedure()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestSqlWorkarounds.Person" });
                container.Resolve<ISqlExecuter>().ExecuteSql(Enumerable.Range(1, 100).Select(x =>
                    "INSERT INTO TestSqlWorkarounds.Person (Name) VALUES ('User" + x.ToString() +"')"));

                var repository = container.Resolve<Common.DomRepository>();
                TestUtility.ShouldFail(() => repository.TestSqlWorkarounds.PersonInfo.Load(), "filter", "PersonFilter", "must be used");

                var result = repository.TestSqlWorkarounds.PersonInfo.Load(new TestSqlWorkarounds.PersonFilter { NamePattern = "%1%", LimitResultCount = 4 });
                var personRepos = repository.TestSqlWorkarounds.Person;
                Assert.AreEqual("User1 5, User10 6, User100 7, User11 6", TestUtility.Dump(result, item => personRepos.Load(new[] { item.PersonID.Value }).Single().Name + " " + item.NameLength));
            }
        }

        [TestMethod]
        public void SqlDependsOnSqlIndexForFirstProperty()
        {
            using (var container = new RhetosTestContainer())
            {
                var features = new Dictionary<string, string>
                {
                    { "base", "DataStructureInfo TestSqlWorkarounds.DependencyBase" },
                    { "base.A", "PropertyInfo TestSqlWorkarounds.DependencyBase.A" },
                    { "base.B", "PropertyInfo TestSqlWorkarounds.DependencyBase.B" },
                    { "base.IndexBA", "SqlIndexMultipleInfo TestSqlWorkarounds.DependencyBase.'B A'" },
                    { "depA", "SqlObjectInfo TestSqlWorkarounds.DependencyA" },
                    { "depB", "SqlObjectInfo TestSqlWorkarounds.DependencyB" },
                    { "depAll", "SqlObjectInfo TestSqlWorkarounds.DependencyAll" }
                };

                Dictionary<Guid, string> featuresById = features
                    .Select(f => new { Name = f.Key, Id = ReadConceptId(f.Value, container) })
                    .ToDictionary(fid => fid.Id, fid => fid.Name);

                var deployedDependencies = ReadConceptDependencies(featuresById.Keys, container)
                    .Select(dep => featuresById[dep.Item1] + "-" + featuresById[dep.Item2]);

                // Second concept depends on first concept.
                var expectedDependencies = new[]
                {
                    "base-base.A", "base-base.B", // Standard properties depend on their entity.
                    "base-base.IndexBA", "base.A-base.IndexBA", "base.B-base.IndexBA", // Standard index depends on its properties.
                    "base.A-depA",
                    "base.B-depB", "base.IndexBA-depB", // SqlDependsOnSqlIndex should be automatically included when depending on its first property.
                    "base.A-depAll", "base.B-depAll",
                    "base.IndexBA-depAll", // SqlDependsOnSqlIndex should be automatically included when depending on its entity.
                    "base-depAll",
                };

                Assert.AreEqual(TestUtility.DumpSorted(expectedDependencies), TestUtility.DumpSorted(deployedDependencies));
            }
        }

        [TestMethod]
        public void SqlDependsOnID()
        {
            using (var container = new RhetosTestContainer())
            {
                var features = new Dictionary<string, string>
                {
                    { "base", "DataStructureInfo TestSqlWorkarounds.DependencyBase" },
                    { "base.A", "PropertyInfo TestSqlWorkarounds.DependencyBase.A" },
                    { "base.B", "PropertyInfo TestSqlWorkarounds.DependencyBase.B" },
                    { "depA", "SqlObjectInfo TestSqlWorkarounds.DependencyA" },
                    { "depB", "SqlObjectInfo TestSqlWorkarounds.DependencyB" },
                    { "depAll", "SqlObjectInfo TestSqlWorkarounds.DependencyAll" },
                    { "depID", "SqlObjectInfo TestSqlWorkarounds.DependencyID" }
                };

                Dictionary<Guid, string> featuresById = features
                    .Select(f => new { Name = f.Key, Id = ReadConceptId(f.Value, container) })
                    .ToDictionary(fid => fid.Id, fid => fid.Name);

                var deployedDependencies = ReadConceptDependencies(featuresById.Keys, container)
                    .Select(dep => featuresById[dep.Item1] + "-" + featuresById[dep.Item2]);

                // Second concept depends on first concept.
                var expectedDependencies = new[]
                {
                    "base-base.A", "base-base.B", // Standard properties depend on their entity.
                    "base.A-depA", // Standard dependency on property.
                    "base.B-depB", // Standard dependency on property.
                    "base-depAll", "base.A-depAll", "base.B-depAll", // SqlDependsOnDataStructure includes all properties.
                    "base-depID" // SqlDependsOnID does not include properties.
                };

                Assert.AreEqual(TestUtility.DumpSorted(expectedDependencies), TestUtility.DumpSorted(deployedDependencies));
            }
        }

        [TestMethod]
        public void SqlDependsOnSqlIndex()
        {
            using (var container = new RhetosTestContainer())
            {
                var features = new Dictionary<string, string>
                {
                    { "index", "SqlIndexMultipleInfo TestSqlWorkarounds.TestIndex.'A B'" },
                    { "dependsOnIndex", "SqlObjectInfo TestSqlWorkarounds.DependsOnIndex" }
                };

                Dictionary<Guid, string> featuresById = features
                    .Select(f => new { Name = f.Key, Id = ReadConceptId(f.Value, container) })
                    .ToDictionary(fid => fid.Id, fid => fid.Name);

                var deployedDependencies = ReadConceptDependencies(featuresById.Keys, container)
                    .Select(dep => featuresById[dep.Item1] + "-" + featuresById[dep.Item2]);

                var expectedDependencies = "index-dependsOnIndex";

                Assert.AreEqual(
                    TestUtility.DumpSorted(expectedDependencies.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s))),
                    TestUtility.DumpSorted(deployedDependencies));
            }
        }

        [TestMethod]
        public void SqlDependsOnCaseInsensitive()
        {
            using (var container = new RhetosTestContainer())
            {
                var features = new Dictionary<string, string>
                {
                    { "1", "SqlViewInfo TestSqlWorkarounds.AutoDependsOn1" },
                    { "1CI", "SqlViewInfo TestSqlWorkarounds.AutoDependsOn1CI" },
                    { "2", "SqlViewInfo TestSqlWorkarounds.AutoDependsOn2" },
                    { "3", "SqlViewInfo TestSqlWorkarounds.AutoDependsOn3" },
                    { "4", "SqlViewInfo TestSqlWorkarounds.AutoDependsOn4" },
                };

                Dictionary<Guid, string> featuresById = features
                    .Select(f => new { Name = f.Key, Id = ReadConceptId(f.Value, container) })
                    .ToDictionary(fid => fid.Id, fid => fid.Name);

                var deployedDependencies = ReadConceptDependencies(featuresById.Keys, container)
                    .Select(dep => featuresById[dep.Item1] + "-" + featuresById[dep.Item2]);

                var expectedDependencies = "2-1, 2-1CI, 3-2, 4-3"; // Second concept depends on first concept.

                Assert.AreEqual(
                    TestUtility.DumpSorted(expectedDependencies.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s))),
                    TestUtility.DumpSorted(deployedDependencies));
            }
        }

        [TestMethod]
        public void SqlDependsOnDataStructureNoProperties()
        {
            using (var container = new RhetosTestContainer())
            {
                var features = new Dictionary<string, string>
                {
                    { "EntityNoProperies", "DataStructureInfo TestSqlWorkarounds.NoProperties" },
                    { "View", "SqlViewInfo TestSqlWorkarounds.DependsOnNoProperties" },
                };

                Dictionary<Guid, string> featuresById = features
                    .Select(f => new { Name = f.Key, Id = ReadConceptId(f.Value, container) })
                    .ToDictionary(fid => fid.Id, fid => fid.Name);

                var deployedDependencies = ReadConceptDependencies(featuresById.Keys, container)
                    .Select(dep => featuresById[dep.Item1] + "-" + featuresById[dep.Item2]);

                var expectedDependencies = "EntityNoProperies-View"; // Second concept depends on first concept.

                Assert.AreEqual(
                    TestUtility.DumpSorted(expectedDependencies.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s))),
                    TestUtility.DumpSorted(deployedDependencies));
            }
        }

        [TestMethod]
        public void SqlDependsOnModule()
        {
            using (var container = new RhetosTestContainer())
            {
                var features = new Dictionary<string, string>
                {
                    { "X", "SqlViewInfo TestSqlWorkarounds2.OtherModuleObject" },
                    { "Entity", "DataStructureInfo TestSqlWorkarounds.E" },
                    { "Function", "SqlFunctionInfo TestSqlWorkarounds.Fun1" },
                    { "Property", "PropertyInfo TestSqlWorkarounds.E.I" },
                    { "View", "SqlViewInfo TestSqlWorkarounds.DependsOnNoProperties" },
                };

                Dictionary<Guid, string> featuresById = features
                    .Select(f => new { Name = f.Key, Id = ReadConceptId(f.Value, container) })
                    .ToDictionary(fid => fid.Id, fid => fid.Name);

                var deployedDependencies = ReadConceptDependencies(featuresById.Keys, container)
                    .Where(dep => featuresById[dep.Item1] == "X" || featuresById[dep.Item2] == "X")
                    .Select(dep => featuresById[dep.Item1] + "-" + featuresById[dep.Item2]);

                var expectedDependencies = "Entity-X, Function-X, Property-X, View-X"; // Second concept depends on first concept.

                Assert.AreEqual(
                    TestUtility.DumpSorted(expectedDependencies.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s))),
                    TestUtility.DumpSorted(deployedDependencies));
            }
        }

        [TestMethod]
        public void AutoSqlDependsOnPolymorphic()
        {
            using (var container = new RhetosTestContainer())
            {
                var features = new Dictionary<string, string>
                {
                    { "Poly", "DataStructureInfo TestSqlWorkarounds.Poly" },
                    { "PolyView", "SqlObjectInfo TestSqlWorkarounds.Poly" },
                    { "PolyImplementation", "DataStructureInfo TestSqlWorkarounds.PolyImplementation" },
                    { "AutoDependsOnPoly", "SqlViewInfo TestSqlWorkarounds.AutoDependsOnPoly" }
                };

                Dictionary<Guid, string> featuresById = features
                    .Select(f => new { Name = f.Key, Id = ReadConceptId(f.Value, container) })
                    .ToDictionary(fid => fid.Id, fid => fid.Name);

                var deployedDependencies = ReadConceptDependencies(featuresById.Keys, container)
                    .Select(dep => featuresById[dep.Item1] + "-" + featuresById[dep.Item2]);

                // The key dependency is "PolyView-AutoDependsOnPoly", it should be generated by AutodetectSqlDependencies:
                var expectedDependencies = "Poly-AutoDependsOnPoly, PolyView-AutoDependsOnPoly"; // Second concept depends on first concept.

                Assert.AreEqual(
                    TestUtility.DumpSorted(expectedDependencies.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s))),
                    TestUtility.DumpSorted(deployedDependencies));
            }
        }

        private static Guid ReadConceptId(string conceptInfoKey, RhetosTestContainer container)
        {
            Guid id = Guid.Empty;
            container.Resolve<ISqlExecuter>().ExecuteReader(
                "SELECT ID FROM Rhetos.AppliedConcept WHERE ConceptInfoKey = " + SqlUtility.QuoteText(conceptInfoKey),
                reader => id = reader.GetGuid(0));
            if (id == Guid.Empty)
                throw new ApplicationException("Cannot find applied concept '" + conceptInfoKey + "'.");
            return id;
        }

        private static List<Tuple<Guid, Guid>> ReadConceptDependencies(IEnumerable<Guid> conceptsId, RhetosTestContainer container)
        {
            var dependencies = new List<Tuple<Guid, Guid>>();
            string ids = string.Join(", ", conceptsId.Select(id => SqlUtility.QuoteGuid(id)));
            container.Resolve<ISqlExecuter>().ExecuteReader(
                "SELECT DependsOnID, DependentID FROM Rhetos.AppliedConceptDependsOn"
                + " WHERE DependentID IN (" + ids + ")"
                + " AND DependsOnID IN (" + ids + ")",
                reader => dependencies.Add(Tuple.Create(reader.GetGuid(0), reader.GetGuid(1))));
            return dependencies;
        }

        [TestMethod]
        public void SqlUserError()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                TestUtility.ShouldFail<Rhetos.UserException>(
                    () => repository.TestSqlWorkarounds.SqlUserError.Insert(new[] { new TestSqlWorkarounds.SqlUserError() }),
                    "custom user message");
            }
        }

        [TestMethod]
        public void WithoutTransaction()
        {
            using (var container = new RhetosTestContainer())
            {
                var sqlExecuter = container.Resolve<ISqlExecuter>();
                var createdViews = new List<string>();
                sqlExecuter.ExecuteReader(
                    "SELECT name FROM sys.objects o WHERE type = 'V' AND SCHEMA_NAME(schema_id) = 'TestSqlWorkarounds' AND name LIKE 'With%Transaction[_]%'",
                    reader => createdViews.Add(reader.GetString(0)));
                Assert.AreEqual("WithoutTransaction_0, WithTransaction_1", TestUtility.DumpSorted(createdViews));
            }
        }

        [TestMethod]
        public void NotNullColumn()
        {
            using (var container = new RhetosTestContainer())
            {
                var sqlExecuter = container.Resolve<ISqlExecuter>();
                sqlExecuter.ExecuteSql(new[] {
                    "DELETE FROM TestSqlWorkarounds.HasNotNullProperty",
                    "INSERT INTO TestSqlWorkarounds.HasNotNullProperty (Name, Code) SELECT 'a', 1" });

                var repository = container.Resolve<Common.DomRepository>();

                Assert.AreEqual("a1", TestUtility.DumpSorted(repository.TestSqlWorkarounds.HasNotNullProperty.Query(), item => item.Name + item.Code));

                TestUtility.ShouldFail(
                    () => sqlExecuter.ExecuteSql(new[] { "INSERT INTO TestSqlWorkarounds.HasNotNullProperty (Name) SELECT 'b'" }),
                    "Cannot insert the value NULL into column 'Code', table '", ".TestSqlWorkarounds.HasNotNullProperty'");

                TestUtility.ShouldFail(
                    () => sqlExecuter.ExecuteSql(new[] { "INSERT INTO TestSqlWorkarounds.HasNotNullProperty (Code) SELECT 2" }),
                    "Cannot insert the value NULL into column 'Name', table '", ".TestSqlWorkarounds.HasNotNullProperty'");

            }
        }

        [TestMethod]
        public void LongIdentifiers()
        {
            using (var container = new RhetosTestContainer())
            {
                var repos = container.Resolve<Common.DomRepository>().TestLongIdentifiers;

                var p1 = new TestLongIdentifiers.LongIdentifier0000020000000003000000000400000000050000000006000000000700000000080000000009000000000C
                {
                    LongName0100000000020000000003000000000400000000050000000006000000000700000000080000000009000000000C = "p1"
                };
                repos.LongIdentifier0000020000000003000000000400000000050000000006000000000700000000080000000009000000000C
                    .Insert(p1);

                var c1 = new TestLongIdentifiers.LongChild100000000020000000003000000000400000000050000000006000000000700000000080000000009000000000C
                {
                    ChildName = "c1",
                    LongIdentifier0000020000000003000000000400000000050000000006000000000700000000080000000009000000000CID = p1.ID
                };
                var c2 = new TestLongIdentifiers.LongChild100000000020000000003000000000400000000050000000006000000000700000000080000000009000000000C
                {
                    ChildName = "c2",
                    LongIdentifier0000020000000003000000000400000000050000000006000000000700000000080000000009000000000CID = p1.ID
                };
                repos.LongChild100000000020000000003000000000400000000050000000006000000000700000000080000000009000000000C
                    .Insert(c1, c2);

                Assert.AreEqual("p1 c1, p1 c2", TestUtility.DumpSorted(
                    repos.LongBrowse00000000020000000003000000000400000000050000000006000000000700000000080000000009000000000C.Query(),
                    item => item.ParentName + " " + item.ChildName));
            }
        }
    }
}
