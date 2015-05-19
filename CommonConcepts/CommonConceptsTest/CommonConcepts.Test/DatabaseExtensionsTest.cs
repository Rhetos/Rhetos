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
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Configuration.Autofac;
using Rhetos.Utilities;
using System.Linq.Expressions;

namespace CommonConcepts.Test
{
    [TestClass]
    public class DatabaseExtensionsTest
    {
        private void TestString(
            IEnumerable<string> testData,
            string testParameter,
            string expectedResult,
            Func<string, Expression<Func<TestDatabaseExtensions.Simple, bool>>> filterExpressionForTest)
        {
            TestStrings(testData, new[] { Tuple.Create<string, string>(testParameter, expectedResult) }, filterExpressionForTest);
        }

        private void TestStrings(
            IEnumerable<string> testData,
            IDictionary<string, string> testQueries,
            Func<string, Expression<Func<TestDatabaseExtensions.Simple, bool>>> filterExpressionForTest)
        {
            TestStrings(testData, testQueries.Select(test => Tuple.Create(test.Key, test.Value)).ToList(), filterExpressionForTest);
        }

        private void TestStrings(
            IEnumerable<string> testData,
            IEnumerable<Tuple<string, string>> testQueries,
            Func<string, Expression<Func<TestDatabaseExtensions.Simple, bool>>> filterExpressionForTest)
        {
            // Test SQL:

            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(
                    new[] { "DELETE FROM TestDatabaseExtensions.Simple" }
                    .Concat(testData.Select(name => "INSERT INTO TestDatabaseExtensions.Simple (Name) SELECT " + SqlUtility.QuoteText(name))));

                var repository = container.Resolve<Common.DomRepository>();

                foreach (var test in testQueries)
                {
                    var filterExpression = filterExpressionForTest(test.Item1);
                    string info = "SQL filter '" + filterExpression.ToString() + "' for input '" + test.Item1 + "'";
                    Console.WriteLine(info);
                    var filtered = repository.TestDatabaseExtensions.Simple.Query().Where(filterExpression).ToList();
                    Assert.AreEqual(test.Item2, TestUtility.DumpSorted(filtered, item => item.Name ?? "<null>"), info);
                }
            }

            // Test C#:

            {
                var items = testData.Select(name => new TestDatabaseExtensions.Simple { Name = name }).ToList();

                foreach (var test in testQueries)
                {
                    var filterExpression = filterExpressionForTest(test.Item1);
                    var filterFunction = filterExpression.Compile();
                    string info = "C# filter '" + filterExpression.ToString() + "' for input '" + test.Item1 + "'";
                    Console.WriteLine(info);
                    var filtered = items.Where(filterFunction).ToList();
                    Assert.AreEqual(test.Item2, TestUtility.DumpSorted(filtered, item => item.Name ?? "<null>"), info);
                }
            }
        }

        private void TestIntegers(
            IEnumerable<int?> testData,
            IEnumerable<Tuple<string, string>> testQueries,
            Func<string, Expression<Func<TestDatabaseExtensions.Simple, bool>>> filterExpressionForTest)
        {
            // Test SQL:

            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(
                    new[] { "DELETE FROM TestDatabaseExtensions.Simple" }
                    .Concat(testData.Select(code => "INSERT INTO TestDatabaseExtensions.Simple (Code) SELECT "
                        + (code != null ? code.ToString() : "NULL"))));

                var repository = container.Resolve<Common.DomRepository>();

                foreach (var test in testQueries)
                {
                    var filterExpression = filterExpressionForTest(test.Item1);
                    string info = "SQL filter '" + filterExpression.ToString() + "' for input '" + test.Item1 + "'";
                    Console.WriteLine(info);
                    var filtered = repository.TestDatabaseExtensions.Simple.Query().Where(filterExpression).ToList();
                    Assert.AreEqual(test.Item2, TestUtility.DumpSorted(filtered, item => item.Code != null ? item.Code.ToString() : "<null>"), info);
                }
            }

            // Test C#:

            {
                var items = testData.Select(code => new TestDatabaseExtensions.Simple { Code = code }).ToList();

                foreach (var test in testQueries)
                {
                    var filterExpression = filterExpressionForTest(test.Item1);
                    var filterFunction = filterExpression.Compile();
                    string info = "C# filter '" + filterExpression.ToString() + "' for input '" + test.Item1 + "'";
                    Console.WriteLine(info);
                    var filtered = items.Where(filterFunction).ToList();
                    Assert.AreEqual(test.Item2, TestUtility.DumpSorted(filtered, item => item.Code != null ? item.Code.ToString() : "<null>"), info);
                }
            }
        }

        [TestMethod]
        public void EqualsOperatorDontTestCase()
        {
            var testData = new[] { "a", "b", null };

            var testQueries = new List<Tuple<string, string>>
            {
                Tuple.Create("a", "a"),
                Tuple.Create<string, string>(null, "<null>"),
            };

            TestStrings(testData, testQueries, parameter => item => item.Name == parameter);

            // Testing for null LITERAL, instead of null VARIABLE.
            // The variable is translated to SQL similar to "WHERE Name = @p OR (Name IS NULL AND @p IS NULL)".
            // The literal is translated to SQL "WHERE Name IS NULL".

            TestString(testData, null, "<null>", parameter => item => item.Name == null);
        }

        [TestMethod]
        public void EqualsCaseInsensitive()
        {
            var testData = new[] { "a", "A", "b", null };

            var testQueries = new List<Tuple<string, string>>
            {
                Tuple.Create("a", "a, A"),
                Tuple.Create("A", "a, A"),
                Tuple.Create<string, string>(null, "<null>"),
            };

            TestStrings(testData, testQueries, parameter => item => item.Name.EqualsCaseInsensitive(parameter));

            // Testing for null LITERAL, instead of null VARIABLE.
            // The variable is translated to SQL similar to "WHERE Name = @p OR (Name IS NULL AND @p IS NULL)".
            // The literal is translated to SQL "WHERE Name IS NULL".

            TestString(testData, null, "<null>", parameter => item => item.Name.EqualsCaseInsensitive(null));
        }

        [TestMethod]
        public void NotEqualsOperatorDontTestCase()
        {
            var testData = new[] { "a", "b", null };

            var testQueries = new List<Tuple<string, string>>
            {
                Tuple.Create("a", "<null>, b"),
                Tuple.Create<string, string>(null, "a, b"),
            };

            TestStrings(testData, testQueries, parameter => item => item.Name != parameter);

            // Testing for null LITERAL, instead of null VARIABLE.
            // The variable is translated to SQL similar to "WHERE Name <> @p OR (Name IS NULL AND @p IS NOT NULL) OR ...".
            // The literal is translated to SQL "WHERE Name IS NOT NULL".

            TestString(testData, null, "a, b", parameter => item => item.Name != null);
        }

        [TestMethod]
        public void NotEqualsCaseInsensitive()
        {
            var testData = new[] { "a", "A", "b", null };

            var testQueries = new List<Tuple<string, string>>
            {
                Tuple.Create("a", "<null>, b"),
                Tuple.Create("A", "<null>, b"),
                Tuple.Create("B", "<null>, a, A"),
                Tuple.Create<string, string>(null, "a, A, b"),
            };

            TestStrings(testData, testQueries, parameter => item => item.Name.NotEqualsCaseInsensitive(parameter));

            // Null value should be provided as a LITERAL, not as a VARIABLE.
            // The variable is translated to SQL similar to "WHERE Name <> @p OR (Name IS NULL AND @p IS NOT NULL) OR ...".
            // The literal is translated to SQL "WHERE Name IS NOT NULL".

            TestString(testData, null, "a, A, b", parameter => item => item.Name.NotEqualsCaseInsensitive(null));
        }

        [TestMethod]
        public void IsLessThen()
        {
            var testData = new[] { "a", "A", "b", "B", null };

            var testQueries = new List<Tuple<string, string>>
            {
                Tuple.Create("a", ""),
                Tuple.Create("A", ""),
                Tuple.Create("b", "a, A"),
                Tuple.Create("B", "a, A"),
                Tuple.Create<string, string>(null, ""),
            };

            TestStrings(testData, testQueries, parameter => item => item.Name.IsLessThen(parameter));
        }

        [TestMethod]
        public void IsLessThenOrEqual()
        {
            var testData = new[] { "a", "A", "b", "B", null };

            var testQueries = new List<Tuple<string, string>>
            {
                Tuple.Create("a", "a, A"),
                Tuple.Create("A", "a, A"),
                Tuple.Create("b", "a, A, b, B"),
                Tuple.Create("B", "a, A, b, B"),
                Tuple.Create<string, string>(null, ""),
            };

            TestStrings(testData, testQueries, parameter => item => item.Name.IsLessThenOrEqual(parameter));
        }

        [TestMethod]
        public void IsGreaterThen()
        {
            var testData = new[] { "a", "A", "b", "B", null };

            var testQueries = new List<Tuple<string, string>>
            {
                Tuple.Create("a", "b, B"),
                Tuple.Create("A", "b, B"),
                Tuple.Create("b", ""),
                Tuple.Create("B", ""),
                Tuple.Create<string, string>(null, ""),
            };

            TestStrings(testData, testQueries, parameter => item => item.Name.IsGreaterThen(parameter));
        }

        [TestMethod]
        public void IsGreaterThenOrEqual()
        {
            var testData = new[] { "a", "A", "b", "B", null };

            var testQueries = new List<Tuple<string, string>>
            {
                Tuple.Create("a", "a, A, b, B"),
                Tuple.Create("A", "a, A, b, B"),
                Tuple.Create("b", "b, B"),
                Tuple.Create("B", "b, B"),
                Tuple.Create<string, string>(null, ""),
            };

            TestStrings(testData, testQueries, parameter => item => item.Name.IsGreaterThenOrEqual(parameter));
        }

        [TestMethod]
        public void IntStartsWith()
        {
            var testData = new int?[] { 1, 123, 2, null };

            var testQueries = new List<Tuple<string, string>>
            {
                Tuple.Create("1", "1, 123"),
                Tuple.Create("123", "123"),
                Tuple.Create("2", "2"),
                Tuple.Create("", "1, 123, 2"), // Different results for C# method and Entity SQL function 'StartsWith".
                Tuple.Create<string, string>(null, ""),
            };

            TestIntegers(testData, testQueries, parameter => item => item.Code.StartsWith(parameter));
        }

        [TestMethod]
        public void StringStartsWithCaseInsensitive()
        {
            var testData = new [] { "a1", "A2", "b", null };

            var testQueries = new List<Tuple<string, string>>
            {
                Tuple.Create("a", "a1, A2"),
                Tuple.Create("b", "b"),
                Tuple.Create("", "a1, A2, b"),
                Tuple.Create<string, string>(null, ""),
            };

            TestStrings(testData, testQueries, parameter => item => item.Name.StartsWithCaseInsensitive(parameter));
        }

        [TestMethod]
        public void ContainsCaseInsensitive()
        {
            var testData = new[] { "ab", "BA", "c", null };

            var testQueries = new List<Tuple<string, string>>
            {
                Tuple.Create("a", "ab, BA"),
                Tuple.Create("c", "c"),
                Tuple.Create("", "ab, BA, c"),
                Tuple.Create<string, string>(null, ""),
            };

            TestStrings(testData, testQueries, parameter => item => item.Name.ContainsCaseInsensitive(parameter));
        }

        [TestMethod]
        public void Like()
        {
            var testData = new[] { @"abc1", @"bbb2", @".*(\" };

            var testQueries = new Dictionary<string, string>
            {
                { "%b%", "abc1, bbb2" },
                { "b%", "bbb2" },
                { "%1", "abc1" },
                { "%", @".*(\, abc1, bbb2" },
                { "%3%", "" },
                { "", "" },
                { "bbb2", "bbb2" },
                { "abc%1", "abc1" },
                { "%abc1%", "abc1" },
                { "%ABC1%", "abc1" },
                { "ABC1", "abc1" },
                { "abc_", "abc1" },
                { "abc__", "" },
                { "%.%", @".*(\" },
                { "%*%", @".*(\" },
                { "%(%", @".*(\" },
                { @"%\%", @".*(\" },
            };

            TestStrings(testData, testQueries, parameter =>
                item => item.Name.Like(parameter));
        }

        [TestMethod]
        public void IntCastToString()
        {
            var testData = new int?[] { null, 0, 1, 123, -123456789 };

            // Test SQL:

            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(
                    new[] { "DELETE FROM TestDatabaseExtensions.Simple" }
                    .Concat(testData.Select(code => "INSERT INTO TestDatabaseExtensions.Simple (Code) SELECT "
                        + (code != null ? code.ToString() : "NULL"))));

                var repository = container.Resolve<Common.DomRepository>();

                var loaded = repository.TestDatabaseExtensions.Simple.Query().Select(item => item.Code.CastToString()).ToList();
                Assert.AreEqual("<null>, 0, 1, 123, -123456789", TestUtility.DumpSorted(loaded, str => str ?? "<null>"));

                var filtered = repository.TestDatabaseExtensions.Simple.Query()
                    .Where(item => item.Code.CastToString().StartsWith("1"))
                    .Select(item => item.Code).ToList();
                Assert.AreEqual("1, 123", TestUtility.DumpSorted(filtered));
            }

            // Test C#:

            {
                var items = testData.Select(code => new TestDatabaseExtensions.Simple { Code = code }).ToList();

                var loaded = items.Select(item => item.Code.CastToString()).ToList();
                Assert.AreEqual("<null>, 0, 1, 123, -123456789", TestUtility.DumpSorted(loaded, str => str ?? "<null>"));
            }
        }
    }
}
