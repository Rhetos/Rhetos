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
            try
            {
                return new Program().Run(args);
            }
            finally
            {
                NLogProvider.FlushAndShutdown();
            }
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
            var buildCommand = new Command("build", "Generates C# code, database model file and other project assets.");
            // CurrentDirectory by default, because rhetos.exe on *build* is expected to be located in NuGet package cache.
            buildCommand.Add(new Argument<DirectoryInfo>("project-root-folder", () => new DirectoryInfo(Environment.CurrentDirectory)) { Description = "Project folder where csproj file is located. If not specified, current working directory is used by default." });
            buildCommand.Add(new Option<bool>("--msbuild-format", false, "Adjust error output format for MSBuild integration."));
            buildCommand.Handler = CommandHandler.Create((DirectoryInfo projectRootFolder, bool msbuildFormat)
                => ReportError(() => Build(projectRootFolder.FullName), msbuildFormat));
            rootCommand.AddCommand(buildCommand);

            var dbUpdateCommand = new Command("dbupdate", "Updates the database structure and initializes the application data in the database.");
            dbUpdateCommand.Add(new Argument<DirectoryInfo>("application-folder", () => new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory)) { Description = "If not specified, it will search for the application at rhetos.exe location and parent directories." });
            dbUpdateCommand.Add(new Option<bool>("--short-transactions", "Commit transaction after creating or dropping each database object."));
            dbUpdateCommand.Add(new Option<bool>("--skip-recompute", "Skip automatic update of computed data with KeepSynchronized. See output log for data that needs updating."));
            dbUpdateCommand.Handler = CommandHandler.Create((DirectoryInfo applicationFolder, bool shortTransactions, bool skipRecompute)
                => ReportError(() => DbUpdate(applicationFolder, shortTransactions, skipRecompute)));
            rootCommand.AddCommand(dbUpdateCommand);

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
                DeploymentUtility.PrintCanonicalError(dslException);

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
                    DeploymentUtility.PrintErrorSummary(e);

                return 1;
            }

            return 0;
        }

        private void Build(string projectRootPath)
        {
            var rhetosProjectContent = new RhetosProjectContentProvider(projectRootPath, LogProvider).Load();

            if (FilesUtility.IsInsideDirectory(AppDomain.CurrentDomain.BaseDirectory, Path.Combine(projectRootPath, "bin")))
                throw new FrameworkException($"Rhetos build command cannot be run from the generated application folder." +
                    $" Visual Studio integration runs it automatically from Rhetos NuGet package tools folder." +
                    $" You can run it manually from Package Manager Console, since the tools folder it is included in PATH.");

            var configuration = new ConfigurationBuilder()
                .AddOptions(rhetosProjectContent.RhetosBuildEnvironment)
                .AddOptions(rhetosProjectContent.RhetosTargetEnvironment)
                .AddOptions(new LegacyPathsOptions
                {
                    BinFolder = null, // // Rhetos CLI does not use bin folder at build-time. Rhetos framework libraries are provided by NuGet.
                    PluginsFolder = null, // Rhetos CLI does not manage plugin libraries directly, they are provided by NuGet.
                    ResourcesFolder = Path.Combine(projectRootPath, "Resources"), // Currently supporting old plugins by default.
                })
                .AddConfigurationManagerConfiguration()
                .AddJsonFile(Path.Combine(projectRootPath, RhetosBuildEnvironment.ConfigurationFileName), optional: true)
                .Build();

            AppDomain.CurrentDomain.AssemblyResolve += GetSearchForAssemblyDelegate(rhetosProjectContent.RhetosProjectAssets.Assemblies.ToArray());

            var build = new ApplicationBuild(configuration, LogProvider, () => rhetosProjectContent.RhetosProjectAssets.Assemblies);
            build.ReportLegacyPluginsFolders(rhetosProjectContent.RhetosProjectAssets.InstalledPackages);
            build.GenerateApplication(rhetosProjectContent.RhetosProjectAssets.InstalledPackages);
        }

        private void DbUpdate(DirectoryInfo applicationFolder, bool shortTransactions, bool skipRecompute)
        {
            var host = Host.Find(applicationFolder.FullName, LogProvider);

            var configuration = host.RhetosRuntime.BuildConfiguration(LogProvider, host.ConfigurationFolder, configurationBuilder =>
            {
                configurationBuilder.AddConfigurationManagerConfiguration();
                configurationBuilder.AddKeyValue($"{OptionsAttribute.GetConfigurationPath<DatabaseOptions>()}:{nameof(DatabaseOptions.SqlCommandTimeout)}", 0);
                configurationBuilder.AddJsonFile(Path.Combine(host.ConfigurationFolder, DbUpdateOptions.ConfigurationFileName), optional: true);
                if (shortTransactions)
                    configurationBuilder.AddKeyValue($"{OptionsAttribute.GetConfigurationPath<DbUpdateOptions>()}:{nameof(DbUpdateOptions.ShortTransactions)}", shortTransactions);
                if (skipRecompute)
                    configurationBuilder.AddKeyValue($"{OptionsAttribute.GetConfigurationPath<DbUpdateOptions>()}:{nameof(DbUpdateOptions.SkipRecompute)}", skipRecompute);
            });

            var assemblyFiles = LegacyUtilities.GetRuntimeAssembliesDelegate(configuration).Invoke(); // Using same assembly locations as the generated application runtime.
            AppDomain.CurrentDomain.AssemblyResolve += GetSearchForAssemblyDelegate(assemblyFiles);

            var deployment = new ApplicationDeployment(configuration, LogProvider, () => assemblyFiles);
            deployment.UpdateDatabase();
            deployment.InitializeGeneratedApplication(host.RhetosRuntime);
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
