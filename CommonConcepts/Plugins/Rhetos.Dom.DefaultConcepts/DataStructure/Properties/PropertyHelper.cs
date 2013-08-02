/*
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using System.Globalization;
using Rhetos.Compiler;


namespace Rhetos.Dom.DefaultConcepts
{
    public static class PropertyHelper
    {
        public class PropertyTag : Tag<PropertyInfo>
        {
            public PropertyTag(TagType tagType, string tagFormat, string nextTagFormat = null)
                : base(tagType, tagFormat, (info, format) => string.Format(CultureInfo.InvariantCulture, format, info.DataStructure.Module.Name, info.DataStructure.Name, info.Name), nextTagFormat)
            { }
        }

        public static readonly PropertyTag PropertyTypeTag = new PropertyTag(TagType.Single, "/*property type {0}.{1}.{2}*/");
        public static readonly PropertyTag AttributeTag = new PropertyTag(TagType.Appendable, "/*property attribute {0}.{1}.{2}*/");
        public static readonly PropertyTag BeforeGetPropertyTag = new PropertyTag(TagType.Appendable, "/*get property {0}.{1}.{2}*/");
        public static readonly PropertyTag BeforeSetPropertyTag = new PropertyTag(TagType.Appendable, "/*set property {0}.{1}.{2}*/");
        public static readonly PropertyTag DefaultValueTag = new PropertyTag(TagType.Single, "/*default value {0}.{1}.{2}*/");

        private static readonly PropertyTag PropertyWithFieldSnippet = new PropertyTag(TagType.CodeSnippet,
@"
        private " + PropertyTypeTag + @" _{2} " + DefaultValueTag + @";
        " + AttributeTag + @"
        public virtual " + PropertyTypeTag + @" {2}
        {{
            get
            {{
                " + BeforeGetPropertyTag + @"
                return _{2};
            }}
            set
            {{
                " + BeforeSetPropertyTag + @"
                _{2} = value;
            }}
        }}
");

        private static readonly PropertyTag PropertyWithoutFieldSnippet = new PropertyTag(TagType.CodeSnippet,
@"
        " + AttributeTag + @"
        public virtual " + PropertyTypeTag + @" {2}
        {{
            get
            {{
                " + BeforeGetPropertyTag + @"
            }}
            set
            {{
                " + BeforeSetPropertyTag + @"
            }}
        }}
");

        public static void GenerateCodeForType(PropertyInfo info, ICodeBuilder codeBuilder, string type, bool serializable, bool addField = true)
        {
            var codeSnippet = addField ? PropertyWithFieldSnippet : PropertyWithoutFieldSnippet;
            codeBuilder.InsertCode(codeSnippet.Evaluate(info), DataStructureCodeGenerator.BodyTag, info.DataStructure);
            codeBuilder.InsertCode(type, PropertyTypeTag, info);
            if (serializable)
                codeBuilder.InsertCode("[DataMember]", AttributeTag, info);
        }
    }
}
