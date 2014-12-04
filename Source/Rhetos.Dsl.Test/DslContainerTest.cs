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
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rhetos.Dsl.Test
{
    [TestClass]
    public class DslContainerTest
    {
        class DslContainerAccessor : DslContainer
        {
            public DslContainerAccessor()
                : base(new ConsoleLogProvider())
            {
            }
        }

        class C0 : IConceptInfo
        {
            [ConceptKey]
            public string Name { get; set; }
        }
        class C1 : IConceptInfo
        {
            [ConceptKey]
            public string Name { get; set; }
            public IConceptInfo Ref1 { get; set; }
        }
        class C2 : IConceptInfo
        {
            [ConceptKey]
            public string Name { get; set; }
            public IConceptInfo Ref1 { get; set; }
            public IConceptInfo Ref2 { get; set; }
        }

        [TestMethod]
        public void UnresolvedReferences()
        {
            var dslContainer = new DslContainerAccessor();

            var missing = new C0 { Name = "X" };

            var a = new C0 { Name = "A" }; // should be resolved
            var b = new C1 { Name = "B", Ref1 = a }; // should be resolved
            var c = new C1 { Name = "C", Ref1 = b }; // should be resolved
            var d = new C2 { Name = "D", Ref1 = c, Ref2 = missing }; // unresolved
            var i = new C1 { Name = "I", Ref1 = d }; // resolved, but references unresolved
            var j = new C1 { Name = "J", Ref1 = i }; // resolved, but indirectly references unresolved

            var e = new C1 { Name = "E" }; // should be resolved, circular reference should not be a problem
            var f = new C1 { Name = "F" }; // should be resolved, circular reference should not be a problem
            e.Ref1 = f;
            f.Ref1 = e;

            var g = new C2 { Name = "G", Ref2 = missing }; // unresolved
            var h = new C1 { Name = "H" }; // resolved, but references unresolved
            g.Ref1 = h;
            h.Ref1 = g;

            var resolved = dslContainer.AddNewConceptsAndReplaceReferences(new IConceptInfo[] { a });
            Assert.ReferenceEquals(a, resolved.Single());

            resolved = dslContainer.AddNewConceptsAndReplaceReferences(new IConceptInfo[] { i, d, c, b, e, f, g, h, i, j });
            Assert.AreEqual("B, C, E, F", TestUtility.DumpSorted(resolved, item => ((dynamic)item).Name));

            resolved = dslContainer.AddNewConceptsAndReplaceReferences(new IConceptInfo[] { });
            Assert.AreEqual("", TestUtility.DumpSorted(resolved, item => ((dynamic)item).Name));

            Assert.AreEqual("A, B, C, E, F", TestUtility.DumpSorted(dslContainer.Concepts, item => ((dynamic)item).Name));

            var ex = TestUtility.ShouldFail<DslSyntaxException>(
                () => dslContainer.ReportErrorForUnresolvedConcepts(),
                "Referenced concept is not defined in DSL scripts",
                missing.GetUserDescription());

            Assert.IsTrue(new IConceptInfo[] { d, i, j, g, h }
                .Any(conceptInfo => ex.Message.Contains(conceptInfo.GetUserDescription())));
        }
    }
}
