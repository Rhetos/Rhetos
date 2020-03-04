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

            var configuration = LoadConfiguration();
            var builder = new RhetosContainerBuilder(configuration, new NLogProvider(), LegacyUtilities.GetListAssembliesDelegate(configuration));
            AddRhetosComponents(builder);
            AutofacServiceHostFactory.Container = builder.Build();

            var logProvider = AutofacServiceHostFactory.Container.Resolve<ILogProvider>();
            _logger = logProvider.GetLogger("Global");
            _logger.Trace("Startup");

            _webServices = AutofacServiceHostFactory.Container.Resolve<WebServices>();

            var _performanceLogger = logProvider.GetLogger("Performance");
            _performanceLogger.Write(stopwatch, "Application configured.");
        }

        private static IConfigurationProvider LoadConfiguration()
        {
            var rhetosAppEnvironment = RhetosAppEnvironmentProvider.Load(AppDomain.CurrentDomain.BaseDirectory);
            var configurationProvider = new ConfigurationBuilder()
                .AddRhetosAppEnvironment(rhetosAppEnvironment)
                .AddConfigurationManagerConfiguration()
                .Build();
            return configurationProvider;
        }

        internal static void AddRhetosComponents(RhetosContainerBuilder builder)
        {
            // General registrations
            builder.AddRhetosRuntime();

            // Specific registrations
            builder.RegisterType<WcfWindowsUserInfo>().As<IUserInfo>().InstancePerLifetimeScope();
            builder.RegisterType<RhetosService>().As<RhetosService>().As<IServerApplication>();
            builder.RegisterType<Rhetos.Web.GlobalErrorHandler>();
            builder.RegisterType<WebServices>();
            builder.GetPluginRegistration().FindAndRegisterPlugins<IService>();
            builder.GetPluginRegistration().FindAndRegisterPlugins<IHomePageSnippet>();

            // Plugin modules
            builder.AddPluginModules();
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