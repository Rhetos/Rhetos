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

using System.ComponentModel.Composition;
using System.Collections.Generic;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("RelatedItem")]
    public class LoggingRelatedItemInfo : IConceptInfo, IValidationConcept
    {
        [ConceptKey]
        public EntityLoggingInfo Logging { get; set; }

        /// <summary>Related entity's table name with schema</summary>
        [ConceptKey]
        public string Table { get; set; }

        /// <summary>GUID column that references the related instance.</summary>
        [ConceptKey]
        public string Column { get; set; }

        /// <summary>Describes what is the logged entity (Logging.Entity) to the related instance (Table).</summary>
        [ConceptKey]
        public string Relation { get; set; }

        public void CheckSemantics(IEnumerable<IConceptInfo> existingConcepts)
        {
            if (string.IsNullOrWhiteSpace(Relation))
                throw new DslSyntaxException(this, "Property 'Relation' must not be empty.");
        }
    }
}
