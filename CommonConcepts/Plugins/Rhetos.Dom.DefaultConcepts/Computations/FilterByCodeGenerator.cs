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
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Text;
using Rhetos.Utilities;
using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(FilterByInfo))]
    public class FilterByCodeGenerator : IConceptCodeGenerator
    {
        public static readonly CsTag<FilterByInfo> AdditionalParametersTypeTag = "AdditionalParametersType";
        public static readonly CsTag<FilterByInfo> AdditionalParametersArgumentTag = "AdditionalParametersArgument";

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (FilterByInfo)conceptInfo;

            string filterImplementationSnippet =
        $@"public global::{info.Source.Module}.{info.Source.Name}[] Filter({info.Parameter} filter_Parameter)
        {{
            Func<Common.DomRepository, {info.Parameter}{AdditionalParametersTypeTag.Evaluate(info)}, {info.Source.Module}.{info.Source.Name}[]> filter_Function =
                {info.Expression};

            return filter_Function(_domRepository, filter_Parameter{AdditionalParametersArgumentTag.Evaluate(info)});
        }}

        ";

            codeBuilder.InsertCode(filterImplementationSnippet, RepositoryHelper.RepositoryMembers, info.Source);
        }
    }
}
