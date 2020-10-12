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
using System.Linq;
using System.Text.RegularExpressions;

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
        private readonly IDslModel _dslModel;

        public ComposableFilterByCodeGenerator(CommonConceptsOptions commonConceptsOptions, IDslModel dslModel)
        {
            _commonConceptsOptions = commonConceptsOptions;
            _dslModel = dslModel;
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

        private static readonly string _suppressAdditionalArgumentsMessage = "// Suppressing additional expression arguments in optimized ComposableFilterBy format"
            + $" (configuration option {OptionsAttribute.GetConfigurationPath<CommonConceptsOptions>()}:{nameof(CommonConceptsOptions.ComposableFilterByOptimizeLambda)})";

        private string GetOptimizedFilterMethod(ComposableFilterByInfo info, string queryableType)
        {
            if (!_commonConceptsOptions.ComposableFilterByOptimizeLambda)
                return null;

            string newLine = "\r\n            ";
            // Extensions that add new arguments to the expression will be ignored, assuming that the additional arguments are not used in the expression body.
            string commentedAdditionalArguments = newLine + _suppressAdditionalArgumentsMessage
                + newLine + "// " + AdditionalParametersArgumentTag.Evaluate(info)
                + newLine + "// " + AdditionalParametersTypeTag.Evaluate(info)
                + newLine;

            var parsedExpression = new ParsedExpression(info.Expression, null, info, BeforeFilterTag.Evaluate(info) + commentedAdditionalArguments);
            if (parsedExpression.ExpressionParameters.Length < 3)
                return null;

            string parameterSource = parsedExpression.ExpressionParameters[0].Name;
            string parameterRepository = parsedExpression.ExpressionParameters[1].Name;
            string parameterFilter = parsedExpression.ExpressionParameters[2].Name;

            // Trying to remove usage of expression arguments other then input source and filter parameters.
            var simplifiedMethodBody = parsedExpression.MethodBody;
            if (_commonConceptsOptions.ComposableFilterByOptimizeRepositoryAndContextUsage)
            {
                var repositoryRegex = new Regex($@"\b{parameterRepository}([\.,\)])");
                simplifiedMethodBody = repositoryRegex.Replace(simplifiedMethodBody, "_domRepository$1");

                if (parsedExpression.ExpressionParameters.Length >= 4
                    && _dslModel.FindByKey($"{nameof(ComposableFilterUseExecutionContextInfo)} {info.GetKeyProperties()}") != null)
                {
                    string parameterContext = parsedExpression.ExpressionParameters[3].Name;
                    if (parameterContext.Contains("context") || parameterContext.Contains("Context"))
                    {
                        var contextRegex = new Regex($@"\b{parameterContext}([\.,\)])");
                        simplifiedMethodBody = contextRegex.Replace(simplifiedMethodBody, "_executionContext$1");
                    }
                }
            }

            // Parameters 0 and 2 are standard Filter method parameters: input query and filter type. If no other parameters are used in the expression,
            // the expression can be simplified by transforming it directly to the standard Filter method without using lambda expressions
            // (build performance optimization for C# compiler).
            var nonStandardParameters = parsedExpression.ExpressionParameters.Where((p, index) => index != 0 && index != 2).Select(p => p.Name).ToList();
            if (nonStandardParameters.Any(parameter => new Regex($@"\b{parameter}\b").IsMatch(simplifiedMethodBody)))
                return null;
            else
                return $@"public {queryableType} Filter({queryableType} {parameterSource}, {info.Parameter} {parameterFilter}){simplifiedMethodBody}

        ";
        }
    }
}
