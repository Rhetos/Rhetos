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
using System.Globalization;
using Rhetos.Utilities;
using Rhetos.Logging;

namespace Rhetos.Dsl
{
    public class DslParser : IDslParser
    {
        protected readonly Tokenizer _tokenizer;
        protected readonly IConceptInfo[] _conceptInfoPlugins;
        protected readonly ILogger _performanceLogger;
        protected readonly ILogger _logger;
        protected readonly ILogger _keywordsLogger;

        public DslParser(Tokenizer tokenizer, IConceptInfo[] conceptInfoPlugins, ILogProvider logProvider)
        {
            _tokenizer = tokenizer;
            _conceptInfoPlugins = conceptInfoPlugins;
            _performanceLogger = logProvider.GetLogger("Performance");
            _logger = logProvider.GetLogger("DslParser");
            _keywordsLogger = logProvider.GetLogger("DslParser.Keywords");
        }

        public IEnumerable<IConceptInfo> ParsedConcepts
        {
            get
            {
                var parsers = CreateGenericParsers();
                var parsedConcepts = ExtractConcepts(parsers);
                var alternativeInitializationGeneratedReferences = InitializeAlternativeInitializationConcepts(parsedConcepts);
                return new[] { CreateInitializationConcept() }
                    .Concat(parsedConcepts)
                    .Concat(alternativeInitializationGeneratedReferences)
                    .ToList();
            }
        }

        //=================================================================

        private IConceptInfo CreateInitializationConcept()
        {
            return new InitializationConcept
            {
                RhetosVersion = SystemUtility.GetRhetosVersion()
            };
        }

        protected MultiDictionary<string,IConceptParser> CreateGenericParsers()
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

        protected IEnumerable<IConceptInfo> ExtractConcepts(MultiDictionary<string, IConceptParser> conceptParsers)
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

        protected IConceptInfo ParseNextConcept(TokenReader tokenReader, Stack<IConceptInfo> context, MultiDictionary<string, IConceptParser> conceptParsers)
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

            int largest = possibleInterpretations.Max(i => i.NextPosition.PositionInTokenList);
            possibleInterpretations.RemoveAll(i => i.NextPosition.PositionInTokenList < largest);
            if (possibleInterpretations.Count > 1)
            {
                string msg = "Ambiguous syntax. " + tokenReader.ReportPosition()
                    + "\r\n Possible interpretations: "
                    + string.Join(", ", possibleInterpretations.Select(i => i.ConceptInfo.GetType().Name))
                    + ".";
                throw new DslSyntaxException(msg);
            }

            tokenReader.CopyFrom(possibleInterpretations.Single().NextPosition);
            return possibleInterpretations.Single().ConceptInfo;
        }

        protected string ReportErrorContext(IConceptInfo conceptInfo, TokenReader tokenReader)
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

        protected void UpdateContextForNextConcept(TokenReader tokenReader, Stack<IConceptInfo> context, IConceptInfo conceptInfo)
        {
            if (tokenReader.TryRead("{"))
                context.Push(conceptInfo);
            else if (!tokenReader.TryRead(";"))
            {
                var sb = new StringBuilder();
                sb.Append(ReportErrorContext(conceptInfo, tokenReader));
                sb.AppendFormat("Expected \";\" or \"{{\".");
                throw new DslSyntaxException(sb.ToString());
            }

            while (tokenReader.TryRead("}"))
            {
                if (context.Count == 0)
                    throw new DslSyntaxException(tokenReader.ReportPosition() + "\r\nUnexpected \"}\". ");
                context.Pop();
            }
        }

        protected IEnumerable<IConceptInfo> InitializeAlternativeInitializationConcepts(IEnumerable<IConceptInfo> parsedConcepts)
        {
            var stopwatch = Stopwatch.StartNew();
            var newConcepts = AlternativeInitialization.InitializeNonparsableProperties(parsedConcepts, _logger);
            _performanceLogger.Write(stopwatch, "DslParser.InitializeAlternativeInitializationConcepts (" + newConcepts.Count() + " new concepts created).");
            return newConcepts;
        }
    }
}