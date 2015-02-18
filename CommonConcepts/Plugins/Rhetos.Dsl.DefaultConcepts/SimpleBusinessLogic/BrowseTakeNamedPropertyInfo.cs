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
using Rhetos.Utilities;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Take")]
    public class BrowseTakeNamedPropertyInfo : IValidatedConcept
    {
        [ConceptKey]
        public BrowseDataStructureInfo Browse { get; set; }

        [ConceptKey]
        public string Name { get; set; }

        public string Path { get; set; }

        public void CheckSemantics(IDslModel existingConcepts)
        {
            DslUtility.ValidatePath(Browse.Source, Path, existingConcepts, this);
            DslUtility.ValidateIdentifier(Name, this, "Specify a valid name before the path, to override the generated name.");
        }
    }

    [Export(typeof(IConceptMacro))]
    public class BrowseTakeNamedPropertyMacro : IConceptMacro<BrowseTakeNamedPropertyInfo>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(BrowseTakeNamedPropertyInfo conceptInfo, IDslModel existingConcepts)
        {
            ValueOrError<PropertyInfo> sourceProperty = DslUtility.GetPropertyByPath(conceptInfo.Browse.Source, conceptInfo.Path, existingConcepts);
            if (sourceProperty.IsError)
                return null; // Creating the browse property may be delayed for other macro concepts to generate the needed properties. If this condition is not resolved, the CheckSemantics function below will throw an exception.

            var browseProperty = DslUtility.CreatePassiveClone(sourceProperty.Value, conceptInfo.Browse);
            browseProperty.Name = conceptInfo.Name;

            var browsePropertySelector = new BrowseFromPropertyInfo { PropertyInfo = browseProperty, Path = conceptInfo.Path };

            return new IConceptInfo[] { browseProperty, browsePropertySelector };
        }
    }
}
