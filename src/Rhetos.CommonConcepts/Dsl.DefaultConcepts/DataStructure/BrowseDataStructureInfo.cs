﻿/*
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
    /// Data structure used for simple data queries, when we only need to select some properties from an entity and other referenced data structures.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Browse")]
    public class BrowseDataStructureInfo : DataStructureInfo, IValidatedConcept, IMacroConcept
    {
        public DataStructureInfo Source { get; set; }

        public new void CheckSemantics(IDslModel existingConcepts)
        {
            base.CheckSemantics(existingConcepts);

            var properties = existingConcepts.FindByReference<PropertyInfo>(p => p.DataStructure, this);

            var propertyWithoutSelector = properties
                .FirstOrDefault(p => !existingConcepts.FindByReference<BrowseFromPropertyInfo>(bfp => bfp.PropertyInfo, p).Any());

            if (propertyWithoutSelector != null)
                throw new DslSyntaxException(
                    string.Format("Browse property {0} does not have a source selected. Probably missing '{1}'.",
                        propertyWithoutSelector.GetUserDescription(),
                        ConceptInfoHelper.GetKeywordOrTypeName(typeof(BrowseFromPropertyInfo))));
        }

        public IEnumerable<IConceptInfo> CreateNewConcepts()
        {
            return new[] { new DataStructureExtendsInfo { Extension = this, Base = Source } };
        }
    }
}
