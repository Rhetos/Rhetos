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

using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;

namespace Rhetos.Dsl.Test
{
    class TestDslParser : DslParser, ITestAccessor
    {
        public TestDslParser(string dsl, IConceptInfo[] conceptInfoPlugins = null)
            : base (
                new TestTokenizer(dsl),
                conceptInfoPlugins ?? Array.Empty<IConceptInfo>(),
                new ConsoleLogProvider(),
                new BuildOptions())
        {
        }

        public IEnumerable<IConceptInfo> ExtractConcepts(MultiDictionary<string, IConceptParser> conceptParsers)
        {
            return (IEnumerable<IConceptInfo>)this.Invoke("ExtractConcepts", conceptParsers);
        }

        public IConceptInfo ParseNextConcept(TokenReader tokenReader, Stack<IConceptInfo> context, MultiDictionary<string, IConceptParser> conceptParsers)
        {
            return this.Invoke("ParseNextConcept", tokenReader, context, conceptParsers).Item1;
        }
    }
}
