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
using System.ComponentModel.Composition;
using Rhetos.Utilities;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// Client application is not allowed to directly insert or update the property.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("DenyUserEdit")]
    public class DenyUserEditPropertyInfo : IValidatedConcept, IMacroConcept
    {
        [ConceptKey]
        public PropertyInfo Property { get; set; }

        public void CheckSemantics(IDslModel existingConcepts)
        {
            if (!(Property.DataStructure is IWritableOrmDataStructure))
                throw new DslConceptSyntaxException(this, this.GetKeywordOrTypeName() + " may only be used on a writable data structure, such as an Entity.");
        }

        public IEnumerable<IConceptInfo> CreateNewConcepts()
        {
            yield return new DataStructureLocalizerInfo { DataStructure = Property.DataStructure };
        }
    }
}
