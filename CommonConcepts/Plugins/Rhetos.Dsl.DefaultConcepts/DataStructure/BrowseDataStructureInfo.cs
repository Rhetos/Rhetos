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
using System.Linq;
using System.Text;
using Rhetos.Dsl;
using System.ComponentModel.Composition;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Browse")]
    public class BrowseDataStructureInfo : DataStructureInfo, IValidationConcept, IMacroConcept
    {
        public DataStructureInfo Source { get; set; }

        public void CheckSemantics(IEnumerable<IConceptInfo> concepts)
        {
            if (Module.Name != Source.Module.Name)
                throw new DslSyntaxException(
                    string.Format("Browse should be created in same module as referenced entity. Expecting {0} instead of {1}.",
                        Source.Module,
                        Module));

            var propertiesWithSelector = new HashSet<string>(
                concepts.OfType<BrowseFromPropertyInfo>()
                    .Where(sp => sp.PropertyInfo.DataStructure == this)
                    .Select(sp => sp.PropertyInfo.Name));

            var propertyWithoutSelector = concepts.OfType<PropertyInfo>()
                .Where(p => p.DataStructure == this)
                .Where(p => !propertiesWithSelector.Contains(p.Name))
                .FirstOrDefault();

            if (propertyWithoutSelector != null)
                throw new DslSyntaxException(
                    string.Format("Browse property {0} does not have a source selected. Probably missing '{1}'.",
                        propertyWithoutSelector.GetUserDescription(),
                        ConceptInfoHelper.GetKeywordOrTypeName(typeof(BrowseFromPropertyInfo))));
        }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            return new[] {new DataStructureExtendsInfo {Extension = this, Base = Source}};
        }
    }
}
