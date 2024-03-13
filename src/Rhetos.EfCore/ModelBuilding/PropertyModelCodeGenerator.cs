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
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using System.ComponentModel.Composition;

namespace Rhetos.EfCore.ModelBuilding
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(PropertyInfo))]
    public class PropertyModelCodeGenerator : IConceptCodeGenerator
    {
        public static readonly CsTag<PropertyInfo> ModelOptionsTag = "ModelOptions";

        private readonly CommonConceptsDatabaseSettings _databaseSettings;

        public PropertyModelCodeGenerator(CommonConceptsDatabaseSettings databaseSettings)
        {
            _databaseSettings = databaseSettings;
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var property = (PropertyInfo)conceptInfo;

            if (!DataStructureModelCodeGenerator.IsSupported(property.DataStructure))
                return;

            string propertyModel = GetPropertyModel(property, _databaseSettings);

            if (propertyModel == null)
                return;

            codeBuilder.InsertCode(
                $"\r\n                entity.Property(e => e.{property.Name}){propertyModel}{ModelOptionsTag.Evaluate(property)};",
                DataStructureModelCodeGenerator.ModelBuilderTag, property.DataStructure);
        }

        public static bool IsSupported(PropertyInfo property) =>
            DataStructureModelCodeGenerator.IsSupported(property.DataStructure)
            && GetPropertyModel(property, _emptyDatabaseSettings) != null;

        private static readonly CommonConceptsDatabaseSettings _emptyDatabaseSettings = new();

        private static string GetPropertyModel(PropertyInfo property, CommonConceptsDatabaseSettings databaseSettings) =>
            property switch
            {
                DecimalPropertyInfo => ".HasPrecision(28, 10).HasColumnType(\"decimal(20, 10)\")",
                MoneyPropertyInfo => $".HasPrecision({databaseSettings.MoneyPrecision}, {databaseSettings.MoneyScale}).HasColumnType(\"money\")",
                ShortStringPropertyInfo => ".HasColumnType(\"nvarchar(max)\")", // No need to cause issues in EF if some view returns more then 256 characters or some query parameter exceeds the limit.
                LongStringPropertyInfo => ".HasColumnType(\"nvarchar(max)\")",
                BinaryPropertyInfo => ".HasColumnType(\"varbinary(max)\")",
                BoolPropertyInfo => ".HasColumnType(\"bit\")",
                IntegerPropertyInfo => ".HasColumnType(\"int\")",
                GuidPropertyInfo => ".HasColumnType(\"uniqueidentifier\")",
                DateTimePropertyInfo => databaseSettings.UseLegacyMsSqlDateTime ? ".HasColumnType(\"datetime\")"
                    : $".HasPrecision({databaseSettings.DateTimePrecision}).HasColumnType(\"datetime2({databaseSettings.DateTimePrecision})\")",
                DatePropertyInfo => ".HasColumnType(\"date\")",
                _ => null
            };
    }
}
