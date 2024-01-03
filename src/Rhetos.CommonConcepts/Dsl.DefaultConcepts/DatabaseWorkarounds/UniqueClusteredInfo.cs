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
    /// Marks the index as clustered.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Clustered")]
    public class UniqueClusteredInfo : SqlIndexClusteredInfo, IAlternativeInitializationConcept
    {
        public UniqueMultiplePropertiesInfo Unique { get; set; }

        public IEnumerable<string> DeclareNonparsableProperties()
        {
            return new[] { nameof(SqlIndex) };
        }

        public void InitializeNonparsableProperties(out IEnumerable<IConceptInfo> createdConcepts)
        {
            if (Unique.Dependency_SqlIndex == null)
                throw new FrameworkException($"{nameof(UniqueMultiplePropertiesInfo)} has not been initialized.");
            SqlIndex = Unique.Dependency_SqlIndex;
            createdConcepts = null;
        }
    }
}
