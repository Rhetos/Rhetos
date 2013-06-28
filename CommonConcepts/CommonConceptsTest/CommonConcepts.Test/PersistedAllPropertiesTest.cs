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
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.TestCommon;
using System.Text.RegularExpressions;

namespace CommonConcepts.Test
{
    [TestClass]
    public class PersistedAllPropertiesTest
    {
        private readonly Type[] _expectedConcepts = new[]
        { 
            typeof(DataStructureExtendsInfo),
            typeof(ShortStringPropertyInfo),
            typeof(DateTimePropertyInfo),
            typeof(ReferencePropertyInfo),
            typeof(SqlIndexMultipleInfo)
        };

        [TestMethod]
        public void QueryableFromRepository()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var createdConcepts = new List<string>();
                var serializedInfo = new StringBuilder();
                var createQuery = new StringBuilder();
                executionContext.SqlExecuter.ExecuteReader(
                    "SELECT DISTINCT InfoType, SerializedInfo, CreateQuery FROM Rhetos.AppliedConcept WHERE SerializedInfo LIKE '%>TestAllPropertiesCopyAllFeatures<%'",
                    reader =>
                        {
                            createdConcepts.Add(reader.GetString(0).Split(',')[0]);
                            serializedInfo.AppendLine(reader.GetString(1));
                            createQuery.AppendLine(reader.GetString(2));
                        });

                Console.WriteLine("Copied concepts:");
                foreach (var createdConcept in createdConcepts.OrderBy(x => x))
                    Console.WriteLine(" - " + createdConcept);

                foreach (var expected in _expectedConcepts)
                    Assert.IsTrue(createdConcepts.Contains(expected.FullName), "'" + expected.FullName + "' was not copied.");

                Console.WriteLine("Create query:");
                Console.WriteLine(createQuery);
                Assert.IsTrue(new Regex(@"TestAllPropertiesCopyAllFeatures.*TheParentID.*ON DELETE CASCADE", RegexOptions.Singleline)
                    .IsMatch(createQuery.ToString()), "Property 'TheParentID' should have cascade delete copied from the source data structure.");

                TestUtility.AssertContains(
                    serializedInfo.ToString(),
                    new [] {
                        "<PropertyNames>Name</PropertyNames>",
                        "<PropertyNames>Name Start</PropertyNames>",
                        "<PropertyNames>Name Start TheParent</PropertyNames>" },
                    "These 3 indexes expected");
            }
        }

        [TestMethod]
        public void BrowseUsingImplicitlyCreatedProperties()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestAllProperties.Base;" });

                var repository = new Common.DomRepository(executionContext);

                repository.TestAllProperties.TestAllPropertiesCopyAllFeatures.Recompute();
                Assert.AreEqual("", TestUtility.DumpSorted(
                    repository.TestAllProperties.UsesImplicitlyCreatedProperties.All(), item => item.ComputedName), "initial state");

                var id = Guid.NewGuid();
                repository.TestAllProperties.Base.Insert(new[] { new TestAllProperties.Base { ID = id } });
                repository.TestAllProperties.Source.Insert(new[] { new TestAllProperties.Source { ID = id, Name = "abc" } });

                repository.TestAllProperties.TestAllPropertiesCopyAllFeatures.Recompute();
                Assert.AreEqual("abc", TestUtility.DumpSorted(
                    repository.TestAllProperties.UsesImplicitlyCreatedProperties.All(), item => item.ComputedName), "after persisting data from 'Source' to 'TestAllPropertiesCopyAllFeatures'.");
            }
        }
    }
}
