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

namespace Rhetos.DatabaseGenerator
{
    [Obsolete("This feature is no longer used by Rhetos, and will be deleted in future versions. Database upgrade relies solely on SQL scripts generated from DSL concepts.")]
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ConceptImplementationVersionAttribute : Attribute
    {
        public Version Version { get; private set; }

        public ConceptImplementationVersionAttribute(int v1, int v2)
        {
            Version = new Version(v1, v2);
        }
        public ConceptImplementationVersionAttribute(int v1, int v2, int v3)
        {
            Version = new Version(v1, v2, v3);
        }
        public ConceptImplementationVersionAttribute(int v1, int v2, int v3, int v4)
        {
            Version = new Version(v1, v2, v3, v4);
        }
    }
}
