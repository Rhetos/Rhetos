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
using System.Globalization;
using System.ComponentModel.Composition;
using Rhetos.Compiler;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Dsl;
using Rhetos.Extensibility;
using System.Diagnostics.Contracts;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(DataStructureInfo))]
    public class DataStructureCodeGenerator : IConceptCodeGenerator
    {
        public class DataStructureTag : Tag<DataStructureInfo>
        {
            public DataStructureTag(TagType tagType, string tagFormat, string nextTagFormat = null, string firstEvaluationContext = null, string nextEvaluationContext = null)
                : base(tagType, tagFormat, (info, format) => string.Format(CultureInfo.InvariantCulture, format, info.Module.Name, info.Name), nextTagFormat, firstEvaluationContext, nextEvaluationContext)
            { }
        }

        public static readonly DataStructureTag AttributesTag = new DataStructureTag(TagType.Appendable, "/*class attribute {0}.{1}*/");
        public static readonly DataStructureTag InterfaceTag = new DataStructureTag(TagType.Appendable, "/*first interface {0}.{1}*/", "/*next interface {0}.{1}*/", " : {0}", ", {0}");
        public static readonly DataStructureTag BodyTag = new DataStructureTag(TagType.Appendable, "/*class body {0}.{1}*/");

        protected static readonly DataStructureTag CodeSnippet = new DataStructureTag(TagType.CodeSnippet, 
@"
    " + AttributesTag + @"
    public partial class {1}" + InterfaceTag + @"
    {{
        " + BodyTag + @"
    }}
");

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            DataStructureInfo info = (DataStructureInfo)conceptInfo;
            codeBuilder.InsertCode(CodeSnippet.Evaluate(info), ModuleCodeGenerator.NamespaceMembersTag, info.Module);
            codeBuilder.InsertCode("[DataContract]", AttributesTag, info);
        }
    }


    public static class DataStructureCodeBuilder
    {
        public static void AddInterfaceAndReference(this ICodeBuilder codeBuilder, Type type, DataStructureInfo dataStructureInfo)
        {
            codeBuilder.AddInterfaceAndReference(type.FullName, type, dataStructureInfo);
        }

        public static void AddInterfaceAndReference(this ICodeBuilder codeBuilder, string typeName, Type type, DataStructureInfo dataStructureInfo)
        {
            codeBuilder.InsertCode(typeName, DataStructureCodeGenerator.InterfaceTag, dataStructureInfo);
            codeBuilder.AddReferencesFromDependency(type);
        }
    }
}
