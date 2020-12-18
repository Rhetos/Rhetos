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
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Compiler;

namespace Rhetos.Dom.DefaultConcepts
{
    public static class PropertyHelper
    {
        public static readonly CsTag<PropertyInfo> AttributeTag = "Attribute";

        private static string PropertySnippet(PropertyInfo info, string propertyType)
        {
            return string.Format(
@"{2}
        public {1} {0} {{ get; set; }}
        ",
            info.Name,
            propertyType,
            AttributeTag.Evaluate(info));
        }

        [Obsolete("Use the GenerateCodeForType function without the 'serializable' argument. All regular properties are serializable.")]
        public static void GenerateCodeForType(PropertyInfo info, ICodeBuilder codeBuilder, string propertyType, bool serializable)
        {
            if (!serializable)
                throw new FrameworkException("All regular properties should be serializable.");
        }

        public static void GenerateCodeForType(PropertyInfo info, ICodeBuilder codeBuilder, string propertyType)
        {
            codeBuilder.InsertCode(PropertySnippet(info, propertyType), DataStructureCodeGenerator.BodyTag, info.DataStructure);
            codeBuilder.InsertCode("[DataMember]", AttributeTag, info);

            if (DslUtility.IsQueryable(info.DataStructure))
                codeBuilder.InsertCode(
                    string.Format(",\r\n                {0} = item.{0}", info.Name),
                    RepositoryHelper.AssignSimplePropertyTag, info.DataStructure);
        }

        public static void GenerateStorageMapping(PropertyInfo info, ICodeBuilder codeBuilder)
        {
            if (info.DataStructure is IWritableOrmDataStructure)
            {
                codeBuilder.InsertCode($@"new PersistenceStorageObjectParameter(""{info.Name}"", new SqlParameter("""", ((object)entity.{info.Name}) ?? DBNull.Value)),
                ", WritableOrmDataStructureCodeGenerator.PersistenceStorageMapperPropertyMappingTag, info.DataStructure);
            }
        }

        public static void GenerateStorageMappingForDecimalTypes(PropertyInfo info, ICodeBuilder codeBuilder, int scale, int precision)
        {
            if (info.DataStructure is IWritableOrmDataStructure)
            {
                var code = $@"new PersistenceStorageObjectParameter(""{info.Name}"", new SqlParameter("""", ((object)entity.{info.Name}) ?? DBNull.Value) {{ Scale = {scale}, Precision = {precision} }}),
                ";
                codeBuilder.InsertCode(code, WritableOrmDataStructureCodeGenerator.PersistenceStorageMapperPropertyMappingTag, info.DataStructure);
            }
        }
    }
}
