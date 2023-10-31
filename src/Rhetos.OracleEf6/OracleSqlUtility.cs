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

using Oracle.ManagedDataAccess.Client;
using System;
using System.Data.Common;
using System.Globalization;
using System.Linq;

namespace Rhetos.Utilities
{
    public class OracleSqlUtility : ISqlUtility
    {
        private readonly DatabaseSettings _databaseSettings;

        private readonly Lazy<string> _setNationalLanguageQuery;

        public OracleSqlUtility(DatabaseSettings databaseSettings)
        {
            _databaseSettings = databaseSettings;
            _setNationalLanguageQuery = new(CreateSetNationalLanguageQuery);

            // This class is a singleton, so there are no performance issues checking the database settings for each instance.
            const string expectedDatabaseLanguage = "Oracle";
            if (databaseSettings.DatabaseLanguage != expectedDatabaseLanguage)
                throw new FrameworkException($"Unsupported database language '{databaseSettings.DatabaseLanguage}'." +
                    $" Assembly '{GetType().Assembly.GetName()}' expects database language '{expectedDatabaseLanguage}'.");
        }

        public string ProviderName => "Oracle.ManagedDataAccess.Client";

        public string TrySetApplicationName(string connectionString)
        {
            return connectionString;
        }

        public DbConnection CreateConnection(string connectionString, IUserInfo userInfo)
        {
            var connection = new OracleConnection(connectionString);
            try
            {
                connection.Open();
                SetSqlUserInfo(connection, userInfo);

                var setNationalLanguage = SetNationalLanguageQuery();
                if (!string.IsNullOrEmpty(setNationalLanguage))
                {
                    var com = connection.CreateCommand();
                    com.CommandText = setNationalLanguage;
                    com.ExecuteNonQuery();
                }
            }
            catch (OracleException ex)
            {
                var csb = new OracleConnectionStringBuilder(connectionString);
                string msg = string.Format(CultureInfo.InvariantCulture, "Could not connect to data source '{0}', userID '{1}'.", csb.DataSource, csb.UserID);
                throw new FrameworkException(msg, ex);
            }
            return connection;
        }

        public static string LimitIdentifierLength(string name)
        {
            const int MaxLength = 30;
            if (name.Length > MaxLength)
            {
                var hashErasedPart = CsUtility.GetStableHashCode(name.Substring(MaxLength - 9)).ToString("X").PadLeft(8, '0');
                return string.Concat(name.AsSpan(0, MaxLength - 9), "_", hashErasedPart);
            }
            return name;
        }

        /// <summary>
        /// Returns an SQL query that is used to set the national language, for string comparison and sorting.
        /// </summary>
        public string SetNationalLanguageQuery()
        {
            return _setNationalLanguageQuery.Value;
        }

        private string CreateSetNationalLanguageQuery()
        {
            if (!string.IsNullOrEmpty(_databaseSettings.DatabaseNationalLanguage))
                return
$@"BEGIN
  EXECUTE IMMEDIATE 'ALTER SESSION SET NLS_COMP=LINGUISTIC';
  EXECUTE IMMEDIATE 'ALTER SESSION SET NLS_SORT={_databaseSettings.DatabaseNationalLanguage}';
END;";
            else
                return "";


            // Alternative way (probably slower):
            //if (!string.IsNullOrEmpty(SqlUtility.NationalLanguage))
            //{
            //    if (_oracleGlobalization == null)
            //    {
            //        _oracleGlobalization = connection.GetSessionInfo();
            //        _oracleGlobalization.Comparison = "LINGUISTIC";
            //        _oracleGlobalization.Sort = SqlUtility.NationalLanguage;
            //    }
            //    connection.SetSessionInfo(_oracleGlobalization);
            //}
        }

        /// <summary>
        /// Provides the user information to the database, for logging and similar features.
        /// </summary>
        public static void SetSqlUserInfo(OracleConnection connection, IUserInfo userInfo)
        {
            connection.ClientInfo = SqlUtility.UserContextInfoText(userInfo);
        }

        /// <summary>
        /// See ISqlUtility.InterpretSqlException.
        /// </summary>
        public RhetosException InterpretSqlException(Exception exception)
        {
            return null;
        }

        public Exception ExtractSqlException(Exception exception)
        {
            return null;
        }

        public string Identifier(string name)
        {
            string error = CsUtility.GetIdentifierError(name);
            if (error != null)
                throw new FrameworkException("Invalid database object name: " + error);

            return LimitIdentifierLength(name);
        }

        public string QuoteText(string value)
        {
            return value != null
                ? "'" + value.Replace("'", "''") + "'"
                : "NULL";
        }

        public string QuoteIdentifier(string sqlIdentifier)
        {
            return sqlIdentifier;
        }

        public string GetSchemaName(string fullObjectName)
        {
            int dotPosition = fullObjectName.IndexOf('.');
            if (dotPosition == -1)
                throw new FrameworkException($"Missing schema name for database object '{fullObjectName}'.");

            var schema = fullObjectName.Substring(0, dotPosition);
            return Identifier(schema);
        }

        public string GetShortName(string fullObjectName)
        {
            int dotPosition = fullObjectName.IndexOf('.');
            if (dotPosition == -1)
                return fullObjectName;

            var shortName = fullObjectName.Substring(dotPosition + 1);

            int secondDot = shortName.IndexOf('.');
            if (secondDot != -1 || string.IsNullOrEmpty(shortName))
                throw new FrameworkException("Invalid database object name: '" + fullObjectName + "'. Expected format is 'schema.name' or 'name'.");
            return Identifier(shortName);
        }

        public string GetFullName(string objectName)
        {
            var schema = GetSchemaName(objectName);
            var name = GetShortName(objectName);
            return schema + "." + name;
        }

        /// <summary>
        /// Vendor-independent database reader.
        /// </summary>
        public Guid ReadGuid(DbDataReader dataReader, int column)
        {
            return new Guid(((OracleDataReader)dataReader).GetOracleBinary(column).Value);
        }

        /// <summary>
        /// Vendor-independent database reader.
        /// </summary>
        public int ReadInt(DbDataReader dataReader, int column)
        {
            return Convert.ToInt32(dataReader.GetInt64(column)); // On some systems, reading from NUMERIC(10) column will return Int64, and GetInt32 would fail.
        }

        public Guid StringToGuid(string guid)
        {
            return new Guid(CsUtility.HexToByteArray(guid));
        }

        public string QuoteGuid(Guid? guid)
        {
            return guid.HasValue
                ? "'" + GuidToString(guid.Value) + "'"
                : "NULL";
        }

        public string GuidToString(Guid? guid)
        {
            return guid.HasValue ? CsUtility.ByteArrayToHex(guid.Value.ToByteArray()) : null;
        }

        public string QuoteDateTime(DateTime? dateTime)
        {
            return dateTime.HasValue
                ? "'" + DateTimeToString(dateTime.Value) + "'"
                : "NULL";
        }

        public string DateTimeToString(DateTime? dateTime)
        {
            return dateTime.HasValue ? dateTime.Value.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff") : null;
        }

        public string QuoteBool(bool? b)
        {
            return b.HasValue ? BoolToString(b.Value) : "NULL";
        }

        public string BoolToString(bool? b)
        {
            return b switch { null => null, true => "1", false => "0" };
        }

        /// <summary>
        /// Returns empty string if the string value is null.
        /// This function is used for compatibility between MsSql and Oracle string behavior.
        /// </summary>
        public string EmptyNullString(DbDataReader dataReader, int column)
        {
            var s = ((OracleDataReader)dataReader).GetOracleString(column);
            return !s.IsNull ? s.Value : "";
        }

        public DateTime GetDatabaseTime(ISqlExecuter sqlExecuter)
        {
            throw new NotImplementedException("GetDatabaseTime function is not yet supported in Rhetos for Oracle database.");
        }

        public string SqlConnectionInfo(string connectionString)
        {
            OracleConnectionStringBuilder cs;
            try
            {
                cs = new OracleConnectionStringBuilder(connectionString);
            }
            catch
            {
                // This is not a blocking error, because other database providers should be supported.
                return "(cannot parse connection string)";
            }

            var elements = new ListOfTuples<string, string>
            {
                { "DataSource", cs.DataSource },
            };

            return
                string.Join(", ", elements
                    .Where(e => !string.IsNullOrWhiteSpace(e.Item2))
                    .Select(e => e.Item1 + "=" + e.Item2));
        }
    }
}
