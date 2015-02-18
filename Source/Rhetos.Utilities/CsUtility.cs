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
using System.Linq;
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
                    return "Given name '" + name + "' is not valid. First character is not an english letter or undescore.";
            }

            {
                foreach (char c in name)
                    if ((c < 'a' || c > 'z') && (c < 'A' || c > 'Z') && c != '_' && (c < '0' || c > '9'))
                        return "Given name '" + name + "' is not valid. Character '" + c + "' is not an english letter or number or undescore.";
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
        /// Use this function to make sure that the <paramref name="items"/> is not a LINQ query
        /// before using it multiple times, in order to aviod the query evaluation every time
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
    }
}
