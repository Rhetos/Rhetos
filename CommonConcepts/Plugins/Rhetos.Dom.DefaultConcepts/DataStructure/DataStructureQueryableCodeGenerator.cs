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

using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using System;
using System.ComponentModel.Composition;
using System.Linq;

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

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            DataStructureInfo info = (DataStructureInfo)conceptInfo;

            if (DslUtility.IsQueryable(info))
            {
                codeBuilder.InsertCode(SnippetQueryableClass(info), ModuleCodeGenerator.CommonQueryableMemebersTag, info.Module);
                codeBuilder.InsertCode("IDetachOverride", InterfaceTag, info);
                codeBuilder.InsertCode("bool IDetachOverride.Detaching { get; set; }\r\n\r\n        ", MembersTag, info);
            }
        }

        protected static string SnippetQueryableClass(DataStructureInfo info)
        {
            return string.Format(
    AttributesTag.Evaluate(info) + @"
    public class {0}_{1} : global::{0}.{1}, IQueryableEntity<{0}.{1}>, System.IEquatable<{0}_{1}>" + InterfaceTag.Evaluate(info) + @"
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
        public static string SetterBodyTag(DataStructureInfo dataStructure, string csPropertyName)
        {
            return string.Format("/*DataStructureQueryable Setter {0}.{1}.{2}*/", dataStructure.Module.Name, dataStructure.Name, csPropertyName);
        }

        /// <param name="csPropertyName">Name of the navigation property in a C# class. A PropertyInfo with that name might not exist in the DSL model.</param>
        /// <param name="additionalSetterCode">Optional.</param>
        public static void AddNavigationPropertyWithBackingField(ICodeBuilder codeBuilder, DataStructureInfo dataStructure, string csPropertyName, string propertyType, string additionalSetterCode)
        {
            string propertySnippet =
                "private " + propertyType + " " + BackingFieldName(csPropertyName) + @";

        " + PropertyAttributeTag(dataStructure, csPropertyName) + @"
        public virtual " + propertyType + @" " + csPropertyName + @"
        {
            get
            {
                " + GetterBodyTag(dataStructure, csPropertyName) + @"
                return " + BackingFieldName(csPropertyName) + @";
            }
            set
            {
                if (((IDetachOverride)this).Detaching) return;
                " + SetterBodyTag(dataStructure, csPropertyName) + @"
                " + BackingFieldName(csPropertyName) + @" = value;" + (!string.IsNullOrEmpty(additionalSetterCode) ? "\r\n                " + additionalSetterCode : "") + @"
            }
        }

        ";

            codeBuilder.InsertCode(propertySnippet, DataStructureQueryableCodeGenerator.MembersTag, dataStructure);
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
        }
    }
}
