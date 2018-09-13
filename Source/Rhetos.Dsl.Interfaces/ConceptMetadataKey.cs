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
    public class ConceptMetadataKey<T>
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }

        /// <param name="name">Name is optional. It is used only for debugging.</param>
        public ConceptMetadataKey(string name = null)
        {
            Id = Guid.NewGuid();
            Name = name;
        }

        /// <summary>
        /// Name is optional. It is used only for debugging.
        /// </summary>
        public static implicit operator ConceptMetadataKey<T>(string name)
        {
            return new ConceptMetadataKey<T>(name);
        }

        public override string ToString()
        {
            return Name != null
                ? Name + ", " + Id.ToString()
                : Id.ToString();
        }
    }
}
