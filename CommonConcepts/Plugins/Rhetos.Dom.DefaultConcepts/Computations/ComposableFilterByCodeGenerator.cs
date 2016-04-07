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
    [ExportMetadata(MefProvider.Implements, typeof(ComposableFilterByInfo))]
    public class ComposableFilterByCodeGenerator : IConceptCodeGenerator
    {
        public static readonly CsTag<ComposableFilterByInfo> AdditionalParametersTypeTag = "AdditionalParametersType";
        public static readonly CsTag<ComposableFilterByInfo> AdditionalParametersArgumentTag = "AdditionalParametersArgument";
        public static readonly CsTag<ComposableFilterByInfo> BeforeFilterTag = "BeforeFilter";

        private static string FilterImplementationSnippet(ComposableFilterByInfo info)
        {
            return string.Format(
        @"public IQueryable<Common.Queryable.{0}_{1}> Filter(IQueryable<Common.Queryable.{0}_{1}> localSource, {2} localParameter)
        {{
            Func<IQueryable<Common.Queryable.{0}_{1}>, Common.DomRepository, {2}" + AdditionalParametersTypeTag.Evaluate(info) + @", IQueryable<Common.Queryable.{0}_{1}>> filterFunction =
            {3};

            " + BeforeFilterTag.Evaluate(info) + @"
            return filterFunction(localSource, _domRepository, localParameter" + AdditionalParametersArgumentTag.Evaluate(info) + @");
        }}

        ",
            info.Source.Module.Name,
            info.Source.Name,
            info.Parameter,
            info.Expression);
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (ComposableFilterByInfo)conceptInfo;
            codeBuilder.InsertCode(FilterImplementationSnippet(info), RepositoryHelper.RepositoryMembers, info.Source);
        }
    }
}
