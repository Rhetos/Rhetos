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

using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// Currency value property, limited to 2 decimals in database.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Money")]
    public class MoneyPropertyInfo : PropertyInfo, IConceptInfo, IAlternativeInitializationConcept
    {
        public MoneyRoundingInfo MoneyRoundingInfo_Dependency { get; set; }

        public IEnumerable<string> DeclareNonparsableProperties()
        {
            yield return nameof(MoneyRoundingInfo_Dependency);
        }

        public void InitializeNonparsableProperties(out IEnumerable<IConceptInfo> createdConcepts)
        {
            MoneyRoundingInfo_Dependency = new MoneyRoundingInfo { DataStructure = DataStructure };
            createdConcepts = new[] { MoneyRoundingInfo_Dependency };
        }
    }

    /// <summary>
    /// Internal concept to make sure the money rounding logic
    /// is yielded ONLY once in the corresponding data structure.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    public class MoneyRoundingInfo : IConceptInfo
    {
        [ConceptKey]
        public DataStructureInfo DataStructure { get; set; }
    }
}
