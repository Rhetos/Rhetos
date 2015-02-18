﻿/*
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
                TestUtility.ShouldFail(() => repository.TestSqlWorkarounds.PersonInfo.All(), "filter", "PersonFilter", "must be used");

                var result = repository.TestSqlWorkarounds.PersonInfo.Filter(new TestSqlWorkarounds.PersonFilter { NamePattern = "%1%", LimitResultCount = 4 });
                Assert.AreEqual("User1 5, User10 6, User100 7, User11 6", TestUtility.Dump(result, item => item.Person.Name + " " + item.NameLength));
            }
        }

        [TestMethod]
        public void SqlDependsOnSqlIndex()
        {
            using (var container = new RhetosTestContainer())
            {
                var features = new Dictionary<string, string>
                {
                    { "base", "DataStructureInfo TestSqlWorkarounds.DependencyBase" },
                    { "baseA", "PropertyInfo TestSqlWorkarounds.DependencyBase.A" },
                    { "baseB", "PropertyInfo TestSqlWorkarounds.DependencyBase.B" },
                    { "baseBAIndex", "SqlIndexMultipleInfo TestSqlWorkarounds.DependencyBase.'B A'" },
                    { "depA", "SqlObjectInfo TestSqlWorkarounds.DependencyA" },
                    { "depB", "SqlObjectInfo TestSqlWorkarounds.DependencyB" },
                    { "depAll", "SqlObjectInfo TestSqlWorkarounds.DependencyAll" }
                };

                Dictionary<Guid, string> featuresById = features
                    .Select(f => new { Name = f.Key, Id = ReadConceptId(f.Value, container) })
                    .ToDictionary(fid => fid.Id, fid => fid.Name);

                var deployedDependencies = ReadConceptDependencies(featuresById.Keys, container)
                    .Select(dep => featuresById[dep.Item1] + "-" + featuresById[dep.Item2]);

                var expectedDependencies = // Second concept depends on first concept.
                    "base-baseA, base-baseB," // Standard properties depend on their entity.
                    + "base-baseBAIndex, baseA-baseBAIndex, baseB-baseBAIndex," // Standard index depends on its properties.
                    + "baseA-depA,"
                    + "baseB-depB, baseBAIndex-depB," // SqlDependsOnSqlIndex should be automatically included when depending on its first property.
                    + "baseA-depAll, baseB-depAll,"
                    + "baseBAIndex-depAll," // SqlDependsOnSqlIndex should be automatically included when depending on its entity.
                    + "base-depAll";

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

        private static Guid ReadConceptId(string conceptInfoKey, RhetosTestContainer container)
        {
            Guid id = Guid.Empty;
            container.Resolve<ISqlExecuter>().ExecuteReader(
                "SELECT ID FROM Rhetos.AppliedConcept WHERE ConceptInfoKey = " + SqlUtility.QuoteText(conceptInfoKey),
                reader => id = reader.GetGuid(0));
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
    }
}
