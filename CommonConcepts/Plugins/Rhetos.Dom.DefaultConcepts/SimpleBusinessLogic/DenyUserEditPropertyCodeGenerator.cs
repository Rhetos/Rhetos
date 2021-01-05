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
using System.ComponentModel.Composition;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(DenyUserEditPropertyInfo))]
    public class DenyUserEditPropertyCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (DenyUserEditPropertyInfo)conceptInfo;
            codeBuilder.InsertCode(CheckChangesOnInsertSnippet(info), WritableOrmDataStructureCodeGenerator.ArgumentValidationTag, info.Property.DataStructure);
            codeBuilder.InsertCode(CheckChangesOnUpdateSnippet(info), WritableOrmDataStructureCodeGenerator.OldDataLoadedTag, info.Property.DataStructure);
        }

        private static string ThrowExceptionSnippet(DenyUserEditPropertyInfo info)
        {
            return $@"if (invalidItem != null)
                    throw new Rhetos.UserException(
                        ""It is not allowed to directly enter {{0}} property of {{1}}."",
                        new[] {{ ""{info.Property.Name}"", ""{info.Property.DataStructure.Module.Name}.{info.Property.DataStructure.Name}"" }},
                        ""DataStructure:{info.Property.DataStructure.Module.Name}.{info.Property.DataStructure.Name},ID:"" + invalidItem.ID + "",Property:{info.Property.Name}"",
                        null);
            ";
        }


        private static string CheckChangesOnInsertSnippet(DenyUserEditPropertyInfo info)
        {
            return $@"if (checkUserPermissions)
            {{
                var invalidItem = insertedNew.Where(newItem => newItem.{GetComparedPropertyName(info.Property)} != null).FirstOrDefault();

                {ThrowExceptionSnippet(info)}
            }}

            ";
        }

        private static string CheckChangesOnUpdateSnippet(DenyUserEditPropertyInfo info)
        {
            return $@"if (checkUserPermissions)
            {{
                var changes = updatedNew.Zip(updated, (newItem, oldItem) => new {{ newItem, oldItem }});
                foreach (var change in changes)
                    if (change.newItem.{GetComparedPropertyName(info.Property)} == null && change.oldItem.{GetComparedPropertyName(info.Property)} != null)
                        change.newItem.{GetComparedPropertyName(info.Property)} = change.oldItem.{GetComparedPropertyName(info.Property)};
                var invalidItem = changes
                    .Where(change => change.newItem.{GetComparedPropertyName(info.Property)} != null && !change.newItem.{GetComparedPropertyName(info.Property)}.Equals(change.oldItem.{GetComparedPropertyName(info.Property)}) || change.newItem.{GetComparedPropertyName(info.Property)} == null && change.oldItem.{GetComparedPropertyName(info.Property)} != null)
                    .Select(change => change.newItem)
                    .FirstOrDefault();

                {ThrowExceptionSnippet(info)}
            }}

            ";
        }

        private static string GetComparedPropertyName(PropertyInfo propertyInfo)
        {
            if (propertyInfo is ReferencePropertyInfo)
                return propertyInfo.Name + "ID";
            return propertyInfo.Name;
        }
    }
}
