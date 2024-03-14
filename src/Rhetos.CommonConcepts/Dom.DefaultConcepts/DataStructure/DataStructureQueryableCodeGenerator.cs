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

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(DataStructureInfo))]
    [ExportMetadata(MefProvider.DependsOn, typeof(DataStructureCodeGenerator))]
    public class DataStructureQueryableCodeGenerator : IConceptCodeGenerator
    {
        public static readonly CsTag<DataStructureInfo> AttributesTag = "QueryableClassAttributes";

        /// <summary>
        /// Add an interface to the queryable model class for the DataStructure.
        /// </summary>
        /// <remarks>
        /// Use codeBuilder.InsertCode to insert <b>FullName of the interface type</b> at this tag.
        /// The tag is configured to automatically add ':' and ',' to separate class name and interfaces.
        /// See also <see cref="DataStructureCodeGenerator.InterfaceTag"/>.
        /// </remarks>
        public static readonly CsTag<DataStructureInfo> InterfaceTag = new ("QueryableClassInterace", TagType.Appendable, ", {0}");

        public static readonly CsTag<DataStructureInfo> MembersTag = "QueryableClassMembers";

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            DataStructureInfo info = (DataStructureInfo)conceptInfo;

            if (DslUtility.IsQueryable(info))
            {
                string module = info.Module.Name;
                string entity = info.Name;

                string queryableClass =
    $@"{AttributesTag.Evaluate(info)}
    public class {module}_{entity} : global::{module}.{entity}, IQueryableEntity<{module}.{entity}>, System.IEquatable<{module}_{entity}>{InterfaceTag.Evaluate(info)}
    {{
        {MembersTag.Evaluate(info)}

        public bool Equals({module}_{entity} other)
        {{
            return other != null && other.ID == ID;
        }}

        /// <summary>Converts the object with navigation properties to a simple object with primitive properties.</summary>
        public {module}.{entity} ToSimple()
        {{
            var item = this;
            return new {module}.{entity}
            {{
                ID = item.ID{RepositoryHelper.AssignSimplePropertyTag.Evaluate(info)}
            }};
        }}
    }}

    ";
                codeBuilder.InsertCode(queryableClass, ModuleCodeGenerator.CommonQueryableMembersTag, info.Module);

                string snippetToSimpleObjectsConversion = $@"/// <summary>Converts the objects with navigation properties to simple objects with primitive properties.</summary>
        public static IQueryable<{module}.{entity}> ToSimple(this IQueryable<Common.Queryable.{module}_{entity}> query)
        {{
            return query.Select(item => new {module}.{entity}
            {{
                ID = item.ID{RepositoryHelper.AssignSimplePropertyTag.Evaluate(info)}
            }});
        }}
        ";
                codeBuilder.InsertCode(snippetToSimpleObjectsConversion, DomInitializationCodeGenerator.QueryExtensionsMembersTag);
            }
        }

        /// <param name="csPropertyName">The csPropertyName argument refers to a C# class property, not the PropertyInfo concept.</param>
        public static string PropertyAttributeTag(DataStructureInfo dataStructure, string csPropertyName)
        {
            return string.Format("/*DataStructureQueryable PropertyAttribute {0}.{1}.{2}*/", dataStructure.Module.Name, dataStructure.Name, csPropertyName);
        }

        /// <param name="csPropertyName">Name of the navigation property in a C# class. A PropertyInfo with that name might not exist in the DSL model.</param>
        public static void AddNavigationProperty(ICodeBuilder codeBuilder, DataStructureInfo dataStructure, string csPropertyName, string propertyType)
        {
            string propertySnippet = $@"{PropertyAttributeTag(dataStructure, csPropertyName)}
        public virtual {propertyType} {csPropertyName} {{ get; init; }}

        ";

            codeBuilder.InsertCode(propertySnippet, MembersTag, dataStructure);
        }

        /// <summary>
        /// Add an interface to the queryable model class for the DataStructure.
        /// </summary>
        public static void AddInterfaceAndReference(ICodeBuilder codeBuilder, Type type, DataStructureInfo dataStructureInfo)
        {
            AddInterfaceAndReference(codeBuilder, type.FullName, dataStructureInfo);
        }

        /// <summary>
        /// Add an interface to the queryable model class for the DataStructure.
        /// </summary>
        public static void AddInterfaceAndReference(ICodeBuilder codeBuilder, string typeName, DataStructureInfo dataStructureInfo)
        {
            codeBuilder.InsertCode(typeName, InterfaceTag, dataStructureInfo);
        }
    }
}
