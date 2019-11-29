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
using System.Reflection;
using System.Text;

namespace Rhetos
{
    public class ApplicationDeployment
    {
        private readonly ILogger _logger;
        private readonly IConfigurationProvider _configurationProvider;
        private readonly ILogProvider _logProvider;
        private readonly RhetosAppEnvironment _rhetosAppEnvironment;
        private readonly FilesUtility _filesUtility;
        private readonly InitializationContext _initializationContext;

        public ApplicationDeployment(IConfigurationProvider configurationProvider, ILogProvider logProvider)
        {
            _logger = logProvider.GetLogger("DeployPackages");
            _configurationProvider = configurationProvider;
            _logProvider = logProvider;
            _filesUtility = new FilesUtility(logProvider);
            _initializationContext = new InitializationContext(configurationProvider, logProvider);
            _rhetosAppEnvironment = new RhetosAppEnvironment(_configurationProvider.GetOptions<RhetosAppOptions>().RootPath);
            LegacyUtilities.Initialize(configurationProvider);
        }

        public void GenerateApplication()
        {
            _logger.Trace("Loading plugins.");
            var stopwatch = Stopwatch.StartNew();

            var builder = new RhetosContainerBuilder(_configurationProvider, _logProvider)
                .AddRhetosDeployment()
                .AddProcessUserOverride();

            AppDomain.CurrentDomain.AssemblyResolve += SearchForAssembly;
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += SearchForAssembly;

            using (var container = builder.Build())
            {
                var performanceLogger = container.Resolve<ILogProvider>().GetLogger("Performance");
                performanceLogger.Write(stopwatch, "DeployPackages.Program: Modules and plugins registered.");
                Plugins.LogRegistrationStatistics("Generating application", container, _logProvider);

                var rhetosAppEnviorment = container.Resolve<RhetosAppEnvironment>();
                ThrowOnObsoleteFolders(rhetosAppEnviorment);
                DeleteObsoleteGeneratedFiles(rhetosAppEnviorment);

                container.Resolve<ILogProvider>().GetLogger("DeployPackages").Trace("Moving old generated files to cache.");
                container.Resolve<GeneratedFilesCache>().MoveGeneratedFilesToCache();
                container.Resolve<FilesUtility>().SafeCreateDirectory(rhetosAppEnviorment.GeneratedFolder);

                container.Resolve<ApplicationGenerator>().ExecuteGenerators();

            }

            AppDomain.CurrentDomain.AssemblyResolve -= SearchForAssembly;
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= SearchForAssembly;
        }

        public void CheckForUpdateDatabaseConditions()
        {
            var missingFile = _rhetosAppEnvironment.DomAssemblyFiles.FirstOrDefault(f => !File.Exists(f));
            if (missingFile != null)
                throw new UserException($"'/DatabaseOnly' switch cannot be used if the server have not been deployed successfully before. Run a regular deployment instead. Missing '{missingFile}'.");
        }

        public void UpdateDatabase()
        {
            _logger.Trace("Loading plugins.");
            var stopwatch = Stopwatch.StartNew();

            var builder = new RhetosContainerBuilder(_configurationProvider, _logProvider)
                .AddRhetosDeployment()
                .AddProcessUserOverride();

            AppDomain.CurrentDomain.AssemblyResolve += SearchForAssembly;

            using (var container = builder.Build())
            {
                var performanceLogger = container.Resolve<ILogProvider>().GetLogger("Performance");
                performanceLogger.Write(stopwatch, "DeployPackages.Program: Modules and plugins registered.");
                Plugins.LogRegistrationStatistics("Generating application", container, _logProvider);
                container.Resolve<ApplicationGenerator>().UpdateDatabase();
            }

            AppDomain.CurrentDomain.AssemblyResolve -= SearchForAssembly;
        }

        public void InitializeGeneratedApplication()
        {
            // Creating a new container builder instead of using builder.Update(), because of severe performance issues with the Update method.

            _logger.Trace("Loading generated plugins.");
            var stopwatch = Stopwatch.StartNew();

            AppDomain.CurrentDomain.AssemblyResolve += SearchForAssembly;

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

            AppDomain.CurrentDomain.AssemblyResolve -= SearchForAssembly;

            RestartWebServer();
        }

        private void ThrowOnObsoleteFolders(RhetosAppEnvironment rhetosAppEnvironment)
        {
            var obsoleteFolders = new string[]
            {
                Path.Combine(rhetosAppEnvironment.RootPath, "DslScripts"),
                Path.Combine(rhetosAppEnvironment.RootPath, "DataMigration")
            };
            var obsoleteFolder = obsoleteFolders.FirstOrDefault(folder => Directory.Exists(folder));
            if (obsoleteFolder != null)
                throw new UserException("Please backup all Rhetos server folders and delete obsolete folder '" + obsoleteFolder + "'. It is no longer used.");
        }

        private void DeleteObsoleteGeneratedFiles(RhetosAppEnvironment rhetosAppEnvironment)
        {
            var deleteObsoleteFiles = new string[]
            {
                Path.Combine(rhetosAppEnvironment.BinFolder, "ServerDom.cs"),
                Path.Combine(rhetosAppEnvironment.BinFolder, "ServerDom.dll"),
                Path.Combine(rhetosAppEnvironment.BinFolder, "ServerDom.pdb")
            };

            foreach (var path in deleteObsoleteFiles)
                if (File.Exists(path))
                {
                    _logger.Info($"Deleting obsolete file '{path}'.");
                    _filesUtility.SafeDeleteFile(path);
                }
        }

        private void RestartWebServer()
        {
            var configFile = Path.Combine(_rhetosAppEnvironment.RootPath, "Web.config");
            if (FilesUtility.SafeTouch(configFile))
                _logger.Trace($"Updated {Path.GetFileName(configFile)} modification date to restart server.");
            else
                _logger.Trace($"Missing {configFile}.");
        }

        protected Assembly SearchForAssembly(object sender, ResolveEventArgs args)
        {
            var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.GetName().Name == new AssemblyName(args.Name).Name);

            if (loadedAssembly != null)
                return loadedAssembly;

            foreach (var folder in new[] { Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), _rhetosAppEnvironment.BinFolder, _rhetosAppEnvironment.PluginsFolder, _rhetosAppEnvironment.GeneratedFolder })
            {
                string pluginAssemblyPath = Path.Combine(folder, new AssemblyName(args.Name).Name + ".dll");
                if (File.Exists(pluginAssemblyPath))
                    return Assembly.LoadFrom(pluginAssemblyPath);
            }
            return null;
        }
    }
}
