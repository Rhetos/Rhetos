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
using Rhetos.Utilities;
using System.ComponentModel.Composition;
using Newtonsoft.Json.Linq;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// Implement a property by a hardcoded entity entry
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Implements")]
    public class SubtypeImplementsReferenceToHardcodedEntityInfo : IConceptInfo
    {
        [ConceptKey]
        public IsSubtypeOfInfo IsSubtypeOf { get; set; }

        [ConceptKey]
        public PropertyInfo Property { get; set; }

        public EntryInfo Entry { get; set; }
    }

    [Export(typeof(IConceptMacro))]
    public class SubtypeImplementsReferenceToHardcodedEntityMacro : IConceptMacro<SubtypeImplementsReferenceToHardcodedEntityInfo>
    {
        private readonly ISqlUtility _sqlUtility;

        public SubtypeImplementsReferenceToHardcodedEntityMacro(ISqlUtility sqlUtility)
        {
            _sqlUtility = sqlUtility;
        }

        public IEnumerable<IConceptInfo> CreateNewConcepts(SubtypeImplementsReferenceToHardcodedEntityInfo conceptInfo, IDslModel existingConcepts)
        {
            return new List<IConceptInfo>
            {
                new SubtypeImplementsPropertyInfo { IsSubtypeOf = conceptInfo.IsSubtypeOf, Property = conceptInfo.Property, Expression = _sqlUtility.QuoteGuid(conceptInfo.Entry.GetIdentifier()) }
            };
        }
    }
}
