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
    [ExportMetadata(MefProvider.Implements, typeof(UserRequiredPropertyInfo))]
    public class UserRequiredPropertyCodeGenerator : IConceptCodeGenerator
    {
        public static readonly CsTag<UserRequiredPropertyInfo> OrCondition = "OrCondition";

        private string CheckDataSnippet(UserRequiredPropertyInfo info)
        {
            string dataStructureFullName = info.Property.DataStructure.FullName;

            return
            @"{
                var invalid = insertedNew.Concat(updatedNew).FirstOrDefault(item => item." + CsPropertyName(info.Property) + @" == null " + OrCondition.Evaluate(info) + @");
                if (invalid != null)
                    throw new Rhetos.UserException(""It is not allowed to enter {0} because the required property {1} is not set."",
                        new[] { """ + dataStructureFullName + @""", """ + info.Property.Name + @""" },
                        ""DataStructure:" + dataStructureFullName + @",ID:"" + invalid.ID.ToString() + "",Property:" + CsPropertyName(info.Property) + @""", null);
            }
            ";
        }

        private string CsPropertyName(PropertyInfo property)
        {
            return property is ReferencePropertyInfo ? property.Name + "ID" : property.Name;
        }

        public static bool IsSupported(PropertyInfo info)
        {
            return info.DataStructure is IWritableOrmDataStructure;
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (UserRequiredPropertyInfo)conceptInfo;
            if (IsSupported(info.Property))
            {
                codeBuilder.InsertCode(CheckDataSnippet(info), WritableOrmDataStructureCodeGenerator.ArgumentValidationTag, info.Property.DataStructure);
                codeBuilder.AddReferencesFromDependency(typeof(UserException));
            }
        }
    }
}
