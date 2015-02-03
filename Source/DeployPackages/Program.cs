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
using Rhetos.Deployment;
using Rhetos.Dom;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace DeployPackages
{
    public class Program
    {
        public static int Main(string[] args)
        {
            ILogger _logger = new ConsoleLogger("DeployPackages.Program", new NLogger("DeployPackages.Program"));
            string oldCurrentDirectory = null;
            try
            {
                var arguments = new Arguments(args);
                if (arguments.Help)
                    return 1;

                if (arguments.StartPaused)
                {
                    // Use for debugging (Attach to Process)
                    Console.WriteLine("Press any key to continue . . .");
                    Console.ReadKey(true);
                }

                Paths.InitializeRhetosServerRootPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".."));

                oldCurrentDirectory = Directory.GetCurrentDirectory();
                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

                if (!Directory.Exists(Paths.GeneratedFolder))
                    Directory.CreateDirectory(Paths.GeneratedFolder);
                foreach (var oldGeneratedFile in Directory.GetFiles(Paths.GeneratedFolder, "*", SearchOption.AllDirectories))
                    File.Delete(oldGeneratedFile);
                if (File.Exists(Paths.DomAssemblyFile))
                    File.Delete(Paths.DomAssemblyFile);

                {
                    Console.Write("Loading plugins ... ");
                    var stopwatch = Stopwatch.StartNew();

                    var builder = new ContainerBuilder();
                    builder.RegisterModule(new AutofacModuleConfiguration(deploymentTime: true));
                    using (var container = builder.Build())
                    {
                        Console.WriteLine("Done.");
                        var _performanceLogger = container.Resolve<ILogProvider>().GetLogger("Performance");
                        _performanceLogger.Write(stopwatch, "DeployPackages.Program: Modules and plugins registered.");
                        Plugins.LogRegistrationStatistics("Generating application", container);

                        if (arguments.Debug)
                            container.Resolve<DomGeneratorOptions>().Debug = true;

                        container.Resolve<ApplicationGenerator>().ExecuteGenerators();
                    }
                }

                // Creating a new container builder instead of using builder.Update, because of severe performance issues with the Update method.
                Plugins.ClearCache();

                {
                    Console.Write("Loading generated plugins ... ");
                    var stopwatch = Stopwatch.StartNew();

                    var builder = new ContainerBuilder();
                    builder.RegisterModule(new AutofacModuleConfiguration(deploymentTime: false));
                    using (var container = builder.Build())
                    {
                        Console.WriteLine("Done.");
                        var _performanceLogger = container.Resolve<ILogProvider>().GetLogger("Performance");
                        _performanceLogger.Write(stopwatch, "DeployPackages.Program: New modules and plugins registered.");
                        Plugins.LogRegistrationStatistics("Initializing application", container);

                        container.Resolve<ApplicationInitialization>().ExecuteInitializers();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                _logger.Error(ex.ToString());

                DeploymentUtility.WriteError(ex.GetType().Name + ": " + ex.Message);
                Console.WriteLine("See DeployPackages.log for more information on error. Enable TraceLog in config file for even more details.");

                if (ex is ReflectionTypeLoadException)
                {
                    var loaderMessages = string.Join("\r\n", ((ReflectionTypeLoadException)ex).LoaderExceptions.Select(le => le.Message).Distinct());
                    _logger.Error(loaderMessages);
                }

                if (Environment.UserInteractive)
                    Thread.Sleep(3000);
                return 1;
            }
            finally
            {
                if (oldCurrentDirectory != null && Directory.Exists(oldCurrentDirectory))
                    Directory.SetCurrentDirectory(oldCurrentDirectory);
            }

            return 0;
        }
    }
}
