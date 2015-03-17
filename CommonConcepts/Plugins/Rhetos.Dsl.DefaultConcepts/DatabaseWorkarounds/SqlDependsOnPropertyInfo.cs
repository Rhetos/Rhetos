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
    [ConceptKeyword("SqlDependsOn")]
    public class SqlDependsOnPropertyInfo : IConceptInfo
    {
        [ConceptKey]
        public IConceptInfo Dependent { get; set; }
        [ConceptKey]
        public PropertyInfo DependsOn { get; set; }
    }

    [Export(typeof(IConceptMacro))]
    public class SqlDependsOnPropertyMacro : IConceptMacro<SqlDependsOnPropertyInfo>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(SqlDependsOnPropertyInfo conceptInfo, IDslModel existingConcepts)
        {
            return existingConcepts.FindByReference<UniqueMultiplePropertiesInfo>(unique => unique.DataStructure, conceptInfo.DependsOn.DataStructure)
                .Where(unique => unique.SqlImplementation() && IsFirstIdentifierInList(conceptInfo.DependsOn.Name, unique.PropertyNames))
                .Select(unique => new SqlDependsOnSqlIndexInfo { Dependent = conceptInfo.Dependent, DependsOn = unique });
        }

        private static bool IsFirstIdentifierInList(string identifier, string list)
        {
            if (!list.StartsWith(identifier))
                return false;
            char next = list.Skip(identifier.Length).FirstOrDefault();
            if (next >= 'a' && next <= 'z' || next >= 'A' && next <= 'Z' || next >= '0' && next <= '9' || next == '_')
                return false;
            return true;
        }
    }
}
