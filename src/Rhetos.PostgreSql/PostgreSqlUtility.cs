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

using Npgsql;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Utilities;
using System.Data.Common;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Rhetos.PostgreSql
{
    public class PostgreSqlUtility : ISqlUtility
    {
        internal const string DatabaseLanguage = "PostgreSql";

        private readonly ILocalizer _localizer;

        public PostgreSqlUtility(ILocalizer localizer, DatabaseSettings databaseSettings)
        {
            _localizer = localizer;

            if (databaseSettings.DatabaseLanguage != DatabaseLanguage)
                throw new FrameworkException($"Unsupported database language '{databaseSettings.DatabaseLanguage}'."
                    + $" Assembly '{GetType().Assembly.GetName().Name}' expects database language '{DatabaseLanguage}'."
                    + $" Configure {ConfigurationProvider.GetKey((DatabaseSettings o) => o.DatabaseLanguage)} in build settings.");
        }

        public string ProviderName => typeof(NpgsqlConnection).Namespace;

        public string TrySetApplicationName(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return connectionString;

            try
            {
                var dbConnectionStringBuilder = new DbConnectionStringBuilder();
                dbConnectionStringBuilder.ConnectionString = connectionString;
                if (dbConnectionStringBuilder.ContainsKey("Application Name") || dbConnectionStringBuilder.ContainsKey("app"))
                    return connectionString;

                string hostAppName = Assembly.GetEntryAssembly()?.GetName()?.Name;
                if (string.IsNullOrEmpty(hostAppName))
                    return connectionString;

                dbConnectionStringBuilder["Application Name"] = hostAppName;
                return dbConnectionStringBuilder.ToString();
            }
#pragma warning disable CA1031 // Do not catch general exception types. This is just an optional information in connection string. It should not fail if the connection string format is not recognized.
            catch
#pragma warning restore CA1031 // Do not catch general exception types
            {
                return connectionString;
            }
        }

        public DbConnection CreateConnection(string connectionString, IUserInfo userInfo)
        {
            var connection = new NpgsqlConnection(connectionString);

            try
            {
                connection.Open();
                using var sqlCommand = CreateUserContextInfoCommand(userInfo);
                sqlCommand.Connection = connection;
                sqlCommand.ExecuteNonQuery();
            }
            catch (PostgresException ex)
            {
                var csb = new NpgsqlConnectionStringBuilder(connectionString);
                string secutiryInfo = string.IsNullOrWhiteSpace(csb.Username) ? $"integrated security account '{Environment.UserName}'" : $"login '{csb.Username}'";
                string msg = $"Could not connect to server '{csb.Host}', database '{csb.Database}' using {secutiryInfo}.";
                throw new FrameworkException(msg, ex);
            }

            return connection;
        }

        /// <summary>
        /// Creates an SQL command that sets context_info connection variable to contain data about the user.
        /// The context_info variable can be used in SQL server to extract user info in certain situations such as logging trigger.
        /// </summary>
        /// <returns>
        /// Returns null is the user is not recognized.
        /// </returns>
        private static NpgsqlCommand CreateUserContextInfoCommand(IUserInfo userInfo)
        {
            string userInfoText = SqlUtility.UserContextInfoText(userInfo);
            byte[] encodedUserInfo = userInfoText.Take(128).Select(c => (byte)(c < 256 ? c : '?')).ToArray();

            // Using SQL query parameter, instead of a literal, to reduce load on the SQL Server execution plan cache.
            var command = new NpgsqlCommand("SET CONTEXT_INFO @userInfo");
            command.Parameters.AddWithValue("@userInfo", encodedUserInfo);
            return command;
        }

        /// <summary>
        /// See ISqlUtility.InterpretSqlException.
        /// </summary>
        public RhetosException InterpretSqlException(Exception exception, bool checkUserPermissions, ConstraintErrorMetadata constraintErrorMetadata)
        {
            if (exception == null || exception is RhetosException)
                return null;

            var sqlException = (PostgresException)ExtractSqlException(exception);
            if (sqlException == null)
                return null;

            //=========================
            // Detect user message in SQL error:

            const string userErrorCode = "R0101"; // Rhetos convention for an error raised in SQL that is intended as a message to the end user.

            if (sqlException.SqlState == userErrorCode)
                return new UserException(sqlException.MessageText, exception);

            //=========================
            // Detect system errors:

            if (sqlException.SqlState == "42501")
                if (sqlException.Message.Contains("permission was denied"))
                    return new FrameworkException("This application lacks sufficient database permissions for this operation. Please make sure that the application process is run under account that has db_owner role for the database.", exception);

            //=========================
            // Detect UNIQUE constraint:

            if (sqlException.SqlState == "23505")
            {
                // See the InterpretUniqueConstraint unit test for regex coverage.
                Regex messageParser = new Regex(@"duplicate key value violates unique constraint ""(.+)""\nDetail: Key \((.+)\)=\((.+)\) already exists\.");
                var parts = messageParser.Match(sqlException.Message).Groups;

                // TODO: Map the parts from MSSQL to PG -- TableName is missing?

                var interpretedException = new UserException("It is not allowed to enter a duplicate record.", exception);

                string tableName = null;
                string constraintName = null;
                interpretedException.Info["Constraint"] = "Unique";
                //if (parts[1].Success)
                    //interpretedException.Info["Table"] = tableName = parts[1].Value;
                if (parts[1].Success)
                    interpretedException.Info["ConstraintName"] = constraintName = parts[1].Value;
                if (parts[2].Success)
                    interpretedException.Info["ConstraintKey"] = parts[2].Value;
                if (parts[3].Success)
                    interpretedException.Info["DuplicateValue"] = parts[3].Value;

                if (constraintName != null)
                    interpretedException.SystemMessage = constraintErrorMetadata?.Invoke(tableName, constraintName);

                return interpretedException;
            }

            //=========================
            // Detect REFERENCE constraint:

            if (sqlException.SqlState == "23503")
            {
                // See the InterpretReferenceConstraint unit test for regex coverage.
                //Regex messageParser = new Regex(@"^(The )?(.+) statement conflicted with (the )?(.+) constraint [""'](.+)[""']."
                    //+ @" The conflict occurred in database [""'](.+)[""'], table [""'](.+?)[""'](, column [""'](.+?)[""'])?");

                Regex messageParser = new Regex(@"update or delete on table ""(.+)"" violates foreign key constraint ""(.+)"" on table ""(.+)""\nDetail: Key \((.+)\)=\((.+)\) is still referenced from table ""(.+)"".");

                var parts = messageParser.Match(sqlException.Message).Groups;

                // TODO: Map the parts from MSSQL to PG.

                string action = parts[2].Value ?? "";
                string constraintType = parts[4].Value ?? "";

                if (_referenceConstraintTypes.Contains(constraintType))
                {
                    UserException interpretedException = null;
                    if (action == "DELETE")
                        interpretedException = new UserException("It is not allowed to delete a record that is referenced by other records.", new string[] { _localizer[parts[7].Value], parts[9].Value }, null, exception);
                    else if (action == "INSERT")
                        interpretedException = new UserException("It is not allowed to enter the record. The entered value references nonexistent record.", new string[] { _localizer[parts[7].Value], parts[9].Value }, null, exception);
                    else if (action == "UPDATE")
                        interpretedException = new UserException("It is not allowed to edit the record. The entered value references nonexistent record.", new string[] { _localizer[parts[7].Value], parts[9].Value }, null, exception);

                    if (interpretedException != null)
                    {
                        string tableName = null;
                        string constraintName = null;
                        interpretedException.Info["Constraint"] = "Reference";
                        interpretedException.Info["Action"] = action;
                        if (parts[5].Success)
                            interpretedException.Info["ConstraintName"] = constraintName = parts[5].Value; // The FK constraint name is ambiguous: The error does not show the schema name and the base table that the INSERT or UPDATE actually happened.
                        if (parts[7].Success)
                            interpretedException.Info[action == "DELETE" ? "DependentTable" : "ReferencedTable"] = tableName = parts[7].Value;
                        if (parts[9].Success)
                            interpretedException.Info[action == "DELETE" ? "DependentColumn" : "ReferencedColumn"] = parts[9].Value;

                        if (constraintName != null)
                            interpretedException.SystemMessage = constraintErrorMetadata?.Invoke(tableName, constraintName);

                        return interpretedException;
                    }
                }
            }

            //=========================
            // Detect PRIMARY KEY constraint:

            if (sqlException.SqlState == "23505" && sqlException.Message.Contains("duplicate key value violates unique constraint"))
            {
                //Regex messageParser = new Regex(@"^Violation of PRIMARY KEY constraint '(.+)'\. Cannot insert duplicate key in object '(.+)'\.( The duplicate key value is \((.+)\)\.)?");
                Regex messageParser = new Regex(@"duplicate key value violates unique constraint ""(.+)"": Key \((.+)\)=(\(.+\)) already exists.");

                var parts = messageParser.Match(sqlException.Message).Groups;

                var errorMetadata = new Dictionary<string, object>();

                // TODO: Map the parts from MSSQL to PG.

                string tableName = null;
                string constraintName = null;
                string duplicateValue = null;
                errorMetadata["Constraint"] = "Primary key";
                if (parts[1].Success)
                    errorMetadata["ConstraintName"] = constraintName = parts[1].Value;
                if (parts[2].Success)
                    errorMetadata["Table"] = tableName = parts[2].Value;
                if (parts[4].Success)
                    errorMetadata["DuplicateValue"] = duplicateValue = parts[4].Value;

                RhetosException interpretedException;
                if (checkUserPermissions && constraintName != null && constraintErrorMetadata?.Invoke(tableName, constraintName) != null)
                    interpretedException = new ClientException(InsertingDuplicateIdMessage + (tableName != null ? $" ({tableName})" : "")) { Info = errorMetadata };
                else
                    interpretedException = new FrameworkException(InsertingDuplicateIdMessage, exception) { Info = errorMetadata };

                return interpretedException;
            }

            //=========================
            // Detect MONEY decimals constraint:

            Regex moneyExceptionMessageTester = new Regex(@"CK_\w+_\w+_money");
            if (sqlException.SqlState == "23514" && moneyExceptionMessageTester.IsMatch(sqlException.Message))
            {
                //const string quote = @"[""']";
                //Regex messageParser = new Regex(@$"conflicted with the CHECK constraint {quote}(?<constraint>CK_\w+_money){quote}\."
                //    + @$" The conflict occurred in database {quote}(.+){quote}, table {quote}(?<table>[\w\.]+){quote}, column {quote}(?<column>\w+){quote}\.");
                Regex messageParser = new Regex(@"violates check constraint ""(?<constraint>CK_\w+_money)""\s+Detail:\s+Key \((?<column>\w+)\)=\((?<value>.+?)\) already exists\.");

                var parts = messageParser.Match(sqlException.Message).Groups;

                // TODO: Map the parts from MSSQL to PG.

                var interpretedException = new UserException("It is not allowed to enter a money value with more than 2 decimals.", exception);

                interpretedException.Info["Constraint"] = "Money";
                if (parts["constraint"].Success)
                    interpretedException.Info["ConstraintName"] = parts["constraint"].Value;
                if (parts["table"].Success)
                    interpretedException.Info["Table"] = parts["table"].Value;
                if (parts["column"].Success)
                    interpretedException.Info["Column"] = parts["column"].Value;

                var systemMessageParts = new[]
                {
                    (Key:"DataStructure", parts["table"].Value),
                    (Key:"Property", parts["column"].Value)
                };
                interpretedException.SystemMessage = string.Join(",", systemMessageParts
                    .Where(part => !string.IsNullOrEmpty(part.Value))
                    .Select(part => $"{part.Key}:{part.Value}"));

                return interpretedException;
            }

            return null;
        }

        public static string LimitIdentifierLength(string name)
        {
            const int MaxLength = 128;
            if (name.Length > MaxLength)
            {
                var hashErasedPart = CsUtility.GetStableHashCode(name.Substring(MaxLength - 9)).ToString("X").PadLeft(8, '0');
                return string.Concat(name.AsSpan(0, MaxLength - 9), "_", hashErasedPart);
            }
            return name;
        }

        private static readonly string[] _referenceConstraintTypes = ["REFERENCE", "SAME TABLE REFERENCE", "FOREIGN KEY", "COLUMN FOREIGN KEY"];

        public DbException ExtractSqlException(Exception exception)
        {
            if (exception is PostgresException sqlException)
                return sqlException;
            if (exception.InnerException != null)
                return ExtractSqlException(exception.InnerException);
            return null;
        }

        private const string InsertingDuplicateIdMessage = "Inserting a record that already exists in database.";

        public string GetSchemaName(string fullObjectName)
        {
            int dotPosition = fullObjectName.IndexOf('.');
            if (dotPosition == -1)
                return "dbo";

            var schema = fullObjectName.Substring(0, dotPosition);
            return Identifier(schema);
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
                ? "N'" + value.Replace("'", "''") + "'"
                : "NULL";
        }

        public string QuoteIdentifier(string sqlIdentifier)
        {
            return "[" + sqlIdentifier.Replace("]", "]]") + "]";
        }

        public string GetShortName(string fullObjectName)
        {
            int dotPosition = fullObjectName.IndexOf('.');
            if (dotPosition == -1)
                return fullObjectName;

            var shortName = fullObjectName.Substring(dotPosition + 1);

            if (string.IsNullOrEmpty(shortName) || shortName.Contains('.'))
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
            return dataReader.GetGuid(column);
        }

        /// <summary>
        /// Vendor-independent database reader.
        /// </summary>
        public int ReadInt(DbDataReader dataReader, int column)
        {
            return dataReader.GetInt32(column);
        }

        public Guid StringToGuid(string guid)
        {
            return Guid.Parse(guid);
        }

        public string QuoteGuid(Guid? guid)
        {
            return guid.HasValue
                ? "'" + GuidToString(guid.Value) + "'"
                : "NULL";
        }

        public string GuidToString(Guid? guid)
        {
            return guid.HasValue ? guid.Value.ToString().ToUpperInvariant() : null;
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
        /// This function is used for string compatibility between different database providers.
        /// </summary>
        public string ReadEmptyNullString(DbDataReader dataReader, int column)
        {
            return dataReader.GetString(column) ?? "";
        }

        public DateTime GetDatabaseTime(ISqlExecuter sqlExecuter)
        {
            return DatabaseTimeCache.GetDatabaseTimeCached(
                () => GetDatabaseTimeUncached(sqlExecuter),
                () => DateTime.Now);
        }

        public DateTime GetDatabaseTimeUncached(ISqlExecuter sqlExecuter)
        {
            DateTime databaseTime = DateTime.MinValue;
            sqlExecuter.ExecuteReader("SELECT SYSDATETIME()",
                reader => databaseTime = reader.GetDateTime(0));
            if (databaseTime == DateTime.MinValue)
                throw new FrameworkException("Cannot read database server time.");

            return DateTime.SpecifyKind(databaseTime, DateTimeKind.Local);
        }

        public void ValidateDbConnection(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentException($"Database connection string is not specified. Please review the application's configuration ({ConnectionString.ConnectionStringConfigurationKey}).");

            // Testing the correct formatting of the connection string.
            try
            {
                new NpgsqlConnectionStringBuilder().ConnectionString = connectionString;
            }
            catch (Exception e)
            {
                throw new ArgumentException($"Database connection string has invalid format. Please review the application's configuration ({ConnectionString.ConnectionStringConfigurationKey}).", e);
            }

            // Testing if any command can be executed.
            // Also testing if the application's account is 'dbo'. The application should have full access to database to create the database on deployment,
            // but also to access the data. User permissions are handled by the Rhetos app.
            bool isDbo = false;
            using (var connection = new NpgsqlConnection(connectionString))
            using (var command = new NpgsqlCommand("SELECT IS_MEMBER('db_owner')", connection))
            {
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(0) && (int)reader[0] == 1)
                            isDbo = true;
                    }
                }
            }

            if (!isDbo)
                throw new FrameworkException("Current user does not have db_owner role for the database.");
        }

        public string SqlConnectionInfo(string connectionString)
        {
            var cs = new NpgsqlConnectionStringBuilder(connectionString);

            var elements = new ListOfTuples<string, string>
            {
                { "Host", cs.Host },
                { "Database", cs.Database },
            };

            return
                string.Join(", ", elements
                    .Where(e => !string.IsNullOrWhiteSpace(e.Item2))
                    .Select(e => e.Item1 + "=" + e.Item2));
        }
    }
}
