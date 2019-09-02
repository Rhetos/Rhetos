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
using Rhetos.Dsl;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Rhetos.DatabaseGenerator
{
    public class DataMigrationScriptBuilder : IDataMigrationScriptBuilder
    {
        private const string BeforeDataMigrationTag = "/*BeforeDataMigration*/";

        private const string AfterDataMigrationTag = "/*AfterDataMigration*/";

        private const string DataMigrationScriptStartTag = "/*DataMigrationScriptStart*/";

        private const string DataMigrationScriptEndTag = "/*DataMigrationScriptEnd*/";

        private readonly CodeBuilder _codeBuilder;

        public string GeneratedCode {
            get { return _codeBuilder.GeneratedCode; }
        }

        public DataMigrationScriptBuilder()
        {
            _codeBuilder = new CodeBuilder("/*", "*/");

            _codeBuilder.InsertCode(BeforeDataMigrationTag + Environment.NewLine + AfterDataMigrationTag);
        }

        public void AddBeforeDataMigrationScript(string script)
        {
            _codeBuilder.InsertCode(Environment.NewLine + MarkAsDataMigartionScript(script), BeforeDataMigrationTag, false);
        }

        public void AddAfterDataMigrationScript(string script)
        {
            _codeBuilder.InsertCode(Environment.NewLine + MarkAsDataMigartionScript(script), AfterDataMigrationTag, false);
        }

        public void InsertCode<T>(string code, Tag<T> tag, T conceptInfo)
            where T : IConceptInfo
        {
            _codeBuilder.InsertCode(code, tag, conceptInfo);
        }

        public bool TagExists(string tag)
        {
            return _codeBuilder.TagExists(tag);
        }

        private static string MarkAsDataMigartionScript(string code)
        {
            return DataMigrationScriptStartTag + code + DataMigrationScriptEndTag + Environment.NewLine;
        }

        public IEnumerable<string> GetBeforeDataMigartionScript()
        {
            var code = _codeBuilder.GeneratedCode;
            var beforeDataMigartionScriptStart = code.IndexOf(BeforeDataMigrationTag);
            var beforeDataMigartionCode = code.Substring(0, beforeDataMigartionScriptStart);

            return GetSubStrings(beforeDataMigartionCode, DataMigrationScriptStartTag, DataMigrationScriptEndTag);
        }

        public IEnumerable<string> GetAfterDataMigartionScript()
        {
            var code = _codeBuilder.GeneratedCode;
            var beforeDataMigartionScriptEnd = code.IndexOf(BeforeDataMigrationTag) + BeforeDataMigrationTag.Length;
            var afterDataMigartionScriptStart = code.IndexOf(AfterDataMigrationTag);
            var afterDataMigartionCode = code.Substring(beforeDataMigartionScriptEnd, afterDataMigartionScriptStart - beforeDataMigartionScriptEnd);

            return GetSubStrings(afterDataMigartionCode, DataMigrationScriptStartTag, DataMigrationScriptEndTag);
        }

        private List<string> GetSubStrings(string input, string startString, string endString)
        {
            var index = input.IndexOf(startString);
            var scripts = new List<string>();
            while (index != -1)
            {
                var endIndex = input.IndexOf(endString, index);
                if (endIndex == -1)
                    break;

                scripts.Add(input.Substring(index + startString.Length, endIndex - index - startString.Length));
                index = input.IndexOf(startString, endIndex);
            }

            return scripts;
        }
    }
}
