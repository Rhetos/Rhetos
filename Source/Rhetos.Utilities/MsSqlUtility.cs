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
using System.Data.Common;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Rhetos.Utilities
{
    public class MsSqlUtility : ISqlUtility
    {
        /// <summary>
        /// Creates an SQL query that sets context_info connection variable to contain data about the user.
        /// The context_info variable can be used in SQL server to extract user info in certain situations such as logging trigger.
        /// </summary>
        public static string SetUserContextInfoQuery(IUserInfo userInfo)
        {
            string text = SqlUtility.UserContextInfoText(userInfo);
            if (string.IsNullOrEmpty(text))
                return "";

            if (text.Length > 128)
                text = text.Substring(1, 128);

            var query = new StringBuilder(text.Length * 2 + 2);
            query.Append("SET CONTEXT_INFO 0x");
            foreach (char c in text)
            {
                int i = c;
                if (i > 255) i = '?';
                query.Append(i.ToString("x2"));
            }

            return query.ToString();
        }

        public static DateTime GetDatabaseTime(ISqlExecuter sqlExecuter)
        {
            DateTime databaseTime = DateTime.MinValue;
            sqlExecuter.ExecuteReader("SELECT GETDATE()",
                reader => databaseTime = reader.GetDateTime(0));
            if (databaseTime == DateTime.MinValue)
                throw new ApplicationException("Cannot read database server time.");
            return databaseTime;
        }

        /// <summary>
        /// See ISqlUtility.InterpretSqlException.
        /// </summary>
        public RhetosException InterpretSqlException(Exception exception)
        {
            if (exception == null || exception is RhetosException)
                return null;

            var sqlException = (SqlException)ExtractSqlException(exception);
            if (sqlException == null)
                return null;

            //=========================
            // Detect user message in SQL error:

            const int userErrorCode = 101; // Rhetos convention for an error raised in SQL that is intended as a message to the end user.

            if (sqlException.State == userErrorCode)
                return new UserException(sqlException.Message, exception);

            if (sqlException.Errors != null)
                foreach (var sqlError in sqlException.Errors.Cast<SqlError>().OrderBy(e => e.LineNumber))
                    if (sqlError.State == userErrorCode)
                        return new UserException(sqlError.Message, exception);

            //=========================
            // Detect system errors:

            if (sqlException.Number == 229 || sqlException.Number == 230)
                if (sqlException.Message.Contains("permission was denied"))
                    return new FrameworkException("Rhetos server lacks sufficient database permissions for this operation. Please make sure that Rhetos Server process has db_owner role for the database.", exception);

            //=========================
            // Detect UNIQUE constraint:

            if (sqlException.Number == 2601)
            {
                // See the InterpretUniqueConstraint unit test for regex coverage.
                Regex messageParser = new Regex(@"^Cannot insert duplicate key row in object '(.+)' with unique index '(.+)'\.( The duplicate key value is \((.+)\)\.)?");
                var parts = messageParser.Match(sqlException.Message).Groups;

                var interpretedException = new UserException("It is not allowed to enter a duplicate record.", exception);

                interpretedException.Info["Constraint"] = "Unique";
                if (parts[1].Success)
                    interpretedException.Info["Table"] = parts[1].Value;
                if (parts[2].Success)
                    interpretedException.Info["ConstraintName"] = parts[2].Value;
                if (parts[4].Success)
                    interpretedException.Info["DuplicateValue"] = parts[4].Value;

                return interpretedException;
            }

            //=========================
            // Detect REFERENCE constraint:

            if (sqlException.Number == 547)
            {
                // See the InterpretReferenceConstraint unit test for regex coverage.
                Regex messageParser = new Regex(@"^(The )?(.+) statement conflicted with (the )?(.+) constraint [""'](.+)[""']. The conflict occurred in database [""'](.+)[""'], table [""'](.+?)[""'](, column [""'](.+?)[""'])?");
                var parts = messageParser.Match(sqlException.Message).Groups;
                string action = parts[2].Value ?? "";
                string constraintType = parts[4].Value ?? "";

                if (_referenceConstraintTypes.Contains(constraintType))
                {
                    UserException interpretedException = null;
                    if (action == "DELETE")
                        interpretedException = new UserException("It is not allowed to delete a record that is referenced by other records.", new string[] { parts[7].Value, parts[9].Value }, null, exception);
                    else if (action == "INSERT")
                        interpretedException = new UserException("It is not allowed to enter the record. The entered value references nonexistent record.", new string[] { parts[7].Value, parts[9].Value }, null, exception);
                    else if (action == "UPDATE")
                        interpretedException = new UserException("It is not allowed to edit the record. The entered value references nonexistent record.", new string[] { parts[7].Value, parts[9].Value }, null, exception);

                    if (interpretedException != null)
                    {
                        interpretedException.Info["Constraint"] = "Reference";
                        interpretedException.Info["Action"] = action;
                        if (parts[5].Success)
                            interpretedException.Info["ConstraintName"] = parts[5].Value; // The FK constraint name is ambiguous: The error does not show the schema name and the base table that the INSERT or UPDATE actually happened.
                        if (parts[7].Success)
                            interpretedException.Info[action == "DELETE" ? "DependentTable" : "ReferencedTable"] = parts[7].Value;
                        if (parts[9].Success)
                            interpretedException.Info[action == "DELETE" ? "DependentColumn" : "ReferencedColumn"] = parts[9].Value;

                        return interpretedException;
                    }
                }
            }

            //=========================
            // Detect PRIMARY KEY constraint:

            if (sqlException.Number == 2627 && sqlException.Message.StartsWith("Violation of PRIMARY KEY constraint"))
            {
                Regex messageParser = new Regex(@"^Violation of PRIMARY KEY constraint '(.+)'\. Cannot insert duplicate key in object '(.+)'\.( The duplicate key value is \((.+)\)\.)?");
                var parts = messageParser.Match(sqlException.Message).Groups;

                var interpretedException = new FrameworkException(InsertingDuplicateIdMessage, exception);

                interpretedException.Info["Constraint"] = "Primary key";
                if (parts[1].Success)
                    interpretedException.Info["ConstraintName"] = parts[1].Value;
                if (parts[2].Success)
                    interpretedException.Info["Table"] = parts[2].Value;
                if (parts[4].Success)
                    interpretedException.Info["DuplicateValue"] = parts[4].Value;

                return interpretedException;
            }

            return null;
        }

        public static string LimitIdentifierLength(string name)
        {
            const int MaxLength = 128;
            if (name.Length > MaxLength)
            {
                var hashErasedPart = name.Substring(MaxLength - 9).GetHashCode().ToString("X");
                return name.Substring(0, MaxLength - 9) + "_" + hashErasedPart;
            }
            return name;
        }

        private static readonly string[] _referenceConstraintTypes = new string[] { "REFERENCE", "SAME TABLE REFERENCE", "FOREIGN KEY", "COLUMN FOREIGN KEY" };

        public Exception ExtractSqlException(Exception exception)
        {
            if (exception is SqlException)
                return (SqlException)exception;
            if (exception.InnerException != null)
                return ExtractSqlException(exception.InnerException);
            return null;
        }

        public static bool IsUniqueError(RhetosException interpretedException, string table, string constraintName)
        {
            if (interpretedException == null)
                return false;
            var info = interpretedException.Info;
            return
                info != null
                && (info.GetValueOrDefault("Constraint") as string) == "Unique"
                && (info.GetValueOrDefault("Table") as string) == table
                && (info.GetValueOrDefault("ConstraintName") as string) == constraintName;
        }

        public static bool IsReferenceErrorOnInsertUpdate(RhetosException interpretedException, string referencedTable, string referencedColumn, string constraintName)
        {
            if (interpretedException == null)
                return false;
            var info = interpretedException.Info;
            return
                info != null
                && (info.GetValueOrDefault("Constraint") as string) == "Reference"
                && ((info.GetValueOrDefault("Action") as string) == "INSERT" || (info.GetValueOrDefault("Action") as string) == "UPDATE")
                && (info.GetValueOrDefault("ReferencedTable") as string) == referencedTable
                && (info.GetValueOrDefault("ReferencedColumn") as string) == referencedColumn
                && (info.GetValueOrDefault("ConstraintName") as string) == constraintName;
        }

        public static bool IsReferenceErrorOnDelete(RhetosException interpretedException, string dependentTable, string dependentColumn, string constraintName)
        {
            if (interpretedException == null)
                return false;
            var info = interpretedException.Info;
            return
                info != null
                && (info.GetValueOrDefault("Constraint") as string) == "Reference"
                && (info.GetValueOrDefault("Action") as string) == "DELETE"
                && (info.GetValueOrDefault("DependentTable") as string) == dependentTable
                && (info.GetValueOrDefault("DependentColumn") as string) == dependentColumn
                && (info.GetValueOrDefault("ConstraintName") as string) == constraintName;
        }

        private const string InsertingDuplicateIdMessage = "Inserting a record that already exists in database.";

        public static void ThrowIfPrimaryKeyErrorOnInsert(RhetosException interpretedException, string tableName)
        {
            if (interpretedException != null
                && interpretedException.Info != null
                && (interpretedException.Info.GetValueOrDefault("Constraint") as string) == "Primary key"
                && (interpretedException.Info.GetValueOrDefault("Table") as string) == tableName)
            {
                string pkValue = interpretedException.Info.GetValueOrDefault("DuplicateValue") as string;
                throw new ClientException(InsertingDuplicateIdMessage + (pkValue != null ? " ID=" + pkValue : ""));
            }
        }

        public static SqlVersion GetSqlVersion(DbConnection connection)
        {
            var majorVersion = Int32.Parse(connection.ServerVersion.Substring(0, 2), CultureInfo.InvariantCulture);

            if (majorVersion >= 11)
            {
                return SqlVersion.Sql11;
            }

            if (majorVersion == 10)
            {
                return SqlVersion.Sql10;
            }

            if (majorVersion == 9)
            {
                return SqlVersion.Sql9;
            }
            return SqlVersion.Sql8;
        }

        public static ServerType GetServerType(DbConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = "SELECT serverproperty('EngineEdition')";
            var reader = command.ExecuteReader();
            int engineEdition = 0;
            while (reader.Read())
            {
                engineEdition = reader.GetInt32(0);
            }
            const int sqlAzureEngineEdition = 5;
            return engineEdition == sqlAzureEngineEdition ? ServerType.Cloud : ServerType.OnPremises;
        }

        public static string GetVersionHint(SqlVersion version, ServerType serverType)
        {
            if (serverType == ServerType.Cloud)
            {
                return SqlProviderManifest.TokenAzure11;
            }

            switch (version)
            {
                case SqlVersion.Sql8:
                    return SqlProviderManifest.TokenSql8;

                case SqlVersion.Sql9:
                    return SqlProviderManifest.TokenSql9;

                case SqlVersion.Sql10:
                    return SqlProviderManifest.TokenSql10;

                case SqlVersion.Sql11:
                    return SqlProviderManifest.TokenSql11;

                default:
                    throw new ArgumentException("Could not determine storage version; a valid storage connection or a version hint is required.");
            }
        }

        public static string QueryForManifestToken(DbConnection conn)
        {
            var sqlVersion = GetSqlVersion(conn);
            var serverType = sqlVersion >= SqlVersion.Sql11 ? GetServerType(conn) : ServerType.OnPremises;
            return GetVersionHint(sqlVersion, serverType);
        }

        public static string GetProviderManifestToken()
        {
            using (SqlConnection sc = new SqlConnection(SqlUtility.ConnectionString))
            {
                sc.Open();
                string providerManifestToken = QueryForManifestToken(sc);
                sc.Close();
                return providerManifestToken;
            }
        }
    }
}
