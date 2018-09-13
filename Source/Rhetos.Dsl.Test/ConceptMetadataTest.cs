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

        static readonly ConceptMetadataKey<string> cmString = new ConceptMetadataKey<string>("cmString");
        static readonly ConceptMetadataKey<int> cmInt = new ConceptMetadataKey<int>();

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

            TestUtility.ShouldFail<FrameworkException>(() => cm.Get(c1, cmInt), "There is no requested metadata", "SomeConcept", "c1", cmInt.Id.ToString());
            TestUtility.ShouldFail<FrameworkException>(() => cm.Get(c3, cmString), "There is no requested metadata", "SomeConcept", "c3", "cmString", cmString.Id.ToString());
            TestUtility.ShouldFail<FrameworkException>(() => cm.Get(c3, cmInt), "There is no requested metadata", "SomeConcept");

            TestUtility.ShouldFail<FrameworkException>(() => cm.Set(c1, cmString, "xyz"),
                "metadata value is already set", "SomeConcept", "c1", "cmString", cmString.Id.ToString(), "abc", "xyz");
        }

        [TestMethod]
        public void ContainsMetadata()
        {
            var cm = new ConceptMetadata();

            var c1 = new SomeConcept { Name = "c1" };
            var c2 = new SomeConcept { Name = "c2" };
            var c3 = new SomeConcept { Name = "c3" };

            cm.Set(c1, cmString, "abc");
            cm.Set(c2, cmString, "def");
            cm.Set(c2, cmInt, 123);

            Assert.AreEqual(true, cm.Contains(c1, cmString));
            Assert.AreEqual(true, cm.Contains(c2, cmString));
            Assert.AreEqual(true, cm.Contains(c2, cmInt));

            Assert.AreEqual(false, cm.Contains(c1, cmInt));
            Assert.AreEqual(false, cm.Contains(c3, cmString));
            Assert.AreEqual(false, cm.Contains(c3, cmInt));
        }
    }
}
