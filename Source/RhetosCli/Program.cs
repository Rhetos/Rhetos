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

using Autofac.Core;
using Rhetos.Dsl;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;

namespace Rhetos
{
    public class Program
    {
        static readonly string ExecuteCommandInCurrentProcessOptionName = "--execute-command-in-current-process";

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
            var rootCommand = new RootCommand();
            var buildCommand = new Command("build", "Generates C# code, database model file and other project assets.");
            // CurrentDirectory by default, because rhetos.exe on *build* is expected to be located in NuGet package cache.
            buildCommand.Add(new Argument<DirectoryInfo>("project-root-folder", () => new DirectoryInfo(Environment.CurrentDirectory)) { Description = "Project folder where csproj file is located. If not specified, current working directory is used by default." });
            buildCommand.Add(new Option<bool>("--msbuild-format", false, "Adjust error output format for MSBuild integration."));
            buildCommand.Handler = CommandHandler.Create((DirectoryInfo projectRootFolder, bool msbuildFormat)
                => ReportError(() => Build(projectRootFolder.FullName), "Build", msbuildFormat));
            rootCommand.AddCommand(buildCommand);

            var dbUpdateCommand = new Command("dbupdate", "Updates the database structure and initializes the application data in the database.");
            dbUpdateCommand.Add(new Argument<FileInfo>("startup-assembly") { Description = "Startup assembly of the host application." });
            dbUpdateCommand.Add(new Option<bool>("--short-transactions", "Commit transaction after creating or dropping each database object."));
            dbUpdateCommand.Add(new Option<bool>("--skip-recompute", "Skip automatic update of computed data with KeepSynchronized. See output log for data that needs updating."));
            //Lack of this switch means that the dbupdate command should start the command rhetos.exe dbupdate
            //in another process with the host applications runtimeconfig.json and deps.json files
            var executeCommandInCurrentProcessOption = new Option<bool>(ExecuteCommandInCurrentProcessOptionName);
            executeCommandInCurrentProcessOption.IsHidden = true;
            dbUpdateCommand.Add(executeCommandInCurrentProcessOption);
            dbUpdateCommand.Handler =
                CommandHandler.Create((FileInfo startupAssembly, bool shortTransactions, bool skipRecompute, bool executeCommandInCurrentProcess) => {
                    if(executeCommandInCurrentProcess)
                        ReportError(() => DbUpdate(startupAssembly.FullName, shortTransactions, skipRecompute), "DbUpdate", msBuildErrorFormat: false);
                    else
                        InvokeDbUpdateAsExternalProcess(startupAssembly.FullName, args);
                });
            rootCommand.AddCommand(dbUpdateCommand);

            return rootCommand.Invoke(args);
        }

        private int ReportError(Action action, string commandName, bool msBuildErrorFormat)
        {
            try
            {
                Logger.Info(() => "Logging configured.");
                action.Invoke();
                Logger.Info($"{commandName} done.");
            }
            catch (DslSyntaxException dslException) when (msBuildErrorFormat)
            {
                PrintCanonicalError(dslException);

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
                    PrintErrorSummary(e);

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

            var configuration = new ConfigurationBuilder(LogProvider)
                .AddOptions(rhetosProjectContent.RhetosBuildEnvironment)
                .AddOptions(rhetosProjectContent.RhetosTargetEnvironment)
                .AddKeyValue(ConfigurationProvider.GetKey((ConfigurationProviderOptions o) => o.LegacyKeysWarning), true)
                .AddKeyValue(ConfigurationProvider.GetKey((LoggingOptions o) => o.DelayedLogTimout), 60.0)
                .AddConfigurationManagerConfiguration()
                .AddJsonFile(Path.Combine(projectRootPath, RhetosBuildEnvironment.ConfigurationFileName), optional: true)
                .Build();

            var projectAssets = rhetosProjectContent.RhetosProjectAssets;
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolver.GetResolveEventHandler(projectAssets.Assemblies, LogProvider, true);

            var build = new ApplicationBuild(configuration, LogProvider, projectAssets.Assemblies, projectAssets.InstalledPackages);
            build.ReportLegacyPluginsFolders();
            build.GenerateApplication();
        }

        private void DbUpdate(string startupAssembly, bool shortTransactions, bool skipRecompute)
        {
            IRhetosHostBuilder CreateHostBuilder()
            {
                var builder = RhetosHost.FindBuilder(startupAssembly);
                builder.ConfigureConfiguration(configurationBuilder =>
                {
                    // Default settings for dbupdate command:
                    configurationBuilder.AddKeyValue(ConfigurationProvider.GetKey((DatabaseOptions o) => o.SqlCommandTimeout), 0);
                    configurationBuilder.AddKeyValue(ConfigurationProvider.GetKey((ConfigurationProviderOptions o) => o.LegacyKeysWarning), true);
                    configurationBuilder.AddKeyValue(ConfigurationProvider.GetKey((LoggingOptions o) => o.DelayedLogTimout), 60.0);
                    // Standard configuration files can override the default settings:
                    configurationBuilder.AddConfigurationManagerConfiguration();
                    configurationBuilder.AddJsonFile(DbUpdateOptions.ConfigurationFileName, optional: true);
                    // CLI switches can override the settings from configuration files:
                    if (shortTransactions)
                        configurationBuilder.AddKeyValue(ConfigurationProvider.GetKey((DbUpdateOptions o) => o.ShortTransactions), shortTransactions);
                    if (skipRecompute)
                        configurationBuilder.AddKeyValue(ConfigurationProvider.GetKey((DbUpdateOptions o) => o.SkipRecompute), skipRecompute);
                });
                return builder;
            }

            var deployment = new ApplicationDeployment(CreateHostBuilder, LogProvider);
            deployment.UpdateDatabase();
            deployment.InitializeGeneratedApplication();
        }

        public static void PrintErrorSummary(Exception ex)
        {
            while ((ex is DependencyResolutionException || ex is AggregateException)
                && ex.InnerException != null)
                ex = ex.InnerException;

            Console.WriteLine();
            Console.WriteLine("=============== ERROR SUMMARY ===============");
            Console.WriteLine($"{ex.GetType().Name}: {ExceptionsUtility.MessageForLog(ex)}");
            Console.WriteLine("=============================================");
            Console.WriteLine();
            Console.WriteLine("See RhetosCli.log for more information on error. Enable TraceLog in nlog.config for even more details.");
            Console.WriteLine($"Rhetos CLI location: {System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName}");
        }

        public static void PrintCanonicalError(DslSyntaxException dslException)
        {
            string origin = dslException.FilePosition?.CanonicalOrigin ?? "Rhetos DSL";
            string canonicalError = $"{origin}: error {dslException.ErrorCode ?? "RH0000"}: {dslException.Message.Replace('\r', ' ').Replace('\n', ' ')}";

            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(canonicalError);
            Console.ForegroundColor = oldColor;
        }

        private int InvokeDbUpdateAsExternalProcess(string rhetosHostDllPath, string[] args)
        {
            var newArgs = new List<string>();
            newArgs.Add("exec");

            var runtimeConfigPath = Path.ChangeExtension(rhetosHostDllPath, "runtimeconfig.json");
            if (!File.Exists(runtimeConfigPath))
            {
                Logger.Error($"Missing {runtimeConfigPath} file required to run the dbupdate command on {rhetosHostDllPath}.");
                return 1;
            }

            newArgs.Add("--runtimeconfig");
            newArgs.Add(runtimeConfigPath);

            var depsFile = Path.ChangeExtension(rhetosHostDllPath, "deps.json");
            if (File.Exists(depsFile))
            {
                newArgs.Add("--depsfile");
                newArgs.Add(depsFile);
            }
            else
            {
                Logger.Warning($"The file {depsFile} was not found. This can cause a 'DllNotFoundException' during the program execution.");
            }

            newArgs.Add(GetType().Assembly.Location);
            newArgs.AddRange(args);
            newArgs.Add(ExecuteCommandInCurrentProcessOptionName);
            return Exe.Run("dotnet", newArgs);
        }
    }
}
