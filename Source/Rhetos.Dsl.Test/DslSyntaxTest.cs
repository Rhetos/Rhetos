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

using System.Data;
using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using Rhetos.Utilities;
using Rhetos.DatabaseGenerator;
using Rhetos.TestCommon;
using System.Linq;

namespace Rhetos.Dsl.Test
{
    [TestClass]
    public class DslSyntaxTest
    {
        class SimpleConceptInfo : IConceptInfo
        {
            [ConceptKey]
            public string Name { get; set; }
        }

        class DerivedConceptInfo : SimpleConceptInfo
        {
            public string DerivedName { get; set; }
        }

        class Derived2ConceptInfo : DerivedConceptInfo
        {
            public string Derived2Name { get; set; }
        }

        class RefConceptInfo : IConceptInfo
        {
            [ConceptKey]
            public string Name { get; set; }
            [ConceptKey]
            public DerivedConceptInfo Reference { get; set; }
        }

        [TestMethod]
        public void ConceptTypeIsAssignableFrom()
        {
            var types = new[] { typeof(SimpleConceptInfo), typeof(DerivedConceptInfo), typeof(Derived2ConceptInfo), typeof(RefConceptInfo) };
            var syntax = DslSyntaxHelper.CreateDslSyntax(types);

            var conceptTypes = types.Select(t => syntax.ConceptTypes.Single(ct => ct.TypeName == t.Name)).ToList();

            var report = new List<string>();

            foreach (var baseType in conceptTypes)
                foreach (var derivedType in conceptTypes)
                    report.Add($"{baseType.TypeName}.IsAssignableFrom({derivedType.TypeName}) = {baseType.IsAssignableFrom(derivedType)}");

            string expected = @"
SimpleConceptInfo.IsAssignableFrom(SimpleConceptInfo) = True
SimpleConceptInfo.IsAssignableFrom(DerivedConceptInfo) = True
SimpleConceptInfo.IsAssignableFrom(Derived2ConceptInfo) = True
SimpleConceptInfo.IsAssignableFrom(RefConceptInfo) = False
DerivedConceptInfo.IsAssignableFrom(SimpleConceptInfo) = False
DerivedConceptInfo.IsAssignableFrom(DerivedConceptInfo) = True
DerivedConceptInfo.IsAssignableFrom(Derived2ConceptInfo) = True
DerivedConceptInfo.IsAssignableFrom(RefConceptInfo) = False
Derived2ConceptInfo.IsAssignableFrom(SimpleConceptInfo) = False
Derived2ConceptInfo.IsAssignableFrom(DerivedConceptInfo) = False
Derived2ConceptInfo.IsAssignableFrom(Derived2ConceptInfo) = True
Derived2ConceptInfo.IsAssignableFrom(RefConceptInfo) = False
RefConceptInfo.IsAssignableFrom(SimpleConceptInfo) = False
RefConceptInfo.IsAssignableFrom(DerivedConceptInfo) = False
RefConceptInfo.IsAssignableFrom(Derived2ConceptInfo) = False
RefConceptInfo.IsAssignableFrom(RefConceptInfo) = True
";

            Assert.AreEqual(expected.Trim(), string.Join("\r\n", report));
        }

        [TestMethod]
        public void ConceptTypeIsInstanceOfType()
        {
            var types = new[] { typeof(SimpleConceptInfo), typeof(DerivedConceptInfo), typeof(Derived2ConceptInfo), typeof(RefConceptInfo) };
            var syntax = DslSyntaxHelper.CreateDslSyntax(types);

            var conceptTypes = types.Select(t => syntax.ConceptTypes.Single(ct => ct.TypeName == t.Name)).ToList();

            var report = new List<string>();

            foreach (var baseType in conceptTypes)
                foreach (var derivedType in conceptTypes)
                {
                    var derivedNode = new ConceptSyntaxNode(derivedType);
                    report.Add($"{baseType.TypeName}.IsInstanceOfType({derivedType.TypeName} node) = {baseType.IsInstanceOfType(derivedNode)}");
                }

            string expected = @"
SimpleConceptInfo.IsInstanceOfType(SimpleConceptInfo node) = True
SimpleConceptInfo.IsInstanceOfType(DerivedConceptInfo node) = True
SimpleConceptInfo.IsInstanceOfType(Derived2ConceptInfo node) = True
SimpleConceptInfo.IsInstanceOfType(RefConceptInfo node) = False
DerivedConceptInfo.IsInstanceOfType(SimpleConceptInfo node) = False
DerivedConceptInfo.IsInstanceOfType(DerivedConceptInfo node) = True
DerivedConceptInfo.IsInstanceOfType(Derived2ConceptInfo node) = True
DerivedConceptInfo.IsInstanceOfType(RefConceptInfo node) = False
Derived2ConceptInfo.IsInstanceOfType(SimpleConceptInfo node) = False
Derived2ConceptInfo.IsInstanceOfType(DerivedConceptInfo node) = False
Derived2ConceptInfo.IsInstanceOfType(Derived2ConceptInfo node) = True
Derived2ConceptInfo.IsInstanceOfType(RefConceptInfo node) = False
RefConceptInfo.IsInstanceOfType(SimpleConceptInfo node) = False
RefConceptInfo.IsInstanceOfType(DerivedConceptInfo node) = False
RefConceptInfo.IsInstanceOfType(Derived2ConceptInfo node) = False
RefConceptInfo.IsInstanceOfType(RefConceptInfo node) = True
";

            Assert.AreEqual(expected.Trim(), string.Join("\r\n", report));
        }

        class C1 : IConceptInfo { }

        class C2 : C1 { }

        class C3 : C2 { }

        class D1 { }

        class D2 : D1 { }

        [TestMethod]
        public void GetBaseConceptInfoTypes()
        {
            var tests = new[]
            {
                typeof(C1),
                typeof(C2),
                typeof(C3),
                typeof(D1),
                typeof(D2),
                typeof(IConceptInfo),
                typeof(object),
            };

            var results = tests.Select(type =>
            (
                In: type,
                Out: DslSyntaxFromPlugins.GetBaseConceptInfoTypes(type)
            )).ToList();

            var report = string.Join("\r\n",
                results.Select(r => $"{r.In.Name}: {TestUtility.DumpSorted(r.Out, t => t.Name)}"));

            Assert.AreEqual(
@"C1: 
C2: C1
C3: C1, C2
D1: 
D2: 
IConceptInfo: 
Object: ",
                report);
        }
    }
}
