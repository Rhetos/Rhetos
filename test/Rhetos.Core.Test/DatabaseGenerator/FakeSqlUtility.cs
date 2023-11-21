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

using Rhetos.Utilities;
using System;
using System.Data.Common;

namespace Rhetos.DatabaseGenerator.Test
{
    internal class FakeSqlUtility : ISqlUtility
    {
        public string ProviderName => throw new NotImplementedException();

        public string BoolToString(bool? b) => throw new NotImplementedException();

        public DbConnection CreateConnection(string connectionString, IUserInfo _userInfo) => throw new NotImplementedException();

        public string DateTimeToString(DateTime? dateTime) => throw new NotImplementedException();

        public string ReadEmptyNullString(DbDataReader dataReader, int column) => dataReader.GetString(column) ?? "";

        public Exception ExtractSqlException(Exception exception) => throw new NotImplementedException();

        public DateTime GetDatabaseTime(ISqlExecuter sqlExecuter) => throw new NotImplementedException();

        public string GetFullName(string objectName) => throw new NotImplementedException();

        public string GetSchemaName(string fullObjectName) => throw new NotImplementedException();

        public string GetShortName(string fullObjectName) => throw new NotImplementedException();

        public string GuidToString(Guid? guid) => throw new NotImplementedException();

        public string Identifier(string name) => throw new NotImplementedException();

        public RhetosException InterpretSqlException(Exception exception) => throw new NotImplementedException();

        public string QuoteBool(bool? b) => throw new NotImplementedException();

        public string QuoteDateTime(DateTime? dateTime) => throw new NotImplementedException();

        public string QuoteGuid(Guid? guid) => throw new NotImplementedException();

        public string QuoteIdentifier(string sqlIdentifier) => "[" + sqlIdentifier.Replace("]", "]]") + "]";

        public string QuoteText(string value) => throw new NotImplementedException();

        public Guid ReadGuid(DbDataReader dataReader, int column) => dataReader.GetGuid(column);

        public int ReadInt(DbDataReader dataReader, int column) => dataReader.GetInt32(column);

        public string SqlConnectionInfo(string connectionString) => throw new NotImplementedException();

        public Guid StringToGuid(string guid) => throw new NotImplementedException();

        public string TrySetApplicationName(string connectionString) => throw new NotImplementedException();

        public void ValidateDbConnection(string connectionString) => throw new NotImplementedException();
    }
}