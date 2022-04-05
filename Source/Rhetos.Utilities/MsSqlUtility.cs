﻿/*
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
using System.Linq;
using System.Text.RegularExpressions;

namespace Rhetos.Utilities
{
    public class MsSqlUtility : ISqlUtility
    {
        public DbConnection CreateConnection(string connectionString, IUserInfo userInfo)
        {
            var connection = new SqlConnection(connectionString);

            try
            {
                connection.Open();
                using var sqlCommand = CreateUserContextInfoCommand(userInfo);
                sqlCommand.Connection = connection;
                sqlCommand.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                var csb = new SqlConnectionStringBuilder(connectionString);
                string secutiryInfo = csb.IntegratedSecurity ? $"integrated security account '{Environment.UserName}'" : $"SQL login '{csb.UserID}'";
                string msg = $"Could not connect to server '{csb.DataSource}', database '{csb.InitialCatalog}' using {secutiryInfo}.";
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
        public static DbCommand CreateUserContextInfoCommand(IUserInfo userInfo)
        {
            string userInfoText = SqlUtility.UserContextInfoText(userInfo);
            byte[] encodedUserInfo = userInfoText.Take(128).Select(c => (byte)(c < 256 ? c : '?')).ToArray();

            // Using SQL query parameter, instead of a literal, to reduce load on the SQL Server execution plan cache.
            var command = new SqlCommand("SET CONTEXT_INFO @userInfo");
            command.Parameters.AddWithValue("@userInfo", encodedUserInfo);
            return command;
        }

        public static DateTime GetDatabaseTime(ISqlExecuter sqlExecuter)
        {
            DateTime databaseTime = DateTime.MinValue;
            sqlExecuter.ExecuteReader("SELECT SYSDATETIME()",
                reader => databaseTime = reader.GetDateTime(0));
            if (databaseTime == DateTime.MinValue)
                throw new FrameworkException("Cannot read database server time.");
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
                    return new FrameworkException("This application lacks sufficient database permissions for this operation. Please make sure that the application process is run under account that has db_owner role for the database.", exception);

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
                Regex messageParser = new Regex(@"^(The )?(.+) statement conflicted with (the )?(.+) constraint [""'](.+)[""']."
                    + @" The conflict occurred in database [""'](.+)[""'], table [""'](.+?)[""'](, column [""'](.+?)[""'])?");
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

            //=========================
            // Detect MONEY decimals constraint:

            Regex moneyExceptionMessageTester = new Regex(@"CK_\w+_\w+_money");
            if (sqlException.Number == 547 && moneyExceptionMessageTester.IsMatch(sqlException.Message))
            {
                const string quote = @"[""']";
                Regex messageParser = new Regex(@$"conflicted with the CHECK constraint {quote}(?<constraint>CK_\w+_money){quote}\."
                    + @$" The conflict occurred in database {quote}(.+){quote}, table {quote}(?<table>[\w\.]+){quote}, column {quote}(?<column>\w+){quote}\.");
                var parts = messageParser.Match(sqlException.Message).Groups;

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
                && GetString(info, "Constraint") == "Unique"
                && GetString(info, "Table") == table
                && GetString(info, "ConstraintName") == constraintName;
        }

        public static bool IsReferenceErrorOnInsertUpdate(RhetosException interpretedException, string referencedTable, string referencedColumn, string constraintName)
        {
            if (interpretedException == null)
                return false;
            var info = interpretedException.Info;
            return
                info != null
                && GetString(info, "Constraint") == "Reference"
                && (GetString(info, "Action") == "INSERT" || GetString(info, "Action") == "UPDATE")
                && GetString(info, "ReferencedTable") == referencedTable
                && GetString(info, "ReferencedColumn") == referencedColumn
                && GetString(info, "ConstraintName") == constraintName;
        }

        public static bool IsReferenceErrorOnDelete(RhetosException interpretedException, string dependentTable, string dependentColumn, string constraintName)
        {
            if (interpretedException == null)
                return false;
            var info = interpretedException.Info;
            return
                info != null
                && GetString(info, "Constraint") == "Reference"
                && GetString(info, "Action") == "DELETE"
                && GetString(info, "DependentTable") == dependentTable
                && GetString(info, "DependentColumn") == dependentColumn
                && GetString(info, "ConstraintName") == constraintName;
        }

        private const string InsertingDuplicateIdMessage = "Inserting a record that already exists in database.";

        public static void ThrowIfPrimaryKeyErrorOnInsert(RhetosException interpretedException, string tableName)
        {
            if (interpretedException != null
                && interpretedException.Info != null
                && GetString(interpretedException.Info, "Constraint") == "Primary key"
                && GetString(interpretedException.Info, "Table") == tableName)
            {
                string pkValue = GetString(interpretedException.Info, "DuplicateValue");
                throw new ClientException(InsertingDuplicateIdMessage + (pkValue != null ? " ID=" + pkValue : ""));
            }
        }

        /// <summary>
        /// Returns null is the key does not exist, or the value is not a string.
        /// </summary>
        private static string GetString(IDictionary<string, object> dictionary, string key)
        {
            return dictionary.TryGetValue(key, out object value) ? value as string : null;
        }

        public static string GetSchemaName(string fullObjectName)
        {
            int dotPosition = fullObjectName.IndexOf('.');
            if (dotPosition == -1)
                return "dbo";

            var schema = fullObjectName.Substring(0, dotPosition);
            return SqlUtility.Identifier(schema);
        }
    }
}
