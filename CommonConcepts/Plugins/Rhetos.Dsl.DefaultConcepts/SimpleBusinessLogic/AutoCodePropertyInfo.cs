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

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("AutoCode")]
    public class AutoCodePropertyInfo : IMacroConcept, IValidatedConcept
    {
        [ConceptKey]
        public PropertyInfo Property { get; set; }

        public void CheckSemantics(IDslModel existingConcepts)
        {
            if (!(Property is ShortStringPropertyInfo) && !(Property is IntegerPropertyInfo))
                throw new DslSyntaxException("AutoCode is only available for ShortString and Integer properties.");
        }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            var newConcets = new List<IConceptInfo>();

            newConcets.Add(new SystemRequiredInfo { Property = Property });
            newConcets.Add(CreateUniqueConstraint());

            return newConcets;
        }

        virtual protected IConceptInfo CreateUniqueConstraint()
        {
            return new UniqueMultiplePropertiesInfo { DataStructure = Property.DataStructure, PropertyNames = Property.Name };
        }
    }
}
