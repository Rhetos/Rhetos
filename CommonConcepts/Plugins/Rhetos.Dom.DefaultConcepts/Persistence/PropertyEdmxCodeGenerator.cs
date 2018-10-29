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
using Rhetos.Persistence;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IEdmxCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(PropertyInfo))]
    [ExportMetadata(MefProvider.DependsOn, typeof(DataStructureEdmxCodeGenerator))]
    public class PropertyEdmxCodeGenerator : IEdmxCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var propertyInfo = conceptInfo as PropertyInfo;
            if (propertyInfo.DataStructure is IOrmDataStructure && IsTypeSupported(propertyInfo.GetType()))
            {
                codeBuilder.InsertCode("\n" + GetPropertyElementForConceptualModel(propertyInfo), DataStructureEdmxCodeGenerator.ConceptualModelEntityTypePropertyTag.Evaluate(propertyInfo.DataStructure));
                codeBuilder.InsertCode("\n" + GetScalarPropertyElement(propertyInfo), DataStructureEdmxCodeGenerator.EntitySetMappingPropertyTag.Evaluate(propertyInfo.DataStructure));
                codeBuilder.InsertCode("\n" + GetPropertyElementForStorageModel(propertyInfo), DataStructureEdmxCodeGenerator.StorageModelEntityTypePropertyTag.Evaluate(propertyInfo.DataStructure));
            }
        }

        public static readonly List<Type> supportedTypes = new List<Type>
        {
            typeof(DecimalPropertyInfo),
            typeof(MoneyPropertyInfo),
            typeof(ShortStringPropertyInfo),
            typeof(LongStringPropertyInfo),
            typeof(BinaryPropertyInfo),
            typeof(BoolPropertyInfo),
            typeof(IntegerPropertyInfo),
            typeof(GuidPropertyInfo),
            typeof(DateTimePropertyInfo),
            typeof(DatePropertyInfo)
        };

        private static bool IsTypeSupported(Type type)
        {
            foreach (var supportedtype in supportedTypes)
            {
                if (supportedtype.IsAssignableFrom(type))
                    return true;
            }
            return false;
        }

        private static string GetPropertyElementForConceptualModel(PropertyInfo propertyInfo)
        {
            var propertyInfoType = propertyInfo.GetType();
            if (typeof(DecimalPropertyInfo).IsAssignableFrom(propertyInfoType))
                return $@"    <Property Name=""{propertyInfo.Name}"" Type=""Decimal"" Precision=""28"" Scale=""10"" />";
            if (typeof(MoneyPropertyInfo).IsAssignableFrom(propertyInfoType))
                return $@"    <Property Name=""{propertyInfo.Name}"" Type=""Decimal"" Precision=""18"" Scale=""2"" />";
            if (typeof(ShortStringPropertyInfo).IsAssignableFrom(propertyInfoType) || propertyInfoType.IsAssignableFrom(typeof(LongStringPropertyInfo)))
                return $@"    <Property Name=""{propertyInfo.Name}"" Type=""String"" MaxLength=""Max"" FixedLength=""false"" Unicode=""true"" />";
            if (typeof(BinaryPropertyInfo).IsAssignableFrom(propertyInfoType))
                return $@"    <Property Name=""{propertyInfo.Name}"" Type=""Binary"" MaxLength=""Max"" FixedLength=""false"" />";
            if (typeof(BoolPropertyInfo).IsAssignableFrom(propertyInfoType))
                return $@"    <Property Name=""{propertyInfo.Name}"" Type=""Boolean"" />";
            if (typeof(IntegerPropertyInfo).IsAssignableFrom(propertyInfoType))
                return $@"    <Property Name=""{propertyInfo.Name}"" Type=""Int32"" />";
            if (typeof(GuidPropertyInfo).IsAssignableFrom(propertyInfoType))
                return $@"    <Property Name=""{propertyInfo.Name}"" Type=""Guid"" />";
            if (typeof(DateTimePropertyInfo).IsAssignableFrom(propertyInfoType))
                return $@"    <Property Name=""{propertyInfo.Name}"" Type=""DateTime"" />";
            if (typeof(DatePropertyInfo).IsAssignableFrom(propertyInfoType))
                return $@"    <Property Name=""{propertyInfo.Name}"" Type=""DateTime"" />";
            return "";
        }

        private static string GetPropertyElementForStorageModel(PropertyInfo propertyInfo)
        {
            var propertyInfoType = propertyInfo.GetType();
            if (typeof(DecimalPropertyInfo).IsAssignableFrom(propertyInfoType))
                return $@"    <Property Name=""{propertyInfo.Name}"" Type=""decimal"" Precision=""28"" Scale=""10"" Nullable=""true"" />";
            if (typeof(MoneyPropertyInfo).IsAssignableFrom(propertyInfoType))
                return $@"    <Property Name=""{propertyInfo.Name}"" Type=""decimal"" Precision=""18"" Scale=""2"" Nullable=""true"" />";
            if (typeof(ShortStringPropertyInfo).IsAssignableFrom(propertyInfoType) || propertyInfoType.IsAssignableFrom(typeof(LongStringPropertyInfo)))
                return $@"    <Property Name=""{propertyInfo.Name}"" Type=""nvarchar(max)"" Nullable=""true"" />";
            if (typeof(BinaryPropertyInfo).IsAssignableFrom(propertyInfoType))
                return $@"    <Property Name=""{propertyInfo.Name}"" Type=""varbinary(max)"" Nullable=""true"" />";
            if (typeof(BoolPropertyInfo).IsAssignableFrom(propertyInfoType))
                return $@"    <Property Name=""{propertyInfo.Name}"" Type=""bit"" Nullable=""true"" />";
            if (typeof(IntegerPropertyInfo).IsAssignableFrom(propertyInfoType))
                return $@"    <Property Name=""{propertyInfo.Name}"" Type=""int"" Nullable=""true"" />";
            if (typeof(GuidPropertyInfo).IsAssignableFrom(propertyInfoType))
                return $@"    <Property Name=""{propertyInfo.Name}"" Type=""uniqueidentifier"" Nullable=""true"" />";
            if (typeof(DateTimePropertyInfo).IsAssignableFrom(propertyInfoType))
                return $@"    <Property Name=""{propertyInfo.Name}"" Type=""datetime"" Nullable=""true"" />";
            if (typeof(DatePropertyInfo).IsAssignableFrom(propertyInfoType))
                return $@"    <Property Name=""{propertyInfo.Name}"" Type=""datetime"" Nullable=""true"" />";
            return "";
        }

        private static string GetScalarPropertyElement(PropertyInfo propertyInfo)
        {
            return $@"        <ScalarProperty Name=""{propertyInfo.Name}"" ColumnName=""{propertyInfo.Name}"" />";
        }
    }
}
