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
using Rhetos.MvcGenerator;

namespace Rhetos.MvcGenerator.DefaultConcepts
{
    [Export(typeof(IMvcGeneratorPlugin))]
    [ExportMetadata(MefProvider.Implements, typeof(PropertyInfo))]
    public class BinaryPropertyCodeGenerator : IMvcGeneratorPlugin
    {
        public class PropertyTag : Tag<PropertyInfo>
        {
            public PropertyTag(TagType tagType, string tagFormat, string nextTagFormat = null, string firstEvaluationContext = null, string nextEvaluationContext = null)
                : base(tagType, tagFormat, (info, format) => string.Format(CultureInfo.InvariantCulture, format, info.DataStructure.Module.Name, info.DataStructure.Name, info.Name), nextTagFormat, firstEvaluationContext, nextEvaluationContext)
            { }
        }

        private static string ImplementationCodeSnippet(PropertyInfo info)
        {
            return string.Format(@"" + MvcGeneratorTags.ImplementationPropertyAttributeMembers.Replace("PROPERTY_ATTRIBUTE", info.DataStructure.Module.Name + "_" + info.DataStructure.Name + "_" + info.Name) + @"
            public byte[] {0} {{ get; set; }}
            
            ", info.Name);
        }

        private static bool _isInitialCallMade;

        public static bool IsTypeSupported(PropertyInfo conceptInfo)
        {
            return conceptInfo is BinaryPropertyInfo;
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            PropertyInfo info = (PropertyInfo)conceptInfo;

            if (IsTypeSupported(info) && DataStructureCodeGenerator.IsTypeSupported(info.DataStructure))
            {
                GenerateInitialCode(codeBuilder);

                codeBuilder.InsertCode(ImplementationCodeSnippet(info), MvcGeneratorTags.ImplementationPropertyMembers.Replace("ENTITY", info.DataStructure.Module.Name + "_" + info.DataStructure.Name));
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