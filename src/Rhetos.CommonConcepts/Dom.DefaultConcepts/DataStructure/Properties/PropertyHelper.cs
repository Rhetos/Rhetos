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

        public static void GenerateCodeForType(PropertyInfo info, ICodeBuilder codeBuilder, string propertyType)
        {
            codeBuilder.InsertCode(PropertySnippet(info, propertyType), DataStructureCodeGenerator.BodyTag, info.DataStructure);

            if (DslUtility.IsQueryable(info.DataStructure))
                codeBuilder.InsertCode(
                    string.Format(",\r\n                {0} = item.{0}", info.Name),
                    RepositoryHelper.AssignSimplePropertyTag, info.DataStructure);
        }

        public static void GenerateStorageMapping(PropertyInfo info, ICodeBuilder codeBuilder, ISqlResources sqlResources, int precision = 0, int scale = 0, string overrideDbParameterType = null)
        {
            if (info.DataStructure is IWritableOrmDataStructure)
            {
                string options = "";
                if (precision > 0)
                    options += $", Precision = {precision}";
                if (scale > 0)
                    options += $", Scale = {scale}";

                string dbParameterType = overrideDbParameterType
                    ?? sqlResources.FindSqlResourceKeyPropertyType("StorageMappingDbType_", info).SqlScript
                    ?? throw new DslConceptSyntaxException(info, $"There is no DbParameter type provided for '{info.GetKeywordOrTypeName()}' property type."
                        + " This property type is not supported on a writable data structure for the current database language.");

                string dbParameterClass = sqlResources.Get("DbParameterClass");

                string code = $@"new PersistenceStorageObjectParameter(""{info.Name}"", new {dbParameterClass}("""", {dbParameterType}) {{ Value = ((object)entity.{info.Name}) ?? DBNull.Value{options} }}),
                ";

                codeBuilder.InsertCode(code, WritableOrmDataStructureCodeGenerator.PersistenceStorageMapperPropertyMappingTag, info.DataStructure);
            }
        }
    }
}
