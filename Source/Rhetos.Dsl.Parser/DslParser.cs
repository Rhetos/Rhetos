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
using System.Diagnostics;
using System.Linq;
using System.Text;
using Rhetos.Utilities;
using Rhetos.Logging;

namespace Rhetos.Dsl
{
    /// <summary>
    /// Performs the syntax analysis of DSL scripts, after the scripts are converted to a list of tokens.
    /// </summary>
    public class DslParser : IDslParser
    {
        private readonly Tokenizer _tokenizer;
        private readonly DslSyntax _syntax;
        private readonly ILogger _keywordsLogger;
        private readonly ILogger _performanceLogger;
        private readonly ILogger _logger;

        public DslParser(Tokenizer tokenizer, DslSyntax dslSyntax, ILogProvider logProvider)
        {
            _tokenizer = tokenizer;
            _syntax = dslSyntax;
            _keywordsLogger = logProvider.GetLogger("DslParser.Keywords"); // Legacy logger name.
            _performanceLogger = logProvider.GetLogger("Performance." + GetType().Name);
            _logger = logProvider.GetLogger(GetType().Name);
        }

        public IEnumerable<ConceptSyntaxNode> ParsedConcepts => GetConcepts();

        public delegate void OnKeywordEvent(ITokenReader tokenReader, string keyword);
        public delegate void OnMemberReadEvent(ITokenReader tokenReader, ConceptSyntaxNode conceptInfo, ConceptMemberSyntax conceptMember, ValueOrError<object> valueOrError);
        public delegate void OnUpdateContextEvent(ITokenReader tokenReader, Stack<ConceptSyntaxNode> context, bool isOpening);

        public event OnKeywordEvent OnKeyword;
        public event OnMemberReadEvent OnMemberRead;
        public event OnUpdateContextEvent OnUpdateContext;

        //=================================================================

        private List<ConceptSyntaxNode> GetConcepts()
        {
            var parsers = CreateGenericParsers(_syntax.ConceptTypes);
            var parsedConcepts = ExtractConcepts(parsers);
            return parsedConcepts;
        }

        public MultiDictionary<string, IConceptParser> CreateGenericParsers(List<ConceptType> conceptTypes)
        {
            var stopwatch = Stopwatch.StartNew();

            var parsableConcepts = conceptTypes
                .Where(c => c.Keyword != null)
                .ToList();

            var parsers = parsableConcepts.ToMultiDictionary(
                concept => concept.Keyword,
                concept =>
                {
                    var parser = new GenericParser(concept);
                    parser.OnMemberRead += OnMemberRead;
                    return (IConceptParser)parser;
                },
                StringComparer.OrdinalIgnoreCase);

            _performanceLogger.Write(stopwatch, "CreateGenericParsers.");

            _keywordsLogger.Trace(() => string.Join(" ", parsers.Select(p => p.Key).OrderBy(keyword => keyword)));

            return parsers;
        }

        private List<ConceptSyntaxNode> ExtractConcepts(MultiDictionary<string, IConceptParser> conceptParsers)
        {
            var stopwatch = Stopwatch.StartNew();

            TokenReader tokenReader = new TokenReader(_tokenizer.GetTokens(), 0);

            var newConcepts = new List<ConceptSyntaxNode>();
            var context = new Stack<ConceptSyntaxNode>();
            var warnings = new List<string>();

            tokenReader.SkipEndOfFile();
            while (!tokenReader.EndOfInput)
            {
                var parsed = ParseNextConcept(tokenReader, context, conceptParsers);
                newConcepts.Add(parsed.ConceptInfo);

                if (parsed.Warnings != null)
                    warnings.AddRange(parsed.Warnings);

                UpdateContextForNextConcept(tokenReader, context, parsed.ConceptInfo);
                OnKeyword?.Invoke(tokenReader, null);

                if (context.Count == 0)
                    tokenReader.SkipEndOfFile();
            }

            _performanceLogger.Write(stopwatch, "ExtractConcepts (" + newConcepts.Count + " concepts).");

            if (context.Count > 0)
            {
                var (dslScript, position) = tokenReader.GetPositionInScript();
                throw new DslSyntaxException($"Expected \"}}\" to close concept \"{context.Peek()}\".",
                    "RH0002", dslScript, position, 0, ReportPreviousConcept(context.Peek()));
            }

            foreach (string warning in warnings)
            {
                if (_syntax.ExcessDotInKey == ExcessDotInKey.Ignore)
                    _logger.Trace(warning);
                else
                    _logger.Warning(warning);
            }
            if (_syntax.ExcessDotInKey == ExcessDotInKey.Error && warnings.Any())
                throw new DslSyntaxException(warnings.First());

            return newConcepts;
        }

        class Interpretation { public ConceptSyntaxNode Node; public TokenReader NextPosition; public List<string> Warnings; }

        private (ConceptSyntaxNode ConceptInfo, List<string> Warnings) ParseNextConcept(TokenReader tokenReader, Stack<ConceptSyntaxNode> context, MultiDictionary<string, IConceptParser> conceptParsers)
        {
            var errorReports = new List<Func<(string formattedError, string simpleError)>>();
            List<Interpretation> possibleInterpretations = new List<Interpretation>();

            var keywordReader = new TokenReader(tokenReader).ReadText(); // Peek, without changing the original tokenReader's position.
            var keyword = keywordReader.IsError ? null : keywordReader.Value;

            OnKeyword?.Invoke(tokenReader, keyword);
            if (keyword != null)
            {
                foreach (var conceptParser in conceptParsers.Get(keyword))
                {
                    TokenReader nextPosition = new TokenReader(tokenReader);
                    var conceptInfoOrError = conceptParser.Parse(nextPosition, context, out var warnings);

                    if (!conceptInfoOrError.IsError)
                        possibleInterpretations.Add(new Interpretation
                        {
                            Node = conceptInfoOrError.Value,
                            NextPosition = nextPosition,
                            Warnings = warnings
                        });
                    else if (!string.IsNullOrEmpty(conceptInfoOrError.Error)) // Empty error means that this parser is not for this keyword.
                    {
                        errorReports.Add(() =>
                            (string.Format("{0}: {1}\r\n{2}", conceptParser.GetType().Name, conceptInfoOrError.Error, tokenReader.ReportPosition()), conceptInfoOrError.Error));
                    }
                }
            }

            if (possibleInterpretations.Count == 0)
            {
                var (dslScript, position) = tokenReader.GetPositionInScript();
                if (errorReports.Count > 0)
                {
                    var errorReportValues = errorReports.Select(x => x.Invoke()).ToList();
                    var errorsReport = string.Join("\r\n", errorReportValues.Select(x => x.formattedError)).Limit(500, "...");
                    var simpleErrorsReport = string.Join("\n", errorReportValues.Select(x => x.simpleError));
                    var simpleMessage = $"Invalid parameters after keyword '{keyword}'. Possible causes: {simpleErrorsReport}";
                    var possibleCauses = $"Possible causes:\r\n{errorsReport}";
                    throw new DslSyntaxException(simpleMessage, "RH0003", dslScript, position, 0, possibleCauses);
                }
                else if (!string.IsNullOrEmpty(keyword))
                {
                    var simpleMessage = $"Unrecognized concept keyword '{keyword}'.";
                    throw new DslSyntaxException(simpleMessage, "RH0004", dslScript, position, 0, null);
                }
                else
                {
                    var simpleMessage = $"Invalid DSL script syntax.";
                    throw new DslSyntaxException(simpleMessage, "RH0005", dslScript, position, 0, null);
            }
            }

            Disambiguate(possibleInterpretations);
            if (possibleInterpretations.Count > 1)
            {
                var interpretations = new List<string>();
                for (int i = 0; i < possibleInterpretations.Count; i++)
                    interpretations.Add($"{i + 1}. {possibleInterpretations[i].Node.Concept.AssemblyQualifiedName}");

                var simpleMessage = $"Ambiguous syntax. There are multiple possible interpretations of keyword '{keyword}': {string.Join(", ", interpretations)}.";
                var (dslScript, position) = tokenReader.GetPositionInScript();

                throw new DslSyntaxException(simpleMessage, "RH0006", dslScript, position, 0, null);
            }

            var parsedStatement = possibleInterpretations.Single();

            tokenReader.CopyFrom(parsedStatement.NextPosition);
            return (parsedStatement.Node, parsedStatement.Warnings);
        }

        private void Disambiguate(List<Interpretation> possibleInterpretations)
        {
            // Interpretation that covers most of the DSL script has priority,
            // because other interpretations are obviously missing some parameters,
            // otherwise the parser would stop earlier on '{' or ';'.
            int largest = possibleInterpretations.Max(i => i.NextPosition.PositionInTokenList);
            possibleInterpretations.RemoveAll(i => i.NextPosition.PositionInTokenList < largest);
            if (possibleInterpretations.Count == 1)
                return;

            // Interpretation with a flat syntax has priority over the interpretation
            // that could be placed in a nested concept.
            // The nested interpretation can be manually enforced in DSL script (if needed)
            // by nesting this concept.
            var interpretationParameters = possibleInterpretations
                .Select(i =>
                {
                    var firstMember = i.Node.Concept.Members.First();
                    return new { Interpretation = i, FirstParameter = firstMember.ConceptType, NestingOptions = GetNestingOptions(firstMember) };
                })
                .ToList();
            var couldBeNested = new HashSet<Interpretation>();
            foreach (var i1 in interpretationParameters)
                foreach (var i2 in interpretationParameters)
                    if (i1 != i2 && i2.NestingOptions.Skip(1).Contains(i1.FirstParameter))
                    {
                        couldBeNested.Add(i2.Interpretation);
                        _logger.Trace(() => $"Interpretation {i1.Interpretation.Node.Concept.TypeName}" +
                            $" has priority over {i2.Interpretation.Node.Concept.TypeName}," +
                            $" because the second one could be nested in {i2.FirstParameter.TypeName} to force that interpretation." +
                            $" Statement: {i1.Interpretation.Node.GetUserDescription()}.");
                    }
            var flatestInterpretations = possibleInterpretations.Except(couldBeNested).ToList();
            if (flatestInterpretations.Count == 1)
            {
                possibleInterpretations.Clear();
                possibleInterpretations.Add(flatestInterpretations.Single());
            }
        }

        private static List<ConceptType> GetNestingOptions(ConceptMemberSyntax conceptMember)
        {
            var options = new List<ConceptType>();
            while (!options.Contains(conceptMember.ConceptType)) // Recursive concept are possible.
            {
                options.Add(conceptMember.ConceptType);
                if (conceptMember.IsConceptInfo)
                    conceptMember = conceptMember.ConceptType.Members.First();
            }
            return options;
        }

        private string ReportPreviousConcept(ConceptSyntaxNode node)
        {
            var sb = new StringBuilder();
            if (node != null)
            {
                sb.AppendFormat("Previous concept: {0}", node.GetUserDescription()).AppendLine();

                foreach (var m in node.Concept.Members)
                    sb.AppendFormat("Property '{0}' ({1}) = {2}",
                        m.Name,
                        m.IsStringType ? "string" : m.IsConceptInfoInterface ? "IConceptInfo" : m.ConceptType.TypeName,
                        m.GetMemberValue(node)?.ToString() ?? "<null>")
                        .AppendLine();
            }
            return sb.ToString();
        }

        private void UpdateContextForNextConcept(TokenReader tokenReader, Stack<ConceptSyntaxNode> context, ConceptSyntaxNode conceptInfo)
        {
            if (tokenReader.TryRead("{"))
            {
                context.Push(conceptInfo);
                OnUpdateContext?.Invoke(tokenReader, context, true);
            }
            else if (!tokenReader.TryRead(";"))
            {
                var (dslScript, position) = tokenReader.GetPositionInScript();
                throw new DslSyntaxException("Expected \";\" or \"{\".",
                    "RH0001", dslScript, position, 0, ReportPreviousConcept(conceptInfo));
            }

            while (tokenReader.TryRead("}"))
            {
                if (context.Count == 0)
                {
                    var simpleMessage = "Unexpected \"}\".";
                    var (dslScript, position) = tokenReader.GetPositionInScript();
                    throw new DslSyntaxException(simpleMessage, "RH0007", dslScript, position, 0, null);

                }
                context.Pop();
                OnUpdateContext?.Invoke(tokenReader, context, false);
            }
        }
    }
}