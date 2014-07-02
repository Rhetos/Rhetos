using Autofac;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace DeployPackages
{
    class ConnectionStringReport
    {
        private static string _checkDboMembershipCmd = "SELECT IS_MEMBER('db_owner')";

        public bool connectivity = false;
        public bool isDbo = false;

        public string error = null;
        public Exception exceptionRaised = null;

        private IContainer container;

        public ConnectionStringReport(IContainer container)
        {
            this.container = container;
            CheckConnectivity();
        }

        public void CheckConnectivity()
        {
            try
            {
                var SqlExecuter = container.Resolve<ISqlExecuter>();
                SqlExecuter.ExecuteReader(_checkDboMembershipCmd, reader =>
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
