using Autofac;
using Autofac.Configuration;
using Rhetos;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Http;
using Autofac.Integration.WebApi;
using System.Reflection;
using System.Linq;
using System.Net.Http.Formatting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace RhetosWebApi
{
    public class MvcApplication : System.Web.HttpApplication
    {
        private static ILogger _logger;
        private static ILogger _performanceLogger;
        private static IEnumerable<IService> _pluginServices;

        // Called only once.
        protected void Application_Start(object sender, EventArgs e)
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            var stopwatch = Stopwatch.StartNew();

            Paths.InitializeRhetosServer();

            var builder = new ContainerBuilder();
            var config = GlobalConfiguration.Configuration;
            
            config.Formatters.Clear();
            config.Formatters.Add(new JsonMediaTypeFormatter());
            config.Formatters.JsonFormatter.SerializerSettings =
            new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver()
            };

            builder.RegisterModule(new ConfigurationSettingsReader("autofacComponents"));

            var container = builder.Build();
            config.DependencyResolver = new AutofacWebApiDependencyResolver(container);

            _logger = container.Resolve<ILogProvider>().GetLogger("Global");
            _performanceLogger = container.Resolve<ILogProvider>().GetLogger("Performance");
            _pluginServices = container.Resolve<IEnumerable<IService>>();
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
            Console.WriteLine("Done");
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
    }
}
