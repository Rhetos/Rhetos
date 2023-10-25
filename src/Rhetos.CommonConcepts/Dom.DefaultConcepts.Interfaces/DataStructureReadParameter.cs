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

namespace Rhetos.Dom.DefaultConcepts
{
    public sealed class DataStructureReadParameter : IEquatable<DataStructureReadParameter>
    {
        /// <summary>
        /// This property may contain the type name as written in C# source code (with or without namespace),
        /// or the type name as provided by Type.ToString.
        /// It is commonly created from filter names and other read concepts in DSL.
        /// It can be used at <see cref="FilterCriteria.Filter"/>.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Type of the filter parameter.
        /// </summary>
        public Type Type { get; }

        public DataStructureReadParameter(string name, Type type)
        {
            Name = name;
            Type = type;
        }

        public override string ToString() => $"{Name}: {Type}";

        public bool Equals(DataStructureReadParameter other)
        {
            // IEquatable implementation is added for DataStructureReadParameters class to return distinct values.
            return string.Equals(other.Name, Name, StringComparison.Ordinal) && other.Type == Type;
        }

        public override bool Equals(object obj) => Equals(obj as DataStructureReadParameter);

        public override int GetHashCode() => HashCode.Combine(Name, Type);
    }
}
