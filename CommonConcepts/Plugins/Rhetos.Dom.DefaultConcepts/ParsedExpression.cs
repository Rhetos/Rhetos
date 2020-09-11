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
        /// <summary>
        /// The expression parameters and body formatted as a method.
        /// For example, the expression "text => text.Length" with a string argument will result with "(string text) { return text.Length; }" (line breaks and indentation not shown).
        /// </summary>
        public string MethodParametersAndBody { get; }

        /// <summary>
        /// If the expression's body is a literal value, returns the text representation, otherwise null.
        /// When a literal value is provided, the generated code may use the literal directly, instead of generating and calling a method.
        /// For example, the expression "text => 123" will result with "123".
        /// </summary>
        public string ResultLiteral { get; }

        private readonly string _expression;
        private readonly string[] _argumentTypes;
        private readonly IConceptInfo _errorContext;

        /// <summary>
        /// Converts code snippets from Expression format to Method format.
        /// </summary>
        public ParsedExpression(string expression, string[] argumentTypes, IConceptInfo errorContext, string insertCode = null)
        {
            _expression = expression;
            _argumentTypes = argumentTypes;
            _errorContext = errorContext;

            // Note: This parser is not intended to detect all errors in the lambda expression. It would be preferred to use the provided expression as it is,
            // and let the C# compiler detect and report the syntax error in the generated code.

            SyntaxTree tree = CSharpSyntaxTree.ParseText(expression, new CSharpParseOptions(kind: SourceCodeKind.Script));
            var errors = tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            if (errors.Any())
                throw new DslSyntaxException(errorContext, $"C# syntax error '{errors.First()}' in code snippet '{expression.Limit(200)}'.");

            var compilationNode = tree.GetCompilationUnitRoot();
            CheckExpectedCodeFormat(compilationNode, SyntaxKind.CompilationUnit, SyntaxKind.GlobalStatement);
            var globalNode = compilationNode.ChildNodes().Single();
            CheckExpectedCodeFormat(globalNode, SyntaxKind.GlobalStatement, SyntaxKind.ExpressionStatement);
            var expressionNode = globalNode.ChildNodes().Single();
            CheckExpectedCodeFormat(expressionNode, SyntaxKind.ExpressionStatement, new[] { SyntaxKind.SimpleLambdaExpression, SyntaxKind.ParenthesizedLambdaExpression });// No need to support "delegate" expression format with AnonymousMethodExpressionSyntax.
            var lambdaNode = expressionNode.ChildNodes().Single();

            if (lambdaNode is SimpleLambdaExpressionSyntax simpleExpression)
            {
                var parameters = new[] { simpleExpression.Parameter };
                CheckParameters(parameters);
                string methodParameters = BuildMethodParameters(parameters);
                string methodBody = BuildMethodBody(simpleExpression.Body, insertCode);
                MethodParametersAndBody = methodParameters + methodBody;
                ResultLiteral = TryBuildResultLiteral(simpleExpression.Body);
            }
            else if (lambdaNode is ParenthesizedLambdaExpressionSyntax parenthesizedExpression)
            {
                var parameters = parenthesizedExpression.ParameterList.Parameters.ToArray();
                CheckParameters(parameters);
                string methodParameters = parameters.All(p => p.Type != null)
                    ? parenthesizedExpression.ParameterList.ToString()
                    : BuildMethodParameters(parameters);
                string methodBody = BuildMethodBody(parenthesizedExpression.Body, insertCode);
                MethodParametersAndBody = methodParameters + methodBody;
                ResultLiteral = TryBuildResultLiteral(parenthesizedExpression.Body);
            }
            else
            {
                throw new DslSyntaxException(errorContext, $"Unexpected node type '{lambdaNode.Kind()}' in code snippet '{expression.Limit(200)}'.");
            }
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

        private void CheckParameters(ParameterSyntax[] parameters)
        {
            if (_argumentTypes.Length != parameters.Length)
                throw new DslSyntaxException(_errorContext, $"The provided code snippet should have {_argumentTypes.Length} parameters instead of {parameters.Length}." +
                    $" Code snippet: '{_expression.Limit(200)}'." +
                    $" Expected parameter types: {string.Join(", ", _argumentTypes)}.");
        }

        private string BuildMethodParameters(ParameterSyntax[] parameters)
        {
            return "(" + string.Join(", ", parameters.Zip(_argumentTypes, (p, at) => $"{at} {p.Identifier.Text}")) + ")";
        }

        private string BuildMethodBody(SyntaxNode body, string insertCode)
        {
            if (body.Kind() == SyntaxKind.Block)
            {
                string code = body.ToString().Trim();
                if (!string.IsNullOrEmpty(insertCode))
                {
                    int blockStart = code.IndexOf('{');
                    if (blockStart < 0)
                        throw new DslSyntaxException(_errorContext, $"Unable to insert code '{insertCode.Limit(40)}' at the beginning of code block '{code.Limit(40)}'. Cannot detect the block start marker '{{'.");
                    code = code.Insert(blockStart + 1, insertCode);
                }
                return "\r\n        " + code;
            }
            else
            {
                return $"\r\n        {{{insertCode}\r\n            return {body.ToString().Trim()};\r\n        }}";
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
