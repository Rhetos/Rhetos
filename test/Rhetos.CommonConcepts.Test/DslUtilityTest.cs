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
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.TestCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.CommonConcepts.Test
{
    internal class SqlResourcesMock : ISqlResources
    {
        public Dictionary<string, string> Resources { get; init; }

        public string TryGet(string key) => Resources.GetValueOrDefault(key);
    }

    [TestClass]
    public class DslUtilityTest
    {
        class OtherType : PropertyInfo { }

        [ConceptKeyword("TestConcept")]
        class ConceptType : PropertyInfo { }

        class DerivedConceptType : ConceptType { }

        class DerivedConceptType2 : ConceptType { }

        [TestMethod]
        public void FindSqlResourceKeyPropertyType()
        {
            var tests = new (Type PropertyType, string ExpectedFoundKey)[]
            {
                (typeof(OtherType), null),
                (typeof(ConceptType), "TestKey_TestConcept"),
                (typeof(DerivedConceptType), "TestKey_TestConcept"), // Does not have its own type-specific SQL resource, so it uses the base class resource.
                (typeof(DerivedConceptType2), "TestKey_DerivedConceptType2"),
            };

            var sqlResources = new SqlResourcesMock
            {
                Resources = new()
                {
                    { "TestKey_TestConcept", "sql" },
                    { "TestKey_DerivedConceptType2", "sql" },
                }
            };

            string report = string.Join(Environment.NewLine, tests.Select(test =>
                $"{test.PropertyType} => {DslUtility.FindSqlResourceKeyPropertyType(sqlResources, "TestKey_", (PropertyInfo)Activator.CreateInstance(test.PropertyType)).ResourceKey}"));

            string expectedReport = string.Join(Environment.NewLine, tests.Select(test =>
                $"{test.PropertyType} => {test.ExpectedFoundKey}"));

            Assert.AreEqual(expectedReport, report);
        }
    }
}
