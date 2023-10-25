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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(MoneyPropertyInfo))]
    public class MoneyPropertyCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (MoneyPropertyInfo)conceptInfo;
            PropertyHelper.GenerateCodeForType(info, codeBuilder, "decimal?");
            PropertyHelper.GenerateStorageMapping(info, codeBuilder, "System.Data.SqlDbType.Money");

            if (info.DataStructure is IWritableOrmDataStructure)
            {
                var property = $"item.{info.Name}";
                int roundingFactor = 100; // 2 decimals.

                codeBuilder.InsertCode(
                    @$"{property} = {property} != null ? (long)({property}.Value * {roundingFactor}m) / {roundingFactor}m : null;
                    ",
                    MoneyRoundingInfoCodeGenerator.MoneyRoundingPropertiesTag,
                    info.MoneyRoundingInfo_Dependency);
            }
        }
    }

    /// <summary>
    /// Inject the <see cref="CommonConceptsRuntimeOptions"/>,
    /// so the money rounding can be conditionally executed depending on runtime setting.
    /// See more: <seealso cref="MoneyPropertyCodeGenerator"/>
    /// </summary>
    [Export(typeof(IConceptMacro))]
    public class MoneyRoundingMacro : IConceptMacro<MoneyRoundingInfo>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(MoneyRoundingInfo conceptInfo, IDslModel existingConcepts)
        {
            var newConcepts = new List<IConceptInfo>();

            if (conceptInfo.DataStructure is IWritableOrmDataStructure)
                newConcepts.Add(
                    new RepositoryUsesInfo
                    {
                        DataStructure = conceptInfo.DataStructure,
                        PropertyName = "_commonConceptsRuntimeOptions",
                        PropertyType = "Rhetos.Dom.DefaultConcepts.CommonConceptsRuntimeOptions"
                    });

            return newConcepts;
        }
    }

    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(MoneyRoundingInfo))]
    public class MoneyRoundingInfoCodeGenerator : IConceptCodeGenerator
    {
        public static readonly CsTag<MoneyRoundingInfo> MoneyRoundingPropertiesTag = "MoneyRounding Properties";

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (MoneyRoundingInfo)conceptInfo;

            if (info.DataStructure is IWritableOrmDataStructure)
            {
                codeBuilder.InsertCode(
            $@"if (_commonConceptsRuntimeOptions.AutoRoundMoney)
            {{
                foreach (var item in insertedNew.Concat(updatedNew))
                {{
                    {MoneyRoundingPropertiesTag.Evaluate(info)}
                }}
            }}
            ",
                WritableOrmDataStructureCodeGenerator.InitializationTag,
                info.DataStructure);
            }
        }
    }
}
