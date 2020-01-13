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

using Rhetos.Deployment;
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

            /*try
            {*/
                var commands = new Dictionary<string, Action<string>>(StringComparer.OrdinalIgnoreCase)
                {
                    { "restore", rhetosAppRootPath => Restore(rhetosAppRootPath, logProvider) },
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

        private static void Restore(string rhetosAppRootPath, NLogProvider logProvider)
        {
            var configurationProvider = RhetosConfigurationBuilder(rhetosAppRootPath).Build();
            var deployment = new ApplicationDeployment(configurationProvider, logProvider, LegacyUtilities.GetListAssembliesDelegate());
            AppDomain.CurrentDomain.AssemblyResolve += GetSearchForAssemblyDelegate(
                Paths.BinFolder);
            deployment.DownloadPackages(false);
        }

        private static void Build(string rhetosAppRootPath, NLogProvider logProvider)
        {
            var configurationProvider = RhetosConfigurationBuilder(rhetosAppRootPath)
                .AddKeyValue(nameof(BuildOptions.CacheFolder), Path.Combine(rhetosAppRootPath, "obj\\Rhetos"))
                .AddKeyValue(nameof(BuildOptions.GeneratedSourceFolder), Path.Combine(rhetosAppRootPath, "RhetosSource"))
                .Build();
            var lockFile = GetLockFile(rhetosAppRootPath);
            var targetFramework = GetTargetFramework(lockFile, null);
            var buildAssemblies = GetBuildAssemblies(lockFile, targetFramework);
            Func<List<string>> getBuildAssemblies = () => buildAssemblies;
            var deployment = new ApplicationDeployment(configurationProvider, logProvider, getBuildAssemblies);
            AppDomain.CurrentDomain.AssemblyResolve += GetSearchForAssemblyDelegate(buildAssemblies.ToArray());
            deployment.GenerateApplication(GetInstalledPackages(lockFile, targetFramework));
        }

        private static void DbUpdate(string rhetosAppRootPath, NLogProvider logProvider)
        {
            var configurationProvider = RhetosConfigurationBuilder(rhetosAppRootPath).Build();
            var deployment = new ApplicationDeployment(configurationProvider, logProvider, LegacyUtilities.GetListAssembliesDelegate());
            AppDomain.CurrentDomain.AssemblyResolve += GetSearchForAssemblyDelegate(
                Paths.BinFolder,
                Paths.PluginsFolder,
                Paths.GeneratedFolder);
            deployment.UpdateDatabase();
        }

        private static void AppInitialize(string rhetosAppRootPath, NLogProvider logProvider)
        {
            var configurationProvider = RhetosConfigurationBuilder(rhetosAppRootPath).Build();
            var deployment = new ApplicationDeployment(configurationProvider, logProvider, LegacyUtilities.GetListAssembliesDelegate());
            AppDomain.CurrentDomain.AssemblyResolve += GetSearchForAssemblyDelegate(
                Paths.BinFolder,
                Paths.PluginsFolder,
                Paths.GeneratedFolder);
            deployment.InitializeGeneratedApplication();
        }

        private static IConfigurationBuilder RhetosConfigurationBuilder(string rhetosAppRootPath)
        {
            return new ConfigurationBuilder()
                .AddRhetosAppConfiguration(rhetosAppRootPath)
                .AddConfigurationManagerConfiguration();
        }

        private static NuGet.ProjectModel.LockFile GetLockFile(string projectRootFolder)
        {
            return NuGet.ProjectModel.LockFileUtilities.GetLockFile(Path.Combine(projectRootFolder, "obj", "project.assets.json"), null);
        }

        private static NuGet.Frameworks.NuGetFramework GetTargetFramework(NuGet.ProjectModel.LockFile lockFile, string target)
        {
            if (string.IsNullOrEmpty(target))
            {
                var targets = lockFile.Targets.Select(x => x.TargetFramework).Distinct();
                if (targets.Count() > 1)
                    throw new FrameworkException("There are multiple targets set. Pass the target version through the command line???");
                if (targets.Count() == 0)
                    throw new FrameworkException("No targets???");

                return targets.First();
            }
            else
            {
                return NuGet.Frameworks.NuGetFramework.Parse("net45");
            }
        }

        private static InstalledPackages GetInstalledPackages(NuGet.ProjectModel.LockFile lockFile, NuGet.Frameworks.NuGetFramework targetFramework)
        {
            var librariesForTargetFramework = lockFile.Targets.First(x => x.TargetFramework == targetFramework).Libraries;

            var installedPackages = new List<InstalledPackage>();
            foreach (var targetLibrary in librariesForTargetFramework)
            {
                var library = lockFile.GetLibrary(targetLibrary.Name, targetLibrary.Version);
                //TODO: It should be checked if this is the correct way to resolve the nuget package folder
                var packageFolder = lockFile.PackageFolders.Select(x => Path.Combine(x.Path, library.Path.Replace('/', '\\'))).FirstOrDefault(x => Directory.Exists(x));
                if (packageFolder == null)
                    throw new FrameworkException($"Could not locate the folder for package {library.Name};");
                var contentFiles = library.Files.Select(x => new ContentFile { PhysicalPath = Path.Combine(packageFolder, x.Replace('/', '\\')), InPackagePath = x.Replace('/', '\\') }).ToList();
                installedPackages.Add(new InstalledPackage(library.Name, library.Version.Version.ToString(), null, null, null, null, contentFiles));
            }

            var packages = librariesForTargetFramework.Select(x => x.Name).ToList();
            var dependencies = librariesForTargetFramework.Select(x => x.Dependencies.Select(y => new Tuple<string, string>(x.Name, y.Id))).SelectMany(x => x);
            Graph.TopologicalSort(packages, dependencies);
            Graph.SortByGivenOrder(installedPackages, packages, x => x.Id);

            return new InstalledPackages { Packages = installedPackages };
        }

        private static List<string> GetBuildAssemblies(NuGet.ProjectModel.LockFile lockFile, NuGet.Frameworks.NuGetFramework targetFramework)
        {
            var buildAssemblies = new List<string>();
            var librariesForTargetFramework = lockFile.Targets.First(x => x.TargetFramework == targetFramework).Libraries;
            foreach (var targetLibrary in librariesForTargetFramework)
            {
                var library = lockFile.GetLibrary(targetLibrary.Name, targetLibrary.Version);
                //TODO: It should be checked if this is the correct way to resolve the nuget package folder
                var packageFolder = lockFile.PackageFolders.Select(x => Path.Combine(x.Path, library.Path.Replace('/', '\\'))).FirstOrDefault(x => Directory.Exists(x));
                if (packageFolder == null)
                    throw new FrameworkException($"Could not locate the folder for package {library.Name};");
                buildAssemblies.AddRange(targetLibrary.CompileTimeAssemblies.Select(y => Path.Combine(packageFolder, y.Path.Replace('/', '\\'))));
            }

            return buildAssemblies.Where(x => !x.EndsWith("_._")).ToList();
        }

        private static ResolveEventHandler GetSearchForAssemblyDelegate(params string[] assemblyList)
        {
            return new ResolveEventHandler((object sender, ResolveEventArgs args) =>
            {
                // TODO: Review if loadedAssembly is needed.
                var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.GetName().Name == new AssemblyName(args.Name).Name);
                if (loadedAssembly != null)
                    return loadedAssembly;

                foreach (var assembly in assemblyList.Where(x => x.EndsWith(new AssemblyName(args.Name).Name + ".dll")))
                {
                    if (File.Exists(assembly))
                        return Assembly.LoadFrom(assembly);
                }
                return null;
            });
        }
    }
}
