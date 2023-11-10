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

using Rhetos.Dom.DefaultConcepts;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Utilities;
using System.ComponentModel.Composition;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    [Export(typeof(IConceptMetadataExtension))]
    public class ShortStringDatabaseColumnTypeMetadata : DatabaseColumnTypeBase<ShortStringPropertyInfo>
    {
        private readonly ISqlResources _sql;

        public ShortStringDatabaseColumnTypeMetadata(ISqlResources sql)
        {
            _sql = sql;
        }

        public override string GetColumnType(ShortStringPropertyInfo concept)
            => _sql.Format("ShortStringPropertyDatabaseDefinition_DataType", ShortStringPropertyInfo.MaxLength);
    }

    [Export(typeof(IConceptMetadataExtension))]
    public class LongStringDatabaseColumnTypeMetadata : DatabaseColumnTypeBase<LongStringPropertyInfo>
    {
        private readonly ISqlResources _sql;

        public LongStringDatabaseColumnTypeMetadata(ISqlResources sql)
        {
            _sql = sql;
        }

        public override string GetColumnType(LongStringPropertyInfo concept)
            => _sql.Format("LongStringPropertyDatabaseDefinition_DataType", ShortStringPropertyInfo.MaxLength);
    }

    [Export(typeof(IConceptMetadataExtension))]
    public class BinaryDatabaseColumnTypeMetadata : DatabaseColumnTypeBase<BinaryPropertyInfo>
    {
        private readonly ISqlResources _sql;

        public BinaryDatabaseColumnTypeMetadata(ISqlResources sql)
        {
            _sql = sql;
        }

        public override string GetColumnType(BinaryPropertyInfo concept)
            => _sql.Get("BinaryPropertyDatabaseDefinition_DataType");
    }

    [Export(typeof(IConceptMetadataExtension))]
    public class BoolDatabaseColumnTypeMetadata : DatabaseColumnTypeBase<BoolPropertyInfo>
    {
        private readonly ISqlResources _sql;

        public BoolDatabaseColumnTypeMetadata(ISqlResources sql)
        {
            _sql = sql;
        }

        public override string GetColumnType(BoolPropertyInfo concept)
            => _sql.Get("BoolPropertyDatabaseDefinition_DataType");
    }

    [Export(typeof(IConceptMetadataExtension))]
    public class DateTimeDatabaseColumnTypeMetadata : DatabaseColumnTypeBase<DateTimePropertyInfo>
    {
        private readonly ISqlResources _sql;
        private readonly CommonConceptsDatabaseSettings _databaseSettings;

        public DateTimeDatabaseColumnTypeMetadata(ISqlResources sql, CommonConceptsDatabaseSettings databaseSettings)
        {
            _sql = sql;
            _databaseSettings = databaseSettings;
        }

        public override string GetColumnType(DateTimePropertyInfo concept)
            => _databaseSettings.UseLegacyMsSqlDateTime
                ? _sql.Get("DateTimePropertyDatabaseDefinition_DataType_Legacy")
                : _sql.Format("DateTimePropertyDatabaseDefinition_DataType", _databaseSettings.DateTimePrecision);
    }

    [Export(typeof(IConceptMetadataExtension))]
    public class DateDatabaseColumnTypeMetadata : DatabaseColumnTypeBase<DatePropertyInfo>
    {
        private readonly ISqlResources _sql;

        public DateDatabaseColumnTypeMetadata(ISqlResources sql)
        {
            _sql = sql;
        }

        public override string GetColumnType(DatePropertyInfo concept)
            => _sql.Get("DatePropertyDatabaseDefinition_DataType");
    }

    [Export(typeof(IConceptMetadataExtension))]
    public class DecimalDatabaseColumnTypeMetadata : DatabaseColumnTypeBase<DecimalPropertyInfo>
    {
        private readonly ISqlResources _sql;

        public DecimalDatabaseColumnTypeMetadata(ISqlResources sql)
        {
            _sql = sql;
        }

        public override string GetColumnType(DecimalPropertyInfo concept)
            => _sql.Get("DecimalPropertyDatabaseDefinition_DataType");
    }

    [Export(typeof(IConceptMetadataExtension))]
    public class GuidDatabaseColumnTypeMetadata : DatabaseColumnTypeBase<GuidPropertyInfo>
    {
        private readonly ISqlResources _sql;

        public GuidDatabaseColumnTypeMetadata(ISqlResources sql)
        {
            _sql = sql;
        }

        public override string GetColumnType(GuidPropertyInfo concept)
            => _sql.Get("GuidPropertyDatabaseDefinition_DataType");
    }

    [Export(typeof(IConceptMetadataExtension))]
    public class IntegerDatabaseColumnTypeMetadata : DatabaseColumnTypeBase<IntegerPropertyInfo>
    {
        private readonly ISqlResources _sql;

        public IntegerDatabaseColumnTypeMetadata(ISqlResources sql)
        {
            _sql = sql;
        }

        public override string GetColumnType(IntegerPropertyInfo concept)
            => _sql.Get("IntegerPropertyDatabaseDefinition_DataType");
    }

    [Export(typeof(IConceptMetadataExtension))]
    public class ReferenceDatabaseColumnTypeMetadata : DatabaseColumnTypeBase<ReferencePropertyInfo>
    {
        private readonly ISqlResources _sql;

        public ReferenceDatabaseColumnTypeMetadata(ISqlResources sql)
        {
            _sql = sql;
        }

        public override string GetColumnType(ReferencePropertyInfo concept)
            => _sql.Get("ReferencePropertyDatabaseDefinition_DataType");
    }

    [Export(typeof(IConceptMetadataExtension))]
    public class MoneyDatabaseColumnTypeMetadata : DatabaseColumnTypeBase<MoneyPropertyInfo>
    {
        private readonly ISqlResources _sql;

        public MoneyDatabaseColumnTypeMetadata(ISqlResources sql)
        {
            _sql = sql;
        }

        public override string GetColumnType(MoneyPropertyInfo concept)
            => _sql.Get("MoneyPropertyDatabaseDefinition_DataType");
    }
}
