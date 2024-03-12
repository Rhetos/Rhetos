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
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Rhetos.TestCommon
{
    public static class TestUtility
    {
        public static Exception ShouldFail(Action action, params string[] expectedErrorContent)
        {
            return ShouldFail<Exception>(action, expectedErrorContent);
        }

        public static TExpectedException ShouldFail<TExpectedException>(Action action, params string[] expectedErrorContent)
            where TExpectedException : Exception
        {
            Exception exception = null;
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            Assert.IsNotNull(exception, "Expected exception did not happen.");

            string message = exception.GetType().Name + ": " + ExceptionsUtility.MessageForLog(exception);
            if (exception is UserException userException && userException.SystemMessage != null)
                message += "\r\n  SystemMessage: " + userException.SystemMessage;
            Console.WriteLine("[ShouldFail] " + message);

            if (!(exception is TExpectedException))
                Assert.Fail(string.Format("Unexpected exception type: {0} instead of a {1}.",
                    exception.GetType().Name,
                    typeof(TExpectedException).Name));

            AssertContains(message, expectedErrorContent, "Exception message is incorrect: " + message, exception.ToString());

            return (TExpectedException)exception;
        }

        /// <summary>
        /// Case-insensitive comparison.
        /// </summary>
        public static void AssertContains(string text, IEnumerable<string> patterns, string message = null, string errorContext = null)
        {
            var patternsCollection = CsUtility.Materialized(patterns);
            if (patternsCollection == null || patternsCollection.Count == 0)
                return;

            if (patternsCollection.Any(string.IsNullOrEmpty))
                throw new ArgumentException("Given list of patterns contains an empty string.");

            Console.WriteLine("[AssertContains] Actual text: '" + text + "'.");

            foreach (var pattern in patternsCollection)
            {
                Console.Write("[AssertContains] Looking for pattern '" + pattern + "'.");

                if (text.Contains(pattern, StringComparison.InvariantCultureIgnoreCase))
                    Console.WriteLine(" Found.");
                else
                {
                    Console.WriteLine(" Not found.");
                    Console.WriteLine(errorContext);
                    Assert.Fail("Text should contain pattern '" + pattern + "'."
                        + (string.IsNullOrEmpty(message) ? " " + message : "")
                        + " " + "The text is '" + text.Limit(2000, true) + "'.");
                }
            }
        }

        /// <summary>
        /// Case-insensitive comparison.
        /// </summary>
        public static void AssertContains(string text, string pattern, string message = null)
        {
            AssertContains(text, new[] { pattern }, message);
        }

        /// <summary>
        /// Case-insensitive comparison.
        /// </summary>
        public static void AssertNotContains(string text, string[] patterns, string message = null)
        {
            if (patterns == null || patterns.Length == 0)
                return;

            Console.WriteLine("[AssertNotContains] Actual text: '" + text + "'.");

            foreach (var pattern in patterns)
            {
                Console.Write("[AssertNotContains] Looking for pattern '" + pattern + "'.");

                if (text.Contains(pattern, StringComparison.CurrentCultureIgnoreCase))
                {
                    Console.WriteLine(" Found.");
                    Assert.Fail("Text should not contain pattern '" + pattern + "'. " + message ?? "");
                }
                else
                    Console.WriteLine(" Not found.");
            }
        }

        /// <summary>
        /// Case-insensitive comparison.
        /// </summary>
        public static void AssertNotContains(string text, string pattern, string message = null)
        {
            Console.WriteLine("[AssertNotContains] Not expecting pattern '" + pattern + "' in text '" + text + "'.");

            if (text.Contains(pattern, StringComparison.CurrentCultureIgnoreCase))
                Assert.Fail("Text should not contain pattern '" + pattern + "'. " + message ?? "");
        }

        /// <summary>
        /// Useful for comparing long texts. Reports first line that doesn't match.
        /// </summary>
        public static void AssertAreEqualByLine(string expected, string actual, string message = null)
        {
            var expectedLines = expected.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            var actualLines = actual.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            for (int line = 0; line < Math.Min(expectedLines.Length, actualLines.Length); line++)
                if (!string.Equals(expectedLines[line], actualLines[line], StringComparison.Ordinal))
                    Assert.Fail($"Line {line + 1} is different.\r\nExpected: <{expectedLines[line]}>.\r\nActual:   <{actualLines[line]}>.{(message != null ? "\r\n" + message : "")}");

            if (actualLines.Length != expectedLines.Length)
                Assert.Fail("Given text has " + actualLines.Length + " lines instead of " + expectedLines.Length + ".");
        }

        public static string DumpSorted<T>(IEnumerable<T> list, Func<T, object> selector = null)
        {
            if (selector == null)
                selector = item => item;
            var result = string.Join(", ", list.Select(element => selector(element)?.ToString() ?? "<null>").OrderBy(text => text));
            Console.WriteLine("[DumpSorted] " + result);
            return result;
        }

        public static string DumpSorted<T, T2>(IQueryable<T> query, Expression<Func<T, T2>> selector = null)
        {
            if (selector != null)
                return DumpSorted(query.Select(selector).ToList(), null);
            else
                return DumpSorted(query.ToList(), null);
        }

        public static string Dump<T>(IEnumerable<T> list, Func<T, object> selector = null)
        {
            if (selector == null)
                selector = item => item;
            var result = string.Join(", ", list.Select(element => selector(element).ToString()));
            Console.WriteLine("[Dump] " + result);
            return result;
        }

        public static string Dump<T, T2>(IQueryable<T> query, Expression<Func<T, T2>> selector = null)
        {
            if (selector != null)
                return Dump(query.Select(selector).ToList(), null);
            else
                return Dump(query.ToList(), null);
            
        }

        /// <summary>
        /// Unit test will be marked as "Inconclusive" if this function fails.
        /// </summary>
        public static void CheckDatabaseAvailability(IUnitOfWorkScope scope, string expectedLanguage = null)
        {
            try
            {
                var connectionString = scope.Resolve<ConnectionString>();
                if (string.IsNullOrEmpty(connectionString.ToString()))
                    throw new ArgumentException("Connection string is empty.");
            }
            catch (Exception ex)
            {
                Assert.Inconclusive($"A live database is needed for this unit test to run. Configure database connection string begin running the tests. {ex.GetType().Name}: {ex.Message}");
            }

            var databaseSettings = scope.Resolve<DatabaseSettings>();
            if (expectedLanguage != null && databaseSettings.DatabaseLanguage != expectedLanguage)
                Assert.Inconclusive($"This test will run only on '{expectedLanguage}' database language, not '{databaseSettings.DatabaseLanguage}'." +
                    $" Configure database language and connection string, then rebuild this project.");
        }

        /// <summary>
        /// Shortens sequences of repeating characters, if longer than 3.
        /// Example: "aaaaaaaaaaXXXbbbb" => "aaa...(10)XXXbbb...(5)"
        /// </summary>
        public static string CompressReport(string text)
        {
            const int showMaxDuplicates = 3;

            var sb = new StringBuilder();
            char lastc = '\0';
            int duplicates = 0;
            foreach (char c in text)
            {
                if (c == lastc)
                    duplicates++;
                else
                {
                    if (duplicates >= showMaxDuplicates)
                        sb.Append('(').Append(duplicates + 1).Append(')');
                    duplicates = 0;
                }

                if (duplicates < showMaxDuplicates)
                    sb.Append(c);
                else if (duplicates == showMaxDuplicates)
                    sb.Append("...");

                lastc = c;
            }
            if (duplicates >= showMaxDuplicates)
                sb.Append('(').Append(duplicates + 1).Append(')');
            return sb.ToString();
        }
    }
}
