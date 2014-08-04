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
    public class AutoCodePropertyInfo : IAlternativeInitializationConcept, IValidationConcept
    {
        [ConceptKey]
        public PropertyInfo Property { get; set; }

        public AutoCodeTriggerInfo Dependency_TriggerInfo { get; set; }

        public IEnumerable<string> DeclareNonparsableProperties()
        {
            return new[] { "Dependency_TriggerInfo" };
        }

        public void InitializeNonparsableProperties(out IEnumerable<IConceptInfo> createdConcepts)
        {
            if (!(Property.DataStructure is EntityInfo))
                throw new DslSyntaxException(this, "AutoCode concept may only be used on properties of entity.");
            Dependency_TriggerInfo = new AutoCodeTriggerInfo { Entity = (EntityInfo)this.Property.DataStructure };
            createdConcepts = new IConceptInfo[] { Dependency_TriggerInfo };
        }

        public void CheckSemantics(IEnumerable<IConceptInfo> existingConcepts)
        {
            if (!(Property is ShortStringPropertyInfo) && !(Property is IntegerPropertyInfo))
                throw new DslSyntaxException("AutoCode is only available for ShortString and Integer properties.");
        }

    }
}
