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

using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using System.ComponentModel.Composition;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    [Export(typeof(IConceptMetadataExtension))]
    public class ShortStringCsPropertyType : ICsPropertyType<ShortStringPropertyInfo>
    {
        public string GetCsPropertyType(PropertyInfo concept) => "string";
    }

    [Export(typeof(IConceptMetadataExtension))]
    public class LongStringCsPropertyType : ICsPropertyType<LongStringPropertyInfo>
    {
        public string GetCsPropertyType(PropertyInfo concept) => "string";
    }

    [Export(typeof(IConceptMetadataExtension))]
    public class BinaryCsPropertyType : ICsPropertyType<BinaryPropertyInfo>
    {
        public string GetCsPropertyType(PropertyInfo concept) => "byte[]";
    }

    [Export(typeof(IConceptMetadataExtension))]
    public class BoolCsPropertyType : ICsPropertyType<BoolPropertyInfo>
    {
        public string GetCsPropertyType(PropertyInfo concept) => "bool?";
    }

    [Export(typeof(IConceptMetadataExtension))]
    public class DateTimeCsPropertyType : ICsPropertyType<DateTimePropertyInfo>
    {
        public string GetCsPropertyType(PropertyInfo concept) => "DateTime?";
    }

    [Export(typeof(IConceptMetadataExtension))]
    public class DateCsPropertyType : ICsPropertyType<DatePropertyInfo>
    {
        public string GetCsPropertyType(PropertyInfo concept) => "DateTime?";
    }

    [Export(typeof(IConceptMetadataExtension))]
    public class DecimalCsPropertyType : ICsPropertyType<DecimalPropertyInfo>
    {
        public string GetCsPropertyType(PropertyInfo concept) => "decimal?";
    }

    [Export(typeof(IConceptMetadataExtension))]
    public class GuidCsPropertyType : ICsPropertyType<GuidPropertyInfo>
    {
        public string GetCsPropertyType(PropertyInfo concept) => "Guid?";
    }

    [Export(typeof(IConceptMetadataExtension))]
    public class IntegerCsPropertyType : ICsPropertyType<IntegerPropertyInfo>
    {
        public string GetCsPropertyType(PropertyInfo concept) => "int?";
    }

    [Export(typeof(IConceptMetadataExtension))]
    public class ReferenceCsPropertyType : ICsPropertyType<ReferencePropertyInfo>
    {
        public string GetCsPropertyType(PropertyInfo concept) => "Guid?";
    }

    [Export(typeof(IConceptMetadataExtension))]
    public class MoneyCsPropertyType : ICsPropertyType<MoneyPropertyInfo>
    {
        public string GetCsPropertyType(PropertyInfo concept) => "decimal?";
    }
}
