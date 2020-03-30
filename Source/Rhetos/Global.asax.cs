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
using Autofac.Integration.Wcf;
using Rhetos.Logging;
using Rhetos.Security;
using Rhetos.Utilities;
using Rhetos.Utilities.ApplicationConfiguration;
using Rhetos.Web;
using System;
using System.Diagnostics;
using System.IO;

namespace Rhetos
{
    public class Global : System.Web.HttpApplication
    {
        private static ILogger _logger;
        private static WebServices _webServices;

        // Called only once.
        protected void Application_Start(object sender, EventArgs e)
        {
            ConfigureApplication();
            _webServices.Initialize();
        }

        private static void ConfigureApplication()
        {
            var stopwatch = Stopwatch.StartNew();
            var logProvider = new NLogProvider();
            var runtimeContextFactory = new RhetosRuntime(true);
            var configuration = runtimeContextFactory.BuildConfiguration(logProvider,
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin"),
                null);
            AutofacServiceHostFactory.Container = runtimeContextFactory.BuildContainer(logProvider, configuration, null);

            _logger = logProvider.GetLogger("Global");
            _logger.Trace("Startup");

            _webServices = AutofacServiceHostFactory.Container.Resolve<WebServices>();

            var _performanceLogger = logProvider.GetLogger("Performance");
            _performanceLogger.Write(stopwatch, "Application configured.");
        }

        // Called once for each application instance.
        public override void Init()
        {
            base.Init();
            _logger.Trace("New application instance");
            _webServices.InitializeApplicationInstance(this);
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
            SafeLogger.Error($"Application error: {ex}");
        }

        protected void Session_End(object sender, EventArgs e)
        {
        }

        protected void Application_End(object sender, EventArgs e)
        {
            SafeLogger.Trace("Shutdown");
        }

        /// <summary>
        /// If an error occur during the application configuration process, a temporary logger will be uses for error reporting.
        /// </summary>
        private ILogger SafeLogger => _logger ?? new NLogProvider().GetLogger("Configuration");
    }
}