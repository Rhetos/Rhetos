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
    /// This class is a helper for implementing row permissions rules that are based on a single function that returns the rule filter expression.
    /// Other types if row permissions rules are possible, that do not inherit this class (see RowPermissionsRuleInfo).
    /// </summary>
    [Export(typeof(IConceptInfo))]
    public class RowPermissionsSingleFunctionRuleInfo : RowPermissionsRuleInfo, IAlternativeInitializationConcept
    {
        /// <summary>
        /// A lambda expression that returns the rule filter.
        /// Expected type: Func&lt;Common.ExecutionContext, Expression&lt;Func&lt;TEntity, bool&gt;&gt;&gt;.
        /// </summary>
        public string FilterExpression { get; set; }

        public RowPermissionsReadInfo Dependency_RowPermissionsRead { get; set; } // Dependency for the code generator.
        public RowPermissionsWriteInfo Dependency_RowPermissionsWrite { get; set; } // Dependency for the code generator.

        public IEnumerable<string> DeclareNonparsableProperties()
        {
            return new[] { "Dependency_RowPermissionsRead", "Dependency_RowPermissionsWrite" };
        }

        public void InitializeNonparsableProperties(out IEnumerable<IConceptInfo> createdConcepts)
        {
            Dependency_RowPermissionsRead = new RowPermissionsReadInfo
            {
                Source = RowPermissionsFilters.DataStructure,
                Parameter = RowPermissionsReadInfo.FilterName,
            };
            Dependency_RowPermissionsWrite = new RowPermissionsWriteInfo
            {
                Source = RowPermissionsFilters.DataStructure,
                Parameter = RowPermissionsWriteInfo.FilterName,
            };
            createdConcepts = null;
        }
    }
}
