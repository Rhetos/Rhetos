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

using System.ComponentModel.Composition;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// Base concept for loading data with provided input parameter.
    /// This concept assumes that there will be a <c>Load</c> method implemented in the repository class,
    /// with parameter of a given type (see <see cref="Parameter"/>),
    /// returning IEnumerable of the <see cref="DataStructure"/> type.
    /// The <see cref="Parameter"/> property also of represents the filter name.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Load")]
    public class LoadInfo : IConceptInfo
    {
        [ConceptKey]
        public DataStructureInfo DataStructure { get; set; }

        /// <summary>
        /// Parameter type. It can be a DataStructure name or any C# type.
        /// It also represents the filter name.
        /// </summary>
        [ConceptKey]
        public string Parameter { get; set; }
    }
}
