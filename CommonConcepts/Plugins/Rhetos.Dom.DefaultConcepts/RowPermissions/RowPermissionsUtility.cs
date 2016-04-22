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

using Rhetos.Dsl.DefaultConcepts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhetos.Dsl;
using Rhetos.Compiler;

namespace Rhetos.Dom.DefaultConcepts
{
    public static class RowPermissionsUtility
    {
        public static string GetSnippetFilterExpression(RowPermissionsSingleFunctionRuleInfo info, bool allowNotDeny)
        {
            return string.Format(
            @"{{
                // {4}
				Func<Common.ExecutionContext, Expression<Func<Common.Queryable.{0}_{1}, bool>>> getRuleFilter =
                    {2};
				Expression<Func<Common.Queryable.{0}_{1}, bool>> ruleFilter = getRuleFilter.Invoke(executionContext);
				filterExpression.{3}(ruleFilter);
			}}
            ",
                info.RowPermissionsFilters.DataStructure.Module.Name,
                info.RowPermissionsFilters.DataStructure.Name,
                info.FilterExpressionFunction,
                allowNotDeny ? "Include" : "Exclude",
                info.Name);
        }

        public static string GetInheritSnippet(RowPermissionsInheritFromInfo info, string permissionExpressionName, string sameMembersTag)
        {
            return string.Format(
            @"{{
                var sameMembers = new Tuple<string, string>[] {{ " + sameMembersTag + @" }};
                var parentRepository = executionContext.Repository.{0}.{1};
                var parentRowPermissionsExpression = {0}._Helper.{1}_Repository.{2}(parentRepository.Query(), repository, executionContext);
                var replacedExpression = new ReplaceWithReference<Common.Queryable.{0}_{1}, Common.Queryable.{3}_{4}>(parentRowPermissionsExpression, ""{5}"" , ""{6}"", sameMembers).NewExpression;
                filterExpression.Include(replacedExpression);
            }}
            ",
                info.Source.Module,
                info.Source.Name,
                permissionExpressionName,
                info.RowPermissionsFilters.DataStructure.Module.Name,
                info.RowPermissionsFilters.DataStructure.Name,
                info.SourceSelector,
                info.RowPermissionsFilters.DataStructure.Name.ToLower() + "Item");
        }
    }
}
