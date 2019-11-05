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
using System.Reflection;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("AutoCodeForEachCached")]
    public class AutoCodeForEachCachedInfo : AutoCodeCachedInfo, IValidatedConcept
    {
        public PropertyInfo Group { get; set; }

        protected override IConceptInfo CreateUniqueConstraint()
        {
            return new UniqueMultiplePropertiesInfo
            {
                DataStructure = Property.DataStructure,
                PropertyNames = $"{Group.Name} {Property.Name}"
            };
        }

        public void CheckSemantics(IDslModel existingConcepts)
        {
            DslUtility.CheckIfPropertyBelongsToDataStructure(Group, Property.DataStructure, this);
        }
    }
}
