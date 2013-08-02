/*
    Copyright (C) 2013 Omega software d.o.o.

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
using System.Diagnostics.Contracts;
using System.Globalization;

namespace Rhetos.Utilities
{
    public static class ScriptPositionReporting
    {
        public static int Line(string dsl, int position)
        {
            Contract.Requires(dsl != null);
            Contract.Requires(position >= 0 && position < dsl.Length);

            return dsl.Substring(0, position).Count(c => c == '\n') + 1;
        }

        public static int Column(string dsl, int position)
        {
            Contract.Requires(dsl != null);
            Contract.Requires(position >= 0 && position < dsl.Length);

            if (position == 0)
                return 1;
            int end = dsl.LastIndexOf('\n', position - 1);
            if (end == -1)
                return position + 1;

            return position - end;
        }

        public static int Position(string dsl, int line, int column)
        {
            int pos = 0;
            while (line > 1)
            {
                pos = dsl.IndexOf('\n', pos);
                if (pos == -1)
                    break;
                pos++;
                line--;
            }
            return pos + column - 1;
        }

        public static string FollowingText(string dsl, int position, int maxLength)
        {
            Contract.Requires(dsl != null);
            Contract.Requires(position >= 0 && position < dsl.Length);
            Contract.Requires(maxLength >= 0);

            if (position >= dsl.Length)
                return "";

            dsl = dsl.Substring(position);
            dsl = string.Join(" ", dsl.Split(new char[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
            if (dsl.Length > maxLength)
                dsl = dsl.Substring(0, maxLength) + "...";
            return dsl;
        }

        public static string FollowingText(string dsl, int line, int column, int maxLength)
        {
            return FollowingText(dsl, Position(dsl, line, column), maxLength);
        }

        public static string PreviousText(string dsl, int position, int maxLength)
        {
            if (position >= dsl.Length)
                position = dsl.Length;
            if (position <= 0)
                return "";

            dsl = dsl.Substring(0, position);
            dsl = string.Join(" ", dsl.Split(new char[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
            if (dsl.Length > maxLength)
                dsl = "..." + dsl.Substring(dsl.Length - maxLength, maxLength);
            return dsl;
        }

        public static string PreviousText(string dsl, int line, int column, int maxLength)
        {
            return PreviousText(dsl, Position(dsl, line, column), maxLength);
        }

        public static string ReportPosition(string text, int line, int column, string filePath = null)
        {
            return string.Format(CultureInfo.InvariantCulture,
                "At line {0}, column {1},{2}\r\nafter: \"{3}\",\r\nbefore: \"{4}\".",
                    line, column,
                    (filePath != null) ? " file '" + filePath + "'," : "",
                    PreviousText(text, line, column, 70),
                    FollowingText(text, line, column, 70));
        }

        public static string ReportPosition(string text, int position, string filePath = null)
        {
            if (position > text.Length)
                position = text.Length;
            if (position < 0)
                position = 0;

            return ReportPosition(text, Line(text, position), Column(text, position), filePath);
        }
    }
}