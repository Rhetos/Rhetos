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
                : base(new ConsoleLogProvider(), new MockPluginsContainer<IDslModelIndex>(new DslModelIndexByType()), new ConfigurationBuilder().Build())
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

        class CBase : IConceptInfo
        {
            [ConceptKey]
            public string Name { get; set; }

            public string Info { get; set; }
        }

        class CDerived : CBase
        {
            public string Info2 { get; set; }
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

            var g = new C2 { Name = "G", Ref2 = missing }; // unresolved
            var h = new C1 { Name = "H" }; // resolved, but references unresolved
            g.Ref1 = h;
            h.Ref1 = g;

            var newConcepts = dslContainer.AddNewConceptsAndReplaceReferences(new IConceptInfo[] { a }).NewUniqueConcepts;
            Assert.AreEqual("A", TestUtility.DumpSorted(newConcepts, item => ((dynamic)item).Name));
            Assert.AreEqual("A", TestUtility.DumpSorted(dslContainer.Concepts, item => ((dynamic)item).Name));

            newConcepts = dslContainer.AddNewConceptsAndReplaceReferences(new IConceptInfo[] { i, d, c, b, g, h, i, j, a }).NewUniqueConcepts;
            Assert.AreEqual("B, C, D, G, H, I, J", TestUtility.DumpSorted(newConcepts, item => ((dynamic)item).Name));
            Assert.AreEqual("A, B, C", TestUtility.DumpSorted(dslContainer.Concepts, item => ((dynamic)item).Name));

            newConcepts = dslContainer.AddNewConceptsAndReplaceReferences(new IConceptInfo[] { i, d, c, b, g, h, i, j, a }).NewUniqueConcepts;
            Assert.AreEqual("", TestUtility.DumpSorted(newConcepts, item => ((dynamic)item).Name));
            Assert.AreEqual("A, B, C", TestUtility.DumpSorted(dslContainer.Concepts, item => ((dynamic)item).Name));

            var ex = TestUtility.ShouldFail<DslSyntaxException>(
                () => dslContainer.ReportErrorForUnresolvedConcepts(),
                "Referenced concept is not defined in DSL scripts",
                missing.GetUserDescription());

            Assert.IsTrue(new IConceptInfo[] { d, i, j, g, h }
                .Any(conceptInfo => ex.Message.Contains(conceptInfo.GetUserDescription())));
        }


        [TestMethod]
        public void CircularReferences()
        {
            var dslContainer = new DslContainerAccessor();

            var a = new C0 { Name = "A" }; // should be resolved
            var b = new C1 { Name = "B" }; // referencing circular dependency
            var c = new C1 { Name = "C" }; // circular dependency
            var d = new C1 { Name = "D" }; // circular dependency
            var e = new C1 { Name = "E" }; // referencing circular dependency
            b.Ref1 = c;
            c.Ref1 = d;
            d.Ref1 = c;
            e.Ref1 = d;

            var report = dslContainer.AddNewConceptsAndReplaceReferences(new IConceptInfo[] { a, b, c, d, e });
            Assert.AreEqual("A, B, C, D, E", TestUtility.DumpSorted(report.NewUniqueConcepts, item => ((dynamic)item).Name));
            Assert.AreEqual("A", TestUtility.DumpSorted(report.NewlyResolvedConcepts, item => ((dynamic)item).Name));
            Assert.AreEqual("A", TestUtility.DumpSorted(dslContainer.Concepts, item => ((dynamic)item).Name));

            var ex = TestUtility.ShouldFail<DslSyntaxException>(
                () => dslContainer.ReportErrorForUnresolvedConcepts(),
                "circular dependency");

            // Circular dependency error message should contain only the circle, not external references to it.
            Assert.IsTrue(new IConceptInfo[] { c, d }.All(conceptInfo => ex.Message.Contains(conceptInfo.GetUserDescription())));
            Assert.IsFalse(new IConceptInfo[] { b, e }.Any(conceptInfo => ex.Message.Contains(conceptInfo.GetUserDescription())));
        }

        [TestMethod]
        public void DifferentConceptSameKey()
        {
            var a1 = new CBase { Name = "A", Info = "1" };
            var a2 = new CBase { Name = "A", Info = "2" };
            var b1 = new CBase { Name = "B", Info = "1" };

            var a1_x = new CDerived { Name = "A", Info = "1", Info2 = "x" };
            var a1_y = new CDerived { Name = "A", Info = "1", Info2 = "y" };
            var a3_x = new CDerived { Name = "A", Info = "3", Info2 = "x" };

            TestDifferentConceptSameKey(new ListOfTuples<IConceptInfo[], string, string, string[]>
            {
                { new IConceptInfo[] { a1_x }, "CDerived A", "CDerived A", null },
                { new IConceptInfo[] { a1, b1 }, "CBase B", "CBase B, CDerived A", null }, // a1 is a base concept for a1_x, and has same values of the mutual properties, so it will be ignored.
            });

            TestDifferentConceptSameKey(new ListOfTuples<IConceptInfo[], string, string, string[]>
            {
                { new IConceptInfo[] { a1_x }, "CDerived A", "CDerived A", null },
                { new IConceptInfo[] { a2 }, null, null, new[] { "DslSyntaxException", "different values", "CDerived A 1 x", "CBase A 2" } }, // a1_x is a derivation of a2, but has different values of the mutual properties.
            });

            TestDifferentConceptSameKey(new ListOfTuples<IConceptInfo[], string, string, string[]>
            {
                { new IConceptInfo[] { a3_x }, "CDerived A", "CDerived A", null },
                { new IConceptInfo[] { a1 }, null, null, new[] { "DslSyntaxException", "different values", "CDerived A 3 x", "CBase A 1" } }, // a1_x is a derivation of a2, but has different values of the mutual properties.
            });

            TestDifferentConceptSameKey(new ListOfTuples<IConceptInfo[], string, string, string[]>
            {
                { new IConceptInfo[] { a1_x }, "CDerived A", "CDerived A", null },
                { new IConceptInfo[] { a1_y }, null, null, new[] { "DslSyntaxException", "different values", "CDerived A 1 x", "CDerived A 1 y" } },
            });

            TestDifferentConceptSameKey(new ListOfTuples<IConceptInfo[], string, string, string[]>
            {
                { new IConceptInfo[] { a1_x }, "CDerived A", "CDerived A", null },
                { new IConceptInfo[] { a3_x }, null, null, new[] { "DslSyntaxException", "different values", "CDerived A 1 x", "CDerived A 3 x" } },
            });
        }

        private void TestDifferentConceptSameKey(ListOfTuples<IConceptInfo[], string, string, string[]> newConceptsSets)
        {
            var dslContainer = new DslContainerAccessor();
            foreach (var newConceptsSet in newConceptsSets)
            {
                if (newConceptsSet.Item4 == null)
                {
                    var report = dslContainer.AddNewConceptsAndReplaceReferences(newConceptsSet.Item1);
                    Assert.AreEqual(newConceptsSet.Item2, TestUtility.DumpSorted(report.NewUniqueConcepts, item => item.GetShortDescription()));
                    Assert.AreEqual(newConceptsSet.Item3, TestUtility.DumpSorted(dslContainer.Concepts, item => item.GetShortDescription()));
                }
                else
                {
                    TestUtility.ShouldFail(() => dslContainer.AddNewConceptsAndReplaceReferences(newConceptsSet.Item1), newConceptsSet.Item4);
                }
            }
        }
    }
}
