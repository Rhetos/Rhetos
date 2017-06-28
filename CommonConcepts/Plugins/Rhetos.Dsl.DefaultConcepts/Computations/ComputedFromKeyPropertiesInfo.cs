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
using Rhetos.Dsl.DefaultConcepts;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("KeyProperties")]
    public class ComputedFromKeyPropertiesInfo : IConceptInfo, IValidatedConcept
    {
        [ConceptKey]
        public EntityComputedFromInfo ComputedFrom { get; set; }

        public string KeyProperties { get; set; }

        public void CheckSemantics(IDslModel existingConcepts)
        {
            var keyProperties = CreateKeyProperties(existingConcepts);
            var invalidPropertyName = keyProperties.Where(kp => kp.Value == null).Select(kp => kp.Key).FirstOrDefault();
            if (invalidPropertyName != null)
                throw new DslSyntaxException(this, $"Cannot find property '{invalidPropertyName}' on '{ComputedFrom.Target.GetKeyProperties()}'.");
        }

        public Dictionary<string, IConceptInfo> CreateKeyProperties(IDslModel existingConcepts)
        {
            var computedPropertiesByTarget = existingConcepts
                .FindByReference<PropertyComputedFromInfo>(cp => cp.Dependency_EntityComputedFrom, ComputedFrom)
                .ToDictionary(cp => cp.Target.Name);

            return KeyProperties.Split(' ')
                .ToDictionary(propertyName => propertyName, propertyName =>
                {
                    if (propertyName == "ID")
                        return new KeyPropertyIDComputedFromInfo
                        {
                            EntityComputedFrom = ComputedFrom
                        };
                    else if (computedPropertiesByTarget.ContainsKey(propertyName))
                        return new KeyPropertyComputedFromInfo
                        {
                            PropertyComputedFrom = computedPropertiesByTarget[propertyName]
                        };
                    else
                        return (IConceptInfo)null;
                });
        }
    }

    [Export(typeof(IConceptMacro))]
    public class ComputedFromKeyPropertiesMacro : IConceptMacro<ComputedFromKeyPropertiesInfo>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(ComputedFromKeyPropertiesInfo conceptInfo, IDslModel existingConcepts)
        {
            DslUtility.ValidatePropertyListSyntax(conceptInfo.KeyProperties, conceptInfo);

            var keyProperties = conceptInfo.CreateKeyProperties(existingConcepts);

            // If *not all* listed property names are found in the DSL model, wait for more iterations of macro evaluation,
            // or return an error in the IValidatedConcept implementation above.
            if (!keyProperties.Values.All(kp => kp != null))
                return null;
            else
                return keyProperties.Values;
        }
    }
}
