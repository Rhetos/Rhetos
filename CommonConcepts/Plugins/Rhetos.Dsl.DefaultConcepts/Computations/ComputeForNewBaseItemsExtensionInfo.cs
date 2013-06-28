/*
    Copyright (C) 2013 Omega software d.o.o.

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

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    public class ComputeForNewBaseItemsExtensionInfo : IConceptInfo, IValidationConcept
    {
        [ConceptKey]
        public DataStructureExtendsInfo Extends { get; set; }

        public string FilterSaveExpression { get; set; }

        public void CheckSemantics(IEnumerable<IConceptInfo> concepts)
        {
            if (!(Extends.Base is IWritableOrmDataStructure))
                throw new DslSyntaxException("ComputeForNewBaseItems can only be used if a persisted data structure extends a writable data structure. Base data structure of type '" + Extends.Base.GetUserDescription() + "' is not supported.");
            
            if (!(Extends.Extension is PersistedDataStructureInfo))
                throw new DslSyntaxException("ComputeForNewBaseItems can only be used if a persisted data structure extends a base entity. Extended data structure of type '" + Extends.Extension.GetUserDescription() + "' is not supported.");
        }
    }
}
