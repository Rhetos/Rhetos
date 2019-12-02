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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Rhetos.Logging;
using Rhetos.Utilities;

namespace Rhetos
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            var logProvider = new NLogProvider();
            var logger = logProvider.GetLogger("DeployPackages");
            logger.Trace(() => "Logging configured.");

            try
            {
                var command = args[0];
                var rhetosServerRootFolder = args[1];

                var configurationProvider = BuildConfigurationProvider(args);
                var deployOptions = configurationProvider.GetOptions<DeployOptions>();
                var deployment = new ApplicationDeployment(configurationProvider, logProvider);
                var rhetosAppEnvironment = new RhetosAppEnvironment(rhetosServerRootFolder);

                if (string.Compare(command, "restore", true) == 0)
                {
                    AppDomain.CurrentDomain.AssemblyResolve += GetSearchForAssemblyDelegate(
                        rhetosAppEnvironment.BinFolder);
                    deployment.DownloadPackages(deployOptions.IgnoreDependencies);
                }
                else if (string.Compare(command, "build", true) == 0)
                {
                    AppDomain.CurrentDomain.AssemblyResolve += GetSearchForAssemblyDelegate(
                        rhetosAppEnvironment.BinFolder,
                        rhetosAppEnvironment.PluginsFolder);
                    deployment.InitialCleanup();
                    deployment.GenerateApplication();
                }
                else if (string.Compare(command, "dbupdate", true) == 0)
                {
                    AppDomain.CurrentDomain.AssemblyResolve += GetSearchForAssemblyDelegate(
                        rhetosAppEnvironment.BinFolder,
                        rhetosAppEnvironment.PluginsFolder,
                        rhetosAppEnvironment.GeneratedFolder);
                    deployment.UpdateDatabase();
                }
                else if (string.Compare(command, "appinitialize", true) == 0)
                {
                    AppDomain.CurrentDomain.AssemblyResolve += GetSearchForAssemblyDelegate(
                        rhetosAppEnvironment.BinFolder,
                        rhetosAppEnvironment.PluginsFolder,
                        rhetosAppEnvironment.GeneratedFolder);
                    deployment.InitializeGeneratedApplication();
                }

                logger.Trace("Done.");
            }
            catch (Exception e)
            {
                logger.Error(e.ToString());

                string typeLoadReport = CsUtility.ReportTypeLoadException(e);
                if (typeLoadReport != null)
                    logger.Error(typeLoadReport);

                if (Environment.UserInteractive)
                    ApplicationDeployment.PrintErrorSummary(e);

                return 1;
            }

            return 0;
        }

        private static IConfigurationProvider BuildConfigurationProvider(string[] args)
        {
            return new ConfigurationBuilder()
                .AddRhetosAppConfiguration(args[1])
                .AddConfigurationManagerConfiguration()
                .Build();
        }

        private static ResolveEventHandler GetSearchForAssemblyDelegate(params string[] folders)
        {
            var searchFolders = new List<string> { Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) }.Union(folders);
            return new ResolveEventHandler((object sender, ResolveEventArgs args) =>
            {
                foreach (var folder in searchFolders)
                {
                    string pluginAssemblyPath = Path.Combine(folder, new AssemblyName(args.Name).Name + ".dll");
                    if (File.Exists(pluginAssemblyPath))
                    {
                        return Assembly.LoadFrom(pluginAssemblyPath);
                    }
                }
                return null;
            });
        }
    }
}
