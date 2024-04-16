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
using Rhetos.DatabaseGenerator.DefaultConcepts;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using Rhetos.Utilities;
using System.ComponentModel.Composition;

namespace Rhetos.EfCore.ModelBuilding
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(PropertyInfo))]
    public class PropertyModelCodeGenerator : IConceptCodeGenerator
    {
        public static readonly CsTag<PropertyInfo> ModelOptionsTag = "ModelOptions";

        private readonly CommonConceptsDatabaseSettings _databaseSettings;
        private readonly ConceptMetadata _conceptMetadata;
        private readonly ISqlUtility _sqlUtility;

        public PropertyModelCodeGenerator(CommonConceptsDatabaseSettings databaseSettings, ConceptMetadata conceptMetadata, ISqlUtility sqlUtility)
        {
            _databaseSettings = databaseSettings;
            _conceptMetadata = conceptMetadata;
            _sqlUtility = sqlUtility;
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var property = (PropertyInfo)conceptInfo;

            if (!IsSupported(property))
                return;

            string columnName = _sqlUtility.Identifier(_conceptMetadata.GetColumnName(property));
            string specifyColumnName = columnName != property.Name ? $".HasColumnName({CsUtility.QuotedString(columnName)})" : "";
            string columnType = _conceptMetadata.GetColumnType(property);
            string propertyModel = GetPropertyModel(property, _databaseSettings, columnType);

            codeBuilder.InsertCode(
                $"\r\n                entity.Property(e => e.{property.Name}){specifyColumnName}{propertyModel}{ModelOptionsTag.Evaluate(property)};",
                DataStructureModelCodeGenerator.ModelBuilderTag, property.DataStructure);
        }

        public static bool IsSupported(PropertyInfo property) =>
            DataStructureModelCodeGenerator.IsSupported(property.DataStructure)
            && GetPropertyModel(property, _emptyDatabaseSettings, "") != null;

        private static readonly CommonConceptsDatabaseSettings _emptyDatabaseSettings = new();

        private static string GetPropertyModel(PropertyInfo property, CommonConceptsDatabaseSettings databaseSettings, string columnType) =>
            property switch
            {
                DecimalPropertyInfo => $".HasPrecision(28, 10).HasColumnType(\"{columnType}\")",
                MoneyPropertyInfo => $".HasPrecision({databaseSettings.MoneyPrecision}, {databaseSettings.MoneyScale}).HasColumnType(\"{columnType}\")",
                ShortStringPropertyInfo => $".HasColumnType(\"{columnType}\")",
                LongStringPropertyInfo => $".HasColumnType(\"{columnType}\")",
                BinaryPropertyInfo => $".HasColumnType(\"{columnType}\")",
                BoolPropertyInfo => $".HasColumnType(\"{columnType}\")",
                IntegerPropertyInfo => $".HasColumnType(\"{columnType}\")",
                GuidPropertyInfo => $".HasColumnType(\"{columnType}\")",
                DateTimePropertyInfo => databaseSettings.UseLegacyMsSqlDateTime ? $".HasColumnType(\"{columnType}\")"
                    : $".HasPrecision({databaseSettings.DateTimePrecision}).HasColumnType(\"{columnType}\")",
                DatePropertyInfo => $".HasColumnType(\"{columnType}\")",
                _ => null
            };
    }
}
