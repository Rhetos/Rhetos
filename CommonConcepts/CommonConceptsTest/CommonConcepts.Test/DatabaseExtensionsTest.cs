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

using Autofac;
using System;
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
        private void TestStrings(
            IEnumerable<string> data,
            Func<string, Expression<Func<TestDatabaseExtensions.Simple, bool>>> filter,
            params (string FilterParameter, string ExpectedResult)[] tests)
        {
            Assert.AreNotEqual(0, tests.Length);

            var report = new List<string>();
            var expectedReport = new List<string>();

            // Test SQL:

            foreach (bool useDatabaseNullSemantics in new[] { false, true })
                using (var container = new RhetosTestContainer())
                {
                    container.SetUseDatabaseNullSemantics(useDatabaseNullSemantics);

                    container.Resolve<ISqlExecuter>().ExecuteSql(
                        new[] { "DELETE FROM TestDatabaseExtensions.Simple" }
                        .Concat(data.Select(name => "INSERT INTO TestDatabaseExtensions.Simple (Name) SELECT " + SqlUtility.QuoteText(name))));

                    var repository = container.Resolve<Common.DomRepository>();

                    foreach (var test in tests)
                    {
                        var expectedOptions = test.ExpectedResult.Split('/');
                        string expectedOption = (!useDatabaseNullSemantics || expectedOptions.Length == 1)
                            ? expectedOptions[0]
                            : expectedOptions[1];

                        var filterExpression = filter(test.FilterParameter);
                        var filtered = repository.TestDatabaseExtensions.Simple.Query().Where(filterExpression).ToList();

                        string reportFormat = $"SQL{(useDatabaseNullSemantics ? " DBNULL" : "")}: {test.FilterParameter ?? "null"} => {{0}}";
                        Console.WriteLine(string.Format(reportFormat, filterExpression));
                        expectedReport.Add(string.Format(reportFormat, expectedOption));
                        report.Add(string.Format(reportFormat, TestUtility.DumpSorted(filtered, item => item.Name ?? "null")));
                    }
                }

            // Test C#:

            {
                var items = data.Select(name => new TestDatabaseExtensions.Simple { Name = name }).ToList();

                foreach (var test in tests)
                {
                    string expectedOption = test.ExpectedResult.Split('/')[0]; // C# result is expected to be same as SQL for useDatabaseNullSemantics=false.

                    var filterExpression = filter(test.FilterParameter);
                    var filterFunction = filterExpression.Compile();
                    var filtered = items.Where(filterFunction).ToList();

                    string reportFormat = $"C#: {test.FilterParameter ?? "null"} => {{0}}";
                    Console.WriteLine(string.Format(reportFormat, filterExpression));
                    expectedReport.Add(string.Format(reportFormat, expectedOption));
                    report.Add(string.Format(reportFormat, TestUtility.DumpSorted(filtered, item => item.Name ?? "null")));
                }
            }

            Assert.AreEqual(string.Join("\r\n", expectedReport), string.Join("\r\n", report));
        }

        private void TestIntegers(
            IEnumerable<int?> data,
            Func<string, Expression<Func<TestDatabaseExtensions.Simple, bool>>> filter,
            params (string FilterParameter, string ExpectedResult)[] tests)
        {
            Assert.AreNotEqual(0, tests.Length);

            // Test SQL:

            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(
                    new[] { "DELETE FROM TestDatabaseExtensions.Simple" }
                    .Concat(data.Select(code => "INSERT INTO TestDatabaseExtensions.Simple (Code) SELECT "
                        + (code != null ? code.ToString() : "NULL"))));

                var repository = container.Resolve<Common.DomRepository>();

                foreach (var test in tests)
                {
                    var filterExpression = filter(test.FilterParameter);
                    string info = "SQL filter '" + filterExpression.ToString() + "' for input '" + test.FilterParameter + "'";
                    Console.WriteLine(info);
                    var filtered = repository.TestDatabaseExtensions.Simple.Query().Where(filterExpression).ToList();
                    Assert.AreEqual(test.ExpectedResult, TestUtility.DumpSorted(filtered, item => item.Code != null ? item.Code.ToString() : "null"), info);
                }
            }

            // Test C#:

            {
                var items = data.Select(code => new TestDatabaseExtensions.Simple { Code = code }).ToList();

                foreach (var test in tests)
                {
                    var filterExpression = filter(test.FilterParameter);
                    var filterFunction = filterExpression.Compile();
                    string info = "C# filter '" + filterExpression.ToString() + "' for input '" + test.FilterParameter + "'";
                    Console.WriteLine(info);
                    var filtered = items.Where(filterFunction).ToList();
                    Assert.AreEqual(test.ExpectedResult, TestUtility.DumpSorted(filtered, item => item.Code != null ? item.Code.ToString() : "null"), info);
                }
            }
        }

        [TestMethod]
        public void EqualsOperator()
        {
            // We are not testing case-sensitivity here. Equals operator is CS in C# and CI in SQL by default.
            var testData = new[] { "a", "b", null };

            TestStrings(testData, parameter => item => item.Name == parameter,
                ("a", "a"),
                (null, "null/")); // For UseDatabaseNullSemantics=true this will generate SQL query "Name = @ParameterNull" and will not return the null values.

            // Testing for null LITERAL, instead of null VARIABLE.
            // The variable is translated to SQL similar to "WHERE Name = @p OR (Name IS NULL AND @p IS NULL)".
            // The literal is translated to SQL "WHERE Name IS NULL".

            TestStrings(testData, parameter => item => item.Name == null,
                (null, "null"));
        }

        [TestMethod]
        public void EqualsCaseInsensitive()
        {
            var testData = new[] { "a", "A", "b", null };

            TestStrings(testData, parameter => item => item.Name.EqualsCaseInsensitive(parameter),
                ("a", "a, A"),
                ("A", "a, A"),
                (null, "null/")); // Same as EqualsOperator(), for UseDatabaseNullSemantics=true, searching by parameter set to null value will not return the null values.

            // Testing for null LITERAL, instead of null VARIABLE.
            // The variable is translated to SQL similar to "WHERE Name = @p OR (Name IS NULL AND @p IS NULL)".
            // The literal is translated to SQL "WHERE Name IS NULL".

            TestStrings(testData, parameter => item => item.Name.EqualsCaseInsensitive(null),
                (null, "null/")); // Different from EqualsOperator(): for UseDatabaseNullSemantics=true, even with constant value, EqualsCaseInsensitive will generate SQL '= null' and not return the null values.
        }

        [TestMethod]
        public void NotEqualsOperator()
        {
            // We are not testing case-sensitivity here. Equals operator is CS in C# and CI in SQL by default.
            var testData = new[] { "a", "b", null };

            TestStrings(testData, parameter => item => item.Name != parameter,
                ("a", "b, null/b"), // For UseDatabaseNullSemantics=true this will generate SQL query "Name <> @ParameterA" and will not return the null values.
                (null, "a, b/")); // For UseDatabaseNullSemantics=true this will generate SQL query "Name <> @ParameterNull" and will return no records.

            // Testing for null LITERAL, instead of null VARIABLE.
            // The variable is translated to SQL similar to "WHERE Name <> @p OR (Name IS NULL AND @p IS NOT NULL) OR ...".
            // The literal is translated to SQL "WHERE Name IS NOT NULL".

            TestStrings(testData, parameter => item => item.Name != null,
                (null, "a, b"));
        }

        [TestMethod]
        public void NotEqualsCaseInsensitive()
        {
            var testData = new[] { "a", "A", "b", null };

            TestStrings(testData, parameter => item => item.Name.NotEqualsCaseInsensitive(parameter),
                ("a", "b, null/b"), // Same as EqualsOperator(), for UseDatabaseNullSemantics=true this will generate SQL query "Name <> @ParameterA" and will not return the null values.
                ("A", "b, null/b"),
                ("B", "a, A, null/a, A"),
                (null, "a, A, b/")); // Same as EqualsOperator(), for UseDatabaseNullSemantics=true this will generate SQL query "Name <> @ParameterNull" and will return no records.

            // Null value should be provided as a LITERAL, not as a VARIABLE.
            // The variable is translated to SQL similar to "WHERE Name <> @p OR (Name IS NULL AND @p IS NOT NULL) OR ...".
            // The literal is translated to SQL "WHERE Name IS NOT NULL".

            TestStrings(testData, parameter => item => item.Name.NotEqualsCaseInsensitive(null),
                (null, "a, A, b/")); // Different from EqualsOperator(): for UseDatabaseNullSemantics=true, even with constant value, EqualsCaseInsensitive will generate SQL '<> null' and not return the null values.
        }

        [TestMethod]
        public void IsLessThen()
        {
            var testData = new[] { "a", "A", "b", "B", null };

            TestStrings(testData, parameter => item => item.Name.IsLessThen(parameter),
                ("a", ""),
                ("A", ""),
                ("b", "a, A"),
                ("B", "a, A"),
                (null, ""));
        }

        [TestMethod]
        public void IsLessThenOrEqual()
        {
            var testData = new[] { "a", "A", "b", "B", null };

            TestStrings(testData, parameter => item => item.Name.IsLessThenOrEqual(parameter),
                ("a", "a, A"),
                ("A", "a, A"),
                ("b", "a, A, b, B"),
                ("B", "a, A, b, B"),
                (null, ""));
        }

        [TestMethod]
        public void IsGreaterThen()
        {
            var testData = new[] { "a", "A", "b", "B", null };

            TestStrings(testData, parameter => item => item.Name.IsGreaterThen(parameter),
                ("a", "b, B"),
                ("A", "b, B"),
                ("b", ""),
                ("B", ""),
                (null, ""));
        }

        [TestMethod]
        public void IsGreaterThenOrEqual()
        {
            var testData = new[] { "a", "A", "b", "B", null };

            TestStrings(testData, parameter => item => item.Name.IsGreaterThenOrEqual(parameter),
                ("a", "a, A, b, B"),
                ("A", "a, A, b, B"),
                ("b", "b, B"),
                ("B", "b, B"),
                (null, ""));
        }

        [TestMethod]
        public void IntStartsWith()
        {
            var testData = new int?[] { 1, 123, 2, null };

            TestIntegers(testData, parameter => item => item.Code.StartsWith(parameter),
                ("1", "1, 123"),
                ("123", "123"),
                ("2", "2"),
                ("", "1, 123, 2"), // Different results for C# method and Entity SQL function 'StartsWith".
                (null, ""));
        }

        [TestMethod]
        public void StringStartsWithCaseInsensitive()
        {
            var testData = new [] { "a1", "A2", "b", null };

            TestStrings(testData, parameter => item => item.Name.StartsWithCaseInsensitive(parameter),
                ("a", "a1, A2"),
                ("b", "b"),
                ("", "a1, A2, b"),
                (null, ""));
        }

        [TestMethod]
        public void ContainsCaseInsensitive()
        {
            var testData = new[] { "ab", "BA", "c", null };

            TestStrings(testData, parameter => item => item.Name.ContainsCaseInsensitive(parameter),
                ("a", "ab, BA"),
                ("c", "c"),
                ("", "ab, BA, c"),
                (null, ""));
        }

        [TestMethod]
        public void Like()
        {
            var testData = new[] { @"abc1", @"bbb2", @".*(\" };

            TestStrings(testData, parameter => item => item.Name.Like(parameter),
                ("%b%", "abc1, bbb2" ),
                ("b%", "bbb2"),
                ("%1", "abc1"),
                ("%", @".*(\, abc1, bbb2"),
                ("%3%", ""),
                ("", ""),
                ("bbb2", "bbb2"),
                ("abc%1", "abc1"),
                ("%abc1%", "abc1"),
                ("%ABC1%", "abc1"),
                ("ABC1", "abc1"),
                ("abc_", "abc1"),
                ("abc__", ""),
                ("%.%", @".*(\"),
                ("%*%", @".*(\"),
                ("%(%", @".*(\"),
                (@"%\%", @".*(\" ));
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
                Assert.AreEqual("0, 1, 123, -123456789, null", TestUtility.DumpSorted(loaded, str => str ?? "null"));

                var filtered = repository.TestDatabaseExtensions.Simple.Query()
                    .Where(item => item.Code.CastToString().StartsWith("1"))
                    .Select(item => item.Code).ToList();
                Assert.AreEqual("1, 123", TestUtility.DumpSorted(filtered));
            }

            // Test C#:

            {
                var items = testData.Select(code => new TestDatabaseExtensions.Simple { Code = code }).ToList();

                var loaded = items.Select(item => item.Code.CastToString()).ToList();
                Assert.AreEqual("0, 1, 123, -123456789, null", TestUtility.DumpSorted(loaded, str => str ?? "null"));
            }
        }
    }
}
