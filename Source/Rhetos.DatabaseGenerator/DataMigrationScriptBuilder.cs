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
        protected const string BeforeDataMigrationTag = "/*BeforedataMigration*/";

        protected const string AfterDataMigrationTag = "/*AfterDataMigration*/";

        protected const string DataMigrationScriptStartTag = "/*DataMigrationScriptStart*/";

        protected const string DataMigrationScriptEndTag = "/*DataMigrationScriptEnd*/";

        protected readonly CodeBuilder _codeBuilder;

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
            var beforedataMigartionScriptStart = code.IndexOf(BeforeDataMigrationTag);
            var beforeDataMigartionCode = code.Substring(0, beforedataMigartionScriptStart);

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
