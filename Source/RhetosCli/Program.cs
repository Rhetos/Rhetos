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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Autofac;
using Newtonsoft.Json.Linq;
using Rhetos.Deployment;
using Rhetos.Dsl;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Utilities;

namespace Rhetos
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            var logProvider = new NLogProvider();
            var logger = logProvider.GetLogger("DeployPackages");
            logger.Trace(() => "Logging configured.");

            /*try
            {*/
                var command = args[0];
                if (string.Compare(command, "build", true) == 0)
                {
                    var projectRootFolder = args[1];
                    var configurationProvider = new ConfigurationBuilder()
                        .AddRhetosDefaultBuildConfiguration(projectRootFolder)
                        .AddConfigurationManagerConfiguration()
                        .Build();
                    var assemblies = GetAssemblies(Path.Combine(projectRootFolder, "obj", "project.assets.json"));
                    var installedPackages = GetPackages(projectRootFolder, Path.Combine(projectRootFolder, "obj", "project.assets.json"));
                    AppDomain.CurrentDomain.AssemblyResolve += GetExplicitSearchForAssemblyDelegate(assemblies);

                    var filesUtility = new FilesUtility(logProvider);
                    var buildOptions = configurationProvider.GetOptions<BuildOptions>();

                    filesUtility.SafeCreateDirectory(buildOptions.GeneratedAssetsFolder);
                    filesUtility.SafeCreateDirectory(buildOptions.GeneratedFilesCacheFolder);
                    filesUtility.SafeCreateDirectory(buildOptions.GeneratedSourceFolder);

                    filesUtility.EmptyDirectory(buildOptions.GeneratedAssetsFolder);
                    filesUtility.EmptyDirectory(buildOptions.GeneratedFilesCacheFolder);
                    filesUtility.EmptyDirectory(buildOptions.GeneratedSourceFolder);

                    var stopwatch = Stopwatch.StartNew();
                    using (var container = new RhetosContainerBuilder(configurationProvider, assemblies, logProvider).AddRhetosBuildModules(installedPackages).Build())
                    {
                        var performanceLogger = container.Resolve<ILogProvider>().GetLogger("Performance");
                        performanceLogger.Write(stopwatch, "DeployPackages.Program: Modules and plugins registered.");
                        ContainerBuilderPluginRegistration.LogRegistrationStatistics("Generating application", container, logProvider);

                        var _deployPackagesLogger = logProvider.GetLogger("DeployPackages");
                        _deployPackagesLogger.Trace("Parsing DSL scripts.");
                        int dslModelConceptsCount = container.Resolve<IDslModel>().Concepts.Count();
                        _deployPackagesLogger.Trace("Application model has " + dslModelConceptsCount + " statements.");

                        var generators = ApplicationGenerator.GetSortedGenerators(container.Resolve<IPluginsContainer<IGenerator>>(), _deployPackagesLogger);
                        foreach (var generator in generators)
                        {
                            _deployPackagesLogger.Trace("Executing " + generator.GetType().Name + ".");
                            generator.Generate();
                        }
                        if (!generators.Any())
                            _deployPackagesLogger.Trace("No additional generators.");
                    }
                }
                else if (string.Compare(command, "dbupdate", true) == 0)
                {
                    var binFolder = args[1];
                    var configurationProvider = new ConfigurationBuilder()
                        .AddWebConfiguration(args[1])
                        .AddConfigurationManagerConfiguration()
                        .Build();
                    var rhetosAppEnvironment = new RhetosAppEnvironment(binFolder, Path.Combine(binFolder, "RhetosGenerated"));
                    var deployOptions = configurationProvider.GetOptions<DeployOptions>();
                    var deployment = new ApplicationDeployment(configurationProvider, logProvider);
                    AppDomain.CurrentDomain.AssemblyResolve += GetSearchForAssemblyDelegate(
                        rhetosAppEnvironment.BinFolder,
                        rhetosAppEnvironment.PluginsFolder,
                        rhetosAppEnvironment.GeneratedFolder);

                    logger.Trace("Loading plugins.");
                    var stopwatch = Stopwatch.StartNew();

                    LegacyUtilities.Initialize(configurationProvider);

                    var builder = new RhetosContainerBuilder(configurationProvider, logProvider, rhetosAppEnvironment)
                        .AddRhetosDbupdateModules()
                        .AddProcessUserOverride();

                    using (var container = builder.Build())
                    {
                        var performanceLogger = container.Resolve<ILogProvider>().GetLogger("Performance");
                        performanceLogger.Write(stopwatch, "DeployPackages.Program: Modules and plugins registered.");
                        ContainerBuilderPluginRegistration.LogRegistrationStatistics("Generating application", container, logProvider);

                    var a = container.Resolve<ISqlExecuter>();
                    container.Resolve<Dbupdate>().Execute();
                    }
                }
                else if (string.Compare(command, "appinitialize", true) == 0)
                {
                    var rhetosServerRootFolder = args[1];
                    var configurationProvider = BuildConfigurationProvider(args);
                    var rhetosAppEnvironment = new RhetosAppEnvironment(rhetosServerRootFolder);
                    var deployOptions = configurationProvider.GetOptions<DeployOptions>();
                    var deployment = new ApplicationDeployment(configurationProvider, logProvider);
                    AppDomain.CurrentDomain.AssemblyResolve += GetSearchForAssemblyDelegate(
                        rhetosAppEnvironment.BinFolder,
                        rhetosAppEnvironment.PluginsFolder,
                        rhetosAppEnvironment.GeneratedFolder);
                    deployment.InitializeGeneratedApplication();
                }

                logger.Trace("Done.");
            /*}
            catch (Exception e)
            {
                logger.Error(e.ToString());

                string typeLoadReport = CsUtility.ReportTypeLoadException(e);
                if (typeLoadReport != null)
                    logger.Error(typeLoadReport);

                if (Environment.UserInteractive)
                    ApplicationDeployment.PrintErrorSummary(e);

                return 1;
            }*/

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

        private static ResolveEventHandler GetExplicitSearchForAssemblyDelegate(params string[] assemblies)
        {
            return new ResolveEventHandler((object sender, ResolveEventArgs args) =>
            {
                var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.GetName().Name == new AssemblyName(args.Name).Name);
                if (loadedAssembly != null)
                    return loadedAssembly;

                string pluginAssemblyPath = assemblies.FirstOrDefault(x => x.EndsWith(new AssemblyName(args.Name).Name + ".dll"));
                if (File.Exists(pluginAssemblyPath))
                    return Assembly.LoadFrom(pluginAssemblyPath);
                return null;
            });
        }

        private static string[] GetAssemblies(string projectAssestsJsonPath)
        {
            var assemblyOrder = new List<string> { "net472", "net45", "netstandard2.0", "net40-Client", "net40" };
            var assemblyPaths = new List<string>();
            JObject o = JObject.Parse(File.ReadAllText(projectAssestsJsonPath));
            var packagesPath = (string)o["project"]["restore"]["packagesPath"];
            foreach (var item in (JObject)o["libraries"])
            {
                var files = ((JArray)item.Value["files"]).
                    Select(x => Path.Combine(packagesPath, (string)item.Value["path"], ((string)x))).
                    Where(x => Path.GetExtension(x) == ".dll" && Directory.GetParent(x).Parent.Name == "lib").
                    GroupBy(x => Path.GetFileName(x)).
                    Select(x => x.Where(y => assemblyOrder.IndexOf(Directory.GetParent(y).Name) != -1).
                        OrderBy(y => assemblyOrder.IndexOf(Directory.GetParent(y).Name)).First());
                assemblyPaths.AddRange(files);
            }
            return assemblyPaths.ToArray();
        }

        private static List<InstalledPackage> GetPackages(string projectFolder, string projectAssestsJsonPath)
        {
            var installedPackages = new List<InstalledPackage>();
            JObject o = JObject.Parse(File.ReadAllText(projectAssestsJsonPath));
            var packagesPath = (string)o["project"]["restore"]["packagesPath"];
            foreach (var item in (JObject)o["libraries"])
            {
                string id = item.Key.Split('/')[0];
                string version = item.Key.Split('/')[1];
                var contentFiles = ((JArray)item.Value["files"]).
                    Select(x => new ContentFile
                    {
                        PhysicalPath = Path.Combine(packagesPath, (string)item.Value["path"], ((string)x)),
                        InPackagePath = ((string)x)
                    }).ToList();
                var packageFolder = "";
                var installedPackage = new InstalledPackage(id, version, null, packageFolder, null, null, contentFiles);

                installedPackages.Add(installedPackage);
            }

            var rhetosOSurceFolderInProject = Path.Combine(projectFolder, "Rhetos");
            var projectFiles = Directory.GetFiles(rhetosOSurceFolderInProject, "*", SearchOption.AllDirectories).Select(x => new ContentFile
            {
                PhysicalPath = x,
                InPackagePath = x.Replace(rhetosOSurceFolderInProject + "\\", "")
            }).ToList();
            installedPackages.Add(new InstalledPackage("", "", null, rhetosOSurceFolderInProject, null, null, projectFiles));

            return installedPackages;
        }
    }
}
