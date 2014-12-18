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
using System.Text;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    public class SqlIndexMultiplePropertyInfo : IConceptInfo, IAlternativeInitializationConcept
    {
        [ConceptKey]
        public SqlIndexMultipleInfo SqlIndex { get; set; }
        [ConceptKey]
        public string Order { get; set; }
        public PropertyInfo Property { get; set; }
        public SqlIndexMultiplePropertyInfo Dependency_PreviousProperty { get; set; } // Used for property ordering when creating the index.

        public void CheckSemantics(IEnumerable<IConceptInfo> concepts)
        {
            DslUtility.CheckIfPropertyBelongsToDataStructure(Property, SqlIndex.Entity, this);
        }

        public IEnumerable<string> DeclareNonparsableProperties()
        {
            return new[] { "Dependency_PreviousProperty" };
        }

        public void InitializeNonparsableProperties(out IEnumerable<IConceptInfo> createdConcepts)
        {
            int order = int.Parse(Order);
            if (order > 0)
                Dependency_PreviousProperty = new SqlIndexMultiplePropertyInfo { SqlIndex = SqlIndex, Order = (order - 1).ToString() };
            else
                Dependency_PreviousProperty = this;
            createdConcepts = null;
        }
    }
}
