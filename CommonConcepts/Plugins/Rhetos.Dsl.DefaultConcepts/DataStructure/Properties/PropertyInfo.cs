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
using Rhetos.Dsl;
using System.ComponentModel.Composition;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// Property is an abstract concept: there is no ConceptKeyword.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    public class PropertyInfo : IConceptInfo
    {
        [ConceptKey]
        public DataStructureInfo DataStructure { get; set; }

        [ConceptKey]
        public string Name { get; set; }

        /// <summary>
        /// Name of the property generated in the simple POCO C# class for the data structure.
        /// This may be different from the database column or the navigation property name.
        /// Returns null, if this concept does not generate the single property on the simple class.
        /// </summary>
        public virtual string GetSimplePropertyName() => Name;

        public override string ToString() => FullName; // For backward compatibility.

        public string FullName => DataStructure.FullName + "." + Name;
    }
}
