﻿/*
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

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Log")]
    public class PropertyLoggingInfo : IConceptInfo, IValidationConcept
    {
        [ConceptKey]
        public EntityLoggingInfo EntityLogging { get; set; }

        [ConceptKey]
        public PropertyInfo Property { get; set; }

        public override string ToString()
        {
            return "Property Logging: " + Property;
        }

        public override int GetHashCode()
        {
            return Property.GetHashCode();
        }

        public void CheckSemantics(IEnumerable<IConceptInfo> concepts)
        {
            if (EntityLogging.Entity != Property.DataStructure)
                throw new DslSyntaxException(string.Format(
                    "Logging on entity {0}.{1} cannot use property form another entity: {2}.{3}.{4}.",
                    EntityLogging.Entity.Module.Name,
                    EntityLogging.Entity.Name,
                    Property.DataStructure.Module.Name,
                    Property.DataStructure.Name,
                    Property.Name));
        }
    }
}
