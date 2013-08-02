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
using System.ComponentModel.Composition;
using System.Globalization;
using System.Xml;
using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using Rhetos.MvcModelGenerator;

namespace Rhetos.MvcModelGenerator.DefaultConcepts
{
    [Export(typeof(IMvcModelGeneratorPlugin))]
    [ExportMetadata(MefProvider.Implements, typeof(DataStructureInfo))]
    public class DataStructureCodeGenerator : IMvcModelGeneratorPlugin
    {
        public static readonly DataStructureCodeGenerator.DataStructureTag ClonePropertiesTag =
            new DataStructureCodeGenerator.DataStructureTag(TagType.Appendable, "/*MvcModel.CloneProperties {0}.{1}*/");

        public class DataStructureTag : Tag<DataStructureInfo>
        {
            public DataStructureTag(TagType tagType, string tagFormat, string nextTagFormat = null, string firstEvaluationContext = null, string nextEvaluationContext = null)
                : base(tagType, tagFormat, (info, format) => string.Format(CultureInfo.InvariantCulture, format, info.Module.Name, info.Name), nextTagFormat, firstEvaluationContext, nextEvaluationContext)
            { }
        }

        private static string ImplementationCodeSnippet(DataStructureInfo info)
        {
            return string.Format(@"
namespace {0} 
{{ 
    public partial class {1} : Rhetos.Mvc.BaseMvcModel
    {{
        {2}
    }}
}}

    ",
                info.Module.Name, 
                info.Name, 
                ClonePropertiesTag.Evaluate(info));
        }

        private static bool _isInitialCallMade;

        public static bool IsTypeSupported(DataStructureInfo conceptInfo)
        {
            return conceptInfo is EntityInfo
                || conceptInfo is BrowseDataStructureInfo
                || conceptInfo is LegacyEntityInfo
                || conceptInfo is LegacyEntityWithAutoCreatedViewInfo
                || conceptInfo is SqlQueryableInfo
                || conceptInfo is QueryableExtensionInfo;
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            DataStructureInfo info = (DataStructureInfo)conceptInfo;

            if (IsTypeSupported(info))
            {
                GenerateInitialCode(codeBuilder);

                codeBuilder.InsertCode(ImplementationCodeSnippet(info), MvcModelGeneratorTags.ModuleMembers);
            }
        }

        private static void GenerateInitialCode(ICodeBuilder codeBuilder)
        {
            if (_isInitialCallMade)
                return;
            _isInitialCallMade = true;
        }
    }
}