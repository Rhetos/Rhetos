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
using System.ComponentModel.Composition;
using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(DefaultValueInfo))]
    public class DefaultValueCodeGenerator : IConceptCodeGenerator
    {
        /// <summary>Inserted code can use enumerables "insertedNew", "updatedNew" and "deletedIds" but without navigation properties, because they are not binded to ORM.
        /// Set bool variable setDefaultValue to false if you don't want default value to be assigned to a property
        /// </summary>
        public static readonly CsTag<DataStructureInfo> DefaultValueValidationTag = "Item DefaultValuetValidation";

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (DefaultValueInfo)conceptInfo;
            codeBuilder.InsertCode(GenerateFuncAndCallForProperty(info, true), WritableOrmDataStructureCodeGenerator.InitializationTag, info.Property.DataStructure);
        }

        private string GenerateFuncAndCallForProperty(DefaultValueInfo info, bool saveFunction)
        {
            var propertyName = info.Property is ReferencePropertyInfo ? info.Property.Name + "ID" : info.Property.Name;
            return $@"
            {{
                var defaultValueFunc_{propertyName} = Function<{info.Property.DataStructure.Module}.{info.Property.DataStructure.Name}>.Create({info.Expression});

                foreach (var _item in insertedNew)
                {{
                    bool setDefaultValue_{propertyName} = _item.{propertyName} == null;
                    {DefaultValueCodeGenerator.DefaultValueValidationTag.Evaluate(info.Property.DataStructure)}
                    if (setDefaultValue_{propertyName})
                        _item.{propertyName} = defaultValueFunc_{propertyName}(_item);
                }}
            }}
            ";
        }
    }
}
