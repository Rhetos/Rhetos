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
using Rhetos.Utilities;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(DataStructureInfo))]
    public class DataStructureQueryableCodeGenerator : IConceptCodeGenerator
    {
        public static readonly CsTag<DataStructureInfo> AttributesTag = "QueryableClassAttributes";
        public static readonly CsTag<DataStructureInfo> InterfaceTag = new CsTag<DataStructureInfo>("QueryableClassInterace", TagType.Appendable, ", {0}");
        public static readonly CsTag<DataStructureInfo> MembersTag = "QueryableClassMembers";

        protected static string SnippetQueryableClass(DataStructureInfo info)
        {
            return string.Format(
    @"{0}
    public class {1}_{2} : global::{1}.{2}{3}
    {{
        {4}
    }}

    ",
            AttributesTag.Evaluate(info),
            info.Module.Name,
            info.Name,
            InterfaceTag.Evaluate(info),
            MembersTag.Evaluate(info));
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            DataStructureInfo info = (DataStructureInfo)conceptInfo;
            codeBuilder.InsertCode(SnippetQueryableClass(info), DomInitializationCodeGenerator.CommonQueryableMemebersTag);
        }

        public static string GetPropertyAttributeTag(DataStructureInfo dataStructure, string propertyName)
        {
            return string.Format("/*PropertyAttribute {0}.{1}.{2}*/", dataStructure.Module.Name, dataStructure.Name, propertyName);
        }

        /// <param name="alternativeScalarPropertyName">
        /// (Optional) Name of the scalar property that the navigational property is based on. It is used in the error message to the user.
        /// </param>
        public static void AddNavigationalProperty(ICodeBuilder codeBuilder, DataStructureInfo dataStructure, string propertyName, string propertyType, string alternativeScalarPropertyName)
        {
            string getterSetterformat = string.IsNullOrEmpty(alternativeScalarPropertyName)
                ? @"get {{ throw new Rhetos.FrameworkException(string.Format(Common.Infrastructure.ErrorGetNavigationalPropertyWithoutOrm, {3})); }}
            set {{ throw new Rhetos.FrameworkException(string.Format(Common.Infrastructure.ErrorSetNavigationalPropertyWithoutOrm, {3})); }}"
                : @"get {{ throw new Rhetos.FrameworkException(string.Format(Common.Infrastructure.ErrorGetNavigationalPropertyWithAlternativeWithoutOrm, {3}, {4})); }}
            set {{ throw new Rhetos.FrameworkException(string.Format(Common.Infrastructure.ErrorSetNavigationalPropertyWithAlternativeWithoutOrm, {3}, {4})); }}";

            string propertySnippet = string.Format(
        @"{2}
        public virtual {1} {0}
        {{
            " + getterSetterformat + @"
        }}

        ",
                propertyName,
                propertyType,
                GetPropertyAttributeTag(dataStructure, propertyName),
                CsUtility.QuotedString(propertyName),
                CsUtility.QuotedString(alternativeScalarPropertyName));

            codeBuilder.InsertCode(propertySnippet, DataStructureQueryableCodeGenerator.MembersTag, dataStructure);
        }

        public static void AddInterfaceAndReference(ICodeBuilder codeBuilder, Type type, DataStructureInfo dataStructureInfo)
        {
            AddInterfaceAndReference(codeBuilder, type.FullName, type, dataStructureInfo);
        }

        public static void AddInterfaceAndReference(ICodeBuilder codeBuilder, string typeName, Type type, DataStructureInfo dataStructureInfo)
        {
            codeBuilder.InsertCode(typeName, DataStructureQueryableCodeGenerator.InterfaceTag, dataStructureInfo);
            codeBuilder.AddReferencesFromDependency(type);
        }
    }
}
