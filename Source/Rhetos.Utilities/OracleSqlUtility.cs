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

using Oracle.ManagedDataAccess.Client;
using System;
using System.Data.Common;
using System.Globalization;

namespace Rhetos.Utilities
{
    public class OracleSqlUtility : ISqlUtility
    {
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

        public static readonly string OracleNationalLanguageKey = "Rhetos:DatabaseOracle:NationalLanguage";
        private static string _setNationalLanguageQuery;

        /// <summary>
        /// Returns an SQL query that is used to set the national language, for string comparison and sorting.
        /// </summary>
        public static string SetNationalLanguageQuery()
        {
            if (_setNationalLanguageQuery == null)
            {
                if (!string.IsNullOrEmpty(SqlUtility.NationalLanguage))
                    _setNationalLanguageQuery = string.Format(@"BEGIN
  EXECUTE IMMEDIATE 'ALTER SESSION SET NLS_COMP=LINGUISTIC';
  EXECUTE IMMEDIATE 'ALTER SESSION SET NLS_SORT={0}';
END;", SqlUtility.NationalLanguage);
                else
                    _setNationalLanguageQuery = "";
            }

            return _setNationalLanguageQuery;

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
            if (userInfo.IsUserRecognized)
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

        public static string GetSchemaName(string fullObjectName)
        {
            int dotPosition = fullObjectName.IndexOf('.');
            if (dotPosition == -1)
                throw new FrameworkException($"Missing schema name for database object '{fullObjectName}'.");

            var schema = fullObjectName.Substring(0, dotPosition);
            return SqlUtility.Identifier(schema);
        }
    }
}
