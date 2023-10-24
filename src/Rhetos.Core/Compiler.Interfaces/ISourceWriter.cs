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

namespace Rhetos.Compiler
{
    public interface ISourceWriter
    {
        /// <summary>
        /// Creates the source file within the designated directory, or overwrites an existing file.
        /// </summary>
        /// <param name="relativePath">
        /// Path should be a file name or a relative path inside the generated source folder.
        /// </param>
        void Add(string relativePath, string content);

        /// <summary>
        /// Deletes all other files it generated source directory, that have not been written in the current build.
        /// </summary>
        void CleanUp();
    }
}
