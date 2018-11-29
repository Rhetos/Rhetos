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
    /// <summary>
    /// Inherits the 'UniqueReference' concept and additionally allows cascade delete and automatic inheritance of row permissions.
    /// From a business perspective, the main difference between 'Extends' and 'UniqueReference' is that extension is considered a part of the base data structure.
    /// In 1:1 relations, the 'Extends' concept is to 'UniqueReference' as 'Reference { Detail; }' is to 'Reference' in 1:N relations.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Extends")]
    public class DataStructureExtendsInfo : UniqueReferenceInfo
    {
    }

    [Export(typeof(IConceptMacro))]
    public class DataStructureExtendsMacro : IConceptMacro<DataStructureExtendsInfo>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(DataStructureExtendsInfo conceptInfo, IDslModel existingConcepts)
        {
            if (UniqueReferenceCascadeDeleteInfo.IsSupported(conceptInfo))
                return new IConceptInfo[] { new UniqueReferenceCascadeDeleteInfo { UniqueReference = conceptInfo } };
            else
                return null;
        }
    }
}
