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
using System.ComponentModel.Composition;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Rhetos.Utilities;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("ComputeForNewBaseItems")]
    public class PersistedComputeForNewBaseItemsWithFilterInfo : IMacroConcept, IValidationConcept
    {
        [ConceptKey]
        public PersistedDataStructureInfo Persisted { get; set; }

        public string FilterSaveExpression { get; set; }

        private UniqueReferenceInfo MyExtendsConceptInfo(IEnumerable<IConceptInfo> existingConcepts)
        {
            return existingConcepts.OfType<UniqueReferenceInfo>()
                .Where(extends => extends.Extension == Persisted)
                .FirstOrDefault();
        }

        public void CheckSemantics(IEnumerable<IConceptInfo> concepts)
        {
            var myExtends = MyExtendsConceptInfo(concepts);
            if (myExtends == null)
                throw new DslSyntaxException("ComputeForNewBaseItemsWithFilter can only be used if the persisted data structure extends a base entity. Use 'Extends' keyword to define the extension is applicable.");
        }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            return new[] { new ComputeForNewBaseItemsInfo
                {
                    EntityComputedFrom = new EntityComputedFromInfo { Target = Persisted, Source = Persisted.Source },
                    FilterSaveExpression = FilterSaveExpression
                } };
        }
    }
}
