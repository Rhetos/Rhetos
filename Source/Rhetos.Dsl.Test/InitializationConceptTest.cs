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
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rhetos.Dsl.Test
{
    [TestClass]
    public class InitializationConceptTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            var ic = new InitializationConcept { RhetosVersion = "1.2.3" };

            Assert.AreEqual("InitializationConcept '1.2.3'", ic.GetKey());
            Assert.AreEqual("InitializationConcept '1.2.3'", ic.GetShortDescription());
            Assert.AreEqual("InitializationConcept '1.2.3'", ic.GetUserDescription());
            Assert.AreEqual("'1.2.3'", ic.GetKeyProperties());
            Assert.AreEqual("Rhetos.Dsl.InitializationConcept '1.2.3'", ic.GetFullDescription());
            Assert.AreEqual(null, ic.GetKeyword());
            Assert.AreEqual("InitializationConcept", ic.GetKeywordOrTypeName());
            Assert.AreEqual(0, ic.GetDirectDependencies().Count());
            Assert.AreEqual(0, ic.GetAllDependencies().Count());
            Assert.AreEqual("Rhetos.Dsl.InitializationConcept RhetosVersion=1.2.3", ic.GetErrorDescription());
            
            Assert.AreEqual(null, ConceptInfoHelper.GetKeyword(typeof(InitializationConcept)));
            Assert.AreEqual("InitializationConcept", ConceptInfoHelper.GetKeywordOrTypeName(typeof(InitializationConcept)));
        }
    }
}
