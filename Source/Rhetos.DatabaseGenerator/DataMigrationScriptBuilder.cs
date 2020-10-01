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

using Rhetos.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.DatabaseGenerator
{
    public class DataMigrationScriptBuilder : CodeBuilder, IDataMigrationScriptBuilder
    {
        private const string BeforeDataMigrationTag = "/*BeforeDataMigration*/";

        private readonly string DataMigrationScriptSplitterTag = Environment.NewLine + "/*DataMigrationScriptSplitter*/" + Environment.NewLine;

        public DataMigrationScriptBuilder() : base("/*", "*/")
        {
            InsertCode(BeforeDataMigrationTag + DataMigrationScriptSplitterTag);
        }

        public void AddBeforeDataMigrationScript(string script)
        {
            InsertCode(script + DataMigrationScriptSplitterTag, BeforeDataMigrationTag);
        }

        public void AddAfterDataMigrationScript(string script)
        {
            InsertCode(script + DataMigrationScriptSplitterTag);
        }

        public GeneratedDataMigrationScripts GetDataMigrationScripts()
        {
            var scripts = GenerateCode().Split(new[] { DataMigrationScriptSplitterTag }, StringSplitOptions.RemoveEmptyEntries);
            int beforeTagPosition = Array.IndexOf(scripts, BeforeDataMigrationTag);
            if (beforeTagPosition == -1)
                throw new FrameworkException($"Internal error when finding {nameof(BeforeDataMigrationTag)}.");
            return new GeneratedDataMigrationScripts
            {
                BeforeDataMigration = scripts.Take(beforeTagPosition),
                AfterDataMigration = scripts.Skip(beforeTagPosition + 1),
            };
        }
    }

    public class GeneratedDataMigrationScripts
    {
        public IEnumerable<string> BeforeDataMigration { get; set; }
        public IEnumerable<string> AfterDataMigration { get; set; }
    }
}
