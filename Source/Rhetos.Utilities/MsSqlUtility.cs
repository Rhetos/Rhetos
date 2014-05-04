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
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace Rhetos.Utilities
{
    public static class MsSqlUtility
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

        /// <summary>
        /// It the function returns null, the exception should not be changed. In that case simply use "throw;".
        /// </summary>
        public static Exception ProcessSqlException(Exception ex)
        {
            if (ex == null || !(ex is SqlException))
                return null;

            var sqlException = (SqlException)ex;

            SqlError[] errorArray = new SqlError[sqlException.Errors.Count];
            sqlException.Errors.CopyTo(errorArray, 0);
            var errors = from e in errorArray
                         orderby e.LineNumber
                         select e;
            foreach (var err in errors)
                if (err.State == 101) // Rhetos convention for an error raised in SQL that is intended as a message to the end user.
                    return new UserException(err.Message);

            return sqlException;
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
    }
}
