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
using Rhetos.Utilities;
using System.ComponentModel.Composition;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    [Export(typeof(ITypeExtension))]
    public class ShortStringDatabaseColumnTypeExtension : IDatabaseColumnType<ShortStringPropertyInfo>
    {
        public string ColumnType {
            get { return Sql.Format("ShortStringPropertyDatabaseDefinition_DataType", ShortStringPropertyInfo.MaxLength); }
        }
    }

    [Export(typeof(ITypeExtension))]
    public class LongStringDatabaseColumnTypeExtension : IDatabaseColumnType<LongStringPropertyInfo>
    {
        public string ColumnType
        {
            get { return Sql.Format("LongStringPropertyDatabaseDefinition_DataType", ShortStringPropertyInfo.MaxLength); }
        }
    }

    [Export(typeof(ITypeExtension))]
    public class BinaryDatabaseColumnTypeExtension : IDatabaseColumnType<BinaryPropertyInfo>
    {
        public string ColumnType
        {
            get { return Sql.Get("BinaryPropertyDatabaseDefinition_DataType"); }
        }
    }

    [Export(typeof(ITypeExtension))]
    public class BoolDatabaseColumnTypeExtension : IDatabaseColumnType<BoolPropertyInfo>
    {
        public string ColumnType
        {
            get { return Sql.Get("BoolPropertyDatabaseDefinition_DataType"); }
        }
    }

    [Export(typeof(ITypeExtension))]
    public class DateTimeDatabaseColumnTypeExtension : IDatabaseColumnType<DateTimePropertyInfo>
    {
        public string ColumnType
        {
            get { return Sql.Get("DateTimePropertyDatabaseDefinition_DataType"); }
        }
    }

    [Export(typeof(ITypeExtension))]
    public class DateDatabaseColumnTypeExtension : IDatabaseColumnType<DatePropertyInfo>
    {
        public string ColumnType
        {
            get { return Sql.Get("DatePropertyDatabaseDefinition_DataType"); }
        }
    }

    [Export(typeof(ITypeExtension))]
    public class DecimalDatabaseColumnTypeExtension : IDatabaseColumnType<DecimalPropertyInfo>
    {
        public string ColumnType
        {
            get { return Sql.Get("DecimalPropertyDatabaseDefinition_DataType"); }
        }
    }

    [Export(typeof(ITypeExtension))]
    public class GuidDatabaseColumnTypeExtension : IDatabaseColumnType<GuidPropertyInfo>
    {
        public string ColumnType
        {
            get { return Sql.Get("GuidPropertyDatabaseDefinition_DataType"); }
        }
    }

    [Export(typeof(ITypeExtension))]
    public class IntegerDatabaseColumnTypeExtension : IDatabaseColumnType<IntegerPropertyInfo>
    {
        public string ColumnType
        {
            get { return Sql.Get("IntegerPropertyDatabaseDefinition_DataType"); }
        }
    }

    [Export(typeof(ITypeExtension))]
    public class ReferenceDatabaseColumnTypeExtension : IDatabaseColumnType<ReferencePropertyInfo>
    {
        public string ColumnType
        {
            get { return Sql.Get("ReferencePropertyDatabaseDefinition_DataType"); }
        }
    }

    [Export(typeof(ITypeExtension))]
    public class MoneyDatabaseColumnTypeExtension : IDatabaseColumnType<MoneyPropertyInfo>
    {
        public string ColumnType
        {
            get { return Sql.Get("MoneyPropertyDatabaseDefinition_DataType"); }
        }
    }
}
