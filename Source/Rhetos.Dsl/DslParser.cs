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
    public class DslParser : IDslParser
    {
        private readonly Tokenizer _tokenizer;
        private readonly IConceptInfo[] _conceptInfoPlugins;
        private readonly ILogger _performanceLogger;
        private readonly ILogger _logger;
        private readonly ILogger _keywordsLogger;
        private readonly ExcessDotInKey _legacySyntax;


        public DslParser(Tokenizer tokenizer, IConceptInfo[] conceptInfoPlugins, ILogProvider logProvider, BuildOptions buildOptions)
        {
            _tokenizer = tokenizer;
            _conceptInfoPlugins = conceptInfoPlugins;
            _performanceLogger = logProvider.GetLogger("Performance");
            _logger = logProvider.GetLogger("DslParser");
            _keywordsLogger = logProvider.GetLogger("DslParser.Keywords");
            _legacySyntax = buildOptions.Dsl__ExcessDotInKey;
        }

        public IEnumerable<IConceptInfo> ParsedConcepts => GetConcepts();

        public delegate void OnKeywordEvent(ITokenReader tokenReader, string keyword);
        public delegate void OnMemberReadEvent(ITokenReader tokenReader, IConceptInfo conceptInfo, ConceptMember conceptMember, ValueOrError<object> valueOrError);
        public delegate void OnUpdateContextEvent(ITokenReader tokenReader, Stack<IConceptInfo> context, bool isOpening);

        private event OnKeywordEvent _onKeyword;
        private event OnMemberReadEvent _onMemberRead;
        private event OnUpdateContextEvent _onUpdateContext;

        public IEnumerable<IConceptInfo> ParseConceptsWithCallbacks(OnKeywordEvent onKeyword,
            OnMemberReadEvent onMemberRead,
            OnUpdateContextEvent onUpdateContext)
        {
            try
            {
                _onKeyword += onKeyword;
                _onMemberRead += onMemberRead;
                _onUpdateContext += onUpdateContext;
                return GetConcepts();
            }
            finally
            {
                _onKeyword -= onKeyword;
                _onMemberRead -= onMemberRead;
                _onUpdateContext -= onUpdateContext;
            }
        }
        //=================================================================

        

        private List<IConceptInfo> GetConcepts()
        {
            var parsers = CreateGenericParsers();
            var parsedConcepts = ExtractConcepts(parsers);
            var alternativeInitializationGeneratedReferences = InitializeAlternativeInitializationConcepts(parsedConcepts);
            return new[] { CreateInitializationConcept() }
                .Concat(parsedConcepts)
                .Concat(alternativeInitializationGeneratedReferences)
                .ToList();
        }

        private IConceptInfo CreateInitializationConcept()
        {
            return new InitializationConcept
            {
                RhetosVersion = SystemUtility.GetRhetosVersion()
            };
        }

        private MultiDictionary<string,IConceptParser> CreateGenericParsers()
        {
            var stopwatch = Stopwatch.StartNew();

            var conceptMetadata = _conceptInfoPlugins
                .Select(conceptInfo => conceptInfo.GetType())
                .Distinct()
                .Select(conceptInfoType => new
                            {
                                conceptType = conceptInfoType,
                                conceptKeyword = ConceptInfoHelper.GetKeyword(conceptInfoType)
                            })
                .Where(cm => cm.conceptKeyword != null)
                .ToList();

            _keywordsLogger.Trace(() => string.Join(" ", conceptMetadata.Select(cm => cm.conceptKeyword).OrderBy(keyword => keyword).Distinct()));

            var result = conceptMetadata.ToMultiDictionary(x => x.conceptKeyword, x =>
            {
                var parser = new GenericParser(x.conceptType, x.conceptKeyword);
                parser.OnMemberRead += _onMemberRead;
                return (IConceptParser) parser;
            }, StringComparer.OrdinalIgnoreCase);
            _performanceLogger.Write(stopwatch, "DslParser.CreateGenericParsers.");
            return result;
        }

        private IEnumerable<IConceptInfo> ExtractConcepts(MultiDictionary<string, IConceptParser> conceptParsers)
        {
            var stopwatch = Stopwatch.StartNew();

            TokenReader tokenReader = new TokenReader(_tokenizer.GetTokens(), 0);

            var newConcepts = new List<IConceptInfo>();
            var context = new Stack<IConceptInfo>();
            var warnings = new List<string>();

            tokenReader.SkipEndOfFile();
            while (!tokenReader.EndOfInput)
            {
                var parsed = ParseNextConcept(tokenReader, context, conceptParsers);
                newConcepts.Add(parsed.ConceptInfo);

                if (parsed.Warnings != null)
                    warnings.AddRange(parsed.Warnings);

                UpdateContextForNextConcept(tokenReader, context, parsed.ConceptInfo);
                _onKeyword?.Invoke(tokenReader, null);

                if (context.Count == 0)
                    tokenReader.SkipEndOfFile();
            }

            _performanceLogger.Write(stopwatch, "DslParser.ExtractConcepts (" + newConcepts.Count + " concepts).");

            if (context.Count > 0)
            {
                var (dslScript, position) = tokenReader.GetPositionInScript();
                throw new DslParseSyntaxException($"Expected \"}}\" to close concept \"{context.Peek()}\".",
                    "RH0002", dslScript, position, 0, ReportPreviousConcept(context.Peek()));
            }

            foreach (string warning in warnings)
            {
                if (_legacySyntax == ExcessDotInKey.Ignore)
                    _logger.Trace(warning);
                else
                    _logger.Warning(warning);
            }
            if (_legacySyntax == ExcessDotInKey.Error && warnings.Any())
                throw new DslSyntaxException(warnings.First());

            return newConcepts;
        }

        class Interpretation { public IConceptInfo ConceptInfo; public TokenReader NextPosition; public List<string> Warnings; }

        private (IConceptInfo ConceptInfo, List<string> Warnings) ParseNextConcept(TokenReader tokenReader, Stack<IConceptInfo> context, MultiDictionary<string, IConceptParser> conceptParsers)
        {
            var errorReports = new List<Func<(string formattedError, string simpleError)>>();
            List<Interpretation> possibleInterpretations = new List<Interpretation>();

            var keywordReader = new TokenReader(tokenReader).ReadText(); // Peek, without changing the original tokenReader's position.
            var keyword = keywordReader.IsError ? null : keywordReader.Value;

            _onKeyword?.Invoke(tokenReader, keyword);
            if (keyword != null)
            {
                foreach (var conceptParser in conceptParsers.Get(keyword))
                {
                    TokenReader nextPosition = new TokenReader(tokenReader);
                    var conceptInfoOrError = conceptParser.Parse(nextPosition, context, out var warnings);

                    if (!conceptInfoOrError.IsError)
                        possibleInterpretations.Add(new Interpretation
                        {
                            ConceptInfo = conceptInfoOrError.Value,
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
                    throw new DslParseSyntaxException(simpleMessage, "RH0003", dslScript, position, 0, possibleCauses);
                }
                else if (!string.IsNullOrEmpty(keyword))
                {
                    var simpleMessage = $"Unrecognized concept keyword '{keyword}'.";
                    throw new DslParseSyntaxException(simpleMessage, "RH0004", dslScript, position, 0, null);
                }
                else
                {
                    var simpleMessage = $"Invalid DSL script syntax.";
                    throw new DslParseSyntaxException(simpleMessage, "RH0005", dslScript, position, 0, null);
            }
            }

            Disambiguate(possibleInterpretations);
            if (possibleInterpretations.Count > 1)
            {
                var interpretations = new List<string>();
                for (int i = 0; i < possibleInterpretations.Count; i++)
                    interpretations.Add($"{i + 1}. {possibleInterpretations[i].ConceptInfo.GetType().AssemblyQualifiedName}");

                var simpleMessage = $"Ambiguous syntax. There are multiple possible interpretations of keyword '{keyword}': {string.Join(", ", interpretations)}.";
                var (dslScript, position) = tokenReader.GetPositionInScript();

                throw new DslParseSyntaxException(simpleMessage, "RH0006", dslScript, position, 0, null);
            }

            var parsedStatement = possibleInterpretations.Single();

            tokenReader.CopyFrom(parsedStatement.NextPosition);
            return (parsedStatement.ConceptInfo, parsedStatement.Warnings);
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
                    var firstMemberType = ConceptMembers.Get(i.ConceptInfo).First().ValueType;
                    return new { Interpretation = i, FirstParameter = firstMemberType, NestingOptions = GetNestingOptions(firstMemberType) };
                })
                .ToList();
            var couldBeNested = new HashSet<Interpretation>();
            foreach (var i1 in interpretationParameters)
                foreach (var i2 in interpretationParameters)
                    if (i1 != i2 && i2.NestingOptions.Skip(1).Contains(i1.FirstParameter))
                    {
                        couldBeNested.Add(i2.Interpretation);
                        _logger.Trace(() => $"Interpretation {i1.Interpretation.ConceptInfo.GetType().Name}" +
                            $" has priority over {i2.Interpretation.ConceptInfo.GetType().Name}," +
                            $" because the second one could be nested in {i2.FirstParameter.Name} to force that interpretation." +
                            $" Statement: {i1.Interpretation.ConceptInfo.GetUserDescription()}.");
                    }
            var flatestInterpretations = possibleInterpretations.Except(couldBeNested).ToList();
            if (flatestInterpretations.Count == 1)
            {
                possibleInterpretations.Clear();
                possibleInterpretations.Add(flatestInterpretations.Single());
            }
        }

        private static List<Type> GetNestingOptions(Type conceptType)
        {
            var options = new List<Type>();
            while (!options.Contains(conceptType)) // Recursive concept are possible.
            {
                options.Add(conceptType);
                if (typeof(IConceptInfo).IsAssignableFrom(conceptType))
                    conceptType = ConceptMembers.Get(conceptType).First().ValueType;
            }
            return options;
        }

        private string ReportPreviousConcept(IConceptInfo conceptInfo)
        {
            var sb = new StringBuilder();

            if (conceptInfo != null)
            {
                sb.AppendFormat("Previous concept: {0}", conceptInfo.GetUserDescription()).AppendLine();
                var properties = conceptInfo.GetType().GetProperties().ToList();
                properties.ForEach(it =>
                    sb.AppendFormat("Property {0} ({1}) = {2}",
                        it.Name,
                        it.PropertyType.Name,
                        it.GetValue(conceptInfo, null) ?? "<null>")
                        .AppendLine());
            }
            return sb.ToString();
        }

        private void UpdateContextForNextConcept(TokenReader tokenReader, Stack<IConceptInfo> context, IConceptInfo conceptInfo)
        {
            if (tokenReader.TryRead("{"))
            {
                context.Push(conceptInfo);
                _onUpdateContext?.Invoke(tokenReader, context, true);
            }
            else if (!tokenReader.TryRead(";"))
            {
                var (dslScript, position) = tokenReader.GetPositionInScript();
                throw new DslParseSyntaxException("Expected \";\" or \"{\".",
                    "RH0001", dslScript, position, 0, ReportPreviousConcept(conceptInfo));
            }

            while (tokenReader.TryRead("}"))
            {
                if (context.Count == 0)
                {
                    var simpleMessage = "Unexpected \"}\".";
                    var (dslScript, position) = tokenReader.GetPositionInScript();
                    throw new DslParseSyntaxException(simpleMessage, "RH0007", dslScript, position, 0, null);

                }
                context.Pop();
                _onUpdateContext?.Invoke(tokenReader, context, false);
            }
        }

        private IEnumerable<IConceptInfo> InitializeAlternativeInitializationConcepts(IEnumerable<IConceptInfo> parsedConcepts)
        {
            var stopwatch = Stopwatch.StartNew();
            var newConcepts = AlternativeInitialization.InitializeNonparsableProperties(parsedConcepts, _logger);
            _performanceLogger.Write(stopwatch, "DslParser.InitializeAlternativeInitializationConcepts (" + newConcepts.Count() + " new concepts created).");
            return newConcepts;
        }
    }
}