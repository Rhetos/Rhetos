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
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Browse")]
    public class BrowseDataStructureInfo : DataStructureInfo, IValidatedConcept, IMacroConcept
    {
        public DataStructureInfo Source { get; set; }

        public void CheckSemantics(IDslModel concepts)
        {
            var properties = concepts.FindByReference<PropertyInfo>(p => p.DataStructure, this);

            var propertyWithoutSelector = properties
                .Where(p => concepts.FindByReference<BrowseFromPropertyInfo>(bfp => bfp.PropertyInfo, p).Count() == 0)
                .FirstOrDefault();

            if (propertyWithoutSelector != null)
                throw new DslSyntaxException(
                    string.Format("Browse property {0} does not have a source selected. Probably missing '{1}'.",
                        propertyWithoutSelector.GetUserDescription(),
                        ConceptInfoHelper.GetKeywordOrTypeName(typeof(BrowseFromPropertyInfo))));
        }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            return new[] { new DataStructureExtendsInfo { Extension = this, Base = Source } };
        }
    }
}
