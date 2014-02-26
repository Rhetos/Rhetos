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
using System.Text;
using Rhetos.Utilities;
using System.Linq;

namespace Rhetos.Dsl
{
    public class DslScriptProvider : IDslSource
    {
        private readonly List<DslScript> _scripts = new List<DslScript>();
        private readonly string _scriptSeparator = Environment.NewLine;

        public DslScriptProvider(params DslScript[] scripts)
        {
            _scripts.AddRange(scripts);
        }

        public string Script
        {
            get { return string.Join(_scriptSeparator, _scripts.Select(it => it.Script)); }
        }

        public string ReportError(int index)
        {
            var location = LocatDslScript(index);
            return ScriptPositionReporting.ReportPosition(location.DslScript.Script, location.Position, location.DslScript.Path);
        }

        public string GetSourceFilePath(int index)
        {
            return LocatDslScript(index).DslScript.Path;
        }

        private class Location { public DslScript DslScript; public int Position; }

        private Location LocatDslScript(int index)
        {
            if (index < 0 || index > Script.Length)
                throw new FrameworkException("Error in DSL script parser. Provided position in script is out of range. Position: " + index + ".");

            int i = index;
            foreach (var s in _scripts)
            {
                if (i >= 0 && i <= s.Script.Length)
                    return new Location { DslScript = s, Position = i };

                i -= s.Script.Length + _scriptSeparator.Length;
            }

            throw new FrameworkException("Error in DSL script parser. Provided position is not within a script. Position: " + index + ".");
        }
    }
}