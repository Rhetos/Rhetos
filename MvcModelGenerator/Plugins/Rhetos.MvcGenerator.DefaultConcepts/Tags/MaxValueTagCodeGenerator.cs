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
    [ExportMetadata(MefProvider.Implements, typeof(MaxValueInfo))]
    public class MaxValueTagCodeGenerator : IMvcGeneratorPlugin
    {
        public class MaxValueTag : Tag<MaxValueInfo>
        {
            public MaxValueTag(TagType tagType, string tagFormat, string nextTagFormat = null, string firstEvaluationContext = null, string nextEvaluationContext = null)
                : base(tagType, tagFormat, (info, format) => string.Format(CultureInfo.InvariantCulture, format, info.Property.DataStructure.Module.Name, info.Property.DataStructure.Name, info.Property.Name, "Required"), nextTagFormat, firstEvaluationContext, nextEvaluationContext)
            { }
        }

        private static string ImplementationCodeSnippet(MaxValueInfo info)
        {
            var typeRange = (info.Property is IntegerPropertyInfo) ? "Integer" :
                (info.Property is MoneyPropertyInfo || info.Property is DecimalPropertyInfo) ? "Decimal" :
                (info.Property is DatePropertyInfo || info.Property is DateTimePropertyInfo) ? "Date" : "";

            return string.Format(@"[MaxValue{0}(MaxValue = ""{1}"", ErrorMessage = ""Value for {2} must be less than or equal to {1}."")]
            ", typeRange, info.Value.ToString(), info.Property.Name);
        }

        private static bool _isInitialCallMade;

        public static bool IsTypeSupported(MaxValueInfo conceptInfo)
        {
            return conceptInfo is MaxValueInfo;
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            MaxValueInfo info = (MaxValueInfo)conceptInfo;

            if (IsTypeSupported(info) && DataStructureCodeGenerator.IsTypeSupported(info.Property.DataStructure))
            {
                GenerateInitialCode(codeBuilder);
                try
                {
                    codeBuilder.InsertCode(ImplementationCodeSnippet(info), MvcGeneratorTags.ImplementationPropertyAttributeMembers.Replace("PROPERTY_ATTRIBUTE", info.Property.DataStructure.Module.Name + "_" + info.Property.DataStructure.Name + "_" + info.Property.Name));
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