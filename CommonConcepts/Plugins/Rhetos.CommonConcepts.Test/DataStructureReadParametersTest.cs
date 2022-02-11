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
using Rhetos.Dom.DefaultConcepts;
using Rhetos.TestCommon;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.CommonConcepts.Test
{
    [TestClass]
    public class DataStructureReadParametersTest
    {
        [TestMethod]
        public void GetReadParameters_Basic()
        {
            var readParameters = new DataStructureReadParameters(new Dictionary<string, KeyValuePair<string, Type>[]> {
                { "RhetosCommonConceptsTestModule.TestDataStructure", FakeRepository.ReadParameterTypes }
            });

            var expectedBasic = new[]
            {
                "RhetosCommonConceptsTestModule.TestFilterClass: RhetosCommonConceptsTestModule.TestFilterClass",
                "IEnumerable<Guid>: System.Collections.Generic.IEnumerable`1[System.Guid]"
            };

            Assert.AreEqual(
                TestUtility.DumpSorted(expectedBasic),
                TestUtility.DumpSorted(
                    readParameters.GetReadParameters("RhetosCommonConceptsTestModule.TestDataStructure", extendedSet: false)));
        }

        [TestMethod]
        public void GetReadParameters_Extended()
        {
            var readParameters = new DataStructureReadParameters(new Dictionary<string, KeyValuePair<string, Type>[]> {
                { "RhetosCommonConceptsTestModule.TestDataStructure", FakeRepository.ReadParameterTypes }
            });

            var expectedExtended = new[]
            {
                // Specified type:

                "RhetosCommonConceptsTestModule.TestFilterClass: RhetosCommonConceptsTestModule.TestFilterClass",
                // Additional type name versions:
                "TestFilterClass: RhetosCommonConceptsTestModule.TestFilterClass", // Optional namespace since the filter is implemented within the same namespace.

                // Standard entity filter types and additional name versions:

                "IEnumerable<Guid>: System.Collections.Generic.IEnumerable`1[System.Guid]",
                "Guid[]: System.Collections.Generic.IEnumerable`1[System.Guid]",
                $"{typeof(IEnumerable<Guid>)}: System.Collections.Generic.IEnumerable`1[System.Guid]", // name is Type.ToString()
            };

            var actualTypes = readParameters.GetReadParameters("RhetosCommonConceptsTestModule.TestDataStructure", extendedSet: true);

            Assert.AreEqual(
                string.Join(Environment.NewLine, expectedExtended.OrderBy(x => x)),
                string.Join(Environment.NewLine, actualTypes.Select(t => t.ToString()).OrderBy(x => x)));
        }

        [TestMethod]
        public void ShortParameterNameHeuristics()
        {
            // Filter name is specified in DSL script in C# format, to be inserted in the generated source code.
            Type complexType = typeof(System.Tuple<System.String, RhetosCommonConceptsTestModule.TestFilterClass, System.Collections.Generic.List<System.String>>);
            string complexTypeName = "System.Tuple<System.String, RhetosCommonConceptsTestModule.TestFilterClass, System.Collections.Generic.List<System.String>>";

            var readParameters = new DataStructureReadParameters(new Dictionary<string, KeyValuePair<string, Type>[]> {
                {
                    "RhetosCommonConceptsTestModule.TestDataStructure",
                    new[]
                    {
                        new KeyValuePair<string, Type>(complexTypeName, complexType)
                    }
                }
            });

            var expectedExtended = new[]
            {
                "System.Tuple<System.String, RhetosCommonConceptsTestModule.TestFilterClass, System.Collections.Generic.List<System.String>>: System.Tuple`3[System.String,RhetosCommonConceptsTestModule.TestFilterClass,System.Collections.Generic.List`1[System.String]]",
                "Tuple<String, TestFilterClass, List<String>>: System.Tuple`3[System.String,RhetosCommonConceptsTestModule.TestFilterClass,System.Collections.Generic.List`1[System.String]]",
                $"{complexType}: {complexType}", // name is Type.ToString()

                "IEnumerable<Guid>: System.Collections.Generic.IEnumerable`1[System.Guid]",
                "Guid[]: System.Collections.Generic.IEnumerable`1[System.Guid]",
                $"{typeof(IEnumerable<Guid>)}: System.Collections.Generic.IEnumerable`1[System.Guid]", // name is Type.ToString()
            };

            var actualTypes = readParameters.GetReadParameters("RhetosCommonConceptsTestModule.TestDataStructure", extendedSet: true);

            Assert.AreEqual(
                string.Join(Environment.NewLine, expectedExtended.OrderBy(x => x)),
                string.Join(Environment.NewLine, actualTypes.Select(t => t.ToString()).OrderBy(x => x)));
        }

        internal class FakeRepository : IRepository
        {
            public static readonly KeyValuePair<string, Type>[] ReadParameterTypes = new KeyValuePair<string, Type>[]
            {
                new KeyValuePair<string, Type>("RhetosCommonConceptsTestModule.TestFilterClass", typeof(RhetosCommonConceptsTestModule.TestFilterClass)),
            };
        }
    }
}

namespace RhetosCommonConceptsTestModule
{
    internal class TestFilterClass
    {
    }
}