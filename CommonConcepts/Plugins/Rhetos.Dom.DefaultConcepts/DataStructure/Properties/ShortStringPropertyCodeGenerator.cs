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
    [ExportMetadata(MefProvider.Implements, typeof(ShortStringPropertyInfo))]
    public class ShortStringPropertyCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            PropertyInfo info = (PropertyInfo)conceptInfo;
            PropertyHelper.GenerateCodeForType(info, codeBuilder, "string");

            if (info.DataStructure is IWritableOrmDataStructure)
                codeBuilder.InsertCode(LimitStringLengthOnSaveSnippet(info), WritableOrmDataStructureCodeGenerator.ArgumentValidationTag, info.DataStructure);

            // TODO: Implement error handling of the maximum length for filter parameters and any other data (sent from client) that is used in a way other than Save function.
        }

        private string LimitStringLengthOnSaveSnippet(PropertyInfo info)
        {
            return string.Format(
            @"{{
                var invalidItem = insertedNew.Concat(updatedNew).Where(newItem => newItem.{2} != null && newItem.{2}.Length > {3}).FirstOrDefault();
                if (invalidItem != null)
                    throw new Rhetos.UserException(
                        ""Maximum length of property {{0}} is {{1}}."",
                        new[] {{ ""{1}.{2}"", ""{3}"" }},
                        ""DataStructure:{0}.{1},ID:"" + invalidItem.ID.ToString() + "",Property:{2}"",
                        null);
            }}
            ",
                    info.DataStructure.Module.Name,
                    info.DataStructure.Name,
                    info.Name,
                    ShortStringPropertyInfo.MaxLength);
        }
    }
}
