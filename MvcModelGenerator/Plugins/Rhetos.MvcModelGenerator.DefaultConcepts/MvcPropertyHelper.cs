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
        public class PropertyTag : Tag<PropertyInfo>
        {
            public PropertyTag(TagType tagType, string tagFormat, string nextTagFormat = null)
                : base(tagType, tagFormat, (info, format) => string.Format(CultureInfo.InvariantCulture, format, info.DataStructure.Module.Name, info.DataStructure.Name, info.Name), nextTagFormat)
            { }
        }

        public static readonly PropertyTag AttributeTag = new PropertyTag(TagType.Appendable, "/*property attribute {0}.{1}.{2}*/");

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
