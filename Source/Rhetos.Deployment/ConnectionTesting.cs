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

namespace Rhetos.Deployment
{
    public class ConnectionStringReport
    {
        private const string _checkDboMembershipMsSql = "SELECT IS_MEMBER('db_owner')";

        public static void ValidateDbConnection(ISqlExecuter sqlExecuter)
        {
            // This validation currently runs only on MS SQL databases.
            if (SqlUtility.DatabaseLanguage == "MsSql")
            {
                bool isDbo = false;
                sqlExecuter.ExecuteReader(_checkDboMembershipMsSql, reader =>
                {
                    if (!reader.IsDBNull(0) && ((int)reader[0] == 1)) isDbo = true;
                });
                if (!isDbo)
                    throw (new FrameworkException("Current user does not have db_owner role for the database."));
            }
        }
    }
}
