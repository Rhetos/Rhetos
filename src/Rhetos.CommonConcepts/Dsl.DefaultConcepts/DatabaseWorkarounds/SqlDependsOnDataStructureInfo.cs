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

using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// It states that the parent object should be created in database after the referenced table or view, including all columns, is created.
    /// Besides the column, the dependency also includes any unique indexes on the referenced table.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("SqlDependsOn")]
    public class SqlDependsOnDataStructureInfo : IConceptInfo
    {
        [ConceptKey]
        public IConceptInfo Dependent { get; set; }

        [ConceptKey]
        public DataStructureInfo DependsOn { get; set; }
    }

    [Export(typeof(IConceptMacro))]
    public class SqlDependsOnDataStructureMacro : IConceptMacro<SqlDependsOnDataStructureInfo>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(SqlDependsOnDataStructureInfo conceptInfo, IDslModel existingConcepts)
        {
            if (conceptInfo.DependsOn is PolymorphicInfo polymorphic)
                return new[]
                {
                    new SqlDependsOnSqlObjectInfo
                    {
                        Dependent = conceptInfo.Dependent,
                        DependsOn = polymorphic.GetUnionViewPrototype()
                    }
                };
            else
                return null;
        }
    }
}
