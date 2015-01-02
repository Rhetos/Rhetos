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

using Rhetos.Configuration.Autofac;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonConcepts.Test.Helpers
{
    public static class DateTimeHelper
    {
        /// <summary>
        /// Helper for CommonConcepts unit tests.
        /// </summary>
        public static string Dump(this DateTime dateTime)
        {
            string dump = dateTime.ToString(@"yyyy-MM-dd\THH\:mm\:ss.FFFF");
            Console.WriteLine("[DateTime] " + dump + " (full: " + dateTime.ToString("o") + ")");
            return dump;
        }

        /// <summary>
        /// Helper for CommonConcepts unit tests.
        /// </summary>
        public static string Dump(this DateTime? dateTime)
        {
            return dateTime.HasValue ? Dump(dateTime.Value) : "null";
        }

        /// <summary>
        /// The result is rounded to hundredths, to simplify unit tests that use database (rounding would happen on Save/Load).
        /// </summary>
        public static DateTime Rounded(this DateTime dateTime)
        {
            int roundedMilliseconds = dateTime.Millisecond / 10 * 10;
            var roundedTime = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second, roundedMilliseconds);
            Console.WriteLine("Rounding: " + dateTime.ToString("o") + " to " + roundedTime.ToString("o"));
            return roundedTime;
        }

        /// <summary>
        /// The result is rounded to hundredths, to simplify unit tests that use database (rounding would happen on Save/Load).
        /// </summary>
        public static DateTime? Rounded(this DateTime? dateTime)
        {
            if (dateTime.HasValue)
                return Rounded(dateTime.Value);
            else
                return null;
        }
    }
}
