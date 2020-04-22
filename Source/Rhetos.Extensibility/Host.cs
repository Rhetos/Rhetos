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
using Newtonsoft.Json;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.IO;
using System.Linq;

namespace Rhetos
{
    public class Host
    {
        /// <summary>
        /// Helper method that creates a run-time Dependency Injection container for Rhetos application.
        /// It bundles Rhetos runtime location (<see cref="Host.Find"/>),
        /// configuration (<see cref="IRhetosRuntime.BuildConfiguration"/>)
        /// and DI registration (<see cref="IRhetosRuntime.BuildContainer"/>).
        /// If you need more control over the system configuration, use these methods directly instead.
        /// </summary>
        /// /// <param name="applicationFolder">
        /// Folder where the Rhetos configuration file is located (see <see cref="RhetosAppEnvironment.ConfigurationFileName"/>),
        /// or any subfolder.
        /// If not specified, the current application's base directory is used by default.
        /// </param>
        /// <param name="logProvider">If not specified, ConsoleLogProvider is used by default.</param>
        /// <returns>Dependency Injection container for Rhetos runtime.</returns>
        public static IContainer CreateRhetosContainer(string applicationFolder = null, ILogProvider logProvider = null,
            Action<IConfigurationBuilder> addCustomConfiguration = null, Action<ContainerBuilder> registerCustomComponents = null)
        {
            applicationFolder = applicationFolder ?? AppDomain.CurrentDomain.BaseDirectory;
            logProvider = logProvider ?? new ConsoleLogProvider();

            var host = Find(applicationFolder, logProvider);
            var configurationProvider = host.RhetosRuntime.BuildConfiguration(logProvider, host.ConfigurationFolder, addCustomConfiguration);
            return host.RhetosRuntime.BuildContainer(logProvider, configurationProvider, registerCustomComponents);
        }

        public IRhetosRuntime RhetosRuntime { get; private set; }

        public string ConfigurationFolder { get; private set; }

        /// <param name="applicationFolder">
        /// Folder where the Rhetos configuration file is located (see <see cref="RhetosAppEnvironment.ConfigurationFileName"/>),
        /// or any subfolder.
        /// </param>
        public static Host Find(string applicationFolder, ILogProvider logProvider)
        {
            var configurationFolder = FindConfiguration(applicationFolder);
            string rhetosRuntimePath = LoadRhetosRuntimePath(configurationFolder);
            IRhetosRuntime rhetosRuntimeInstance = CreateRhetosRuntimeInstance(logProvider, rhetosRuntimePath);

            return new Host
            {
                RhetosRuntime = rhetosRuntimeInstance,
                ConfigurationFolder = configurationFolder,
            };
        }

        private static string FindConfiguration(string applicationFolder)
        {
            var configurationDirectory = new DirectoryInfo(applicationFolder);
            Exception searchException = null;
            try
            {
                while (configurationDirectory != null
                    && !configurationDirectory.GetFiles().Any(file => string.Equals(file.Name, RhetosAppEnvironment.ConfigurationFileName, StringComparison.OrdinalIgnoreCase)))
                {
                    configurationDirectory = configurationDirectory.Parent;
                }
            }
            catch (Exception e)
            {
                searchException = e;
            }
            if (configurationDirectory == null || searchException != null)
                throw new FrameworkException(
                    $"Cannot find application's configuration ({RhetosAppEnvironment.ConfigurationFileName})" +
                    $" in '{Path.GetFullPath(applicationFolder)}' or any parent folder." +
                    $" Make sure the specified folder is correct and that the build has passed successfully.",
                    searchException);
            return configurationDirectory.FullName;
        }

        private static string LoadRhetosRuntimePath(string configurationFolder)
        {
            string configurationFile = Path.Combine(configurationFolder, RhetosAppEnvironment.ConfigurationFileName);
            string serialized = File.ReadAllText(configurationFile);

            var runtimeSettings = JsonConvert.DeserializeObject<RhetosAppOptions>(serialized);
            if (string.IsNullOrEmpty(runtimeSettings.RhetosRuntimePath))
                throw new FrameworkException($"Configuration setting '{nameof(RhetosAppOptions.RhetosRuntimePath)}' is not specified in '{configurationFile}'.");

            string rhetosRuntimePath = Path.Combine(configurationFolder, runtimeSettings.RhetosRuntimePath);
            return rhetosRuntimePath;
        }

        private static IRhetosRuntime CreateRhetosRuntimeInstance(ILogProvider logProvider, string rhetosRuntimePath)
        {
            var pluginScanner = new PluginScanner(() => new[] { rhetosRuntimePath }, Path.GetDirectoryName(rhetosRuntimePath), logProvider, new PluginScannerOptions());
            var rhetosRuntimeTypes = pluginScanner.FindPlugins(typeof(IRhetosRuntime)).Select(x => x.Type).ToList();

            if (rhetosRuntimeTypes.Count == 0)
                throw new FrameworkException($"No implementation of interface {nameof(IRhetosRuntime)} found with Export attribute.");

            if (rhetosRuntimeTypes.Count > 1)
                throw new FrameworkException($"Found multiple implementation of the type {nameof(IRhetosRuntime)}.");

            var rhetosRuntimeInstance = (IRhetosRuntime)Activator.CreateInstance(rhetosRuntimeTypes.Single());
            return rhetosRuntimeInstance;
        }
    }
}
