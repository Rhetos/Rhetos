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
using Rhetos;
using Rhetos.Deployment;
using Rhetos.Dom;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace DeployPackages
{
    public class ApplicationDeployment
    {
        private readonly ILogger _logger;
        private readonly IConfigurationProvider _configurationProvider;
        private readonly ILogProvider _logProvider;

        public ApplicationDeployment(IConfigurationProvider configurationProvider, ILogProvider logProvider)
        {
            _logger = logProvider.GetLogger("DeployPackages");
            _configurationProvider = configurationProvider;
            _logProvider = logProvider;
        }
        
        public void GenerateApplication()
        {
            _logger.Trace("Loading plugins.");
            var stopwatch = Stopwatch.StartNew();

            var builder = new RhetosContainerBuilder(_configurationProvider, _logProvider)
                .AddRhetosDeployment()
                .AddProcessUserOverride();

            using (var container = builder.Build())
            {
                var performanceLogger = container.Resolve<ILogProvider>().GetLogger("Performance");
                performanceLogger.Write(stopwatch, "DeployPackages.Program: Modules and plugins registered.");
                Plugins.LogRegistrationStatistics("Generating application", container, _logProvider);

                container.Resolve<ApplicationGenerator>().ExecuteGenerators();
            }
        }

        public void InitializeGeneratedApplication()
        {
            // Creating a new container builder instead of using builder.Update(), because of severe performance issues with the Update method.

            _logger.Trace("Loading generated plugins.");
            var stopwatch = Stopwatch.StartNew();

            var builder = new RhetosContainerBuilder(_configurationProvider, _logProvider)
                .AddApplicationInitialization()
                .AddRhetosRuntime()
                .AddProcessUserOverride();

            using (var container = builder.Build())
            {
                var performanceLogger = container.Resolve<ILogProvider>().GetLogger("Performance");
                var initializers = ApplicationInitialization.GetSortedInitializers(container);

                performanceLogger.Write(stopwatch, "DeployPackages.Program: New modules and plugins registered.");
                Plugins.LogRegistrationStatistics("Initializing application", container, _logProvider);

                if (!initializers.Any())
                {
                    _logger.Trace("No server initialization plugins.");
                }
                else
                {
                    foreach (var initializer in initializers)
                        ApplicationInitialization.ExecuteInitializer(container, initializer);
                }
            }

            RestartWebServer();
        }

        private void RestartWebServer()
        {
            var rhetosAppEnvironment = new RhetosAppEnvironment(_configurationProvider.GetOptions<RhetosAppOptions>().RootPath);
            var configFile = Path.Combine(rhetosAppEnvironment.RootPath, "Web.config");
            if (FilesUtility.SafeTouch(configFile))
                _logger.Trace($"Updated {Path.GetFileName(configFile)} modification date to restart server.");
            else
                _logger.Trace($"Missing {configFile}.");
        }
    }
}
