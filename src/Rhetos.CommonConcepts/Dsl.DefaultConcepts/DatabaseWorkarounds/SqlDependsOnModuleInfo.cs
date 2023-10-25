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
using System.Linq;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// This object should be created in database after all database objects in the given module are created.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("SqlDependsOn")]
    public class SqlDependsOnModuleInfo : IConceptInfo
    {
        [ConceptKey]
        public IConceptInfo Dependent { get; set; }
        [ConceptKey]
        public ModuleInfo DependsOn { get; set; }
    }

    [Export(typeof(IConceptMacro))]
    public class SqlDependsOnModuleMacro : IConceptMacro<SqlDependsOnModuleInfo>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(SqlDependsOnModuleInfo conceptInfo, IDslModel existingConcepts)
        {
            var newConcepts = new List<IConceptInfo>();

            newConcepts.AddRange(existingConcepts.FindByReference<DataStructureInfo>(item => item.Module, conceptInfo.DependsOn)
                .Where(item => item != conceptInfo.Dependent)
                .Select(item => new SqlDependsOnDataStructureInfo { Dependent = conceptInfo.Dependent, DependsOn = item }));

            newConcepts.AddRange(existingConcepts.FindByReference<SqlFunctionInfo>(item => item.Module, conceptInfo.DependsOn)
                .Where(item => item != conceptInfo.Dependent)
                .Select(item => new SqlDependsOnSqlFunctionInfo { Dependent = conceptInfo.Dependent, DependsOn = item }));

            newConcepts.AddRange(existingConcepts.FindByType<SqlIndexMultipleInfo>()
                .Where(item => item.DataStructure.Module == conceptInfo.DependsOn)
                .Where(item => item != conceptInfo.Dependent && item.DataStructure != conceptInfo.Dependent)
                .Select(item => new SqlDependsOnSqlIndexInfo { Dependent = conceptInfo.Dependent, DependsOn = item }));

            newConcepts.AddRange(existingConcepts.FindByReference<SqlObjectInfo>(item => item.Module, conceptInfo.DependsOn)
                .Where(item => item != conceptInfo.Dependent)
                .Select(item => new SqlDependsOnSqlObjectInfo { Dependent = conceptInfo.Dependent, DependsOn = item }));

            newConcepts.AddRange(existingConcepts.FindByReference<SqlViewInfo>(item => item.Module, conceptInfo.DependsOn)
                .Where(item => item != conceptInfo.Dependent)
                .Select(item => new SqlDependsOnSqlViewInfo { Dependent = conceptInfo.Dependent, DependsOn = item }));

            return newConcepts;
        }
    }
}
