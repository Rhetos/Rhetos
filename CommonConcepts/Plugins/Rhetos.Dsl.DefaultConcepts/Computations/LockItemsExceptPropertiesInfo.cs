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
    [ConceptKeyword("LockExcept")]
    public class LockItemsExceptPropertiesInfo : IConceptInfo, IValidationConcept
    {
        [ConceptKey]
        public DataStructureInfo Source { get; set; }

        [ConceptKey]
        public string FilterType { get; set; }

        public string Title { get; set; }

        /// <summary>A list of properties that should not be locked.</summary>
        public string ExceptProperties { get; set; }

        public void CheckSemantics(IEnumerable<IConceptInfo> existingConcepts)
        {
            DslUtility.ValidatePropertyListSyntax(ExceptProperties, this);
        }
    }

    [Export(typeof(IConceptMacro))]
    public class LockItemsExceptPropertiesMacro : IConceptMacro<LockItemsExceptPropertiesInfo>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(LockItemsExceptPropertiesInfo conceptInfo, IDslModel existingConcepts)
        {
            var dontLockProperties = new HashSet<string>(conceptInfo.ExceptProperties.Split(' '));

            var lockProperties = existingConcepts
                .FindByReference<PropertyInfo>(p => p.DataStructure, conceptInfo.Source)
                .Where(p => !dontLockProperties.Contains(p.Name)).ToList();

            return lockProperties.Select(p => new LockItemsLockPropertyInfo { Lock = conceptInfo, Property = p });
        }
    }
}
