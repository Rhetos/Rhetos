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
using Rhetos.Deployment;

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
            Logger = LogProvider.GetLogger("DeployPackages");
        }

        public int Run(string[] args)
        {
            Logger.Trace(() => "Logging configured.");

            var rootCommand = new RootCommand();
            var buildCommand = new Command("build", "Generates the Rhetos application inside the <project-root-folder>. If <project-root-folder> is not set it will use the current working directory.");
            buildCommand.Add(new Argument<DirectoryInfo>("project-root-folder", () => new DirectoryInfo(Environment.CurrentDirectory)));
            buildCommand.Add(new Option<string[]>("--assemblies", Array.Empty<string>(), "List of assemblies outside of referenced NuGet packages that will be used during the build."));
            buildCommand.Add(new Option<string>("--assembly-name", () => null, "The name of the assembly which will contain the generated source files."));
            buildCommand.Handler = CommandHandler.Create((DirectoryInfo projectRootFolder, string[] assemblies, string assemblyName) => ReportError(() => Build(projectRootFolder.FullName, assemblies, assemblyName)));
            rootCommand.AddCommand(buildCommand);

            var dbUpdateCommand = new Command("dbupdate", "Updates the database based on the generated files from the build process. If <application-root-folder> is not set it will use the current working directory.");
            dbUpdateCommand.Add(new Argument<DirectoryInfo>("application-root-folder", () => new DirectoryInfo(Environment.CurrentDirectory)));
            dbUpdateCommand.Handler = CommandHandler.Create((DirectoryInfo applicationRootFolder) => ReportError(() => DbUpdate(applicationRootFolder.FullName)));
            rootCommand.AddCommand(dbUpdateCommand);

            var appInitializeCommand = new Command("appinitialize", "Initializes the application. If <application-root-folder> is not set it will use the current working directory.");
            appInitializeCommand.Add(new Argument<DirectoryInfo>("application-root-folder", () => new DirectoryInfo(Environment.CurrentDirectory)));
            appInitializeCommand.Handler = CommandHandler.Create((DirectoryInfo applicationRootFolder) => ReportError(() => AppInitialize(applicationRootFolder.FullName)));
            rootCommand.AddCommand(appInitializeCommand);

            return rootCommand.Invoke(args);
        }

        private int ReportError(Action action)
        {
            try
            {
                action.Invoke();
                Logger.Trace("Done.");
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

        private void Build(string projectRootPath, string[] assemblies, string assemblyName)
        {
            string binFolder = Path.Combine(projectRootPath, "bin");
            if (FilesUtility.IsSameDirectory(AppDomain.CurrentDomain.BaseDirectory, binFolder))
                throw new FrameworkException($"Rhetos build command cannot be run from the generated application.");

            var nuget = new NuGetUtilities(projectRootPath, LogProvider, null);
            assemblyName = assemblyName ?? nuget.ProjectName; // TODO: We are using the project name as the output assembly name because this is almost always the case. This is only temporary.
            var configurationProvider = new ConfigurationBuilder()
                .AddRhetosAppEnvironment(new RhetosAppEnvironment
                {
                    RootFolder = projectRootPath,
                    BinFolder = binFolder,
                    AssetsFolder = Path.Combine(projectRootPath, "RhetosAssets"),
                    AssemblyName = assemblyName,
                    // TODO: Rhetos CLI should not use LegacyPluginsFolder. Referenced plugins are automatically copied to output bin folder by NuGet. It is used by DeployPackages.exe when downloading packages and in legacy application runtime for assembly resolver and probing paths.
                    // TODO: Set LegacyPluginsFolder to null after reviewing impact to AspNetFormsAuth CLI utilities and similar packages.
                    LegacyPluginsFolder = Path.Combine(projectRootPath, "bin"),
                    LegacyAssetsFolder = Path.Combine(projectRootPath, "Resources"),
                })
                .AddKeyValue(nameof(BuildOptions.ProjectFolder), projectRootPath)
                .AddKeyValue(nameof(BuildOptions.CacheFolder), Path.Combine(projectRootPath, "obj\\Rhetos"))
                .AddKeyValue(nameof(BuildOptions.GeneratedSourceFolder), Path.Combine(projectRootPath, "RhetosSource"))
                .AddConfigurationManagerConfiguration()
                .AddJsonFile(Path.Combine(projectRootPath, "rhetos-build.settings.json"), optional: true)
                .Build();

            var packagesBuildAssemblies = nuget.GetBuildAssemblies();

            var allAssemblies = packagesBuildAssemblies.Select(a => Path.GetFullPath(a)).Union(assemblies.Select(a => Path.GetFullPath(a)));

            var multipleAssembliesWithsameName = allAssemblies.GroupBy(a => Path.GetFileName(a)).Where(a => a.Count() > 1);
            if (multipleAssembliesWithsameName.Any())
            {
                Logger.Info("Detected multiple assemblies with the same name:" + Environment.NewLine +
                    string.Join(Environment.NewLine, multipleAssembliesWithsameName.Select(a => $"{a.Key} on locations {string.Join(", ", a)}")));
            }

            AppDomain.CurrentDomain.AssemblyResolve += GetSearchForAssemblyDelegate(allAssemblies.ToArray());

            var deployment = new ApplicationDeployment(configurationProvider, LogProvider, () => allAssemblies);

            var installedPackages = nuget.GetInstalledPackages();
            installedPackages.Add(GetProjectAsInstalledPackage(nuget, projectRootPath));

            deployment.GenerateApplication(new InstalledPackages { Packages = installedPackages });
        }

        private InstalledPackage GetProjectAsInstalledPackage(NuGetUtilities nuGetUtilities, string projectRootPath)
        {
            //TODO: We should add the possibility to specify content files as an option
            //MSBuild knows which files are part of the project that it is building so we should use only those files
            var contentFiles = Directory.GetFiles(projectRootPath, "*", SearchOption.AllDirectories)
                .Select(f => new ContentFile { PhysicalPath = f, InPackagePath = FilesUtility.AbsoluteToRelativePath(projectRootPath, f) })
                .Where(c => (!c.InPackagePath.StartsWith("bin") && !c.InPackagePath.StartsWith("obj")))
                .ToList();
            //TODO: We are using the project name as the PackageName for the project.
            //This could be a problem if we change the project name later during the development because then all the data-migration script will be executed again
            //It should be revised if this is the desired behavior
            return new InstalledPackage(nuGetUtilities.ProjectName, "", null, null, null, null, contentFiles);
        }

        private void DbUpdate(string rhetosAppRootPath)
        {
            var deployment = SetupRuntime(rhetosAppRootPath);
            deployment.UpdateDatabase();
        }

        private void AppInitialize(string rhetosAppRootPath)
        {
            var deployment = SetupRuntime(rhetosAppRootPath);
            deployment.InitializeGeneratedApplication();
        }

        private ApplicationDeployment SetupRuntime(string rhetosAppRootPath)
        {
            var configurationProvider = new ConfigurationBuilder()
                .AddRhetosAppConfiguration(rhetosAppRootPath)
                .AddKeyValue(nameof(DatabaseOptions.SqlCommandTimeout), 0)
                .AddConfigurationManagerConfiguration()
                .Build();

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
