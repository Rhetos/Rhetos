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
    [ExportMetadata(MefProvider.Implements, typeof(DenyUserEditInfo))]
    public class DenyUserEditCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (DenyUserEditInfo)conceptInfo;
            codeBuilder.InsertCode(CheckChangesSnippet(info), WritableOrmDataStructureCodeGenerator.OldDataLoadedTag, info.Property.DataStructure);
            codeBuilder.AddReferencesFromDependency(typeof(UserException));
        }

        private static string CheckChangesSnippet(DenyUserEditInfo info)
        {
            return string.Format(
@"            if (checkUserPermissions)
            {{
                var invalidItem = updatedNew.Zip(updated, (newItem, oldItem) => new {{ newItem, oldItem }})
                    .Where(change => change.newItem.{3} != null && !change.newItem.{3}.Equals(change.oldItem.{3}) || change.newItem.{3} == null && change.oldItem.{3} != null)
                    .Select(change => change.newItem)
                    .FirstOrDefault();

                if (invalidItem == null)
                    invalidItem = insertedNew.Where(newItem => newItem.{3} != null).FirstOrDefault();

                if (invalidItem != null)
                    throw new Rhetos.UserException(
                        ""It is not allowed to directly enter {2} property of {0}.{1}."",
                        ""DataStructure:{0}.{1},ID:"" + invalidItem.ID + "",Property:{2}"");
            }}
",
                info.Property.DataStructure.Module.Name,
                info.Property.DataStructure.Name,
                info.Property.Name,
                GetComparedPropertyName(info.Property));
        }

        private static string GetComparedPropertyName(PropertyInfo propertyInfo)
        {
            if (propertyInfo is ReferencePropertyInfo)
                return propertyInfo.Name + "ID";
            return propertyInfo.Name;
        }
    }
}
