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
using System.Linq;

namespace Rhetos
{
    public class Program
    {
        const string ExecuteCommandInCurrentProcessOptionName = "--execute-command-in-current-process";

        public static int Main(string[] args)
        {
            try
            {
                return Run(args);
            }
            finally
            {
                NLogProvider.FlushAndShutdown();
            }
        }

        private readonly ILogProvider _logProvider;

        private Program(VerbosityLevel verbosity, string[] traceLoggers)
        {
            // NLog should automatically read the configuration from application-specific configuration file (e.g. 'rhetos.exe.nlog')
            // and if the file does not exist it should try to read it from nlog.config,
            // but there is a bug in NLog which is causing first to try to read the nlog.config and then rhetos.exe.nlog configuration file.
            // As this is a breaking changes the fix will be released in version 5 of NLog so remove this code after upgrading to NLog 5.
            NLog.LogManager.LogFactory.SetCandidateConfigFilePaths(new[] { LoggingConfigurationPath });

            // "ConsoleLog" target by default logs min-level 'Info'. See the initial rules in 'rhetos.exe.nlog' file.
            // Diagnostic and trace options include additional loggers.
            if (verbosity == VerbosityLevel.Diagnostic)
            {
                NLog.LogManager.LogFactory.Configuration.AddRuleForOneLevel(NLog.LogLevel.Trace, "ConsoleLog");
            }
            else
            {
                if (traceLoggers != null)
                    foreach (var traceLogger in traceLoggers)
                        NLog.LogManager.LogFactory.Configuration.AddRuleForOneLevel(NLog.LogLevel.Trace, "ConsoleLog", traceLogger);
            }

            _logProvider = new NLogProvider();
        }

        private static string LoggingConfigurationPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rhetos.exe.nlog");

        enum VerbosityLevel
        {
            /// <summary>
            /// Console output includes all loggers with "Info" level and higher. See 'rhetos.exe.nlog' file.
            /// </summary>
            Normal,
            /// <summary>
            /// Console output includes all trace loggers.
            /// </summary>
            Diagnostic
        };

        public static int Run(string[] args)
        {
            var rootCommand = new RootCommand();
            rootCommand.Add(new Option<VerbosityLevel>("--verbosity", () => VerbosityLevel.Normal, "Output verbosity level. Allowed values are normal and diagnostic."));
            rootCommand.Add(new Option<string[]>("--trace", () => Array.Empty<string>(), "Output additional trace loggers specified by name."));

            var buildCommand = new Command("build", "Generates C# code, database model file and other project assets.");
            // CurrentDirectory by default, because rhetos.exe on *build* is expected to be located in NuGet package cache.
            buildCommand.Add(new Argument<DirectoryInfo>("project-root-folder", () => new DirectoryInfo(Environment.CurrentDirectory)) { Description = "Project folder where csproj file is located. If not specified, current working directory is used by default." });
            buildCommand.Add(new Option<bool>("--msbuild-format", () => false, "Adjust error output format for MSBuild integration."));
            buildCommand.Handler = CommandHandler.Create((DirectoryInfo projectRootFolder, bool msbuildFormat, VerbosityLevel verbosity, string[] trace) =>
            {
                var program = new Program(verbosity, trace);
                program.SafeExecuteCommand(() => program.Build(projectRootFolder.FullName), "Build", msbuildFormat);
            });
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
                CommandHandler.Create((FileInfo startupAssembly, bool shortTransactions, bool skipRecompute, bool executeCommandInCurrentProcess, VerbosityLevel verbosity, string[] trace) =>
                {
                    var program = new Program(verbosity, trace);
                    if (executeCommandInCurrentProcess)
                        return program.SafeExecuteCommand(() => program.DbUpdate(startupAssembly.FullName, shortTransactions, skipRecompute), "DbUpdate", msBuildErrorFormat: false);
                    else
                        return program.InvokeDbUpdateAsExternalProcess(startupAssembly.FullName, args);
                });
            rootCommand.AddCommand(dbUpdateCommand);

            return rootCommand.Invoke(args);
        }

        private int SafeExecuteCommand(Action action, string commandName, bool msBuildErrorFormat)
        {
            var logger = _logProvider.GetLogger("Rhetos " + commandName);
            logger.Info(() => $"Started in {AppDomain.CurrentDomain.BaseDirectory}");
            try
            {
                action.Invoke();
                logger.Info($"Done.");
            }
            catch (DslSyntaxException dslException) when (msBuildErrorFormat)
            {
                PrintCanonicalError(dslException);

                // Detailed exception info is logged as additional information, not as an error, to avoid duplicate error reporting.
                logger.Info(dslException.ToString());

                return 1;
            }
            catch (Exception e)
            {
                logger.Error(e.ToString());

                string typeLoadReport = CsUtility.ReportTypeLoadException(e);
                if (typeLoadReport != null)
                    logger.Error(typeLoadReport);

                if (Environment.UserInteractive)
                    PrintErrorSummary(e);

                return 1;
            }

            return 0;
        }

        private void Build(string projectRootPath)
        {
            var rhetosProjectContent = new RhetosProjectContentProvider(projectRootPath, _logProvider).Load();

            if (FilesUtility.IsInsideDirectory(AppDomain.CurrentDomain.BaseDirectory, Path.Combine(projectRootPath, "bin")))
                throw new FrameworkException($"Rhetos build command cannot be run from the generated application folder." +
                    $" Visual Studio integration runs it automatically from Rhetos NuGet package tools folder." +
                    $" You can run it manually from Package Manager Console, since the tools folder it is included in PATH.");

            var configuration = new ConfigurationBuilder(_logProvider)
                .AddOptions(rhetosProjectContent.RhetosBuildEnvironment)
                .AddOptions(rhetosProjectContent.RhetosTargetEnvironment)
                .AddKeyValue(ConfigurationProvider.GetKey((ConfigurationProviderOptions o) => o.LegacyKeysWarning), true)
                .AddKeyValue(ConfigurationProvider.GetKey((LoggingOptions o) => o.DelayedLogTimout), 60.0)
                .AddJsonFile(Path.Combine(projectRootPath, RhetosBuildEnvironment.ConfigurationFileName), optional: true)
                .Build();

            var projectAssets = rhetosProjectContent.RhetosProjectAssets;
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolver.GetResolveEventHandler(projectAssets.Assemblies, _logProvider, true);

            var build = new ApplicationBuild(configuration, _logProvider, projectAssets.Assemblies, projectAssets.InstalledPackages);
            build.ReportLegacyPluginsFolders();
            build.GenerateApplication();
        }

        private void DbUpdate(string startupAssembly, bool shortTransactions, bool skipRecompute)
        {
            RhetosHost CreateRhetosHost(Action<IRhetosHostBuilder> configureRhetosHost)
            {
                return RhetosHost.CreateFrom(startupAssembly, builder =>
                {
                    builder.ConfigureConfiguration(configurationBuilder =>
                    {
                        // Default settings for dbupdate command:
                        configurationBuilder.AddKeyValue(ConfigurationProvider.GetKey((DatabaseOptions o) => o.SqlCommandTimeout), 0);
                        configurationBuilder.AddKeyValue(ConfigurationProvider.GetKey((ConfigurationProviderOptions o) => o.LegacyKeysWarning), true);
                        configurationBuilder.AddKeyValue(ConfigurationProvider.GetKey((LoggingOptions o) => o.DelayedLogTimout), 60.0);
                        // Standard configuration files can override the default settings:
                        configurationBuilder.AddJsonFile(DbUpdateOptions.ConfigurationFileName, optional: true);
                        // CLI switches can override the settings from configuration files:
                        if (shortTransactions)
                            configurationBuilder.AddKeyValue(ConfigurationProvider.GetKey((DbUpdateOptions o) => o.ShortTransactions), shortTransactions);
                        if (skipRecompute)
                            configurationBuilder.AddKeyValue(ConfigurationProvider.GetKey((DbUpdateOptions o) => o.SkipRecompute), skipRecompute);
                    });
                    configureRhetosHost.Invoke(builder);
                });
            }

            var deployment = new ApplicationDeployment(CreateRhetosHost, _logProvider);
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
            Console.WriteLine($"See RhetosCli.log for more information on error. Enable TraceLog in '{LoggingConfigurationPath}' for even more details.");
        }

        /// <summary>
        /// Outputs error in canonical message format. This format can be recognized by MSBuild and reported in IDE with link directly to position in source file.
        /// See https://github.com/dotnet/msbuild/blob/master/src/Shared/CanonicalError.cs for more details.
        /// </summary>
        public static void PrintCanonicalError(DslSyntaxException dslException)
        {
            string origin = dslException.FilePosition?.CanonicalOrigin ?? "Rhetos DSL";
            string canonicalError = $"{origin}: error {dslException.ErrorCode ?? "RH0000"}: {dslException.Message.Replace('\r', ' ').Replace('\n', ' ')}";

            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(canonicalError);
            Console.ForegroundColor = oldColor;
        }

        private int InvokeDbUpdateAsExternalProcess(string rhetosHostDllPath, string[] baseArgs)
        {
            var logger = _logProvider.GetLogger("Rhetos DbUpdate base");

            var newArgs = new List<string>();
            newArgs.Add("exec");

            var runtimeConfigPath = Path.ChangeExtension(rhetosHostDllPath, "runtimeconfig.json");
            if (!File.Exists(runtimeConfigPath))
            {
                logger.Error($"Missing {runtimeConfigPath} file required to run the dbupdate command on {rhetosHostDllPath}.");
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
                logger.Warning($"The file {depsFile} was not found. This can cause a 'DllNotFoundException' during the program execution.");
            }

            newArgs.Add(GetType().Assembly.Location);
            newArgs.AddRange(baseArgs);
            newArgs.Add(ExecuteCommandInCurrentProcessOptionName);

            logger.Trace(() => "dotnet args: " + string.Join(", ", newArgs.Select(arg => "\"" + (arg ?? "null") + "\"")));
            return Exe.Run("dotnet", newArgs);
        }
    }
}
