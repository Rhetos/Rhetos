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

using Autofac;
using Autofac.Configuration;
using Autofac.Integration.Wcf;
using Rhetos.Dom;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Rhetos
{
    public class Global : System.Web.HttpApplication
    {
        private static ILogger _logger;
        private static ILogger _performanceLogger;
        private static IEnumerable<IService> _pluginServices;

        // Called only once.
        protected void Application_Start(object sender, EventArgs e)
        {
            var stopwatch = Stopwatch.StartNew();

            var builder = new ContainerBuilder();
            builder.RegisterModule(new ConfigurationSettingsReader("autofacComponents"));
            AutofacServiceHostFactory.Container = builder.Build();

            _logger = AutofacServiceHostFactory.Container.Resolve<ILogProvider>().GetLogger("Global");
            _performanceLogger = AutofacServiceHostFactory.Container.Resolve<ILogProvider>().GetLogger("Performance");
            _pluginServices = AutofacServiceHostFactory.Container.Resolve<IEnumerable<IService>>();

            _performanceLogger.Write(stopwatch, "Autofac initialized.");

            foreach (var service in _pluginServices)
            {
                try
                {
                    service.Initialize();
                    _performanceLogger.Write(stopwatch, "Service " + service.GetType().FullName + ".Initialize");
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                        _logger.Error(ex.ToString());
                    throw;
                }
            }

            _performanceLogger.Write(stopwatch, "All services initialized.");
        }

        // Called once for each application instance.
        public override void Init()
        {
            base.Init();

            if (_pluginServices != null)
                foreach (var service in _pluginServices)
                {
                    try
                    {
                        var stopwatch = Stopwatch.StartNew();
                        service.InitializeApplicationInstance(this);
                        _performanceLogger.Write(stopwatch, "Service " + service.GetType().FullName + ".InitializeApplicationInstance");
                    }
                    catch (Exception ex)
                    {
                        if (_logger != null)
                            _logger.Error(ex.ToString());
                        throw;
                    }
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
            if (_logger != null)
                _logger.Error("Application error: " + ex.ToString());
        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {

        }
    }
}