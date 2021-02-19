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

namespace Rhetos.CommonConcepts.Test
{
    [TestClass]
    public class DataStructureReadParametersTest
    {
        [TestMethod]
        public void GetReadParameters_Basic()
        {
            var repositoryNamedPluginsMock = new RepositoryNamedPluginsMock();
            repositoryNamedPluginsMock.Plugins["RhetosCommonConceptsTestModule.TestDataStructure"] = new IRepository[] { new FakeRepository() };
            IDataStructureReadParameters readParameters = new DataStructureReadParameters(repositoryNamedPluginsMock);

            var expectedBasic = new[]
            {
                "RhetosCommonConceptsTestModule.TestFilterClass: RhetosCommonConceptsTestModule.TestFilterClass",
                "System.Collections.Generic.IEnumerable<System.Guid>: System.Collections.Generic.IEnumerable`1[System.Guid]"
            };

            Assert.AreEqual(
                TestUtility.DumpSorted(expectedBasic),
                TestUtility.DumpSorted(
                    readParameters.GetReadParameters("RhetosCommonConceptsTestModule.TestDataStructure", extendedSet: false)));
        }

        [TestMethod]
        public void GetReadParameters_Extended()
        {
            var repositoryNamedPluginsMock = new RepositoryNamedPluginsMock();
            repositoryNamedPluginsMock.Plugins["RhetosCommonConceptsTestModule.TestDataStructure"] = new IRepository[] { new FakeRepository() };
            IDataStructureReadParameters readParameters = new DataStructureReadParameters(repositoryNamedPluginsMock);

            var expectedExtended = new[]
            {
                "RhetosCommonConceptsTestModule.TestFilterClass: RhetosCommonConceptsTestModule.TestFilterClass",
                "TestFilterClass: RhetosCommonConceptsTestModule.TestFilterClass",

                "System.Collections.Generic.IEnumerable<System.Guid>: System.Collections.Generic.IEnumerable`1[System.Guid]",
                "IEnumerable<System.Guid>: System.Collections.Generic.IEnumerable`1[System.Guid]",
                "System.Guid[]: System.Guid[]",
                "Guid[]: System.Guid[]",
            };

            Assert.AreEqual(
                TestUtility.DumpSorted(expectedExtended),
                TestUtility.DumpSorted(
                    readParameters.GetReadParameters("RhetosCommonConceptsTestModule.TestDataStructure", extendedSet: true)));
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