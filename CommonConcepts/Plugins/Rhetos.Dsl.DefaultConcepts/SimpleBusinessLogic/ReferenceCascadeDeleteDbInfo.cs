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
    /// A low-level concept for generating cascade delete in database.
    /// It should be rarely used because deleting records directly in database
    /// circumvents any business logic implemented in the application related to those records.
    /// If the legacy option CommonConcepts.Legacy.CascadeDeleteInDatabase is enabled,
    /// this concept will be created automatically for each Reference with CascadeDelete (for example, on Detail concept)
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("CascadeDeleteInDatabase")]
    public class ReferenceCascadeDeleteDbInfo : IConceptInfo
    {
        [ConceptKey]
        public ReferencePropertyDbConstraintInfo ReferenceDbConstraint { get; set; }
    }
}
