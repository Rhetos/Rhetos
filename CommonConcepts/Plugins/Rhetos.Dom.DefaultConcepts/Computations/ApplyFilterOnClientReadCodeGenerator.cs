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
using System.Globalization;
using System.ComponentModel.Composition;
using Microsoft.CSharp.RuntimeBinder;
using Rhetos.Utilities;
using Rhetos.Compiler;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Dsl;
using Rhetos.Extensibility;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(ApplyFilterOnClientReadWhereInfo))]
    public class ApplyFilterOnClientReadCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (ApplyFilterOnClientReadWhereInfo)conceptInfo;

            string dataStructureSnippet = CsUtility.QuotedString(info.DataStructure.GetKeyProperties());
            string filterNameSnippet = CsUtility.QuotedString(info.FilterName);
            string whereSnippet = string.IsNullOrEmpty(info.Where) ? "" : (", " + info.Where);

            string snippet = $"{{ {dataStructureSnippet}, {filterNameSnippet}{whereSnippet} }},\r\n            ";

            codeBuilder.InsertCode(snippet, ModuleCodeGenerator.ApplyFiltersOnClientReadTag);
        }
    }
}
