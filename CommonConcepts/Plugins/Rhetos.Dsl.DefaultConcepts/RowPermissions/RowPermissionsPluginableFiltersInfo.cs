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
    public class RowPermissionsPluginableFiltersInfo : IAlternativeInitializationConcept
    {
        [ConceptKey]
        public DataStructureInfo DataStructure { get; set; }
        public RowPermissionsReadInfo Dependency_RowPermissionsRead { get; set; }
        public RowPermissionsWriteInfo Dependency_RowPermissionsWrite { get; set; }

        public static readonly CsTag<RowPermissionsPluginableFiltersInfo> ReadFilterExpressionsTag = "ReadFilterExpressions";
        public static readonly CsTag<RowPermissionsPluginableFiltersInfo> WriteFilterExpressionsTag = "WriteFilterExpressions";


        public IEnumerable<string> DeclareNonparsableProperties()
        {
            return new[] { "Dependency_RowPermissionsRead", "Dependency_RowPermissionsWrite" };
        }

        public void InitializeNonparsableProperties(out IEnumerable<IConceptInfo> createdConcepts)
        {
            Dependency_RowPermissionsRead = new RowPermissionsReadInfo() { SimplifiedExpression = GetSnippetRowPermissionsFilter(ReadFilterExpressionsTag), Source = DataStructure };
            Dependency_RowPermissionsWrite = new RowPermissionsWriteInfo() { SimplifiedExpression = GetSnippetRowPermissionsFilter(WriteFilterExpressionsTag), Source = DataStructure };

            createdConcepts = new IConceptInfo[] { Dependency_RowPermissionsRead, Dependency_RowPermissionsWrite };
        }

        public string GetSnippetRowPermissionsFilter(CsTag<RowPermissionsPluginableFiltersInfo> filterExpressionTag)
        {
            return string.Format(@"(items, repository, context) =>
		{{
            var filterExpression = new FilterExpression<{0}.{1}>();
			{2}
			return filterExpression.Filter(items);
		}}",
                DataStructure.Module.Name,
                DataStructure.Name,
                filterExpressionTag.Evaluate(this));
        }
    }
}
