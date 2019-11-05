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

        public DslParser(Tokenizer tokenizer, IConceptInfo[] conceptInfoPlugins, ILogProvider logProvider)
        {
            _tokenizer = tokenizer;
            _conceptInfoPlugins = conceptInfoPlugins;
            _performanceLogger = logProvider.GetLogger("Performance");
            _logger = logProvider.GetLogger("DslParser");
            _keywordsLogger = logProvider.GetLogger("DslParser.Keywords");
        }

        public IEnumerable<IConceptInfo> ParsedConcepts => GetConcepts();

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

            var result = conceptMetadata.ToMultiDictionary(x => x.conceptKeyword, x => (IConceptParser)new GenericParser(x.conceptType, x.conceptKeyword), StringComparer.OrdinalIgnoreCase);
            _performanceLogger.Write(stopwatch, "DslParser.CreateGenericParsers.");
            return result;
        }

        private IEnumerable<IConceptInfo> ExtractConcepts(MultiDictionary<string, IConceptParser> conceptParsers)
        {
            var stopwatch = Stopwatch.StartNew();

            TokenReader tokenReader = new TokenReader(_tokenizer.GetTokens(), 0);

            List<IConceptInfo> newConcepts = new List<IConceptInfo>();
            Stack<IConceptInfo> context = new Stack<IConceptInfo>();

            tokenReader.SkipEndOfFile();
            while (!tokenReader.EndOfInput)
            {
                IConceptInfo conceptInfo = ParseNextConcept(tokenReader, context, conceptParsers);
                newConcepts.Add(conceptInfo);

                UpdateContextForNextConcept(tokenReader, context, conceptInfo);

                if (context.Count == 0)
                    tokenReader.SkipEndOfFile();
            }

            _performanceLogger.Write(stopwatch, "DslParser.ExtractConcepts (" + newConcepts.Count + " concepts).");

            if (context.Count > 0)
                throw new DslSyntaxException(string.Format(
                    ReportErrorContext(context.Peek(), tokenReader)
                    + "Expected \"}\" at the end of the script to close concept \"{0}\".", context.Peek()));

            return newConcepts;
        }

        class Interpretation { public IConceptInfo ConceptInfo; public TokenReader NextPosition; }

        private IConceptInfo ParseNextConcept(TokenReader tokenReader, Stack<IConceptInfo> context, MultiDictionary<string, IConceptParser> conceptParsers)
        {
            var errorReports = new List<Func<string>>();
            List<Interpretation> possibleInterpretations = new List<Interpretation>();

            var keywordReader = new TokenReader(tokenReader).ReadText(); // Peek, without changing the original tokenReader's position.
            var keyword = keywordReader.IsError ? null : keywordReader.Value;

            if (keyword != null)
            {
                foreach (var conceptParser in conceptParsers.Get(keyword))
                {
                    TokenReader nextPosition = new TokenReader(tokenReader);
                    var conceptInfoOrError = conceptParser.Parse(nextPosition, context);

                    if (!conceptInfoOrError.IsError)
                        possibleInterpretations.Add(new Interpretation
                        {
                            ConceptInfo = conceptInfoOrError.Value,
                            NextPosition = nextPosition
                        });
                    else if (!string.IsNullOrEmpty(conceptInfoOrError.Error)) // Empty error means that this parser is not for this keyword.
                    {
                        errorReports.Add(() => string.Format("{0}: {1}\r\n{2}", conceptParser.GetType().Name, conceptInfoOrError.Error, tokenReader.ReportPosition()));
                    }
                }
            }

            if (possibleInterpretations.Count == 0)
            {
                if (errorReports.Count > 0)
                {
                    string errorsReport = string.Join("\r\n", errorReports.Select(x => x.Invoke())).Limit(500, "...");
                    throw new DslSyntaxException($"Invalid parameters after keyword '{keyword}'. {tokenReader.ReportPosition()}\r\n\r\nPossible causes:\r\n{errorsReport}");
                }
                else if (!string.IsNullOrEmpty(keyword))
                    throw new DslSyntaxException($"Unrecognized concept keyword '{keyword}'. {tokenReader.ReportPosition()}");
                else
                    throw new DslSyntaxException($"Invalid DSL script syntax. {tokenReader.ReportPosition()}");
            }

            Disambiguate(possibleInterpretations);
            if (possibleInterpretations.Count > 1)
            {
                var report = new List<string>();
                report.Add($"Ambiguous syntax. {tokenReader.ReportPosition()}");
                report.Add($"There are multiple possible interpretations of keyword '{keyword}':");
                for (int i = 0; i < possibleInterpretations.Count; i++)
                    report.Add($"{i + 1}. {possibleInterpretations[i].ConceptInfo.GetType().AssemblyQualifiedName}");

                throw new DslSyntaxException(string.Join("\r\n", report));
            }

            tokenReader.CopyFrom(possibleInterpretations.Single().NextPosition);
            return possibleInterpretations.Single().ConceptInfo;
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

        private string ReportErrorContext(IConceptInfo conceptInfo, TokenReader tokenReader)
        {
            var sb = new StringBuilder();
            sb.AppendLine(tokenReader.ReportPosition());
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
                context.Push(conceptInfo);
            else if (!tokenReader.TryRead(";"))
            {
                var sb = new StringBuilder();
                sb.Append(ReportErrorContext(conceptInfo, tokenReader));
                sb.Append("Expected \";\" or \"{\".");
                throw new DslSyntaxException(sb.ToString());
            }

            while (tokenReader.TryRead("}"))
            {
                if (context.Count == 0)
                    throw new DslSyntaxException(tokenReader.ReportPosition() + "\r\nUnexpected \"}\". ");
                context.Pop();
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