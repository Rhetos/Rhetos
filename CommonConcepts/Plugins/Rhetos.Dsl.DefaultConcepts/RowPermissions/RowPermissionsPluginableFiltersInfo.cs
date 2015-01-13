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
    /// <summary>
    /// The root concept for row permission rules.
    /// It allows combining multiple rules and inheriting rules from one data structure to another.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("RowPermissions")]
    public class RowPermissionsPluginableFiltersInfo : IConceptInfo
    {
        [ConceptKey]
        public DataStructureInfo DataStructure { get; set; }

        public static readonly CsTag<RowPermissionsReadInfo> ReadFilterExpressionsTag = "ReadFilterExpressions";
        public static readonly CsTag<RowPermissionsWriteInfo> WriteFilterExpressionsTag = "WriteFilterExpressions";
    }

    [Export(typeof(IConceptMacro))]
    public class RowPermissionsPluginableFiltersMacro : IConceptMacro<RowPermissionsPluginableFiltersInfo>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(RowPermissionsPluginableFiltersInfo conceptInfo, IDslModel existingConcepts)
        {
            var rowPermissionsRead = new RowPermissionsReadInfo()
            {
                Source = conceptInfo.DataStructure,
                Parameter = RowPermissionsReadInfo.FilterName
            };
            rowPermissionsRead.SimplifiedExpression = GetSnippetRowPermissionsFilter(conceptInfo,
                RowPermissionsPluginableFiltersInfo.ReadFilterExpressionsTag.Evaluate(rowPermissionsRead));

            var rowPermissionsWrite = new RowPermissionsWriteInfo()
            {
                Source = conceptInfo.DataStructure,
                Parameter = RowPermissionsWriteInfo.FilterName
            };
            rowPermissionsWrite.SimplifiedExpression = GetSnippetRowPermissionsFilter(conceptInfo,
                RowPermissionsPluginableFiltersInfo.WriteFilterExpressionsTag.Evaluate(rowPermissionsWrite));

            CheckForIncompatibleSpecificRowPermissionsFilter(existingConcepts, rowPermissionsRead, conceptInfo);
            CheckForIncompatibleSpecificRowPermissionsFilter(existingConcepts, rowPermissionsWrite, conceptInfo);

            return new IConceptInfo[] { rowPermissionsRead, rowPermissionsWrite };
        }

        public string GetSnippetRowPermissionsFilter(RowPermissionsPluginableFiltersInfo conceptInfo, string filtersTag)
        {
            return string.Format(@"(items, repository, executionContext) =>
		{{
            var filterExpression = new FilterExpression<{0}.{1}>();
			{2}
			return filterExpression.GetFilter();
		}}",
                conceptInfo.DataStructure.Module.Name,
                conceptInfo.DataStructure.Name,
                filtersTag);
        }

        // Check if the data structure already contains a specific row permissions filter that is not rule-based. Such filter is not compatible with RowPermissionsPluginableFiltersInfo.
        private void CheckForIncompatibleSpecificRowPermissionsFilter(IDslModel existingConcepts, IConceptInfo newRowPermissionsFilter, RowPermissionsPluginableFiltersInfo conceptInfo)
        {
            IConceptInfo oldRowPermissions = existingConcepts.FindByKey(newRowPermissionsFilter.GetKey());
            if (oldRowPermissions == null)
                return;

            string filterName;
            string newFilterExpression;
            string oldFilterExpression = null;

            if (newRowPermissionsFilter is RowPermissionsReadInfo)
            {
                filterName = RowPermissionsReadInfo.FilterName;
                newFilterExpression = ((RowPermissionsReadInfo)newRowPermissionsFilter).SimplifiedExpression;
                if (oldRowPermissions is RowPermissionsReadInfo)
                    oldFilterExpression = ((RowPermissionsReadInfo)oldRowPermissions).SimplifiedExpression;
            }
            else
            {
                filterName = RowPermissionsWriteInfo.FilterName;
                newFilterExpression = ((RowPermissionsWriteInfo)newRowPermissionsFilter).SimplifiedExpression;
                if (oldRowPermissions is RowPermissionsWriteInfo)
                    oldFilterExpression = ((RowPermissionsWriteInfo)oldRowPermissions).SimplifiedExpression;
            }

            if (oldFilterExpression == null || oldFilterExpression != newFilterExpression)
                throw new DslSyntaxException(conceptInfo, "Cannot use row permissions rules or row permissions inheritance on "
                    + conceptInfo.DataStructure.GetUserDescription() + " because it already contains a specific row permissions filter ("
                    + filterName + ")."
                    + " Use RowPermissions concept instead of the specific filter, to create rules-based row permissions.");
        }
    }
}
