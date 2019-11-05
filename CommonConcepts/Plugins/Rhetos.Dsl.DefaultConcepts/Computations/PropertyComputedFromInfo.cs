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
using System.Text;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("ComputedFrom")]
    public class PropertyComputedFromInfo : IValidatedConcept, IAlternativeInitializationConcept
    {
        [ConceptKey]
        public PropertyInfo Target { get; set; }

        [ConceptKey]
        public PropertyInfo Source { get; set; }

        public EntityComputedFromInfo Dependency_EntityComputedFrom { get; set; }

        public void InternalCheck()
        {
            if (!(Target.DataStructure is EntityInfo))
                throw new FrameworkException(string.Format(
                    "{0} can only be used on properties of {1}. {2} is a member of {3}.",
                    this.GetKeywordOrTypeName(),
                    ConceptInfoHelper.GetKeywordOrTypeName(typeof(EntityInfo)),
                    Target.GetUserDescription(),
                    Target.DataStructure.GetKeywordOrTypeName()));
        }

        public void CheckSemantics(IDslModel existingConcepts)
        {
            InternalCheck();
        }

        public IEnumerable<string> DeclareNonparsableProperties()
        {
            return new[] { "Dependency_EntityComputedFrom" };
        }

        public void InitializeNonparsableProperties(out IEnumerable<IConceptInfo> createdConcepts)
        {
            InternalCheck();
            Dependency_EntityComputedFrom = new EntityComputedFromInfo { Target = (EntityInfo)Target.DataStructure, Source = Source.DataStructure };
            createdConcepts = new[] { Dependency_EntityComputedFrom };
        }
    }
}
