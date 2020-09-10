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
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using System.ComponentModel.Composition;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(ComposableFilterByInfo))]
    public class ComposableFilterByCodeGenerator : IConceptCodeGenerator
    {
        public static readonly CsTag<ComposableFilterByInfo> AdditionalParametersTypeTag = "AdditionalParametersType";
        public static readonly CsTag<ComposableFilterByInfo> AdditionalParametersArgumentTag = "AdditionalParametersArgument";
        public static readonly CsTag<ComposableFilterByInfo> BeforeFilterTag = "BeforeFilter";

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (ComposableFilterByInfo)conceptInfo;
            string queryableType = $"IQueryable<Common.Queryable.{info.Source.Module.Name}_{info.Source.Name}>";

            string filter =
        $@"public {queryableType} Filter({queryableType} localSource, {info.Parameter} localParameter)
        {{
            Func<{queryableType}, Common.DomRepository, {info.Parameter}{AdditionalParametersTypeTag.Evaluate(info)}, {queryableType}> filterFunction =
            {info.Expression};

            {BeforeFilterTag.Evaluate(info)}
            return filterFunction(localSource, _domRepository, localParameter{AdditionalParametersArgumentTag.Evaluate(info)});
        }}

        ";

            codeBuilder.InsertCode(filter, RepositoryHelper.RepositoryMembers, info.Source);
        }
    }
}
