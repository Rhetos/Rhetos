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

using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Detail")]
    public class ReferenceDetailInfo : IMacroConcept
    {
        [ConceptKey]
        public ReferencePropertyInfo Reference { get; set; }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            var newConcepts = new List<IConceptInfo>();

            // Even this method generates no other features, the Detail concept can be useful
            // in business modelling (inheriting row permissions to SqlQueryable, for example)
            if (Reference.DataStructure is IWritableOrmDataStructure)
            {
                newConcepts.Add(new ReferenceCascadeDeleteInfo { Reference = Reference });
                newConcepts.Add(new SqlIndexInfo { Property = Reference });
                newConcepts.Add(new SystemRequiredInfo { Property = Reference });
            }

            return newConcepts;
        }
        
        public override string ToString()
        {
            return "Detail " + Reference.ToString();
        }

        public override int GetHashCode()
        {
            return Reference.GetHashCode();
        }
    }
}
