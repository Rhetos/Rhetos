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
using System.ComponentModel.Composition;
using Rhetos.Utilities;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// Includes the entity's property in temporal data management.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("History")]
    public class EntityHistoryPropertyInfo : IMacroConcept, IAlternativeInitializationConcept
    {
        [ConceptKey]
        public PropertyInfo Property { get; set; }

        public EntityHistoryInfo Dependency_EntityHistory { get; set; }

        public SqlFunctionInfo Dependency_AtTimeSqlFunction { get; set; }
        public SqlQueryableInfo Dependency_HistorySqlQueryable { get; set; }
        public WriteInfo Dependency_Write { get; set; }


        public IEnumerable<string> DeclareNonparsableProperties()
        {
            return new[] { "Dependency_EntityHistory", "Dependency_AtTimeSqlFunction", "Dependency_HistorySqlQueryable", "Dependency_Write" };
        }

        public void InitializeNonparsableProperties(out IEnumerable<IConceptInfo> createdConcepts)
        {
            if (!(Property.DataStructure is EntityInfo))
                throw new DslSyntaxException(this, "History concept may only be used on entity or its property.");
            Dependency_EntityHistory = new EntityHistoryInfo {Entity = (EntityInfo) Property.DataStructure};
            Dependency_AtTimeSqlFunction = new SqlFunctionInfo {Module = Property.DataStructure.Module, Name = Property.DataStructure.Name + "_AtTime"};
            Dependency_HistorySqlQueryable = new SqlQueryableInfo {Module = Property.DataStructure.Module, Name = Property.DataStructure.Name + "_History"};
            Dependency_Write = new WriteInfo {DataStructure = Dependency_HistorySqlQueryable};

            createdConcepts = new IConceptInfo[] { Dependency_EntityHistory };
        }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            return new [] { new PropertyFromInfo { Destination = Dependency_EntityHistory.Dependency_ChangesEntity, Source = Property } };
        }

    }
}
