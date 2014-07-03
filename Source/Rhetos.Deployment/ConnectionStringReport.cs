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
