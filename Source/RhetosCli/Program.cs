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

using Rhetos.Dsl;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Rhetos
{
    public class Program
    {
        public static int Main(string[] args)
        {
            return new Program().Run(args);
        }

        private ILogProvider LogProvider { get; }
        private ILogger Logger { get; }

        public Program()
        {
            LogProvider = new NLogProvider();
            Logger = LogProvider.GetLogger("Rhetos");
        }

        public int Run(string[] args)
        {
            Logger.Info(() => "Logging configured.");

            var rootCommand = new RootCommand();
            var buildCommand = new Command("build", "Generates the Rhetos application inside the <project-root-folder>. If <project-root-folder> is not set it will use the current working directory.");
            // CurrentDirectory by default, because rhetos.exe on *build* is expected to be located in NuGet package cache.
            buildCommand.Add(new Argument<DirectoryInfo>("project-root-folder", () => new DirectoryInfo(Environment.CurrentDirectory)));
            buildCommand.Add(new Option<bool>("--msbuild-format", false, "Adjust error output format for MSBuild integration."));
            buildCommand.Handler = CommandHandler.Create((DirectoryInfo projectRootFolder, bool msbuildFormat) => ReportError(() => Build(projectRootFolder.FullName), msbuildFormat));
            rootCommand.AddCommand(buildCommand);

            var dbUpdateCommand = new Command("dbupdate", "Updates the database, based on the generated files from the build process.");
            dbUpdateCommand.Add(new Argument<DirectoryInfo>("application-folder", () => new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory)) { Description = "If not specified, it will search for the application at rhetos.exe location and parent directories." });
            dbUpdateCommand.Handler = CommandHandler.Create((DirectoryInfo applicationFolder) => ReportError(() => DbUpdate(applicationFolder)));
            rootCommand.AddCommand(dbUpdateCommand);

            var appInitializeCommand = new Command("appinitialize", "Initializes the application.");
            appInitializeCommand.Add(new Argument<DirectoryInfo>("application-folder", () => new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory)) { Description = "If not specified, it will search for the application at rhetos.exe location and parent directories." });
            appInitializeCommand.Handler = CommandHandler.Create((DirectoryInfo applicationFolder) => ReportError(() => AppInitialize(applicationFolder)));
            rootCommand.AddCommand(appInitializeCommand);

            return rootCommand.Invoke(args);
        }

        private int ReportError(Action action, bool msBuildErrorFormat = false)
        {
            try
            {
                action.Invoke();
                Logger.Info("Done.");
            }
            catch (DslSyntaxException dslException) when (msBuildErrorFormat)
            {
                ApplicationDeployment.PrintCanonicalError(dslException);

                // Detailed exception info is logged as additional information, not as an error, to avoid duplicate error reporting.
                Logger.Info(dslException.ToString());

                return 1;
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());

                string typeLoadReport = CsUtility.ReportTypeLoadException(e);
                if (typeLoadReport != null)
                    Logger.Error(typeLoadReport);

                if (Environment.UserInteractive)
                    ApplicationDeployment.PrintErrorSummary(e);

                return 1;
            }

            return 0;
        }

        private void Build(string projectRootPath)
        {
            var buildEnvironmentAndAssets = new RhetosProjectAssetsFileProvider(projectRootPath, LogProvider).Load();

            string binFolder = Path.Combine(projectRootPath, "bin");
            if (FilesUtility.IsSameDirectory(AppDomain.CurrentDomain.BaseDirectory, binFolder))
                throw new FrameworkException($"Rhetos build command cannot be run from the generated application folder." +
                    $" Visual Studio integration runs it automatically from Rhetos NuGet package tools folder." +
                    $" You can run it manually from Package Manager Console, it is included in PATH.");

            var configurationProvider = new ConfigurationBuilder()
                .AddOptions(buildEnvironmentAndAssets.RhetosBuildEnvironment)
                .AddConfigurationManagerConfiguration()
                .AddJsonFile(Path.Combine(projectRootPath, "rhetos-build.settings.json"), optional: true)
                .Build();

            AppDomain.CurrentDomain.AssemblyResolve += GetSearchForAssemblyDelegate(buildEnvironmentAndAssets.RhetosProjectAssets.Assemblies.ToArray());
            var deployment = new ApplicationDeployment(configurationProvider, LogProvider, () => buildEnvironmentAndAssets.RhetosProjectAssets.Assemblies);
            deployment.GenerateApplication(buildEnvironmentAndAssets.RhetosProjectAssets.InstalledPackages);
        }

        private void DbUpdate(DirectoryInfo applicationFolder)
        {
            var host = Host.Find(applicationFolder.FullName, LogProvider);
            var deployment = SetupRuntime(host);
            deployment.UpdateDatabase();
        }

        private void AppInitialize(DirectoryInfo applicationFolder)
        {
            var host = Host.Find(applicationFolder.FullName, LogProvider);
            var deployment = SetupRuntime(host);
            deployment.InitializeGeneratedApplication(host.RhetosRuntime);
        }

        private ApplicationDeployment SetupRuntime(Host host)
        {
            var configurationProvider = host.RhetosRuntime.BuildConfiguration(LogProvider, host.ConfigurationFolder, configurationBuilder => {
                configurationBuilder.AddKeyValue(nameof(DatabaseOptions.SqlCommandTimeout), 0);
            });

            var assemblyFiles = LegacyUtilities.GetListAssembliesDelegate(configurationProvider).Invoke(); // Using same assembly locations as the generated application runtime.
            AppDomain.CurrentDomain.AssemblyResolve += GetSearchForAssemblyDelegate(assemblyFiles);

            return new ApplicationDeployment(configurationProvider, LogProvider, () => assemblyFiles);
        }

        private ResolveEventHandler GetSearchForAssemblyDelegate(params string[] assemblyList)
        {
            return new ResolveEventHandler((object sender, ResolveEventArgs args) =>
            {
                // TODO: Review if loadedAssembly is needed.
                var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.GetName().Name == new AssemblyName(args.Name).Name);
                if (loadedAssembly != null)
                    return loadedAssembly;

                foreach (var assembly in assemblyList.Where(x => Path.GetFileNameWithoutExtension(x) == new AssemblyName(args.Name).Name))
                {
                    if (File.Exists(assembly))
                        return Assembly.LoadFrom(assembly);
                }
                return null;
            });
        }
    }
}
