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

namespace Rhetos.Dsl
{
    /// <summary>
    /// It is possible to nest a concept within a parent concept ("module x { entity y; }") or to use it explicitly ("entity x.y;").
    /// Parent reference is defined by a property that references another concept, marked with <see cref="ConceptParentAttribute"/> or the first property by default.
    /// Derived concept can override parent property with its own property marked with <see cref="ConceptParentAttribute"/>;
    /// this allows construction of recursive concepts such as menu items.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class ConceptParentAttribute : Attribute
    {
    }
}
