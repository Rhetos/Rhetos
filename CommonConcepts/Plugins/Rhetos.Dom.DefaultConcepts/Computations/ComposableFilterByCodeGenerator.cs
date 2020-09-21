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
using Rhetos.Utilities;
using System;
using System.ComponentModel.Composition;
using System.Configuration;
using System.Linq;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(ComposableFilterByInfo))]
    public class ComposableFilterByCodeGenerator : IConceptCodeGenerator
    {
        public static readonly CsTag<ComposableFilterByInfo> AdditionalParametersTypeTag = "AdditionalParametersType";
        public static readonly CsTag<ComposableFilterByInfo> AdditionalParametersArgumentTag = "AdditionalParametersArgument";
        public static readonly CsTag<ComposableFilterByInfo> BeforeFilterTag = "BeforeFilter";
        private readonly CommonConceptsOptions _commonConceptsOptions;

        public ComposableFilterByCodeGenerator(CommonConceptsOptions commonConceptsOptions)
        {
            _commonConceptsOptions = commonConceptsOptions;
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (ComposableFilterByInfo)conceptInfo;
            string queryableType = $"IQueryable<Common.Queryable.{info.Source.Module.Name}_{info.Source.Name}>";

            string filterMethod = GetOptimizedFilterMethod(info, queryableType) ??
            $@"public {queryableType} Filter({queryableType} localSource, {info.Parameter} localParameter)
        {{
            Func<{queryableType}, Common.DomRepository, {info.Parameter}{AdditionalParametersTypeTag.Evaluate(info)}, {queryableType}> filterFunction =
            {info.Expression};

            {BeforeFilterTag.Evaluate(info)}
            return filterFunction(localSource, _domRepository, localParameter{AdditionalParametersArgumentTag.Evaluate(info)});
        }}

        ";

            codeBuilder.InsertCode(filterMethod, RepositoryHelper.RepositoryMembers, info.Source);
        }

        private string GetOptimizedFilterMethod(ComposableFilterByInfo info, string queryableType)
        {
            if (!_commonConceptsOptions.ComposableFilterByOptimizeLambda)
                return null;

            string newLine = "\r\n            ";
            // Extensions that add new arguments to the expression will be ignored, assuming that the additional arguments are not used in the expression body.
            string commentedAdditionalArguments = newLine + "// Suppressing additional expression arguments in optimized ComposableFilterBy format"
                + $" (configuration option {OptionsAttribute.GetConfigurationPath<CommonConceptsOptions>()}:{nameof(CommonConceptsOptions.ComposableFilterByOptimizeLambda)})"
                + newLine + "// " + AdditionalParametersArgumentTag.Evaluate(info)
                + newLine + "// " + AdditionalParametersTypeTag.Evaluate(info)
                + newLine;

            var parsedExpression = new ParsedExpression(info.Expression, null, info, BeforeFilterTag.Evaluate(info) + commentedAdditionalArguments);

            // Parameters 0 and 2 are standard Filter method parameters: input query and filter type. If no other parameters are used in the expression,
            // the expression can be simplified by transforming it directly to the standard Filter method without using lambda expressions
            // (build performance optimization for C# compiler).
            var nonStandardParameters = parsedExpression.ExpressionParameters.Where((p, index) => index != 0 && index != 2).Select(p => p.Identifier.Text).ToList();
            if (nonStandardParameters.Any(parameter => parsedExpression.MethodBody.Contains(parameter)) || parsedExpression.ExpressionParameters.Length < 3)
                return null;

            string parameterSource = parsedExpression.ExpressionParameters[0].Identifier.Text;
            string parameterFilter = parsedExpression.ExpressionParameters[2].Identifier.Text;
            return $@"public {queryableType} Filter({queryableType} {parameterSource}, {info.Parameter} {parameterFilter}){parsedExpression.MethodBody}

        ";
        }
    }
}
