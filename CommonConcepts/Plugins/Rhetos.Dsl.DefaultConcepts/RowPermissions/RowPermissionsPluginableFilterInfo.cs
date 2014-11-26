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
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("RowPermissions")]
    public class RowPermissionsPluginableFilterInfo : RowPermissionsInfo, IAlternativeInitializationConcept
    {
        public static readonly CsTag<RowPermissionsPluginableFilterInfo> FilterExpressionsTag = "FilterExpressions";

        public new IEnumerable<string> DeclareNonparsableProperties()
        {
            return base.DeclareNonparsableProperties().Concat(
                new[] { "SimplifiedExpression" });
        }

        public new void InitializeNonparsableProperties(out IEnumerable<IConceptInfo> createdConcepts)
        {
            Parameter = RowPermissionsInfo.FilterName; // Initialize key property, needed for FilterExpressionsTag.
            SimplifiedExpression = GetSnippetRowPermissionsFilter();
            base.InitializeNonparsableProperties(out createdConcepts); // Initialize filter expression.
        }

        public string GetSnippetRowPermissionsFilter()
        {
            return string.Format(@"(items, repository, context) =>
		{{
            var filterExpression = new FilterExpression<{0}.{1}>();
			{2}
			return filterExpression.Filter(items);
		}}",
                Source.Module.Name,
                Source.Name,
                FilterExpressionsTag.Evaluate(this));
        }
    }
}
