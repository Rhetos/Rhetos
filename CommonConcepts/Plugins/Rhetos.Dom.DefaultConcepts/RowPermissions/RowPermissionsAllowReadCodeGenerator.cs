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
using Rhetos.Dsl.DefaultConcepts;
using System.Globalization;
using System.ComponentModel.Composition;
using Rhetos.Extensibility;
using Rhetos.Dsl;
using Rhetos.Compiler;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(RowPermissionsAllowReadInfo))]
    public class RowPermissionsAllowReadCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (RowPermissionsAllowReadInfo)conceptInfo;

            codeBuilder.InsertCode(GetSnippetRuleFilterExpression(info), RowPermissionsPluginableFilterInfo.FilterExpressionsTag, info.RowPermissionsFilter);
        }

        private string GetSnippetRuleFilterExpression(RowPermissionsAllowReadInfo info)
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
				var {2}Function = DomUtility.Function(() =>
                    {3});
				var {2} = {2}Function.Invoke();
				{5}filterExpression.Include({4});
			}}
            ",
                info.RowPermissionsFilter.Source.Module.Name,
                info.RowPermissionsFilter.Source.Name,
                info.Name,
                info.GroupSelector,
                info.PermissionPredicate,
                checkRuleCondition);
        }
    }
}
