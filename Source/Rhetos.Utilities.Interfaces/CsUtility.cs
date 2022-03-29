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

        public static string Indent(string lines, int indentation)
        {
            return string.Join("\r\n", lines.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n').Select(line => new string(' ', indentation) + line));
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
        /// Type casting with detaile error description.
        /// </summary>
        public static T Cast<T>(object o, string objectName) where T: class
        {
            if (o is null)
                return null;
            if (o is T t)
                return t;

            if (o.GetType().ToString() != typeof(T).ToString())
                throw new ArgumentException($"Unexpected object type. The provided '{objectName}' is a '{o.GetType()}' instead of '{typeof(T)}'.");
            else
                throw new ArgumentException($"Unexpected object type. The provided '{objectName}' is a '{o.GetType().AssemblyQualifiedName}' instead of '{typeof(T).AssemblyQualifiedName}'.");
        }

        /// <summary>
        /// Returns null if the argument is a valid identifier, error message otherwise.
        /// </summary>
        public static string GetIdentifierError(string name)
        {
            if (name == null)
                return "Identifier name is null.";

            if (name == "")
                return "Identifier name is empty.";

            if (IsNotLetterOrUnderscore(name[0]))
                return $"Identifier name '{CsUtility.Limit(name, 200, true)}' is not valid. First character is not an English letter or underscore.";

            foreach (char c in name)
                if (IsNotLetterOrUnderscore(c) && (c < '0' || c > '9'))
                    return $"Identifier name '{CsUtility.Limit(name, 200, true)}' is not valid. Character '{c}' is not an English letter or number or underscore.";

            return null;
        }

        private static bool IsNotLetterOrUnderscore(char c) => (c < 'a' || c > 'z') && (c < 'A' || c > 'Z') && c != '_';

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
        /// When joining zero or one collections, no new IEnumerable "wrapper" objects are created.
        /// </summary>
        public static IEnumerable<T> Concatenate<T>(List<IEnumerable<T>> _values)
        {
            if (_values.Count == 0)
                return Enumerable.Empty<T>();

            IEnumerable<T> all = _values[0];
            for (int x = 1; x < _values.Count; x++)
                all = all.Concat(_values[x]);
            return all;
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
            if (ex is ReflectionTypeLoadException typeLoadException)
                loaderExceptions = typeLoadException.LoaderExceptions.GroupBy(exception => exception.Message).Select(group => group.First()).ToList();
            else if (ex is FileLoadException)
                loaderExceptions = new List<Exception> { ex };
            else if (ex is FileNotFoundException fileNotFoundException && fileNotFoundException.FusionLog != null)
                loaderExceptions = new List<Exception> { ex };
            else
                return null;

            var report = new List<string>();
            report.Add(
                (string.IsNullOrEmpty(errorContext) ? errorContext + " " : "")
                + "Please check if the assembly is missing or has a different version.");
            report.AddRange(ReportLoaderExceptions(loaderExceptions));
            if (referencedAssembliesPaths != null)
                report.AddRange(ReportAssemblyLoadErrors(referencedAssembliesPaths));

            return string.Join(Environment.NewLine, report);
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
                    Exception[] reportExceptions;

                    if (ex is ReflectionTypeLoadException rtle && rtle.LoaderExceptions?.Any() == true)
                        reportExceptions = rtle.LoaderExceptions;
                    else
                        reportExceptions = new[] { ex };

                    foreach (var exceptionInfo in reportExceptions.Select(re => re.GetType().Name + ": " + re.Message).Distinct().Take(5))
                        report.Add($"* '{Path.GetFileName(assemblyPath)}' throws {exceptionInfo}");
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
            while (strIndex < strings.Count && prefixIndex < prefixes.Count)
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

        /// <summary>
        /// Shortens the text if needed to match the limit.
        /// If shortened, the text will end with hash value that represent the erased suffix.
        /// This reduces name collisions if two string have same prefix longer than <paramref name="maxLength"/>.
        /// </summary>
        public static string LimitWithHash(this string text, int maxLength)
        {
            const int minimalLimit = 10;
            if (maxLength < minimalLimit)
                throw new ArgumentException($"Minimal limit for {nameof(LimitWithHash)} is {minimalLimit}.");

            if (text.Length > maxLength)
            {
                var hashErasedPart = CsUtility.GetStableHashCode(text.Substring(maxLength - 9)).ToString("X").PadLeft(8, '0');
                return text.Substring(0, maxLength - 9) + "_" + hashErasedPart;
            }
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

        /// <summary>
        /// Generates GUID based on a string.
        /// </summary>
        public static Guid GenerateGuid(string s)
        {
            using (var hashing = System.Security.Cryptography.SHA256.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(s);
                byte[] hashBytes = hashing.ComputeHash(inputBytes).Take(16).ToArray();
                return new Guid(hashBytes);
            }
        }

        /// <summary>
        /// Returns the underlying generic type with concrete type arguments.
        /// </summary>
        public static Type GetUnderlyingGenericType(Type type, Type genericType)
        {
            if (genericType.IsInterface)
                throw new ArgumentException("Interfaces are not supported.");

            if (!genericType.IsGenericType)
                throw new ArgumentException("The type must be a generic type.");

            if (genericType.GenericTypeArguments.Length != 0)
                throw new ArgumentException("The generic type should not have any type arguments.");

            while (type != null && type != typeof(object))
            {
                var cur = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
                if (genericType == cur)
                    return type;
                type = type.BaseType;
            }

            return null;
        }

        /// <summary>
        /// Standard GetHashCode() function in not guaranteed to return same result in different environments.
        /// </summary>
        public static int GetStableHashCode(string implementationName)
        {
            if (string.IsNullOrEmpty(implementationName))
                return 0;

            const int seed = 1737350767;
            int hash = seed;
            foreach (char c in implementationName)
            {
                hash += c;
                hash *= seed;
            }
            return hash;
        }

        public static void InvokeAll<T>(T target, IEnumerable<Action<T>> actions)
        {
            foreach (var action in actions)
                action.Invoke(target);
        }

        private static MethodInfo CloneMethod = typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);

        public static T ShallowCopy<T>(T o)
        {
            return (T)CloneMethod.Invoke(o, null);
        }
    }
}
