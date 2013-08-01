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
    [ExportMetadata(MefProvider.Implements, typeof(RegExMatchInfo))]
    public class RegExTagCodeGenerator : IMvcModelGeneratorPlugin
    {
        public class RegExTag : Tag<RegExMatchInfo>
        {
            public RegExTag(TagType tagType, string tagFormat, string nextTagFormat = null, string firstEvaluationContext = null, string nextEvaluationContext = null)
                : base(tagType, tagFormat, (info, format) => string.Format(CultureInfo.InvariantCulture, format, info.Property.DataStructure.Module.Name, info.Property.DataStructure.Name, info.Property.Name, "Required"), nextTagFormat, firstEvaluationContext, nextEvaluationContext)
            { }
        }

        private static string ImplementationCodeSnippet(RegExMatchInfo info)
        {
            return string.Format(@"[RegularExpression(@""{1}"", ErrorMessage = ""Property {0} doesn't match required format."")]
            ", info.Property.Name, info.Regex2);
        }

        private static bool _isInitialCallMade;

        public static bool IsTypeSupported(RegExMatchInfo conceptInfo)
        {
            return conceptInfo is RegExMatchInfo;
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            RegExMatchInfo info = (RegExMatchInfo)conceptInfo;

            if (IsTypeSupported(info) && DataStructureCodeGenerator.IsTypeSupported(info.Property.DataStructure))
            {
                GenerateInitialCode(codeBuilder);
                try
                {
                    codeBuilder.InsertCode(ImplementationCodeSnippet(info), MvcModelGeneratorTags.ImplementationPropertyAttributeMembers.Replace("PROPERTY_ATTRIBUTE", info.Property.DataStructure.Module.Name + "_" + info.Property.DataStructure.Name + "_" + info.Property.Name));
                }
                catch { }
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