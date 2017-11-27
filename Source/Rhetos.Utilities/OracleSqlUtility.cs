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
using System.Linq;
using System.Text;
using Oracle.ManagedDataAccess.Client;

namespace Rhetos.Utilities
{
    public class OracleSqlUtility : ISqlUtility
    {
        public static string LimitIdentifierLength(string name)
        {
            const int MaxLength = 30;
            if (name.Length > MaxLength)
            {
                var hashErasedPart = name.Substring(MaxLength - 9).GetHashCode().ToString("X");
                return name.Substring(0, MaxLength - 9) + "_" + hashErasedPart;
            }
            return name;
        }

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
    }
}
