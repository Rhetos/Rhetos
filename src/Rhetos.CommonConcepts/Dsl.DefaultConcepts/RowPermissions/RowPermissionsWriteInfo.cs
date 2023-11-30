﻿/*
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

using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// Direct implementation of row permission filter, without extendable rules such as AllowRead and Deny. Alternative to extendable RowPermissions concept.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("RowPermissionsWrite")]
    public class RowPermissionsWriteInfo : QueryFilterExpressionInfo, IAlternativeInitializationConcept
    {
        public static readonly string FilterName = "Common.RowPermissionsWriteItems";
        public static readonly string PermissionsExpressionName = "GetRowPermissionsWriteExpression";

        public string SimplifiedExpression { get; set; }

        public IEnumerable<string> DeclareNonparsableProperties()
        {
            return new[] { "Parameter", "Expression" };
        }

        public void InitializeNonparsableProperties(out IEnumerable<IConceptInfo> createdConcepts)
        {
            Parameter = FilterName;
            Expression = RowPermissionsInfo.CreateComposableFilterSnippet(PermissionsExpressionName, Source);
            createdConcepts = null;
        }
    }
}
