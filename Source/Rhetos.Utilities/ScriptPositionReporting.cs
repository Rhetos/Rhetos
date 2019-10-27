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
using System.Diagnostics.Contracts;
using System.Globalization;

namespace Rhetos.Utilities
{
    public static class ScriptPositionReporting
    {
        public static int Line(string text, int position)
        {
            return text.Substring(0, position).Count(c => c == '\n') + 1;
        }

        public static int Column(string text, int position)
        {
            if (position == 0)
                return 1;
            int end = text.LastIndexOf('\n', position - 1);
            if (end == -1)
                return position + 1;

            return position - end;
        }

        public static int Position(string text, int line, int column)
        {
            int pos = 0;
            while (line > 1)
            {
                pos = text.IndexOf('\n', pos);
                if (pos == -1)
                    break;
                pos++;
                line--;
            }
            return pos + column - 1;
        }

        public static string FollowingText(string text, int position, int maxLength)
        {
            if (position >= text.Length)
                return "";

            text = text.Substring(position, Math.Min(maxLength * 2, text.Length - position));
            text = string.Join(" ", text.Split(new char[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
            text = text.Limit(maxLength, "...");
            return text;
        }

        public static string PreviousText(string text, int position, int maxLength)
        {
            if (position >= text.Length)
                position = text.Length;
            if (position <= 0)
                return "";

            text = text.Substring(0, position);
            text = string.Join(" ", text.Split(new char[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
            if (text.Length > maxLength)
                text = "..." + text.Substring(text.Length - maxLength, maxLength);
            return text;
        }

        public static string ReportPosition(string text, int position, string filePath = null)
        {
            position = PositionWithinRange(text, position);
            string fileInfo = filePath != null ? " file '" + filePath + "'," : "";
            string fileAndPositionInfo = $"At line {Line(text, position)}, column {Column(text, position)},{fileInfo}\r\n";
            return $"{fileAndPositionInfo}{ReportPreviousAndFollowingText(text, position)}";
        }

        public static string ReportPreviousAndFollowingText(string text, int position)
        {
            position = PositionWithinRange(text, position);
            return $" after: \"{PreviousText(text, position, 70)}\",\r\n before: \"{FollowingText(text, position, 70)}\".";
        }
        private static int PositionWithinRange(string text, int position)
        {
            if (position > text.Length)
                position = text.Length;
            if (position < 0)
                position = 0;
            return position;
        }
    }
}