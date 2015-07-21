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
    [ExportMetadata(MefProvider.DependsOn, typeof(DataStructureCodeGenerator))]
    public class DataStructureQueryableCodeGenerator : IConceptCodeGenerator
    {
        public static readonly CsTag<DataStructureInfo> AttributesTag = "QueryableClassAttributes";
        public static readonly CsTag<DataStructureInfo> InterfaceTag = new CsTag<DataStructureInfo>("QueryableClassInterace", TagType.Appendable, ", {0}");
        public static readonly CsTag<DataStructureInfo> MembersTag = "QueryableClassMembers";

        protected static string SnippetQueryableClass(DataStructureInfo info)
        {
            return string.Format(
    AttributesTag.Evaluate(info) + @"
    public class {0}_{1} : global::{0}.{1}, System.IEquatable<{0}_{1}>" + InterfaceTag.Evaluate(info) + @"
    {{
        " + MembersTag.Evaluate(info) + @"

        public bool Equals({0}_{1} other)
        {{
            return other != null && other.ID == ID;
        }}
    }}

    ",
            info.Module.Name,
            info.Name);
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            DataStructureInfo info = (DataStructureInfo)conceptInfo;

            if (DslUtility.IsQueryable(info))
                codeBuilder.InsertCode(SnippetQueryableClass(info), DomInitializationCodeGenerator.CommonQueryableMemebersTag);
        }

        /// <param name="csPropertyName">The csPropertyName argument refers to a C# class property, not the PropertyInfo concept.</param>
        public static string PropertyAttributeTag(DataStructureInfo dataStructure, string csPropertyName)
        {
            return string.Format("/*DataStructureQueryable PropertyAttribute {0}.{1}.{2}*/", dataStructure.Module.Name, dataStructure.Name, csPropertyName);
        }

        /// <param name="csPropertyName">The csPropertyName argument refers to a C# class property, not the PropertyInfo concept.</param>
        public static string GetterBodyTag(DataStructureInfo dataStructure, string csPropertyName)
        {
            return string.Format("/*DataStructureQueryable Getter {0}.{1}.{2}*/", dataStructure.Module.Name, dataStructure.Name, csPropertyName);
        }

        /// <param name="csPropertyName">The csPropertyName argument refers to a C# class property, not the PropertyInfo concept.</param>
        public static string SetterTag(DataStructureInfo dataStructure, string csPropertyName)
        {
            return string.Format("/*DataStructureQueryable Setter {0}.{1}.{2}*/", dataStructure.Module.Name, dataStructure.Name, csPropertyName);
        }

        /// <param name="csPropertyName">
        /// Name of the navigation property in a C# class. A PropertyInfo with that name might not exist in the DSL model.</param>
        /// <param name="alternativeScalarPropertyName">
        /// (Optional) Name of the scalar property that the navigation property is based on. It is used in the error message to the user.
        /// </param>
        public static void AddNavigationProperty(ICodeBuilder codeBuilder, DataStructureInfo dataStructure, string csPropertyName, string propertyType, string alternativeScalarPropertyName)
        {
            string quotedProperty = CsUtility.QuotedString(csPropertyName);
            string quotedAlternative = CsUtility.QuotedString(alternativeScalarPropertyName);

            string instanceGetterException = string.IsNullOrEmpty(alternativeScalarPropertyName)
                ? "throw new Rhetos.FrameworkException(string.Format(Common.Infrastructure.ErrorGetNavigationPropertyWithoutOrm, " + quotedProperty + "));"
                : "throw new Rhetos.FrameworkException(string.Format(Common.Infrastructure.ErrorGetNavigationPropertyWithAlternativeWithoutOrm, " + quotedProperty + ", " + quotedAlternative + "));";

            string propertySnippet =
                PropertyAttributeTag(dataStructure, csPropertyName) + @"
        public virtual " + propertyType + @" " + csPropertyName + @"
        {
            get
            {
                " + GetterBodyTag(dataStructure, csPropertyName) + instanceGetterException + @"
            }
            " + SetterTag(dataStructure, csPropertyName) + @"
        }

        ";

            codeBuilder.InsertCode(propertySnippet, DataStructureQueryableCodeGenerator.MembersTag, dataStructure);
        }

        public static void AddNavigationPropertyBackingField(ICodeBuilder codeBuilder, DataStructureInfo dataStructure, string csPropertyName, string propertyType, string additionalCode)
        {
            codeBuilder.InsertCode(
                "private " + propertyType + " " + BackingFieldName(csPropertyName) + ";\r\n\r\n        ",
                DataStructureQueryableCodeGenerator.MembersTag, dataStructure);

            codeBuilder.InsertCode(
                "return " + BackingFieldName(csPropertyName) + ";\r\n                //",
                DataStructureQueryableCodeGenerator.GetterBodyTag(dataStructure, csPropertyName));

            string setter = @"set
            {
                if (((IDetachOverride)this).Detaching) return;
                " + BackingFieldName(csPropertyName) + @" = value;" + (!string.IsNullOrEmpty(additionalCode) ? "\r\n                " + additionalCode : "") + @"
            }";
            codeBuilder.InsertCode(setter, DataStructureQueryableCodeGenerator.SetterTag(dataStructure, csPropertyName));
        }

        private static string BackingFieldName(string csPropertyName)
        {
            return "_" + char.ToLower(csPropertyName.First()) + csPropertyName.Substring(1);
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
