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
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// Writes the current user's ID when saving a new record.
    /// It should be applied on a Reference property that references Common.Principal.
    /// It is often used together with concepts DenyUserEdit and SystemRequired.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("CreatedBy")]
    public class CreatedByInfo : IValidatedConcept
    {
        [ConceptKey]
        public ReferencePropertyInfo Property { get; set; }

        public void CheckSemantics(IDslModel existingConcepts)
        {
            string referenced = Property.Referenced.GetKeyProperties();
            const string commonPrincipal = "Common.Principal";
            if (referenced != commonPrincipal)
                throw new DslSyntaxException(this, "This property must reference '" + commonPrincipal + "' instead of '" + referenced + "'.");
        }
    }
}
