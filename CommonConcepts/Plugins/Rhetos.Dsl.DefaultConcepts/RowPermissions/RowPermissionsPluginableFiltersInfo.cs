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
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// The root concept for row permission rules.
    /// It allows combining multiple rules and inheriting rules from one data structure to another.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("RowPermissions")]
    public class RowPermissionsPluginableFiltersInfo : IConceptInfo, IValidatedConcept, IAlternativeInitializationConcept
    {
        [ConceptKey]
        public DataStructureInfo DataStructure { get; set; }

        public RowPermissionsReadInfo Dependency_RowPermissionsRead { get; set; }

        public RowPermissionsWriteInfo Dependency_RowPermissionsWrite { get; set; }

        public static readonly CsTag<RowPermissionsReadInfo> ReadFilterExpressionsTag = "ReadFilterExpressions";
        public static readonly CsTag<RowPermissionsWriteInfo> WriteFilterExpressionsTag = "WriteFilterExpressions";

        public IEnumerable<string> DeclareNonparsableProperties()
        {
            return new[] { "Dependency_RowPermissionsRead", "Dependency_RowPermissionsWrite" };
        }

        public void InitializeNonparsableProperties(out IEnumerable<IConceptInfo> createdConcepts)
        {
            var rowPermissionsRead = new RowPermissionsReadInfo()
            {
                Source = DataStructure,
                Parameter = RowPermissionsReadInfo.FilterName
            };
            rowPermissionsRead.SimplifiedExpression = GetSnippetRowPermissionsFilter(ReadFilterExpressionsTag.Evaluate(rowPermissionsRead));

            var rowPermissionsWrite = new RowPermissionsWriteInfo()
            {
                Source = DataStructure,
                Parameter = RowPermissionsWriteInfo.FilterName
            };
            rowPermissionsWrite.SimplifiedExpression = GetSnippetRowPermissionsFilter(WriteFilterExpressionsTag.Evaluate(rowPermissionsWrite));

            Dependency_RowPermissionsRead = rowPermissionsRead;
            Dependency_RowPermissionsWrite = rowPermissionsWrite;

            createdConcepts = new IConceptInfo[] { rowPermissionsRead, rowPermissionsWrite };
        }

        public void CheckSemantics(IDslModel existingConcepts)
        {
            CheckForIncompatibleSpecificRowPermissionsFilter(existingConcepts, Dependency_RowPermissionsRead);
            CheckForIncompatibleSpecificRowPermissionsFilter(existingConcepts, Dependency_RowPermissionsWrite);
        }

        public string GetSnippetRowPermissionsFilter(string filtersTag)
        {
            return string.Format(@"(items, repository, executionContext) =>
		{{
            var filterExpression = new FilterExpression<Common.Queryable.{0}_{1}>();
			{2}
			return filterExpression.GetFilter();
		}}",
                DataStructure.Module.Name,
                DataStructure.Name,
                filtersTag);
        }

        // Check if the data structure already contains a specific row permissions filter that is not rule-based. Such filter is not compatible with RowPermissionsPluginableFiltersInfo.
        private void CheckForIncompatibleSpecificRowPermissionsFilter(IDslModel existingConcepts, IConceptInfo newRowPermissionsFilter)
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
                throw new DslSyntaxException(this, "Cannot use row permissions rules or row permissions inheritance on "
                    + DataStructure.GetUserDescription() + " because it already contains a specific row permissions filter ("
                    + filterName + ")."
                    + " Use RowPermissions concept instead of the specific filter, to create rules-based row permissions.");
        }
    }
}
