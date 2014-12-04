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

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("AutodetectSqlDependencies")]
    public class ModuleAutoSqlDependsOnInfo : IConceptInfo
    {
        [ConceptKey]
        public ModuleInfo Module { get; set; }
    }

    [Export(typeof(IConceptMacro))]
    [ConceptKeyword("AutodetectSqlDependencies")]
    public class ModuleAutoSqlDependsOnMacro : IConceptMacro<ModuleAutoSqlDependsOnInfo>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(ModuleAutoSqlDependsOnInfo conceptInfo, IDslModel existingConcepts)
        {
            var newConcepts = new List<IConceptInfo>();

            newConcepts.AddRange(existingConcepts.FindByType<SqlViewInfo>().Select(ci => new AutoSqlViewDependsOnInfo { Dependent = ci }));
            newConcepts.AddRange(existingConcepts.FindByType<SqlFunctionInfo>().Select(ci => new AutoSqlFunctionDependsOnInfo { Dependent = ci }));
            newConcepts.AddRange(existingConcepts.FindByType<SqlQueryableInfo>().Select(ci => new AutoSqlQueryableDependsOnInfo { Dependent = ci }));
            newConcepts.AddRange(existingConcepts.FindByType<SqlTriggerInfo>().Select(ci => new AutoSqlTriggerDependsOnInfo { Dependent = ci }));
            newConcepts.AddRange(existingConcepts.FindByType<SqlProcedureInfo>().Select(ci => new AutoSqlProcedureDependsOnInfo { Dependent = ci }));
            newConcepts.AddRange(existingConcepts.FindByType<LegacyEntityInfo>().Select(ci => new AutoLegacyEntityDependsOnInfo { Dependent = ci }));

            return newConcepts;
        }
    }
}