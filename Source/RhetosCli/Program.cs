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
using System.CommandLine;
using System.CommandLine.Invocation;

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
                var rootCommand = new RootCommand();
                var buildCommand = new Command("build", "Generates the Rhetos application inside the <project-root-folder>. If <project-root-folder> is not set it will use the current working directory.");
                buildCommand.Add(new Argument<DirectoryInfo>("project-root-folder", () => new DirectoryInfo(Environment.CurrentDirectory)));
                buildCommand.Add(new Option<string[]>("--assemblies", "List of assemblies outside of refrenced nuget packages that will be used during the build."));
                buildCommand.Handler = CommandHandler.Create((DirectoryInfo projectRootFolder, string[] assemblies) => Build(projectRootFolder.FullName, assemblies, logProvider));
                rootCommand.AddCommand(buildCommand);

                var dbUpdateCommand = new Command("dbupdate", "Updates the database based on the generated files from the build process. If <application-root-folder> is not set it will use the current working directory.");
                dbUpdateCommand.Add(new Argument<DirectoryInfo>("application-root-folder", () => new DirectoryInfo(Environment.CurrentDirectory)));
                dbUpdateCommand.Handler = CommandHandler.Create((DirectoryInfo applicationRootFolder) => DbUpdate(applicationRootFolder.FullName, logProvider));
                rootCommand.AddCommand(dbUpdateCommand);

                var appInitializeCommand = new Command("appinitialize", "Initializes the application. If <application-root-folder> is not set it will use the current working directory.");
                appInitializeCommand.Add(new Argument<DirectoryInfo>("application-root-folder", () => new DirectoryInfo(Environment.CurrentDirectory)));
                appInitializeCommand.Handler = CommandHandler.Create((DirectoryInfo applicationRootFolder) => AppInitialize(applicationRootFolder.FullName, logProvider));
                rootCommand.AddCommand(appInitializeCommand);

                rootCommand.Invoke(args);

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

        private static void Build(string rhetosAppRootPath, string[] assemblies, NLogProvider logProvider)
        {
            var configurationProvider = new ConfigurationBuilder()
                .AddRhetosAppEnvironment(new RhetosAppEnvironment
                {
                    RootFolder = rhetosAppRootPath,
                    BinFolder = Path.Combine(rhetosAppRootPath, "bin"),
                    AssetsFolder = Path.Combine(rhetosAppRootPath, "bin"), // TODO: Move assets to a separate folder ("RhetosAssets"), after removing DLL compiling from AssemblyGenerator. Current solution might have issues with AssetsFolder cleanup by Rhetos that might unintentionally remove other files from bin folder.
                    LegacyPluginsFolder = Path.Combine(rhetosAppRootPath, "bin"),
                    LegacyAssetsFolder = Path.Combine(rhetosAppRootPath, "Resources"),
                })
                .AddKeyValue(nameof(BuildOptions.ProjectFolder), rhetosAppRootPath)
                .AddKeyValue(nameof(BuildOptions.CacheFolder), Path.Combine(rhetosAppRootPath, "obj\\Rhetos"))
                .AddKeyValue(nameof(BuildOptions.GeneratedSourceFolder), Path.Combine(rhetosAppRootPath, "RhetosSource"))
                .AddConfigurationManagerConfiguration()
                .Build();

            var nuget = new NuGetUtilities(rhetosAppRootPath, logProvider, null);
            var packagesBuildAssemblies = nuget.GetBuildAssemblies();
            
            var allAssemblies = packagesBuildAssemblies.Select(x => Path.GetFullPath(x)).Union(assemblies.Select(x => Path.GetFullPath(x))).Distinct();

            var multipleAssembliesWithsameName = allAssemblies.GroupBy(x => Path.GetFileName(x)).Where(x => x.Count() > 1);
            if (multipleAssembliesWithsameName.Any())
            {
                logProvider.GetLogger("DeployPackages").Info("Detected multiple assemblies with the same name:" + Environment.NewLine + 
                    string.Join(Environment.NewLine ,multipleAssembliesWithsameName.Select(x => $"{x.Key} on locations {string.Join(", ", x)}")));
            }

            AppDomain.CurrentDomain.AssemblyResolve += GetSearchForAssemblyDelegate(allAssemblies.ToArray());

            var deployment = new ApplicationDeployment(configurationProvider, logProvider, () => allAssemblies);
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
