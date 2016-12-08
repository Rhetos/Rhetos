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
using Rhetos.Dsl.DefaultConcepts;
using System.Globalization;
using System.ComponentModel.Composition;
using Rhetos.Extensibility;
using Rhetos.Dsl;
using Rhetos.Compiler;

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
            codeBuilder.AddReferencesFromDependency(typeof(UserException));
        }

        private static string ThrowExceptionSnippet(DenyUserEditPropertyInfo info)
        {
            return string.Format(
            @"if (invalidItem != null)
                    throw new Rhetos.UserException(
                        ""It is not allowed to directly enter {{0}} property of {{1}}."",
                        new[] {{ ""{2}"", ""{0}.{1}"" }},
                        ""DataStructure:{0}.{1},ID:"" + invalidItem.ID + "",Property:{2}"",
                        null);
            ",
                info.Property.DataStructure.Module.Name,
                info.Property.DataStructure.Name,
                info.Property.Name,
                GetComparedPropertyName(info.Property));
        }


        private static string CheckChangesOnInsertSnippet(DenyUserEditPropertyInfo info)
        {
            return string.Format(
            @"if (checkUserPermissions)
            {{
                var invalidItem = insertedNew.Where(newItem => newItem.{3} != null).FirstOrDefault();

                {4}
            }}

            ",
                info.Property.DataStructure.Module.Name,
                info.Property.DataStructure.Name,
                info.Property.Name,
                GetComparedPropertyName(info.Property),
                ThrowExceptionSnippet(info));
        }

        private static string CheckChangesOnUpdateSnippet(DenyUserEditPropertyInfo info)
        {
            return string.Format(
            @"if (checkUserPermissions)
            {{
                var changes = updatedNew.Zip(updated, (newItem, oldItem) => new {{ newItem, oldItem }});
                foreach (var change in changes)
                    if (change.newItem.{3} == null && change.oldItem.{3} != null)
                        change.newItem.{3} = change.oldItem.{3};
                var invalidItem = changes
                    .Where(change => change.newItem.{3} != null && !change.newItem.{3}.Equals(change.oldItem.{3}) || change.newItem.{3} == null && change.oldItem.{3} != null)
                    .Select(change => change.newItem)
                    .FirstOrDefault();

                {4}
            }}

            ",
                info.Property.DataStructure.Module.Name,
                info.Property.DataStructure.Name,
                info.Property.Name,
                GetComparedPropertyName(info.Property),
                ThrowExceptionSnippet(info));
        }

        private static string GetComparedPropertyName(PropertyInfo propertyInfo)
        {
            if (propertyInfo is ReferencePropertyInfo)
                return propertyInfo.Name + "ID";
            return propertyInfo.Name;
        }
    }
}
