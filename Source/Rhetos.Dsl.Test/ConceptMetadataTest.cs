using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.TestCommon;

namespace Rhetos.Dsl.Test
{
    [TestClass]
    public class ConceptMetadataTest
    {
        class SomeConcept : IConceptInfo
        {
            [ConceptKey]
            public string Name { get; set; }
        }

        static readonly ConceptMetadataType<string> cmString = new ConceptMetadataType<string>("cmString");
        static readonly ConceptMetadataType<int> cmInt = new ConceptMetadataType<int>();

        [TestMethod]
        public void GetSetMetadata()
        {
            var cm = new ConceptMetadata();

            var c1 = new SomeConcept { Name = "c1" };
            var c2 = new SomeConcept { Name = "c2" };
            var c3 = new SomeConcept { Name = "c3" };

            cm.Set(c1, cmString, "abc");
            cm.Set(c2, cmString, "def");
            cm.Set(c2, cmInt, 123);

            Assert.AreEqual("abc", cm.Get(c1, cmString));
            Assert.AreEqual("def", cm.Get(c2, cmString));
            Assert.AreEqual(123, cm.Get(c2, cmInt));

            TestUtility.ShouldFail<FrameworkException>(() => cm.Get(c1, cmInt), "There is no metadata", "SomeConcept", "c1", cmInt.Id.ToString());
            TestUtility.ShouldFail<FrameworkException>(() => cm.Get(c3, cmString), "There is no metadata", "SomeConcept", "c3", "cmString", cmString.Id.ToString());
            TestUtility.ShouldFail<FrameworkException>(() => cm.Get(c3, cmInt), "There is no metadata", "SomeConcept");

            TestUtility.ShouldFail<FrameworkException>(() => cm.Set(c1, cmString, "abc"), "metadata is already set", "SomeConcept", "c1", "cmString", cmString.Id.ToString());
        }
    }
}
