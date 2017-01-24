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

using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IDslModelIndex))]
    public class SqlObjectsIndex : IDslModelIndex
    {
        /// <summary>
        /// Contains concepts indexed by the name of the SQL object they generate.
        /// Contains only SQL objects that are important in dependency analysis.
        /// For example: stored procedures are ignored since their dependencies do not dictate order of their creation.
        /// </summary>
        public MultiDictionary<string, IConceptInfo> ConceptsBySqlName { get; private set; }

        public SqlObjectsIndex()
        {
            ConceptsBySqlName = new MultiDictionary<string, IConceptInfo>(StringComparer.InvariantCultureIgnoreCase);
        }

        public void Add(IConceptInfo concept)
        {
            if (concept is DataStructureInfo)
            {
                var dataStructure = (DataStructureInfo)concept;
                ConceptsBySqlName.Add(dataStructure.Module.Name + "." + dataStructure.Name, concept);
            }
            else if (concept is SqlViewInfo)
            {
                var sqlView = (SqlViewInfo)concept;
                ConceptsBySqlName.Add(sqlView.Module.Name + "." + sqlView.Name, concept);
            }
            else if (concept is SqlFunctionInfo)
            {
                var sqlFunction = (SqlFunctionInfo)concept;
                ConceptsBySqlName.Add(sqlFunction.Module.Name + "." + sqlFunction.Name, concept);
            }
            else if (concept is PolymorphicUnionViewInfo)
            {
                var polymorphicUnionView = (PolymorphicUnionViewInfo)concept;
                ConceptsBySqlName.Add(polymorphicUnionView.Module.Name + "." + polymorphicUnionView.Name, concept);
            }
        }
    }
}
