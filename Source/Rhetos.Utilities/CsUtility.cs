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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Rhetos.Utilities
{
    public static class CsUtility
    {
        /// <summary>
        /// Generates a C# string constant.
        /// </summary>
        public static string QuotedString(string text)
        {
            if (text == null)
                return "null";
            return "@\"" + text.Replace("\"", "\"\"") + "\"";
        }

        /// <summary>
        /// Changes special characters in text to alphanumeric characters and '_'.
        /// Different texts will always produce different results.
        /// </summary>
        public static string TextToIdentifier(string text)
        {
            var result = new StringBuilder(200);

            var charArray = text.ToCharArray();
            foreach (char c in charArray)
                if (char.IsLetterOrDigit(c))
                    result.Append(c);
                else if (c == '_')
                    result.Append("__");
                else
                {
                    result.Append('_');
                    result.Append(((int)c).ToString("X"));
                    result.Append('_');
                }

            return result.ToString();
        }

        /// <summary>
        /// Reads a value from the dictionary, with extended error handling.
        /// Parameter exceptionMessage can contain format tag {0} that will be replaced by missing key.
        /// </summary>
        public static TValue GetValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<string> exceptionMessage)
        {
            TValue value;
            if (!dictionary.TryGetValue(key, out value))
                throw new FrameworkException(string.Format(exceptionMessage(), key));
            return value;
        }

        /// <summary>
        /// Reads a value from the dictionary, with extended error handling.
        /// Parameter exceptionMessage can contain format tag {0} that will be replaced by missing key.
        /// </summary>
        public static TValue GetValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, string exceptionMessage)
        {
            return GetValue(dictionary, key, () => exceptionMessage);
        }

        /// <summary>
        /// Reads a value from the dictionary or returns default if the dictionary does not contain the key.
        /// This method helps when the TryGetValue() method cannot be called directly with anonymous value type.
        /// </summary>
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            TValue value;
            if (dictionary.TryGetValue(key, out value))
                return value;
            return default(TValue);
        }

        /// <summary>
        /// Reads a value from the dictionary or returns an empty List if the dictionary does not contain the key.
        /// </summary>
        public static List<TValue> GetValueOrEmpty<TKey, TValue>(this IDictionary<TKey, List<TValue>> dictionary, TKey key)
        {
            List<TValue> value;
            if (dictionary.TryGetValue(key, out value))
                return value;
            return new List<TValue>();
        }

        /// <summary>
        /// Returns null if the argument is a valid identifier, error message otherwise.
        /// </summary>
        public static string GetIdentifierError(string name)
        {
            if (name == null)
                return "Given name is null.";

            if (string.IsNullOrEmpty(name))
                return "Given name is empty.";

            {
                char c = name[0];
                if ((c < 'a' || c > 'z') && (c < 'A' || c > 'Z') && c != '_')
                    return "Given name '" + name + "' is not valid. First character is not an English letter or underscore.";
            }

            {
                foreach (char c in name)
                    if ((c < 'a' || c > 'z') && (c < 'A' || c > 'Z') && c != '_' && (c < '0' || c > '9'))
                        return "Given name '" + name + "' is not valid. Character '" + c + "' is not an English letter or number or underscore.";
            }

            return null;
        }

        /// <summary>
        /// Result does not include implemented interfaces, only base classes.
        /// Result includes the given type.
        /// </summary>
        public static List<Type> GetClassHierarchy(Type type)
        {
            var types = new List<Type>();
            while (type != typeof(object))
            {
                types.Add(type);
                type = type.BaseType;
            }
            types.Reverse();
            return types;
        }

        /// <summary>
        /// If <paramref name="items"/> is not a List or an Array, converts it to a List&lt;<typeparamref name="T"/>&gt;.
        /// If the parameter is null, it will remain null.
        /// Use this function to make sure that the <paramref name="items"/> is not a LINQ query
        /// before using it multiple times, in order to avoid the query evaluation every time
        /// (sometimes it means reading data from the database on every evaluation).
        /// </summary>
        public static void Materialize<T>(ref IEnumerable<T> items)
        {
            if (items != null && !(items is IList))
                items = items.ToList();
        }

        /// <summary>
        /// Use this method to sort strings respecting the number values in the string.
        /// Example: new[] { "a10", "a11", "a9", "b3-11", "b3-2" }.OrderBy(s => GetNaturalSortString(s))
        /// Returns: "a9", "a10", "a11", "b3-2", "b3-11"
        /// </summary>
        public static string GetNaturalSortString(string s)
        {
            var result = new StringBuilder();
            foreach (var match in _splitNumericGroups.Matches(s))
            {
                var part = match.ToString();
                if (char.IsDigit(part[0]))
                    result.Append(new string('0', Math.Max(10 - part.Length, 0)) + part);
                else
                    result.Append(part);
            }
            return result.ToString();
        }

        private static readonly Regex _splitNumericGroups = new Regex(@"(\d+|\D+)");

        /// <summary>
        /// Creates a detailed report message for the ReflectionTypeLoadException.
        /// Returns null if the exception cannot be interpreted as a type load exception.
        /// </summary>
        public static string ReportTypeLoadException(Exception ex, string errorContext = null, IEnumerable<string> referencedAssembliesPaths = null)
        {
            List<Exception> loaderExceptions;
            if (ex is ReflectionTypeLoadException)
                loaderExceptions = ((ReflectionTypeLoadException)ex).LoaderExceptions.GroupBy(exception => exception.Message).Select(group => group.First()).ToList();
            else if (ex is FileLoadException)
                loaderExceptions = new List<Exception> { ex };
            else
                return null;

            var report = new List<string>();
            report.Add(
                (string.IsNullOrEmpty(errorContext) ? errorContext + " " : "")
                + "Please check if the assembly is missing or has a different version.");
            report.AddRange(ReportLoaderExceptions(loaderExceptions));
            report.AddRange(ReportAssemblyLoadErrors(referencedAssembliesPaths));

            return string.Join("\r\n", report);
        }

        private static IEnumerable<string> ReportLoaderExceptions(List<Exception> distinctLoaderExceptions)
        {
            const int maxErrors = 5;

            bool fusionLogReported = false;
            foreach (var loaderException in distinctLoaderExceptions.Take(maxErrors))
            {
                yield return loaderException.GetType().Name + ": " + loaderException.Message;

                if (!fusionLogReported && loaderException is FileLoadException && !string.IsNullOrEmpty(((FileLoadException)loaderException).FusionLog))
                {
                    yield return ((FileLoadException)loaderException).FusionLog;
                    fusionLogReported = true;
                }

                if (!fusionLogReported && loaderException is FileNotFoundException && !string.IsNullOrEmpty(((FileNotFoundException)loaderException).FusionLog))
                {
                    yield return ((FileNotFoundException)loaderException).FusionLog;
                    fusionLogReported = true;
                }
            }

            if (distinctLoaderExceptions.Count > maxErrors)
                yield return "...";
        }

        private static IEnumerable<string> ReportAssemblyLoadErrors(IEnumerable<string> referencedAssembliesPaths)
        {
            var report = new List<string>();
            foreach (string assemblyPath in referencedAssembliesPaths)
            {
                try
                {
                    Assembly.LoadFrom(assemblyPath).GetTypes();
                }
                catch (Exception ex)
                {
                    Exception[] reportExceptions = (ex as ReflectionTypeLoadException)?.LoaderExceptions;
                    if (reportExceptions == null || !reportExceptions.Any())
                        reportExceptions = new[] { ex };

                    foreach (var exceptionInfo in reportExceptions.Select(re => re.GetType().Name + ": " + re.Message).Distinct().Take(5))
                        report.Add($"* '{Path.GetFileName(assemblyPath)}' throws {exceptionInfo}.");
                }
            }
            return report;
        }

        /// <summary>
        /// Returns a subset of the given strings that match the given prefixes.
        /// String comparison is ordinal, case insensitive.
        /// </summary>
        public static List<string> MatchPrefixes(List<string> strings, List<string> prefixes)
        {
            strings.Sort(StringComparer.OrdinalIgnoreCase);
            prefixes.Sort(StringComparer.OrdinalIgnoreCase);

            int strIndex = 0;
            int prefixIndex = 0;
            var matches = new List<string>();
            while (strIndex < strings.Count() && prefixIndex < prefixes.Count())
            {
                string str = strings[strIndex];
                string prefix = prefixes[prefixIndex];

                if (str.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add(str);
                    strIndex++;
                }
                else if (string.Compare(str, prefix, StringComparison.OrdinalIgnoreCase) < 0)
                    strIndex++;
                else
                    prefixIndex++;
            }

            return matches;
        }

        public static string ByteArrayToHex(byte[] ba)
        {
            if (ba == null)
                return null;
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:X2}", b);
            return hex.ToString();
        }

        public static byte[] HexToByteArray(string hex)
        {
            if (hex == null)
                return null;
            int NumberChars = hex.Length / 2;
            byte[] bytes = new byte[NumberChars];
            StringReader sr = new StringReader(hex);
            for (int i = 0; i < NumberChars; i++)
                bytes[i] = Convert.ToByte(new string(new char[2] { (char)sr.Read(), (char)sr.Read() }), 16);
            sr.Dispose();
            return bytes;
        }

        public class Group<TItem, TKey>
        {
            public TKey Key;
            public List<TItem> Items;
        }

        /// <summary>
        /// Groups items to batches by their group value, keeping the order of the items.
        /// This may result with two items with the same key ending in different groups, if there is an item with different key between them.
        /// </summary>
        public static List<Group<TItem, TKey>> GroupItemsKeepOrdering<TItem, TKey>(IEnumerable<TItem> items, Func<TItem, TKey> keySelector)
        {
            var batches = new List<Group<TItem, TKey>>();
            Group<TItem, TKey> currentBatch = null;

            foreach (var item in items)
            {
                TKey key = keySelector(item);
                if (currentBatch == null || !currentBatch.Key.Equals(key))
                {
                    currentBatch = new Group<TItem, TKey> { Key = key, Items = new List<TItem>() };
                    batches.Add(currentBatch);
                }

                currentBatch.Items.Add(item);
            }

            return batches;
        }

        public static string Limit(this string text, int maxLength, bool appendTotalLengthInfo = false)
        {
            if (text.Length > maxLength)
            {
                if (!appendTotalLengthInfo)
                    return text.Substring(0, maxLength);
                else
                    return text.Substring(0, maxLength) + "... (total length " + text.Length + ")";
            }
            else
                return text;
        }

        /// <param name="trimMark">The suffix that will be appended if the text is trimmed (for example: "...").
        /// The resulting text length with the suffix included will be maxLength.</param>
        public static string Limit(this string text, int maxLength, string trimMark)
        {
            if (text.Length > maxLength)
            {
                trimMark = trimMark.Limit(maxLength);
                return text.Substring(0, maxLength - trimMark.Length) + trimMark;
            }
            else
                return text;
        }

        private static char[] lineSplitters = new char[] { '\r', '\n' };

        public static string FirstLine(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var lineEnd = text.IndexOfAny(lineSplitters);
            if (lineEnd != -1)
                return text.Substring(0, lineEnd);
            else
                return text;
        }

        public static string ReportSegment(string query, int showPosition, int maxLength)
        {
            int start = showPosition - maxLength / 2;
            int end = showPosition + (maxLength + 1) / 2;
            if (start < 0)
            {
                end += -start;
                start = 0;
            }
            if (end > query.Length)
            {
                start -= end - query.Length;
                end = query.Length;
            }
            if (start < 0)
                start = 0;

            string prefix = start > 0 ? "..." : "";
            string suffix = end < query.Length ? "..." : "";

            return prefix
                + query.Substring(start, end - start).Replace(@"\", @"\\").Replace("\r", @"\r").Replace("\n", @"\n").Replace("\t", @"\t")
                + suffix;
        }

        /// <summary>
        /// Simplified type name for logging and reporting, without namespace and assembly information.
        /// </summary>
        public static string GetShortTypeName(Type type)
        {
            if (!type.IsGenericType)
                return type.Name;
            else
                return type.Name + "<" + string.Join(", ", type.GetGenericArguments().Select(argumentType => GetShortTypeName(argumentType))) + ">";
        }
    }
}
