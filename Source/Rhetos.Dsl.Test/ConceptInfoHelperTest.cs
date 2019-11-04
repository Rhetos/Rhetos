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

using Rhetos.Dsl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using Rhetos.TestCommon;

namespace Rhetos.Dsl.Test
{

    [TestClass]
    public class ConceptInfoHelperTest
    {
        #region Sample concept classes

        class SimpleConceptInfo : IConceptInfo
        {
            [ConceptKey]
            public string Name { get; set; }
            public string Data { get; set; }

            public SimpleConceptInfo() { }

            public SimpleConceptInfo(string name, string data)
            {
                Name = name;
                Data = data;
            }
        }

        class DerivedConceptInfo : SimpleConceptInfo
        {
            public string Extra { get; set; }

            public DerivedConceptInfo(string name, string data, string extra)
                : base(name, data)
            {
                Extra = extra;
            }
        }

        class DerivedWithKeyInfo : SimpleConceptInfo
        {
            [ConceptKey]
            public string Extra { get; set; }

            public DerivedWithKeyInfo(string name, string data, string extra)
                : base(name, data)
            {
                Extra = extra;
            }
        }

        class RefConceptInfo : IConceptInfo
        {
            [ConceptKey]
            public string Name { get; set; }
            [ConceptKey]
            public SimpleConceptInfo Reference { get; set; }

            public RefConceptInfo() { }
            public RefConceptInfo(string name, SimpleConceptInfo reference)
            {
                Name = name;
                Reference = reference;
            }
        }

        class RefRefConceptInfo : IConceptInfo
        {
            [ConceptKey]
            public string Name { get; set; }
            [ConceptKey]
            public RefConceptInfo Reference { get; set; }

            public RefRefConceptInfo() { }
            public RefRefConceptInfo(string name, RefConceptInfo reference)
            {
                Name = name;
                Reference = reference;
            }
        }

        class RefIntConceptInfo : IConceptInfo
        {
            [ConceptKey]
            public string Name { get; set; }
            [ConceptKey]
            public IConceptInfo Reference { get; set; }
        }

        #endregion

        //=========================================================================


        [TestMethod]
        public void GetKey_Simple()
        {
            Assert.AreEqual("SimpleConceptInfo a", new SimpleConceptInfo("a", "b").GetKey());
            Assert.AreEqual("SimpleConceptInfo 'a x'", new SimpleConceptInfo("a x", "b").GetKey());
            Assert.AreEqual("SimpleConceptInfo ''", new SimpleConceptInfo("", "b").GetKey());
            Assert.AreEqual("SimpleConceptInfo '\"'", new SimpleConceptInfo("\"", "b").GetKey(), "Should use single quotes when text contains double quotes.");
            Assert.AreEqual("SimpleConceptInfo \"'\"", new SimpleConceptInfo("'", "b").GetKey(), "Should use double quotes when text contains single quotes.");
            Assert.AreEqual("SimpleConceptInfo '''\"'", new SimpleConceptInfo("'\"", "b").GetKey(), "Should use single quote when text contains both quotes.");
            Assert.AreEqual("SimpleConceptInfo a123_a", new SimpleConceptInfo("a123_a", "b").GetKey());
        }

        [TestMethod]
        public void GetKey_Derived()
        {
            Assert.AreEqual("SimpleConceptInfo a", new DerivedConceptInfo("a", "b", "c").GetKey());
        }

        [TestMethod]
        [ExpectedException(typeof(FrameworkException))]
        public void GetKey_DerivationMustNotHaveKey()
        {
            try
            {
                new DerivedWithKeyInfo("a", "b", "c").GetKey();
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.Contains("DerivedWithKeyInfo"));
                Assert.IsTrue(ex.Message.Contains("Extra"));
                throw;
            }
        }

        [TestMethod]
        public void GetKey_Reference()
        {
            Assert.AreEqual(
                "RefConceptInfo a.b",
                new RefConceptInfo("a", new SimpleConceptInfo("b", "c")).GetKey());
        }

        [TestMethod]
        public void GetKey_ReferenceToInterface()
        {
            Assert.AreEqual(
                "RefIntConceptInfo a.SimpleConceptInfo:b",
                new RefIntConceptInfo { Name = "a", Reference = new DerivedConceptInfo("b", "c", "d")}.GetKey());
        }

        //=========================================================================


        [TestMethod]
        public void GetKeyProperties_Reference()
        {
            Assert.AreEqual(
                "a.b",
                new RefConceptInfo("a", new SimpleConceptInfo("b", "c")).GetKeyProperties());
        }

        //=========================================================================


        [TestMethod]
        public void GetFullDescription_Simple()
        {
            Assert.AreEqual("Rhetos.Dsl.Test.ConceptInfoHelperTest+SimpleConceptInfo a b", new SimpleConceptInfo("a", "b").GetFullDescription());
            Assert.AreEqual("Rhetos.Dsl.Test.ConceptInfoHelperTest+SimpleConceptInfo \"'\" ''", new SimpleConceptInfo("'", "").GetFullDescription());
        }

        [TestMethod]
        public void GetFullDescription_Derived()
        {
            Assert.AreEqual("Rhetos.Dsl.Test.ConceptInfoHelperTest+DerivedConceptInfo a b c", new DerivedConceptInfo("a", "b", "c").GetFullDescription());
        }

        [TestMethod]
        [ExpectedException(typeof(FrameworkException))]
        public void GetFullDescription_DerivationMustNotHaveKey()
        {
            try
            {
                new DerivedWithKeyInfo("a", "b", "c").GetFullDescription();
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.Contains("DerivedWithKeyInfo"));
                Assert.IsTrue(ex.Message.Contains("Extra"));
                throw;
            }
        }

        [TestMethod]
        public void GetFullDescription_ReferencedConceptIsDescribedWithKeyOnly()
        {
            Assert.AreEqual(
                "Rhetos.Dsl.Test.ConceptInfoHelperTest+RefConceptInfo a.b",
                new RefConceptInfo("a", new DerivedConceptInfo("b", "c", "d")).GetFullDescription());
        }

        //=========================================================================

        [TestMethod]
        public void GetDirectDependencies_Empty()
        {
            var conceptInfo = new SimpleConceptInfo("s", "d");
            var dependencies = conceptInfo.GetDirectDependencies();
            Assert.AreEqual("()", Dump(dependencies));
        }

        [TestMethod]
        public void GetDirectDependencies_NotRecursive()
        {
            var simpleConceptInfo = new SimpleConceptInfo("s", "d");
            var refConceptInfo = new RefConceptInfo("r", simpleConceptInfo);
            var refRefConceptInfo = new RefRefConceptInfo("rr", refConceptInfo);

            var dependencies = refRefConceptInfo.GetDirectDependencies();
            Assert.AreEqual(Dump(new IConceptInfo[] { refConceptInfo }), Dump(dependencies));
        }

        [TestMethod]
        public void GetAllDependencies_Recursive()
        {
            var simpleConceptInfo = new SimpleConceptInfo("s", "d");
            var refConceptInfo = new RefConceptInfo("r", simpleConceptInfo);
            var refRefConceptInfo = new RefRefConceptInfo("rr", refConceptInfo);

            var dependencies = refRefConceptInfo.GetAllDependencies();
            Assert.AreEqual(Dump(new IConceptInfo[] { simpleConceptInfo, refConceptInfo }), Dump(dependencies));
        }

        private static string Dump(IEnumerable<IConceptInfo> list)
        {
            var result = "(" + string.Join(",", list.Select(Dump).OrderBy(s => s)) + ")";
            Console.WriteLine(result);
            return result;
        }

        private static string Dump(IConceptInfo ci)
        {
            var result = ci.GetKey();
            Console.WriteLine(result);
            return result;
        }

        //=========================================================================

        [TestMethod]
        public void GetErrorDescription()
        {
            var simpleConceptInfo = new SimpleConceptInfo { Name = "s", Data = "d" };
            var refConceptInfo = new RefConceptInfo { Name = "r", Reference = simpleConceptInfo };
            var refRefConceptInfo = new RefRefConceptInfo { Name = "rr", Reference = refConceptInfo };

            Assert.AreEqual("Rhetos.Dsl.Test.ConceptInfoHelperTest+RefRefConceptInfo Name=rr Reference=r.s", refRefConceptInfo.GetErrorDescription());

            refRefConceptInfo.Name = null;
            Assert.AreEqual("Rhetos.Dsl.Test.ConceptInfoHelperTest+RefRefConceptInfo Name=<null> Reference=r.s", refRefConceptInfo.GetErrorDescription());
            refRefConceptInfo.Name = "rr";

            refRefConceptInfo.Reference = null;
            Assert.AreEqual("Rhetos.Dsl.Test.ConceptInfoHelperTest+RefRefConceptInfo Name=rr Reference=<null>", refRefConceptInfo.GetErrorDescription());
            refRefConceptInfo.Reference = refConceptInfo;

            simpleConceptInfo.Name = null;
            TestUtility.AssertContains(refRefConceptInfo.GetErrorDescription(),
                new[] { refRefConceptInfo.GetType().FullName, "Name=rr", "Reference=", "null" });
            simpleConceptInfo.Name = "s";

            Assert.AreEqual("<null>", ConceptInfoHelper.GetErrorDescription(null));

            Assert.AreEqual(typeof(SimpleConceptInfo).FullName + " Name=s Data=<null>", new SimpleConceptInfo { Name = "s", Data = null }.GetErrorDescription());
        }
    }
}