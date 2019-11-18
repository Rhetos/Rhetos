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
using Rhetos.Logging;
using Rhetos.Persistence;
using Rhetos.Security;
using Rhetos.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Rhetos.Configuration.Autofac
{
    /// <summary>
    /// Inherit this class and override virtual functions to customize it.
    /// </summary>
    public class RhetosTestContainer : IDisposable
    {

        // Global:
        private static IContainer _iocContainer;
        private static object _containerInitializationLock = new object();
        protected static ILogger _performanceLogger = new ConsoleLogger("Performance");
        private static RhetosAppEnvironment _rhetosAppEnvironment;

        // Instance per test or session:
        protected ILifetimeScope _lifetimeScope;
        protected bool _commitChanges;
        protected string _explicitRhetosServerFolder;
        public event Action<ContainerBuilder> InitializeSession;

        /// <param name="commitChanges">
        /// Whether database updates (by ORM repositories) will be committed or rollbacked.
        /// Note: Database updates done by SqlExecuter are always instantly committed.
        /// </param>
        /// <param name="rhetosServerFolder">
        /// If not set, the class will try to automatically locate Rhetos server, looking from current directory.
        /// </param>
        public RhetosTestContainer(bool commitChanges = false, string rhetosServerFolder = null)
        {
            if (rhetosServerFolder != null)
                if (!Directory.Exists(rhetosServerFolder))
                    throw new ArgumentException("The given folder does not exist: " + Path.GetFullPath(rhetosServerFolder) + ".");

            _commitChanges = commitChanges;
            _explicitRhetosServerFolder = rhetosServerFolder;
        }

        /// <summary>
        /// No need to call this method directly before calling Resolve().
        /// Calling Initialize() is needed only when directly accessing static (global) Rhetos properties before resolving any component.
        /// </summary>
        public void Initialize()
        {
            InitializeLifetimeScope();
        }

        public T Resolve<T>()
        {
            InitializeLifetimeScope();
            return _lifetimeScope.Resolve<T>();
        }

        public void Dispose()
        {
            if (!_commitChanges && _lifetimeScope != null)
                _lifetimeScope.Resolve<IPersistenceTransaction>().DiscardChanges();

            if (_lifetimeScope != null)
                _lifetimeScope.Dispose();
        }

        private void InitializeLifetimeScope()
        {
            if (_lifetimeScope == null)
            {
                if (_iocContainer == null)
                {
                    lock (_containerInitializationLock)
                        if (_iocContainer == null)
                        {
                            var rhetosAppRootPath = SearchForRhetosServerRootFolder();
                            var configurationProvider = new ConfigurationBuilder()
                                .AddRhetosAppConfiguration(rhetosAppRootPath)
                                .AddConfigurationManagerConfiguration()
                                .Build();

                            _rhetosAppEnvironment = new RhetosAppEnvironment(rhetosAppRootPath);
                            _iocContainer = InitializeIocContainer(configurationProvider);
                        }
                }

                if (InitializeSession != null)
                    _lifetimeScope = _iocContainer.BeginLifetimeScope(InitializeSession);
                else
                    _lifetimeScope = _iocContainer.BeginLifetimeScope();

            }
        }

        private static bool IsValidRhetosServerDirectory(string path)
        {
            return
                File.Exists(Path.Combine(path, @"web.config"))
                && File.Exists(Path.Combine(path, @"bin\Rhetos.dll"));
        }

        protected string SearchForRhetosServerRootFolder()
        {
            if (_explicitRhetosServerFolder != null)
                return _explicitRhetosServerFolder;

            var folder = new DirectoryInfo(Environment.CurrentDirectory);

            if (IsValidRhetosServerDirectory(folder.FullName))
                return folder.FullName;

            // Unit testing subfolder.
            if (folder.Name == "Out")
                folder = folder.Parent.Parent.Parent;

            // Unit testing at project level, not at solution level. It depends on the way the testing has been started.
            if (folder.Name == "Debug")
                folder = folder.Parent.Parent.Parent.Parent.Parent; // Climbing up CommonConcepts\CommonConceptsTest\CommonConcepts.Test\bin\Debug.

            if (folder.GetDirectories().Any(subDir => subDir.Name == "Source"))
                folder = new DirectoryInfo(Path.Combine(folder.FullName, @".\Source\Rhetos\"));

            // For unit tests, project's source folder name is ".\Source\Rhetos".
            if (folder.Name == "Rhetos" && IsValidRhetosServerDirectory(folder.FullName))
                return folder.FullName;

            throw new ApplicationException("Cannot locate a valid Rhetos server's folder from '" + Environment.CurrentDirectory + "'. Unexpected folder '" + folder.FullName + "'.");
        }

        private IContainer InitializeIocContainer(IConfigurationProvider configurationProvider)
        {
            AppDomain.CurrentDomain.AssemblyResolve += SearchForAssembly;

            // General registrations:
            var builder = new RhetosContainerBuilder(configurationProvider, new ConsoleLogProvider())
                .AddRhetosRuntime();

            // Specific registrations override:
            builder.RegisterType<ProcessUserInfo>().As<IUserInfo>();
            builder.RegisterType<ConsoleLogProvider>().As<ILogProvider>();

            // Build the container:
            var sw = Stopwatch.StartNew();
            var container = builder.Build();
            _performanceLogger.Write(sw, "RhetosTestContainer: Built IoC container");
            return container;
        }

        protected Assembly SearchForAssembly(object sender, ResolveEventArgs args)
        {
            foreach (var folder in new[] { _rhetosAppEnvironment.PluginsFolder, _rhetosAppEnvironment.GeneratedFolder, _rhetosAppEnvironment.BinFolder })
            {
                string pluginAssemblyPath = Path.Combine(folder, new AssemblyName(args.Name).Name + ".dll");
                if (File.Exists(pluginAssemblyPath))
                    return Assembly.LoadFrom(pluginAssemblyPath);
            }
            return null;
        }
    }
}
