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
        protected readonly IDslSource _dslSource;
        protected readonly IConceptInfo[] _conceptInfoPlugins;
        protected readonly ILogger _performanceLogger;
        protected readonly ILogger _logger;

        public DslParser(IDslSource dslSource, IConceptInfo[] conceptInfoPlugins, ILogProvider logProvider)
        {
            _dslSource = dslSource;
            _conceptInfoPlugins = conceptInfoPlugins;
            _performanceLogger = logProvider.GetLogger("Performance");
            _logger = logProvider.GetLogger("DslParser");
        }

        public IEnumerable<IConceptInfo> ParsedConcepts
        {
            get
            {
                IEnumerable<IConceptParser> parsers = CreateGenericParsers();
                var parsedConcepts = ExtractConcepts(parsers);
                var alternativeInitializationGeneratedReferences = ResolveAlternativeInitializationConcepts(parsedConcepts);
                return parsedConcepts.Concat(alternativeInitializationGeneratedReferences).ToArray();
            }
        }

        //=================================================================


        protected IEnumerable<IConceptParser> CreateGenericParsers()
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

            _logger.Trace(() => "DSL keywords: " + string.Join(" ",
                conceptMetadata.Select(cm => cm.conceptKeyword).OrderBy(keyword => keyword).Distinct()));

            var result = conceptMetadata.Select(cm => new GenericParser(cm.conceptType, cm.conceptKeyword)).ToList<IConceptParser>();
            _performanceLogger.Write(stopwatch, "DslParser.CreateGenericParsers.");
            return result;
        }

        protected IEnumerable<IConceptInfo> ExtractConcepts(IEnumerable<IConceptParser> conceptParsers)
        {
            var stopwatch = Stopwatch.StartNew();

            TokenReader tokenReader = new TokenReader(Tokenizer.GetTokens(_dslSource), 0);

            List<IConceptInfo> newConcepts = new List<IConceptInfo>();
            Stack<IConceptInfo> context = new Stack<IConceptInfo>();
            while (!tokenReader.EndOfInput)
            {
                IConceptInfo conceptInfo = ParseNextConcept(tokenReader, context, conceptParsers);
                newConcepts.Add(conceptInfo);

                UpdateContextForNextConcept(tokenReader, context, conceptInfo);
            }

            _performanceLogger.Write(stopwatch, "DslParser.ExtractConcepts.");

            if (context.Count > 0)
                throw new DslSyntaxException(string.Format(
                    ReportErrorContext(context.Peek(), _dslSource.Script.Length - 1)
                    + "Expected \"}\" at the end of the script to close concept \"{0}\".", context.Peek()));

            return newConcepts;
        }

        struct Interpretation { public IConceptInfo ConceptInfo; public TokenReader NextPosition; }

        struct ErrorContext
        {
            public string Error; public int Postion; public string ParserName; public IDslSource DslSource;
            public override string ToString()
            {
                return ParserName + ": " + Error + "\r\n" + DslSource.ReportError(Postion);
            }
        }

        protected IConceptInfo ParseNextConcept(TokenReader tokenReader, Stack<IConceptInfo> context, IEnumerable<IConceptParser> conceptParsers)
        {
            var errors = new List<ErrorContext>();
            List<Interpretation> possibleInterpretations = new List<Interpretation>();

            foreach (var conceptParser in conceptParsers)
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
                    errors.Add(new ErrorContext
                    {
                        Error = conceptInfoOrError.Error,
                        Postion = tokenReader.CurrentPosition,
                        ParserName = conceptParser.GetType().Name,
                        DslSource = _dslSource
                    });
            }

            if (possibleInterpretations.Count == 0)
            {
                string msg = "Unrecognized concept. " + _dslSource.ReportError(tokenReader.CurrentPosition);
                if (errors.Count > 0)
                {
                    string listedErrors = string.Join("\r\n", errors);
                    if (listedErrors.Length > 500) listedErrors = listedErrors.Substring(0, 500) + "...";
                    msg = msg + "\r\n\r\nPossible causes:\r\n" + listedErrors;
                }
                throw new DslSyntaxException(msg);
            }

            int largest = possibleInterpretations.Max(i => i.NextPosition.PositionInTokenList);
            possibleInterpretations.RemoveAll(i => i.NextPosition.PositionInTokenList < largest);
            if (possibleInterpretations.Count > 1)
            {
                string msg = "Ambiguous syntax. " + _dslSource.ReportError(tokenReader.CurrentPosition)
                    + "\r\n Possible interpretations: "
                    + string.Join(", ", possibleInterpretations.Select(i => i.ConceptInfo.GetType().Name))
                    + ".";
                throw new DslSyntaxException(msg);
            }

            tokenReader.CopyFrom(possibleInterpretations.Single().NextPosition);
            return possibleInterpretations.Single().ConceptInfo;
        }

        protected string ReportErrorContext(IConceptInfo conceptInfo, int index)
        {
            var sb = new StringBuilder();
            sb.AppendLine(_dslSource.ReportError(index));
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
                sb.Append(ReportErrorContext(conceptInfo, tokenReader.CurrentPosition));
                sb.AppendFormat("Expected \";\" or \"{{\".");
                throw new DslSyntaxException(sb.ToString());
            }

            while (tokenReader.TryRead("}"))
            {
                if (context.Count == 0)
                    throw new DslSyntaxException(_dslSource.ReportError(tokenReader.CurrentPosition) + "\r\nUnexpected \"}\". ");
                context.Pop();
            }
        }

        protected List<IConceptInfo> ResolveAlternativeInitializationConcepts(IEnumerable<IConceptInfo> parsedConcepts)
        {
            var newConcets = new List<IConceptInfo>();
            foreach (var alternativeInitializationConcept in parsedConcepts.OfType<IAlternativeInitializationConcept>())
                newConcets.AddRange(AlternativeInitialization.InitializeNonparsablePropertiesRecursive(alternativeInitializationConcept));
            return newConcets;
        }

    }
}