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

using Rhetos.Compiler;
using Rhetos.Dsl.DefaultConcepts;
using System;

namespace Rhetos.Dom.DefaultConcepts
{
    public static class RowPermissionsUtility
    {
        public static string CreateRuleExpressionMethod(ICodeBuilder codeBuilder, RowPermissionsSingleFunctionRuleInfo info)
        {
            var target = info.RowPermissionsFilters.DataStructure;
            var queryableType = $"Common.Queryable.{target.Module.Name}_{target.Name}";

            string methodName = $"GetRowPermissionsRule_{info.Name}";
            var methodParameters = new[] { "Common.ExecutionContext" };
            string additionalParameters = $",\r\n            // Additional parameters for backward compatibility, should be removed in future releases:"
                + $"\r\n            IQueryable<{queryableType}> items, Common.DomRepository repository";
            // TODO: Remove additionalParameters in next major release. The lambda expression (code snippet) in row permission concepts has only parameter "context",
            // but developers sometimes used 'repository' variable from the parent method that was accidentally available in the code snippet.
            // The additionalParameters will provide all previously available variables to avoid breaking changes in minor release.

            var parsedExpression = new ParsedExpression(info.FilterExpressionFunction, methodParameters, info, additionalParameters: additionalParameters);
            string filterMethod =
        $@"private Expression<Func<{queryableType}, bool>> {methodName}{parsedExpression.MethodParametersAndBody}

        ";
            codeBuilder.InsertCode(filterMethod, RepositoryHelper.RepositoryMembers, target);
            return methodName;
        }

        public static string GetSnippetFilterExpression(string methodName, bool allowNotDeny)
        {
            return $@"filterExpression.{(allowNotDeny ? "Include" : "Exclude")}({methodName}(executionContext, items, repository));
            ";
        }

        public static string GetInheritSnippet(RowPermissionsInheritFromInfo info, string permissionExpressionName,
            string sameMembersTag, string extensionReferenceTag)
        {
            var source = info.Source;
            var target = info.RowPermissionsFilters.DataStructure;
            string parameterName = string.Concat(target.Name.Substring(0, 1).ToLowerInvariant(), target.Name.AsSpan(1), "Item");

            return
            $@"{{
                // Inheriting row permissions from {source.Module.Name}.{source.Name}:
                var sameMembers = new Tuple<string, string>[] {{ {sameMembersTag} }};
                var parentRepository = _domRepository.{source.Module.Name}.{source.Name};
                var parentRowPermissionsExpression = parentRepository.{permissionExpressionName}(parentRepository.Query(), _domRepository, _executionContext);
                var replacedExpression = new ReplaceWithReference<Common.Queryable.{source.Module.Name}_{source.Name}, Common.Queryable.{target.Module.Name}_{target.Name}>(
                    parentRowPermissionsExpression, ""{info.SourceSelector}"" , ""{parameterName}"", sameMembers {extensionReferenceTag});
                filterExpression.Include(replacedExpression.NewExpression);
            }}
            ";
        }
    }
}
