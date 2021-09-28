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

using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Linq;
using System.Collections.Generic;

namespace Rhetos.Dsl.Test
{
    class TestDslParser : DslParser, ITestAccessor
    {
        private readonly DslSyntax _syntax;

        public TestDslParser(string dsl)
            : this(dsl, Array.Empty<IConceptInfo>())
        {
        }

        public TestDslParser(string dsl, IConceptInfo[] conceptInfoPlugins)
            : this(dsl, DslSyntaxHelper.CreateDslSyntax(conceptInfoPlugins))
        {
        }

        public TestDslParser(string dsl, DslSyntax syntax)
            : base (
                new TestTokenizer(dsl),
                new Lazy<DslSyntax>(() => syntax),
                new ConsoleLogProvider())
        {
            _syntax = syntax;
        }

        public IEnumerable<IConceptInfo> ExtractConcepts(MultiDictionary<string, IConceptParser> conceptParsers)
        {
            return (IEnumerable<IConceptInfo>)this.Invoke(nameof(ExtractConcepts), conceptParsers);
        }

        public IConceptInfo ParseNextConcept(TokenReader tokenReader, Stack<IConceptInfo> context, MultiDictionary<string, IConceptParser> conceptParsers)
        {
            var newContext = context == null ? null
                : new Stack<ConceptSyntaxNode>(context.Select(ci => _syntax.CreateConceptSyntaxNode(ci)).Reverse());

            var parsedNode = this.Invoke(nameof(ParseNextConcept), tokenReader, newContext, conceptParsers).Item1;

            return ConceptInfoHelper.ConvertNodeToConceptInfo(parsedNode);
        }
    }
}
