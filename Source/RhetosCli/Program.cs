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
using System.Collections.Generic;
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
                var commands = new Dictionary<string, Action<string>>(StringComparer.OrdinalIgnoreCase)
                {
                    { "build", folder => Build(folder, logProvider) },
                    { "dbupdate", folder => DbUpdate(folder, logProvider) },
                    { "appinitialize", folder => AppInitialize(folder, logProvider) },
                };

                string usageReport = "Usage:\r\n"
                    + "rhetos.exe build <project folder>\r\n"
                    + "rhetos.exe dbupdate <project folder>\r\n"
                    + "rhetos.exe appinitialize <project folder>\r\n";

                if (args.Length != 2)
                {
                    logger.Error("Invalid command-line arguments.");
                    Console.WriteLine("\r\n" + usageReport);
                    return 1;
                }
                else if (!commands.ContainsKey(args[0]))
                {
                    logger.Error($"Invalid command '{args[0]}'.");
                    Console.WriteLine("\r\n" + usageReport);
                    return 1;
                }
                else
                {
                    string rhetosAppRootPath = args[1];
                    commands[args[0]].Invoke(rhetosAppRootPath);
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

        private static void Build(string rhetosAppRootPath, NLogProvider logProvider)
        {
            var configurationProvider = new ConfigurationBuilder()
                .AddRhetosAppEnvironment(new RhetosAppEnvironment
                {
                    RootPath = rhetosAppRootPath,
                    BinFolder = Path.Combine(rhetosAppRootPath, "bin"),
                    AssetsFolder = Path.Combine(rhetosAppRootPath, "bin"),
                    LegacyPluginsFolder = Path.Combine(rhetosAppRootPath, "bin", "Plugins"),
                    LegacyAssetsFolder = Path.Combine(rhetosAppRootPath, "Resources"),
                })
                .AddKeyValue(nameof(BuildOptions.CacheFolder), Path.Combine(rhetosAppRootPath, "obj\\Rhetos"))
                .AddKeyValue(nameof(BuildOptions.GeneratedSourceFolder), Path.Combine(rhetosAppRootPath, "RhetosSource"))
                .AddConfigurationManagerConfiguration()
                .Build();

            var nuget = new NuGetUtilities(rhetosAppRootPath, logProvider, null);
            var buildAssemblies = nuget.GetBuildAssemblies();
            AppDomain.CurrentDomain.AssemblyResolve += GetSearchForAssemblyDelegate(buildAssemblies.ToArray());

            var deployment = new ApplicationDeployment(configurationProvider, logProvider, () => buildAssemblies);
            deployment.GenerateApplication(nuget.GetInstalledPackages());
        }

        private static void DbUpdate(string rhetosAppRootPath, NLogProvider logProvider)
        {
            var deployment = SetupRuntime(rhetosAppRootPath, logProvider);
            deployment.UpdateDatabase();
        }

        private static void AppInitialize(string rhetosAppRootPath, NLogProvider logProvider)
        {
            var deployment = SetupRuntime(rhetosAppRootPath, logProvider);
            deployment.InitializeGeneratedApplication();
        }

        private static ApplicationDeployment SetupRuntime(string rhetosAppRootPath, NLogProvider logProvider)
        {
            var configurationProvider = new ConfigurationBuilder()
                .AddRhetosAppConfiguration(rhetosAppRootPath)
                .AddConfigurationManagerConfiguration()
                .Build();

            string binFolder = configurationProvider.GetOptions<RhetosAppEnvironment>().BinFolder;
            var assemblyList = Directory.GetFiles(binFolder, "*.dll");
            AppDomain.CurrentDomain.AssemblyResolve += GetSearchForAssemblyDelegate(assemblyList);

            return new ApplicationDeployment(configurationProvider, logProvider, () => assemblyList);
        }

        private static ResolveEventHandler GetSearchForAssemblyDelegate(params string[] assemblyList)
        {
            return new ResolveEventHandler((object sender, ResolveEventArgs args) =>
            {
                // TODO: Review if loadedAssembly is needed.
                var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.GetName().Name == new AssemblyName(args.Name).Name);
                if (loadedAssembly != null)
                    return loadedAssembly;

                foreach (var assembly in assemblyList.Where(x => Path.GetFileNameWithoutExtension(x) ==  new AssemblyName(args.Name).Name))
                {
                    if (File.Exists(assembly))
                        return Assembly.LoadFrom(assembly);
                }
                return null;
            });
        }
    }
}
