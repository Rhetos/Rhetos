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

using Rhetos;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace Rhetos.Deployment
{
    public class ConnectionStringReport
    {
        private static string _checkDboMembershipCmd = "SELECT IS_MEMBER('db_owner')";

        public bool connectivity = false;
        public bool isDbo = false;

        public string error = null;
        public Exception exceptionRaised = null;

        private ISqlExecuter sqlExecuter;

        public ConnectionStringReport(ISqlExecuter sqlExecuter)
        {
            this.sqlExecuter = sqlExecuter;
            CheckConnectivity();
        }

        void CheckConnectivity()
        {
            // temporary fix to support non MsSql databases
            if (SqlUtility.DatabaseLanguage != "MsSql")
            {
                connectivity = true;
                isDbo = true;
            }
            else
            {
                try
                {
                    sqlExecuter.ExecuteReader(_checkDboMembershipCmd, reader =>
                        {
                            if (!reader.IsDBNull(0) && ((int)reader[0] == 1)) isDbo = true;
                        });
                    connectivity = true;
                }
                catch (Exception e)
                {
                    connectivity = false;
                    error = e.Message;
                    exceptionRaised = e;
                }
            }
        }
    }
}
