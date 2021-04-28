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
    [ExportMetadata(MefProvider.Implements, typeof(ShortStringPropertyInfo))]
    public class ShortStringPropertyCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            PropertyInfo info = (PropertyInfo)conceptInfo;
            PropertyHelper.GenerateCodeForType(info, codeBuilder, "string");
            PropertyHelper.GenerateStorageMapping(info, codeBuilder, "System.Data.SqlDbType.NVarChar"); // Size is automatically set by SqlProperty, it should not be specified in storage mapping, to avoid automatic truncating longer strings.

            if (info.DataStructure is IWritableOrmDataStructure)
                codeBuilder.InsertCode(LimitStringLengthOnSaveSnippet(info), WritableOrmDataStructureCodeGenerator.OldDataLoadedTag, info.DataStructure);

            // TODO: Implement error handling of the maximum length for filter parameters and any other data (sent from client) that is used in a way other than Save function.
        }

        private string LimitStringLengthOnSaveSnippet(PropertyInfo info)
        {
            return $@"foreach (var newItem in insertedNew.Concat(updatedNew))
                {nameof(ShortStringPropertyCodeGenerator)}.{nameof(CheckMaxLength)}(newItem.{info.Name}, newItem, ""{info.DataStructure.Module.Name}"", ""{info.DataStructure.Name}"", ""{info.Name}"");

            ";
        }

        public static void CheckMaxLength(string propertyValue, IEntity invalidItem, string moduleName, string entityName, string propertyName)
        {
            if (propertyValue != null && propertyValue.Length > ShortStringPropertyInfo.MaxLength)
                throw new UserException(
                    "Maximum length of property {0} is {1}.",
                    new object[] { $"{entityName}.{propertyName}", ShortStringPropertyInfo.MaxLength },
                    $"DataStructure:{moduleName}.{entityName},ID:{invalidItem.ID},Property:{propertyName}",
                    null);
        }
    }
}
