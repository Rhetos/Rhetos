/*
    Copyright (C) 2013 Omega software d.o.o.

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
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using Autofac.Integration.Wcf;
using Autofac;
using Rhetos.Configuration.Autofac;
using System.Configuration;
using System.IO;
using Autofac.Configuration;
using Rhetos.Logging;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceModel.Description;

namespace Rhetos
{
    public class Global : System.Web.HttpApplication
    {
        private ILogger ErrorLogger;

        protected void Application_Start(object sender, EventArgs e)
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new ConfigurationSettingsReader("autofacComponents"));
            AutofacServiceHostFactory.Container = builder.Build();

            ErrorLogger = AutofacServiceHostFactory.Container.Resolve<ILogProvider>().GetLogger("Unhandled exception");
            
            foreach (var service in AutofacServiceHostFactory.Container.Resolve<IEnumerable<IService>>())
            {
                service.Initialize();
                ErrorLogger.Trace("Service " + service.GetType().FullName + " initialized.");
            }
        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {

        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {
            var ex = Server.GetLastError();
            if (ErrorLogger != null)
                ErrorLogger.Error(ex.ToString());
        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {

        }
    }
}