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

using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

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
                var rhetosAppOptions = configurationProvider.GetOptions<RhetosAppOptions>();
                var deployment = new ApplicationDeployment(configurationProvider, logProvider, LegacyUtilities.GetListAssembliesDelegate());

                if (string.Compare(command, "restore", true) == 0)
                {
                    AppDomain.CurrentDomain.AssemblyResolve += GetSearchForAssemblyDelegate(
                        rhetosAppOptions.BinFolder);
                    deployment.DownloadPackages(false);
                }
                else if (string.Compare(command, "build", true) == 0)
                {
                    AppDomain.CurrentDomain.AssemblyResolve += GetSearchForAssemblyDelegate(
                        Paths.BinFolder,
                        Paths.PluginsFolder);
                    deployment.GenerateApplication();
                }
                else if (string.Compare(command, "dbupdate", true) == 0)
                {
                    AppDomain.CurrentDomain.AssemblyResolve += GetSearchForAssemblyDelegate(
                        rhetosAppOptions.BinFolder,
                        Paths.PluginsFolder,
                        Paths.GeneratedFolder);
                    deployment.UpdateDatabase();
                }
                else if (string.Compare(command, "appinitialize", true) == 0)
                {
                    AppDomain.CurrentDomain.AssemblyResolve += GetSearchForAssemblyDelegate(
                        rhetosAppOptions.BinFolder,
                        Paths.PluginsFolder,
                        Paths.GeneratedFolder);
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
            return new ResolveEventHandler((object sender, ResolveEventArgs args) =>
            {
                // TODO: Review if loadedAssembly is needed.
                var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.GetName().Name == new AssemblyName(args.Name).Name);
                if (loadedAssembly != null)
                    return loadedAssembly;

                foreach (var folder in folders)
                {
                    string pluginAssemblyPath = Path.Combine(folder, new AssemblyName(args.Name).Name + ".dll");
                    if (File.Exists(pluginAssemblyPath))
                        return Assembly.LoadFrom(pluginAssemblyPath);
                }
                return null;
            });
        }
    }
}
