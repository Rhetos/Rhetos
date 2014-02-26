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
using System.Reflection;

namespace Rhetos.Compiler
{
    public interface ICodeBuilder
    {
        /// <summary>
        /// Use AddReferencesFromDependency to safely add reference to exact assembly, instead of using dll's short name to guess assembly version that should be used.
        /// </summary>
        void AddReference(string shortName);
        void AddReferencesFromDependency(Type type);

        void InsertCode(string code);
        void InsertCode(string code, string tag);
        void InsertCode(string firstCode, string nextCode, string firstTag, string nextTag);
        void ReplaceCode(string code, string tag);

        bool TagExists(string tag);
    }
}
