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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Rhetos.Dsl.Test
{
    [TestClass]
    public class DslContainerTest
    {
        class DslContainerAccessor : DslContainer
        {
            public DslContainerAccessor()
                : base(new ConsoleLogProvider(),
                      new MockPluginsContainer<IDslModelIndex>(new DslModelIndexByType()))
            {
            }

            public List<IConceptInfo> ResolvedConcepts
            {
                get
                {
                    return (List<IConceptInfo>)typeof(DslContainer)
                        .GetField("_resolvedConcepts", BindingFlags.NonPublic | BindingFlags.Instance)
                        .GetValue(this);
                }
                set
                {
                    typeof(DslContainer)
                        .GetField("_resolvedConcepts", BindingFlags.NonPublic | BindingFlags.Instance)
                        .SetValue(this, value);
                }
            }
        }

        [DebuggerDisplay("C0 {Name}")]
        class C0 : IConceptInfo
        {
            [ConceptKey]
            public string Name { get; set; }
        }

        [DebuggerDisplay("C1 {Name}")]
        class C1 : IConceptInfo
        {
            [ConceptKey]
            public string Name { get; set; }
            public IConceptInfo Ref1 { get; set; }
        }

        [DebuggerDisplay("C2 {Name}")]
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

            var newUniqueConcepts = dslContainer.AddNewConceptsAndReplaceReferences(new IConceptInfo[] { a });
            Assert.AreEqual("A", TestUtility.DumpSorted(newUniqueConcepts, item => ((dynamic)item).Name));
            Assert.AreEqual("A", TestUtility.DumpSorted(dslContainer.Concepts, item => ((dynamic)item).Name));

            newUniqueConcepts = dslContainer.AddNewConceptsAndReplaceReferences(new IConceptInfo[] { i, d, c, b, g, h, i, j, a });
            Assert.AreEqual("B, C, D, G, H, I, J", TestUtility.DumpSorted(newUniqueConcepts, item => ((dynamic)item).Name));
            Assert.AreEqual("A, B, C", TestUtility.DumpSorted(dslContainer.Concepts, item => ((dynamic)item).Name));

            newUniqueConcepts = dslContainer.AddNewConceptsAndReplaceReferences(new IConceptInfo[] { i, d, c, b, g, h, i, j, a });
            Assert.AreEqual("", TestUtility.DumpSorted(newUniqueConcepts, item => ((dynamic)item).Name));
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

            var newUniqueConcepts = dslContainer.AddNewConceptsAndReplaceReferences(new IConceptInfo[] { a, b, c, d, e });
            Assert.AreEqual("A, B, C, D, E", TestUtility.DumpSorted(newUniqueConcepts, item => ((dynamic)item).Name));
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
                    var newUniqueConcepts = dslContainer.AddNewConceptsAndReplaceReferences(newConceptsSet.Item1);
                    Assert.AreEqual(newConceptsSet.Item2, TestUtility.DumpSorted(newUniqueConcepts, item => item.GetShortDescription()));
                    Assert.AreEqual(newConceptsSet.Item3, TestUtility.DumpSorted(dslContainer.Concepts, item => item.GetShortDescription()));
                }
                else
                {
                    TestUtility.ShouldFail(() => dslContainer.AddNewConceptsAndReplaceReferences(newConceptsSet.Item1), newConceptsSet.Item4);
                }
            }
        }

        [TestMethod]
        public void Sorting()
        {
            var cInit = new InitializationConcept { RhetosVersion = "init" };
            var c01 = new C0 { Name = "1" };
            var c02 = new C0 { Name = "2" };
            var c03 = new C0 { Name = "3" };
            var cB1 = new CBase { Name = "1" };
            var cB2 = new CBase { Name = "2" };
            var c2Dependant = new C2 { Name = "dep", Ref1 = c01, Ref2 = cB2 };

            var testData = new List<IConceptInfo> { c02, c01, cB2, c2Dependant, cInit, c03, cB1 };

            // The expected positions may somewhat change if the sorting algorithm internals change.
            var tests = new List<(InitialConceptsSort SortMethod, List<IConceptInfo> ExpectedResult)>
            {
                // cInit always goes first. cDependant is moved to the end, even though it does not need to be moved
                // because it is after referenced c01 and cB2. Putting all level 2 concept after level 1,
                // instead of keeping the initial sort based on dynamic evaluation, should result with
                // less changes in the final generated code.
                ( InitialConceptsSort.None, new List<IConceptInfo> { cInit, c02, c01, cB2, c03, cB1, c2Dependant } ),

                // Keeping the InitialConceptsSort ordering between the concepts of the same level.
                ( InitialConceptsSort.Key, new List<IConceptInfo> { cInit, c01, c02, c03, cB1, cB2, c2Dependant } ),

                // Keeping the InitialConceptsSort ordering between the concepts of the same level.
                ( InitialConceptsSort.KeyDescending, new List<IConceptInfo> { cInit, cB2, cB1, c03, c02, c01, c2Dependant } ),
            };

            foreach (var test in tests)
            {
                var dslContainer = new DslContainerAccessor { ResolvedConcepts = testData };
                dslContainer.SortReferencesBeforeUsingConcept(test.SortMethod);
                Assert.AreEqual(
                    TestUtility.Dump(test.ExpectedResult, c => c.GetKey()),
                    TestUtility.Dump(dslContainer.ResolvedConcepts, c => c.GetKey()),
                    $"SortConceptsMethod: {test.SortMethod}");
            }
        }

        [TestMethod]
        public void SortingCircular()
        {
            // Circular dependencies are not supported in Rhetos,
            // but this test verifies robustness of the algorithm implementation
            // and error detection.

            var cA = new C1 { Name = "A" };
            var cB = new C1 { Name = "B" };
            cA.Ref1 = cB;
            cB.Ref1 = cA;
            var cInit = new InitializationConcept { RhetosVersion = "init" };
            var cDependant = new C2 { Name = "dep", Ref1 = cA, Ref2 = cB };

            var testData = new List<IConceptInfo> { cDependant, cA, cB, cInit };
            var dslContainer = new DslContainerAccessor { ResolvedConcepts = testData };
            TestUtility.ShouldFail<FrameworkException>(
                () => dslContainer.SortReferencesBeforeUsingConcept(InitialConceptsSort.None),
                "C1 A");
        }

        [TestMethod]
        public void SortInitOnly()
        {
            var cInit = new InitializationConcept { RhetosVersion = "init" };

            var testData = new List<IConceptInfo> { cInit };
            var dslContainer = new DslContainerAccessor { ResolvedConcepts = testData };
            dslContainer.SortReferencesBeforeUsingConcept(InitialConceptsSort.None);
            Assert.AreEqual(
                TestUtility.Dump(testData, c => c.GetKey()),
                TestUtility.Dump(dslContainer.ResolvedConcepts, c => c.GetKey()));
        }

        [TestMethod]
        public void SortEmpty()
        {
            var testData = new List<IConceptInfo> { };
            var dslContainer = new DslContainerAccessor { ResolvedConcepts = testData };
            dslContainer.SortReferencesBeforeUsingConcept(InitialConceptsSort.None);
            Assert.AreEqual(
                TestUtility.Dump(testData, c => c.GetKey()),
                TestUtility.Dump(dslContainer.ResolvedConcepts, c => c.GetKey()));
        }
    }
}
