/*
    Copyright (C) 2013 Omega software d.o.o.

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
    public class ModuleAutoSqlDependsOnInfo : IMacroConcept
    {
        [ConceptKey]
        public ModuleInfo Module { get; set; }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            var newConcepts = new List<IConceptInfo>();

            newConcepts.AddRange(existingConcepts.OfType<SqlViewInfo>().Select(ci => new AutoSqlViewDependsOnInfo { Dependent = ci }));
            newConcepts.AddRange(existingConcepts.OfType<SqlFunctionInfo>().Select(ci => new AutoSqlFunctionDependsOnInfo { Dependent = ci }));
            newConcepts.AddRange(existingConcepts.OfType<SqlQueryableInfo>().Select(ci => new AutoSqlQueryableDependsOnInfo { Dependent = ci }));
            newConcepts.AddRange(existingConcepts.OfType<SqlTriggerInfo>().Select(ci => new AutoSqlTriggerDependsOnInfo { Dependent = ci }));
            newConcepts.AddRange(existingConcepts.OfType<SqlProcedureInfo>().Select(ci => new AutoSqlProcedureDependsOnInfo { Dependent = ci }));
            newConcepts.AddRange(existingConcepts.OfType<LegacyEntityInfo>().Select(ci => new AutoLegacyEntityDependsOnInfo { Dependent = ci }));

            return newConcepts;
        }
    }
}