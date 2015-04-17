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
using Rhetos.Dsl;
using System.ComponentModel.Composition;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// Enables lazy loading of navigation properties.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("LazyLoadReferences")]
    public class LazyLoadDataStructureInfo : IConceptInfo
    {
        [ConceptKey]
        public DataStructureInfo DataStructure { get; set; }
    }

    [Export(typeof(IConceptMacro))]
    public class LazyLoadDataStructureMacro : IConceptMacro<LazyLoadDataStructureInfo>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(LazyLoadDataStructureInfo conceptInfo, IDslModel existingConcepts)
        {
            var newConcepts = new List<IConceptInfo>();

            newConcepts.AddRange(existingConcepts.FindByReference<ReferencePropertyInfo>(rp => rp.DataStructure, conceptInfo.DataStructure)
                .Where(rp => DslUtility.IsQueryable(rp.DataStructure) && DslUtility.IsQueryable(rp.Referenced))
                .Select(rp => new LazyLoadReferenceInfo { Reference = rp }));

            newConcepts.AddRange(existingConcepts.FindByReference<LinkedItemsInfo>(li => li.DataStructure, conceptInfo.DataStructure)
                .Where(li => DslUtility.IsQueryable(li.DataStructure) && DslUtility.IsQueryable(li.ReferenceProperty.DataStructure))
                .Select(li => new LazyLoadLinkedItemsInfo { LinkedItems = li }));

            newConcepts.AddRange(existingConcepts.FindByReference<DataStructureExtendsInfo>(e => e.Extension, conceptInfo.DataStructure)
                .Where(e => DslUtility.IsQueryable(e.Extension) && DslUtility.IsQueryable(e.Base))
                .Select(e => new LazyLoadBaseInfo { Extends = e }));

            newConcepts.AddRange(existingConcepts.FindByReference<DataStructureExtendsInfo>(e => e.Base, conceptInfo.DataStructure)
                .Where(e => DslUtility.IsQueryable(e.Extension) && DslUtility.IsQueryable(e.Base))
                .Select(e => new LazyLoadExtensionInfo { Extends = e }));

            return newConcepts;
        }
    }
}
