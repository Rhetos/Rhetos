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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Rhetos.Dsl;
using Rhetos.Utilities;
using System;
using System.Linq;

namespace Rhetos.Dom.DefaultConcepts
{
    /// <summary>
    /// Helper class for converting code snippets from Expression format to Method format, and other similar transformations.
    /// This can help optimize compiler time on very large applications, since lambda expressions are more difficult to compile then simple methods.
    /// </summary>
    public class ParsedExpression
    {
        public ExpressionParameter[] ExpressionParameters { get; }

        /// <summary>
        /// Returns null if the argument types are not provided.
        /// </summary>
        public string MethodParameters { get; }

        public string MethodBody { get; }

        /// <summary>
        /// The expression parameters and body formatted as a method.
        /// For example, the expression "text => text.Length" with a string argument will result with "(string text) { return text.Length; }" (line breaks and indentation not shown).
        /// </summary>
        public string MethodParametersAndBody => MethodParameters != null ? MethodParameters + MethodBody: throw new DslSyntaxException(_errorContext, "The argument types are not provided");

        /// <summary>
        /// If the expression's body is a literal value, returns the text representation, otherwise null.
        /// When a literal value is provided, the generated code may use the literal directly, instead of generating and calling a method.
        /// For example, the expression "text => 123" will result with "123".
        /// </summary>
        public string ResultLiteral { get; }

        private readonly string _expression;
        /// <summary>May be null if not provided by the caller.</summary>
        private readonly string[] _argumentTypes;
        private readonly IConceptInfo _errorContext;
        private readonly string _additionalParameters;

        /// <summary>
        /// Converts code snippets from Expression format to Method format.
        /// </summary>
        /// <param name="expression">Lambda expression that will be parsed and converted to a method.</param>
        /// <param name="argumentTypes">
        /// If null, the generated method parameters will not be available.
        /// The <paramref name="argumentTypes"/> parameter will be ignored it the <paramref name="expression"/> has explicitly specified argument types.
        /// </param>
        /// <param name="errorContext">Concept that is referenced in the generated exception, in case of an error.</param>
        /// <param name="insertCode">Additional code that will be inserted before the code from the lambda expression body.</param>
        /// <param name="additionalParameters">Additional method arguments that will be added at the end of the generated method parameters.</param>
        public ParsedExpression(string expression, string[] argumentTypes, IConceptInfo errorContext, string insertCode = null, string additionalParameters = null)
        {
            _expression = expression;
            _argumentTypes = argumentTypes;
            _errorContext = errorContext;
            _additionalParameters = additionalParameters;

            // Note: This parser is not intended to detect all errors in the lambda expression. It would be preferred to use the provided expression as it is,
            // and let the C# compiler detect and report the syntax error in the generated code.

            SyntaxNode lambdaNode = ParseExpression();
            if (lambdaNode is SimpleLambdaExpressionSyntax simpleExpression)
            {
                var parametersSyntax = new[] { simpleExpression.Parameter };
                ExpressionParameters = BuildExpressionParameters(parametersSyntax);
                MethodParameters = BuildMethodParameters(parametersSyntax);
                MethodBody = BuildMethodBody(simpleExpression.Body, insertCode);
                ResultLiteral = TryBuildResultLiteral(simpleExpression.Body);
            }
            else if (lambdaNode is ParenthesizedLambdaExpressionSyntax parenthesizedExpression)
            {
                var parametersSyntax = parenthesizedExpression.ParameterList.Parameters.ToArray();
                ExpressionParameters = BuildExpressionParameters(parametersSyntax);
                MethodParameters = BuildMethodParameters(parametersSyntax, parenthesizedExpression.ParameterList);
                MethodBody = BuildMethodBody(parenthesizedExpression.Body, insertCode);
                ResultLiteral = TryBuildResultLiteral(parenthesizedExpression.Body);
            }
            else
                throw new DslSyntaxException(errorContext, $"Unexpected node type '{lambdaNode.Kind()}' in code snippet '{expression.Limit(200)}'.");
        }

        private SyntaxNode ParseExpression()
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(_expression, new CSharpParseOptions(kind: SourceCodeKind.Script, documentationMode: DocumentationMode.None));
            var errors = tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            if (errors.Any())
                throw new DslSyntaxException(_errorContext, $"C# syntax error '{errors.First()}' in code snippet '{_expression.Limit(200)}'.");

            var compilationNode = tree.GetCompilationUnitRoot();
            CheckExpectedCodeFormat(compilationNode, SyntaxKind.CompilationUnit, SyntaxKind.GlobalStatement);
            var globalNode = compilationNode.ChildNodes().Single();
            CheckExpectedCodeFormat(globalNode, SyntaxKind.GlobalStatement, SyntaxKind.ExpressionStatement);
            var expressionNode = globalNode.ChildNodes().Single();
            CheckExpectedCodeFormat(expressionNode, SyntaxKind.ExpressionStatement, new[] { SyntaxKind.SimpleLambdaExpression, SyntaxKind.ParenthesizedLambdaExpression });// No need to support "delegate" expression format with AnonymousMethodExpressionSyntax.
            var lambdaNode = expressionNode.ChildNodes().Single();
            return lambdaNode;
        }

        private void CheckExpectedCodeFormat(SyntaxNode node, SyntaxKind expectedNodeKind, params SyntaxKind[] expectedChildKinds)
        {
            if (node.Kind() != expectedNodeKind)
                throw new DslSyntaxException(_errorContext, $"The provided code snippet should be formatted as a C# lambda expression." +
                    $" Code snippet '{_expression.Limit(200)}' is '{node.Kind()}' instead of '{expectedNodeKind}'.");

            var childNodes = node.ChildNodes();
            string expectedChildKindsText = string.Join(" or ", expectedChildKinds);

            if (!childNodes.Any())
                throw new DslSyntaxException(_errorContext, $"The provided code snippet should be formatted as a C# lambda expression." +
                    $" Code snippet '{_expression.Limit(200)}' has no content." +
                    $" Expected content type is '{expectedChildKindsText}'.");

            if (childNodes.Count() > 1)
                throw new DslSyntaxException(_errorContext, $"The provided code snippet should be formatted as a C# lambda expression." +
                    $" Code snippet '{_expression.Limit(200)}' contains multiple nodes while only one is expected." +
                    $" Expected child node type is '{expectedChildKindsText}'." +
                    $" The provided snippet contains {childNodes.Count()} child nodes: {string.Join(", ", childNodes.Select(n => n.Kind()))}.");

            if (!expectedChildKinds.Contains(childNodes.Single().Kind()))
                throw new DslSyntaxException(_errorContext, $"The provided code snippet should be formatted as a C# lambda expression." +
                    $" Code snippet '{_expression.Limit(200)}' is '{childNodes.Single().Kind()}' instead of '{expectedChildKindsText}'.");
        }

        private ExpressionParameter[] BuildExpressionParameters(ParameterSyntax[] parametersSyntax)
        {
            if (_argumentTypes != null && _argumentTypes.Length != parametersSyntax.Length)
                throw new DslSyntaxException(_errorContext, $"The provided code snippet should have {_argumentTypes.Length} parameters instead of {parametersSyntax.Length}." +
                    $" Code snippet: '{_expression.Limit(200)}'." +
                    $" Expected parameter types: {string.Join(", ", _argumentTypes)}.");

            var parameters = new ExpressionParameter[parametersSyntax.Length];

            bool useTypesFromExpression = UseTypesFromExpression(parametersSyntax);

            for (int p = 0; p < parametersSyntax.Length; p++)
                parameters[p] = new ExpressionParameter
                {
                    Name = parametersSyntax[p].Identifier.Text,
                    Type = useTypesFromExpression ? parametersSyntax[p].Type.ToString() : _argumentTypes?[p]
                };

            return parameters;
        }

        private static bool UseTypesFromExpression(ParameterSyntax[] parameters) => parameters.All(p => p.Type != null);

        private string BuildMethodParameters(ParameterSyntax[] parameters, ParameterListSyntax originalParameters = null)
        {
            if (_argumentTypes == null)
                return null;

            if (UseTypesFromExpression(parameters) && originalParameters != null)
                return "(" + originalParameters.Parameters.ToFullString().Trim() + (_additionalParameters ?? "") + ")";
            else
                return "(" + string.Join(", ", parameters.Zip(_argumentTypes, (p, at) => $"{at} {p.Identifier.Text}")) + (_additionalParameters ?? "") + ")";
        }

        private string BuildMethodBody(SyntaxNode body, string insertCode)
        {
            if (body is BlockSyntax block)
            {
                string code =
                    block.OpenBraceToken.TrailingTrivia.ToFullString()
                    + block.Statements.ToFullString()
                    + block.CloseBraceToken.LeadingTrivia.ToFullString();

                return $"\r\n        {{{insertCode}\r\n            {code.Trim()}\r\n        }}";
            }
            else
            {
                string code;
                if (body.Parent is SimpleLambdaExpressionSyntax simpleLambda)
                    // Trailing trivia is not used, to avoid hiding the semicolon with a line comment.
                    code = simpleLambda.ArrowToken.TrailingTrivia.ToFullString() + body.GetLeadingTrivia().ToFullString() + body.ToString();
                else
                    code = body.ToFullString();

                return $"\r\n        {{{insertCode}\r\n            return {code.Trim()};\r\n        }}";
            }
        }

        private string TryBuildResultLiteral(SyntaxNode body)
        {
            if (body is LiteralExpressionSyntax literalExpression)
                return literalExpression.Token.Text;
            else
                return null;
        }
    }
}
