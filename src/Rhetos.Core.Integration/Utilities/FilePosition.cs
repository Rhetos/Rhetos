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

using Rhetos.Utilities;

namespace Rhetos.Dsl
{
    /// <summary>
    /// Position in file for reporting location of an error.
    /// File path is required, but the file does not need to exit on disk.
    /// Position is optional. It can represent one location (begin only) or a range (begin and end).
    /// </summary>
    public class FilePosition
    {
        public string Path { get; }
        public int BeginLine { get; }
        public int BeginColumn { get; }
        public int EndLine { get; }
        public int EndColumn { get; }

        public FilePosition(string filePath, string fileContent = "", int positionBegin = 0, int positionEnd = 0)
        {
            Path = filePath;

            if (!string.IsNullOrEmpty(filePath) && !string.IsNullOrEmpty(fileContent))
            {
                (BeginLine, BeginColumn) = ScriptPositionReporting.GetLineColumn(fileContent, positionBegin);
                (EndLine, EndColumn) = ScriptPositionReporting.GetLineColumn(fileContent, positionEnd);
            }
        }

        public string CanonicalOrigin
        {
            get
            {
                if (!string.IsNullOrEmpty(Path))
                {
                    string location;

                    if (BeginLine > 0)
                    {
                        if (BeginColumn > 0)
                        {
                            if (EndLine > BeginLine || EndLine == BeginLine && EndColumn > BeginColumn)
                                location = $"({BeginLine},{BeginColumn},{EndLine},{EndColumn})";
                            else
                                location = $"({BeginLine},{BeginColumn})";
                        }
                        else
                            location = $"({BeginLine})";
                    }
                    else
                        location = "";

                    return Path + location;

                }
                else
                    return null;
            }
        }
    }
}
