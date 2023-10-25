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
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.TestCommon;

namespace Rhetos.CommonConcepts.Test
{
    [TestClass]
    public class DataStructureUtilityTest
    {
        [TestMethod]
        public void SplitModuleName()
        {
            {
                var split = DataStructureUtility.SplitModuleName("a.bcd");
                Assert.AreEqual("a/bcd", split.Item1 + "/" + split.Item2);
            }
            {
                var split = DataStructureUtility.SplitModuleName("abc.d");
                Assert.AreEqual("abc/d", split.Item1 + "/" + split.Item2);
            }
            {
                TestUtility.ShouldFail<FrameworkException>(() => DataStructureUtility.SplitModuleName("abc"), "abc", "format", "module.name");
                TestUtility.ShouldFail<FrameworkException>(() => DataStructureUtility.SplitModuleName(""), "format", "module.name");
            }
        }

        [TestMethod]
        public void IsAssemblyQualifiedNameTest_False()
        {
            var tests = new[]
            {
                "a",
                "UserQuery+C",
                "List<Tuple<string, string>>",
                "Tuple<string, string>",
                "(a, b, c)",
                "Dict<int, string[]>",
                "Dict<int, string[]>[]",
                "Dict<string[], int>[]",
                "Dict<string[], int>",
            };
            foreach (var test in tests)
            {
                var repositoryUses = new RepositoryUsesInfo { PropertyType = test };
                repositoryUses.CheckSemantics(null); // This should not throw an error.
            }
        }

        [TestMethod]
        public void IsAssemblyQualifiedNameTest_True()
        {
            var tests = new[]
            {
                "UserQuery+C, query_quytvu, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null",
                "UserQuery+C, query_quytvu, Version=0.0.0.0",
                "UserQuery+C, query_quytvu",
                "UserQuery.C, query_quytvu, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null",
                "UserQuery.C, query_quytvu, Version=0.0.0.0",
                "UserQuery.C, query_quytvu",
                "a, b",
                "a.b, c.d",
                "a, x.y",
                "UserQuery+C, x.y",
                "List<Tuple<string, string>>, x.y",
                "Tuple<string, string>, x.y",
                "(a, b, c), x.y",
                "Dict<int, string[]>, x.y",
                "Dict<int, string[]>[], x.y",
                "Dict<string[], int>[], x.y",
                "Dict<string[], int>, x.y",
            };
            foreach (var test in tests)
            {
                var repositoryUses = new RepositoryUsesInfo { PropertyType = test };
                TestUtility.ShouldFail<DslSyntaxException>(
                    () => repositoryUses.CheckSemantics(null),
                    "Use a full type name with namespace, as written in C# source, instead of the assembly qualified name");
            }
        }
    }
}
