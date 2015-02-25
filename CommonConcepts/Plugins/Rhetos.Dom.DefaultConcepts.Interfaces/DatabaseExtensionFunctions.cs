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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Rhetos.Dom.DefaultConcepts
{
    /// <summary>
    /// This class contains a set of SQL-compatible functions that can be evaluated in both LINQ2HN and SQL.
    /// </summary>
    public static class DatabaseExtensionFunctions
    {
        /// <summary>
        /// If b is null, SQL query will use IS NULL instead of the equality operator.
        /// </summary>
        public static bool EqualsCaseInsensitive(this string a, string b)
        {
            if (b == null)
                return a == null;
            else
                return a != null && a.Equals(b, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// If b is null, SQL query will use IS NOT NULL instead of the inequality operator.
        /// </summary>
        public static bool NotEqualsCaseInsensitive(this string a, string b)
        {
            if (b == null)
                return a != null;
            else
                return a != null && !a.Equals(b, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsLessThen(this string a, string b)
        {
            return a != null && b != null && String.Compare(a, b, StringComparison.InvariantCultureIgnoreCase) < 0;
        }

        public static bool IsLessThenOrEqual(this string a, string b)
        {
            return a != null && b != null && String.Compare(a, b, StringComparison.InvariantCultureIgnoreCase) <= 0;
        }

        public static bool IsGreaterThen(this string a, string b)
        {
            return a != null && b != null && String.Compare(a, b, StringComparison.InvariantCultureIgnoreCase) > 0;
        }

        public static bool IsGreaterThenOrEqual(this string a, string b)
        {
            return a != null && b != null && String.Compare(a, b, StringComparison.InvariantCultureIgnoreCase) >= 0;
        }

        public static bool StartsWith(this int? a, string b)
        {
            return a != null && b != null && a.ToString().StartsWith(b);
        }

        public static bool StartsWithCaseInsensitive(this string a, string b)
        {
            return a != null && b != null && a.StartsWith(b, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool ContainsCaseInsensitive(this string a, string b)
        {
            return a != null && b != null && a.ToLowerInvariant().Contains(b.ToLowerInvariant());
        }

        public static bool Like(this string text, string pattern)
        {
            if (text == null || pattern == null)
                return false;

            pattern = Regex.Escape(pattern);
            pattern = pattern.Replace("%", ".*");
            pattern = pattern.Replace("_", ".");
            pattern = "^" + pattern + "$";
           
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            return regex.IsMatch(text);
        }

        public static string CastToString(this int? a)
        {
            if (a == null)
                return null;

            return a.ToString();
        }
    }
}
