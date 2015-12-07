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
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.TestCommon;
using Rhetos.Dsl;
using System.Reflection;
using System.Text.RegularExpressions;
using Rhetos;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.CommonConcepts.Test.Mocks;

namespace CommonConcepts.Test
{
    [TestClass]
    public class SqlAnalysisTest
    {
        private static Regex TextStart = new Regex(@"--|/\*|\'|\[|""");
        private static Regex MultilineNext = new Regex(@"\*/|/\*");
        private static char[] EolCharacters = new[] { '\r', '\n' };
        private static Regex SquareBracketEnd = new Regex(@"\]+");

        private static string RemoveCommentsAndText_AlternativeImplementation(string sql)
        {
            var result = new StringBuilder(sql.Length);
            int lastPosition = 0;

            bool singleLine = false;
            int multiLine = 0;
            bool singleQuote = false;
            bool doubleQuote = false;
            bool squareBracket = false;

            while (true)
            {
                if (singleLine)
                {
                    int end = sql.IndexOfAny(EolCharacters, lastPosition);
                    if (end == -1)
                        break;
                    singleLine = false;
                    lastPosition = end;
                }
                else if (multiLine > 0)
                {
                    var match = MultilineNext.Match(sql, lastPosition);
                    if (!match.Success)
                        break;
                    if (match.Value == "/*")
                        multiLine++;
                    else
                        multiLine--;
                    lastPosition = match.Index + match.Length;
                }
                else if (singleQuote)
                {
                    int end = sql.IndexOf('\'', lastPosition);
                    if (end == -1)
                        break;
                    singleQuote = false;
                    lastPosition = end + 1;
                }
                else if (squareBracket)
                {
                    var match = SquareBracketEnd.Match(sql, lastPosition);
                    if (!match.Success)
                    {
                        result.Append(sql, lastPosition, sql.Length - lastPosition);
                        break;
                    }
                    result.Append(sql, lastPosition, match.Index + match.Length - lastPosition);
                    if (match.Length % 2 == 1)
                        squareBracket = false;
                    lastPosition = match.Index + match.Length;
                }
                else if (doubleQuote)
                {
                    int end = sql.IndexOf('\"', lastPosition);
                    if (end == -1)
                    {
                        result.Append(sql, lastPosition, sql.Length - lastPosition);
                        break;
                    }
                    result.Append(sql, lastPosition, end + 1 - lastPosition);
                    doubleQuote = false;
                    lastPosition = end + 1;
                }
                else
                {
                    var match = TextStart.Match(sql, lastPosition);
                    if (!match.Success)
                    {
                        result.Append(sql, lastPosition, sql.Length - lastPosition);
                        break;
                    }
                    result.Append(sql, lastPosition, match.Index - lastPosition);
                    switch (match.Value)
                    {
                        case "--": singleLine = true; break;
                        case "/*": multiLine = 1; break;
                        case "'": singleQuote = true; break;
                        case "[": squareBracket = true; result.Append(sql, match.Index, 1); break;
                        case "\"": doubleQuote = true; result.Append(sql, match.Index, 1); break;
                        default: throw new FrameworkException("Unexpected match pattern '" + match.Value + "' on SQL dependency analysis.");
                    }
                    lastPosition = match.Index + match.Length;
                }
            }

            return result.ToString();
        }

        [TestMethod]
        public void RemoveCommentsAndTextTest()
        {
            var tests = new Dictionary<string, string>()
            {
                { "", null },
                { " ab ", null },
                { "/a-a-a*/a*/", null },

                { "--", "" },
                { "-", null },
                { "a-", null },
                { "a-b", null },
                { "-b", null },
                { "a--b", "a" },
                { "1--2\r3--4\n5--6\r\n", "1\r3\n5\r\n" },
                { "--\r", "\r" },
                { "--\n", "\n" },
                { "---\n", "\n" },

                { "/**/", "" },
                { "/*/", "" },
                { "/*/1", "" },
                { "/*/1*/2", "2" },
                { "/**", "" },
                { "/", "/" },
                { "*", "*" },
                { "1/*2/*3*/4*/5", "15" },
                { "1/*\r/*\n*/\r\n*/2", "12" },
                { "\r\n1\r\n/*2*/\r\n3\r\n", "\r\n1\r\n\r\n3\r\n" },
                { "/***/1", "1" },
                { "/*//**/1*/2", "2" },

                { "''", "" },
                { "1'23'4", "14" },
                { "1'2''''3'''4", "14" },

                { "1/*2", "1" },
                { "1'2", "1" },
                { "1[2", null },
                { "1\"2", null },

                { "1/*2--3*/4--5", "14" },
                { "1--2/*3\r\n4*/", "1\r\n4*/" },
                { "1'2--3\r\n4'5", "15" },
                { "1'2/*3\r\n4'5*/", "15*/" },
                { "1'2[3'4]", "14]" },

                { "[]", null },
                { "1[2'3]4''5", "1[2'3]45" },
                { "1[2--3]4", null },
                { "1[2/*\r\n*/\r\n3]4", null },
                { "1[2]]]]3--4]]]5--6", "1[2]]]]3--4]]]5" },

                { "\"\"", null },
                { "\"1", null },
                { "1\"", null },
                { "1\"23\"4", null },
                { "1\"2/*3\"4*/", null },
                { "1\"2--3\"4", null },
                { "1\"2'3\"4", null },
                { "1\"2]3\"4", null },
                { "1\"2[3\"4/*1", "1\"2[3\"4" },
            };

            foreach (var test in tests)
            {
                string result = SqlAnalysis.RemoveCommentsAndText(test.Key);
                string alternativeResult = RemoveCommentsAndText_AlternativeImplementation(test.Key);
                Assert.AreEqual(test.Value ?? test.Key, result, "Input: " + test.Key);
                Assert.AreEqual(alternativeResult, result, "Alternative implementation.");
            }
        }

        [TestMethod]
        public void RemoveCommentsAndText_LargeRandomTest()
        {
            var elements = new[] { "'", "\"", "[", "]", "]]", "--", "/*", "*/", "-", "/", "*", "a", "b", "c", "\r\n" };
            for (int seed = 0; seed < 20; seed++)
            {
                var sb = new StringBuilder();
                Random random = new Random(seed);
                for (int c = 0; c < 10000; c++)
                    sb.Append(elements[random.Next(elements.Length)]);
                string sql = sb.ToString();

                string result = SqlAnalysis.RemoveCommentsAndText(sql);
                string alternativeResult = RemoveCommentsAndText_AlternativeImplementation(sql);
                Console.WriteLine(string.Format("seed, sql, result, alternativeResult: {0}, {1}, {2}, {3}.", seed, sql.Length, result.Length, alternativeResult.Length));
                Assert.AreEqual(alternativeResult, result);
            }
        }
     
        private static List<string> ExtractPossibleObjectsAccessor(string sql)
        {
            var method = typeof(SqlAnalysis).GetMethod("ExtractPossibleObjects", BindingFlags.Static | BindingFlags.NonPublic);
            return (List<string>)method.Invoke(null, new object[] { sql });
        }

        [TestMethod]
        public void ExtractPossibleObjectsTest()
        {
            var tests = new Dictionary<string, string>()
            {
                { "select a.b from ccc.ddd a", "ccc.ddd" },
                { "select a.b from c.d a inner join e.f x", "c.d, e.f" },
                { "select a.b from c.d a inner join c.d x", "c.d" },
                { "select a.b from c.d inner join e.f on g.h=x.y", "c.d, e.f" },
                { "select count(*) from a.b, c.d", "a.b, c.d" },
                { "select e.e, f.f from a.b e, c.d f", "a.b, c.d" },
                { "select count(*) from a.b e join c.d(e.f)", "a.b, c.d" },
                { "select a.b(), c.d from e.f c", "a.b, e.f" },
                { "select a.b(c.d) from e.f c", "a.b, e.f" },
                { "select count(*) from a.a, b.b where (a.a.id in select x, y.y(getdate()) from c.c, d.d)", "a.a, b.b, c.c, d.d, y.y" },

                { "select x.x,d.d\r\n(\r\n) from\na.a\njoin\nb.b\r\n,\r\nc.c", "a.a, b.b, c.c, d.d" },
                { "select a.1, [a].[2]() from [a].[3], [a].[4], a.[5], [a].6, [a.7]", "a.2, a.3, a.4, a.5, a.6" },
                { "select a.1, /*a.2 from a.3, a.4", "" },
                { "select a.1 from --a.2 a.3 \r\n a.4 as a.5", "a.4" },
                { "select 'a.1 from a.2, a.3'", "" },
                { "select /*a.1(), */a.2() from a.3/*inner join a.4, a.5*/", "a.2, a.3" },
                { "select a.1 from \"a\".\"2\", a.3", "a.2, a.3" },
                { "select a.1 from a.2, /*a.3, a.4*/ a.5, [a.6], \"a.9\", 'a.11, a.12', a.13", "a.13, a.2, a.5" },
                { "select a.1 from a/*xxx*/./*xxx*/2/*xxx*/ join /*xxx*/a.3", "a.2, a.3" }
            };

            foreach (var test in tests)
                Assert.AreEqual(
                    test.Value,
                    TestUtility.Dump(ExtractPossibleObjectsAccessor(test.Key)),
                    "Input: " + test.Key); 
        }

        class SimpleConceptInfo : IConceptInfo
        {
            [ConceptKey]
            public string Name { get; set; }
            public SimpleConceptInfo(string name)
            {
                Name = name;
            }
            public override string ToString()
            {
                return Name;
            }
        }

        [TestMethod]
        public void GenerateDependenciesTest()
        {
            var dependent = new SimpleConceptInfo("dd");
            dependent.GetKeyProperties();
            var existingConcepts = new DslModelMock {
                new EntityInfo { Module = new ModuleInfo { Name = "a" }, Name = "b" },
                new DataStructureInfo { Module = new ModuleInfo { Name = "c" }, Name = "d" },
                new SqlViewInfo { Module = new ModuleInfo { Name = "e" }, Name = "f" },
                new SqlFunctionInfo { Module = new ModuleInfo { Name = "g" }, Name = "h" }, };

            var tests = new Dictionary<string, string>
            {
                { "select a.b from c.d, x.y", "SqlDependsOnDataStructureInfo c.d" },
                { "select * from a.b, c.d, e.f, g.h", "SqlDependsOnDataStructureInfo a.b, SqlDependsOnDataStructureInfo c.d, "
                    + "SqlDependsOnSqlViewInfo e.f, SqlDependsOnSqlFunctionInfo g.h" },
                { "with x.y as (select * from a.b) select * from x.y", "SqlDependsOnDataStructureInfo a.b" }
            };

            foreach (var test in tests)
            {
                Console.WriteLine("Test: " + test.Key);
                var dependencies = SqlAnalysis.GenerateDependencies(dependent, existingConcepts, test.Key);

                foreach (dynamic dependency in dependencies)
                    Assert.AreEqual("dd", dependency.Dependent.ToString());

                var actual = TestUtility.Dump(dependencies,
                    dep => dep.GetType().Name + " " + ConceptInfoHelper.GetKeyProperties(((dynamic)dep).DependsOn));
                Assert.AreEqual(test.Value, actual, "Input: " + test.Key);
            }
        }

        [TestMethod]
        public void GenerateDependenciesTestToObject()
        {
            var dependent = new SimpleConceptInfo("dd");
            dependent.GetKeyProperties();
            var existingConcepts = new DslModelMock {
                new EntityInfo { Module = new ModuleInfo { Name = "a" }, Name = "b" },
                new DataStructureInfo { Module = new ModuleInfo { Name = "c" }, Name = "d" },
                new SqlViewInfo { Module = new ModuleInfo { Name = "e" }, Name = "f" },
                new SqlFunctionInfo { Module = new ModuleInfo { Name = "g" }, Name = "h" },
                new EntityInfo { Module = new ModuleInfo { Name = "x" }, Name = "y" },
                new SqlViewInfo { Module = new ModuleInfo { Name = "x" }, Name = "y" },
                new SqlFunctionInfo { Module = new ModuleInfo { Name = "x" }, Name = "y" },};

            var tests = new Dictionary<string, string>
            {
                { "a.a", "" },
                { "b.b", "" },
                { "a.b.a", "" },
                { "a.a.b", "" },
                { "a", "" },
                { "b", "" },

                { "a.b", "SqlDependsOnDataStructureInfo a.b" },
                { "c.d", "SqlDependsOnDataStructureInfo c.d" },
                { "e.f", "SqlDependsOnSqlViewInfo e.f" },
                { "g.h", "" },
                { "g.h(GETDATE())", "SqlDependsOnSqlFunctionInfo g.h" },

                { "x.y(GETDATE())", "SqlDependsOnSqlFunctionInfo x.y" },
                { "x.y", "SqlDependsOnDataStructureInfo x.y, SqlDependsOnSqlViewInfo x.y" },
            };

            foreach (var test in tests)
            {
                Console.WriteLine("Test: " + test.Key);
                var dependencies = SqlAnalysis.GenerateDependenciesToObject(dependent, existingConcepts, test.Key);

                foreach (dynamic dependency in dependencies)
                    Assert.AreEqual("dd", dependency.Dependent.ToString());

                var actual = TestUtility.Dump(dependencies,
                    dep => dep.GetType().Name + " " + ConceptInfoHelper.GetKeyProperties(((dynamic)dep).DependsOn));
                Assert.AreEqual(test.Value, actual, "Input: " + test.Key);
            }
        }
    }
}
