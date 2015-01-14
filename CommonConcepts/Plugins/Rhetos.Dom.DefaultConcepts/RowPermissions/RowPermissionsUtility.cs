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

namespace Rhetos.Dom.DefaultConcepts
{
    public static class RowPermissionsUtility
    {
        public static string GetSnippetFilterExpression(RowPermissionsStandardRuleInfo info, bool allowNotDeny)
        {
            string checkRuleCondition;

            if (!string.IsNullOrEmpty(info.Condition))
                checkRuleCondition = string.Format(
                @"Func<bool> ruleCondition = () => {0};
				if (ruleCondition.Invoke())
					",
                    info.Condition);
            else
                checkRuleCondition = "";

            return string.Format(
            @"{{
				var {2}Function = Function<Common.ExecutionContext>.Create({3});
				var {2} = {2}Function.Invoke(executionContext);
				{5}filterExpression.{6}({4});
			}}
            ",
                info.RowPermissionsFilters.DataStructure.Module.Name,
                info.RowPermissionsFilters.DataStructure.Name,
                info.Name,
                info.GroupSelector,
                info.PermissionPredicate,
                checkRuleCondition,
                allowNotDeny ? "Include" : "Exclude");
        }

        public static string GetInheritSnippet(RowPermissionsInheritFromInfo info, string permissionExpressionName)
        {
            return string.Format(
            @"{{
                var parentRepository = executionContext.Repository.{0}.{1};
                var parentRowPermissionsExpression = {0}._Helper.{1}_Repository.{2}(parentRepository.Query(), repository, executionContext);
                var replacedExpression = new ReplaceWithReference<{0}.{1}, {3}>(parentRowPermissionsExpression, ""{4}"" , ""{5}"").NewExpression;
                filterExpression.Include(replacedExpression);
            }}
            ",
                info.Source.Module,
                info.Source.Name,
                permissionExpressionName,
                info.RowPermissionsFilters.DataStructure.GetKeyProperties(),
                info.SourceSelector,
                info.RowPermissionsFilters.DataStructure.Name.ToLower() + "Item");
        }
    }
}
