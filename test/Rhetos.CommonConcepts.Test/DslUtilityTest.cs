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
