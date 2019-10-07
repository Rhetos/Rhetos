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
using Rhetos.Configuration.Autofac;
using Rhetos.Deployment;
using Rhetos.Dom;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Utilities;
using Rhetos.Utilities.ApplicationConfiguration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DeployPackages
{
    public static class Program
    {
        private readonly static Dictionary<string, string> validArguments =  new Dictionary<string, string>()
        {
            { "/StartPaused", "Use for debugging with Visual Studio (Attach to Process)." },
            { "/Debug", "Generates unoptimized dlls (ServerDom.*.dll, e.g.) for debugging." },
            { "/NoPause", "Don't pause on error. Use this switch for build automation." },
            { "/IgnoreDependencies", "Allow installing incompatible versions of Rhetos packages." },
            { "/ShortTransactions", "Commit transaction after creating or dropping each database object." },
            { "/DatabaseOnly", "Keep old plugins and files in bin\\Generated." },
            { "/SkipRecompute", "Use this if you want to skip all computed data." }
        };

        public static int Main(string[] args)
        {
            ILogger logger = new ConsoleLogger("DeployPackages"); // Using the simplest logger outside of try-catch block.
            string oldCurrentDirectory = null;
            DeployOptions deployOptions = null;

            try
            {
                if (!ValidateArguments(args))
                    return 1;

                deployOptions = new ConfigurationBuilder()
                    .AddCommandLineArguments(args, "/")
                    .Build()
                    .ConfigureOptions<DeployOptions>();

                logger = DeploymentUtility.InitializationLogProvider.GetLogger("DeployPackages"); // Setting the final log provider inside the try-catch block, so that the simple ConsoleLogger can be used (see above) in case of an initialization error.

                if (deployOptions.StartPaused)
                    StartPaused();

                var rhetosAppRootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..");
                var configurationProvider = new ConfigurationBuilder()
                    .AddConfigurationManagerConfiguration()
                    .AddWebConfiguration(rhetosAppRootPath)
                    .SetRhetosAppRootPath(rhetosAppRootPath)
                    .Build();

                Paths.InitializeRhetosServerRootPath(rhetosAppRootPath);

                oldCurrentDirectory = Directory.GetCurrentDirectory();
                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

                var packageManager = new PackageManager(logger, deployOptions);
                packageManager.InitialCleanup();
                packageManager.DownloadPackages();

                var deployManager = new ApplicationDeployment(logger, deployOptions);
                deployManager.GenerateApplication();
                deployManager.InitializeGeneratedApplication();

                logger.Trace("Done.");
            }
            catch (Exception ex)
            {
                logger.Error(ex.ToString());
                if (ex is ReflectionTypeLoadException)
                    logger.Error(CsUtility.ReportTypeLoadException((ReflectionTypeLoadException)ex));

                InteractiveExceptionInfo(ex, !(deployOptions?.NoPause ?? false));

                return 1;
            }
            finally
            {
                if (oldCurrentDirectory != null && Directory.Exists(oldCurrentDirectory))
                    Directory.SetCurrentDirectory(oldCurrentDirectory);
            }

            return 0;
        }

        private static void StartPaused()
        {
            if (!Environment.UserInteractive)
                throw new Rhetos.UserException("DeployPackages parameter 'StartPaused' must not be set, because the application is executed in a non-interactive environment.");

            // Use for debugging (Attach to Process)
            Console.WriteLine("Press any key to continue . . .");
            Console.ReadKey(true);
        }

        private static bool ValidateArguments(string[] args)
        {
            if (args.Contains("/?"))
            {
                ShowHelp();
                return false;
            }

            var invalidArgument = args.FirstOrDefault(a => !validArguments.Keys.Contains(a, StringComparer.InvariantCultureIgnoreCase));
            if (invalidArgument != null)
            {
                ShowHelp();
                throw new ApplicationException($"Unexpected command-line argument: '{invalidArgument}'.");
            }
            return true;
        }

        private static void ShowHelp()
        {
            Console.WriteLine("Command-line arguments:");
            foreach (var argument in validArguments)
                Console.WriteLine($"{argument.Key.PadRight(20)} {argument.Value}");
        }

        private static void InteractiveExceptionInfo(Exception e, bool pauseOnError)
        {
            if (!Environment.UserInteractive) return;

            PrintSummary(e);

            if (pauseOnError)
            {
                Console.WriteLine("Press any key to continue . . .  (use /NoPause switch to avoid pause on error)");
                Console.ReadKey(true);
            }
        }

        private static void PrintSummary(Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("=============== ERROR SUMMARY ===============");
            Console.WriteLine(ex.GetType().Name + ": " + ExceptionsUtility.SafeFormatUserMessage(ex));
            Console.WriteLine("=============================================");
            Console.WriteLine();
            Console.WriteLine("See DeployPackages.log for more information on error. Enable TraceLog in DeployPackages.exe.config for even more details.");
        }
    }
}
