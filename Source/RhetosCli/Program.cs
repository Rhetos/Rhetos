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
                    { "build", rhetosAppRootPath => Build(rhetosAppRootPath, logProvider) },
                    { "dbupdate", rhetosAppRootPath => DbUpdate(rhetosAppRootPath, logProvider) },
                    { "appinitialize", rhetosAppRootPath => AppInitialize(rhetosAppRootPath, logProvider) },
                };

                string usageReport = "Usage: rhetos.exe <command> <rhetos app folder>\r\n\r\nCommands: "
                    + string.Join(", ", commands.Keys);

                if (args.Length != 2)
                {
                    logger.Error($"Invalid command-line arguments.\r\n\r\n{usageReport}");
                    return 1;
                }
                else if (!commands.ContainsKey(args[0]))
                {
                    logger.Error($"Invalid command '{args[0]}'.\r\n\r\n{usageReport}");
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
                .AddKeyValue(nameof(AssetsOptions.AssetsFolder), Path.Combine(rhetosAppRootPath, @"bin"))
                .AddKeyValue(nameof(RhetosAppOptions.BinFolder), Path.Combine(rhetosAppRootPath, @"bin"))
                .AddKeyValue(nameof(BuildOptions.CacheFolder), Path.Combine(rhetosAppRootPath, "obj\\Rhetos"))
                .AddKeyValue(nameof(BuildOptions.GeneratedSourceFolder), Path.Combine(rhetosAppRootPath, "RhetosSource"))
                .AddConfigurationManagerConfiguration()
                .Build();

            var lockFile = NugetUtilities.FindLockFile(rhetosAppRootPath);
            var targetFramework = NugetUtilities.ResolveTargetFramework(lockFile, null);
            var buildAssemblies = NugetUtilities.GetBuildAssemblies(lockFile, targetFramework);
            var deployment = new ApplicationDeployment(configurationProvider, logProvider, () => buildAssemblies);
            AppDomain.CurrentDomain.AssemblyResolve += GetSearchForAssemblyDelegate(buildAssemblies.ToArray());
            deployment.GenerateApplication(NugetUtilities.GetInstalledPackages(lockFile, targetFramework));
        }

        private static void DbUpdate(string binFolder, NLogProvider logProvider)
        {
            var configurationProvider = new ConfigurationBuilder()
                .AddKeyValue(nameof(AssetsOptions.AssetsFolder), binFolder)
                .AddKeyValue(nameof(RhetosAppOptions.BinFolder), binFolder)
                .AddWebConfiguration(binFolder)
                .AddConfigurationManagerConfiguration()
                .Build();
            var assemblyList = Directory.GetFiles(binFolder, "*.dll");
            var deployment = new ApplicationDeployment(configurationProvider, logProvider, () => assemblyList);
            AppDomain.CurrentDomain.AssemblyResolve += GetSearchForAssemblyDelegate(assemblyList);
            deployment.UpdateDatabase();
        }

        private static void AppInitialize(string binFolder, NLogProvider logProvider)
        {
            var configurationProvider = new ConfigurationBuilder()
                .AddKeyValue(nameof(AssetsOptions.AssetsFolder), binFolder)
                .AddKeyValue(nameof(RhetosAppOptions.BinFolder), binFolder)
                .AddWebConfiguration(binFolder)
                .AddConfigurationManagerConfiguration()
                .Build();
            var assemblyList = Directory.GetFiles(binFolder, "*.dll");
            var deployment = new ApplicationDeployment(configurationProvider, logProvider, () => assemblyList);
            AppDomain.CurrentDomain.AssemblyResolve += GetSearchForAssemblyDelegate(assemblyList);
            deployment.InitializeGeneratedApplication();
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
