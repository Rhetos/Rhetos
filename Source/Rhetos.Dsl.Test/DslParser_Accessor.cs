﻿/*
    Copyright (C) 2013 Omega software d.o.o.

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
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rhetos.Dsl.Test
{
    class TestDslParser : DslParser
    {
        public TestDslParser(string dsl, IConceptInfo[] conceptInfoPlugins = null)
            : base (
                new DslSourceHelper(dsl),
                conceptInfoPlugins != null ? conceptInfoPlugins : new IConceptInfo[] { },
                new ConsoleLogProvider())
        {
        }

        new public IEnumerable<IConceptInfo> ExtractConcepts(IEnumerable<IConceptParser> conceptParsers)
        {
            return base.ExtractConcepts(conceptParsers);
        }

        new public IConceptInfo ParseNextConcept(TokenReader tokenReader, Stack<IConceptInfo> context, IEnumerable<IConceptParser> conceptParsers)
        {
            return base.ParseNextConcept(tokenReader, context, conceptParsers);
        }
    }
}
