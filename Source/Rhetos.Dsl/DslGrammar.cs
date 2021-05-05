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
    public class DslGrammar
    {
        private readonly IConceptInfo[] _conceptInfoPlugins;
        private readonly ILogger _keywordsLogger;
        private readonly ILogger _performanceLogger;

        public DslGrammar(IConceptInfo[] conceptInfoPlugins, ILogProvider logProvider)
        {
            _conceptInfoPlugins = conceptInfoPlugins;
            _keywordsLogger = logProvider.GetLogger("DslParser.Keywords"); // Legacy logger name.
            _performanceLogger = logProvider.GetLogger("Performance." + GetType().Name);
        }

        public MultiDictionary<string, IConceptParser> CreateGenericParsers(DslParser.OnMemberReadEvent onMemberRead)
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
                parser.OnMemberRead += onMemberRead;
                return (IConceptParser)parser;
            }, StringComparer.OrdinalIgnoreCase);
            _performanceLogger.Write(stopwatch, "CreateGenericParsers.");
            return result;
        }
    }
}