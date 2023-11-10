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
    public class ShortStringCsPropertyType : CsPropertyTypeBase<ShortStringPropertyInfo>
    {
        public override string GetCsPropertyType(ShortStringPropertyInfo concept) => "string";
    }

    [Export(typeof(IConceptMetadataExtension))]
    public class LongStringCsPropertyType : CsPropertyTypeBase<LongStringPropertyInfo>
    {
        public override string GetCsPropertyType(LongStringPropertyInfo concept) => "string";
    }

    [Export(typeof(IConceptMetadataExtension))]
    public class BinaryCsPropertyType : CsPropertyTypeBase<BinaryPropertyInfo>
    {
        public override string GetCsPropertyType(BinaryPropertyInfo concept) => "byte[]";
    }

    [Export(typeof(IConceptMetadataExtension))]
    public class BoolCsPropertyType : CsPropertyTypeBase<BoolPropertyInfo>
    {
        public override string GetCsPropertyType(BoolPropertyInfo concept) => "bool?";
    }

    [Export(typeof(IConceptMetadataExtension))]
    public class DateTimeCsPropertyType : CsPropertyTypeBase<DateTimePropertyInfo>
    {
        public override string GetCsPropertyType(DateTimePropertyInfo concept) => "DateTime?";
    }

    [Export(typeof(IConceptMetadataExtension))]
    public class DateCsPropertyType : CsPropertyTypeBase<DatePropertyInfo>
    {
        public override string GetCsPropertyType(DatePropertyInfo concept) => "DateTime?";
    }

    [Export(typeof(IConceptMetadataExtension))]
    public class DecimalCsPropertyType : CsPropertyTypeBase<DecimalPropertyInfo>
    {
        public override string GetCsPropertyType(DecimalPropertyInfo concept) => "decimal?";
    }

    [Export(typeof(IConceptMetadataExtension))]
    public class GuidCsPropertyType : CsPropertyTypeBase<GuidPropertyInfo>
    {
        public override string GetCsPropertyType(GuidPropertyInfo concept) => "Guid?";
    }

    [Export(typeof(IConceptMetadataExtension))]
    public class IntegerCsPropertyType : CsPropertyTypeBase<IntegerPropertyInfo>
    {
        public override string GetCsPropertyType(IntegerPropertyInfo concept) => "int?";
    }

    [Export(typeof(IConceptMetadataExtension))]
    public class ReferenceCsPropertyType : CsPropertyTypeBase<ReferencePropertyInfo>
    {
        public override string GetCsPropertyType(ReferencePropertyInfo concept) => "Guid?";
    }

    [Export(typeof(IConceptMetadataExtension))]
    public class MoneyCsPropertyType : CsPropertyTypeBase<MoneyPropertyInfo>
    {
        public override string GetCsPropertyType(MoneyPropertyInfo concept) => "decimal?";
    }
}
