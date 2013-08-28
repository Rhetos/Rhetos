using Rhetos.Compiler;
using Rhetos.Dsl.DefaultConcepts;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Rhetos.MvcModelGenerator.DefaultConcepts
{

    public static class MvcPropertyHelper
    {
        public static readonly CsTag<PropertyInfo> AttributeTag = "Attribute";

        private static string ImplementationCodeSnippet(PropertyInfo info, string type, string nameSuffix)
        {
            return string.Format(@"
        " + AttributeTag.Evaluate(info) + @"
        public {1} {0}{2} {{ get; set; }}
        ", info.Name, type, nameSuffix);
        }

        public static void GenerateCodeForType(PropertyInfo info, ICodeBuilder codeBuilder, string type, string nameSuffix = "")
        {
            codeBuilder.InsertCode(ImplementationCodeSnippet(info, type, nameSuffix), DataStructureCodeGenerator.ClonePropertiesTag, info.DataStructure);
        }
    }
}
