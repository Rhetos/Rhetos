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

        public static string GetInheritSnippet(RowPermissionsInheritFromInfo info, string permissionExpressionName,
            string sameMembersTag, string extensionReferenceTag)
        {
            var source = info.Source;
            var target = info.RowPermissionsFilters.DataStructure;

            return
            $@"{{
                var sameMembers = new Tuple<string, string>[] {{ {sameMembersTag} }};
                var parentRepository = executionContext.Repository.{source.Module.Name}.{source.Name};
                var parentRowPermissionsExpression = {source.Module.Name}._Helper.{source.Name}_Repository.{permissionExpressionName}(parentRepository.Query(), repository, executionContext);
                var replacedExpression = new ReplaceWithReference<Common.Queryable.{source.Module.Name}_{source.Name}, Common.Queryable.{target.Module.Name}_{target.Name}>(parentRowPermissionsExpression, ""{info.SourceSelector}"" , ""{ParameterName(target)}"", sameMembers {extensionReferenceTag}).NewExpression;
                filterExpression.Include(replacedExpression);
            }}
            ";
        }

        private static string ParameterName(DataStructureInfo target)
        {
            return target.Name.Substring(0, 1).ToLower() + target.Name.Substring(1) + "Item";
        }
    }
}
