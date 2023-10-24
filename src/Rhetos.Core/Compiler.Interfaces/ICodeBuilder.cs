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
    public interface ICodeBuilder
    {
        /// <summary>
        /// Insert code that is grouped into files.
        /// <see cref="InsertCode(string)"/> uses empty string for the path.
        /// </summary>
        void InsertCodeToFile(string code, string path);

        void InsertCode(string code);

        void InsertCode(string code, string tag);

        void InsertCode(string firstCode, string nextCode, string firstTag, string nextTag);

        void InsertCode(string code, string tag, bool insertAfterTag);

        void InsertCode(string firstCode, string nextCode, string firstTag, string nextTag, bool insertAfterTag);

        void ReplaceCode(string code, string tag);

        bool TagExists(string tag);
    }
}
