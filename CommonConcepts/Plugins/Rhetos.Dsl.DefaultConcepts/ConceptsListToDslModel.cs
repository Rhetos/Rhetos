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

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>This is a temporary solution until IValidationConcept.CheckSemantics is upgraded to use IDslModel.</summary>
    public class ConceptsListToDslModel : IDslModel
    {
        public ConceptsListToDslModel(IEnumerable<IConceptInfo> concepts)
        {
            Concepts = concepts;
        }

        public IEnumerable<IConceptInfo> Concepts { get; set; }

        public IConceptInfo FindByKey(string conceptKey)
        {
            // TODO: This entire class should be removed after implementing concept validation with IDslModel argument.
            throw new NotImplementedException();
        }

        public T GetIndex<T>() where T : IDslModelIndex
        {
            throw new NotImplementedException();
        }
    }
}
