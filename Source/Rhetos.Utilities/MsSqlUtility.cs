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
using System.Data.SqlClient;
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

            var query = new StringBuilder(text.Length*2 + 2);
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
            // Detect UNIQUE constaint:

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
            // Detect REFERENCE constaint:

            if (sqlException.Number == 547)
            {
                // See the InterpretReferenceConstraint unit test for regex coverage.
                Regex messageParser = new Regex(@"^(The )?(.+) statement conflicted with (the )?(.+) constraint [""'](.+)[""']. The conflict occurred in database [""'](.+)[""'], table [""'](.+?)[""'](, column [""'](.+?)[""'])?");
                var parts = messageParser.Match(sqlException.Message).Groups;
                string action = parts[2].Value ?? "";
                string constraintType = parts[4].Value ?? "";

                if (_referenceConstraintTypes.Contains(constraintType) && _referenceConstraintMessageByAction.ContainsKey(action))
                {
                    var interpretedException = new UserException(_referenceConstraintMessageByAction[action], exception);

                    interpretedException.Info["Constraint"] = "Reference";
                    interpretedException.Info["Action"] = action;
                    if (parts[5].Success)
                        interpretedException.Info["ConstraintName"] = parts[5].Value; // The FK constraint name is ambiguous: The error does not show the schema name and the base table that the INSERT or UPDATE acctually happened.
                    if (parts[7].Success)
                        interpretedException.Info[action == "DELETE" ? "DependentTable" : "ReferencedTable"] = parts[7].Value;
                    if (parts[9].Success)
                        interpretedException.Info[action == "DELETE" ? "DependentColumn" : "ReferencedColumn"] = parts[9].Value;

                    return interpretedException;
                }
            }

            return null;
        }
        
        private static readonly string[] _referenceConstraintTypes = new string[] { "REFERENCE", "SAME TABLE REFERENCE", "FOREIGN KEY", "COLUMN FOREIGN KEY" };

        private static readonly SortedDictionary<string, string> _referenceConstraintMessageByAction = new SortedDictionary<string, string>
        {
            { "DELETE", "It is not allowed to delete a record that is referenced by other records." },
            { "INSERT", "It is not allowed to enter the record. The entered value references nonexistent record." },
            { "UPDATE", "It is not allowed to edit the record. The entered value references nonexistent record." },
        };

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
    }
}
