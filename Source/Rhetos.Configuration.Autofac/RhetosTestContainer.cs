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
using Rhetos.Configuration.Autofac;
using Rhetos.Extensibility;
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

        // Instance per test or session:
        protected ILifetimeScope _lifetimeScope;
        protected bool _commitChanges;

        /// <param name="commitChanges">
        /// Whether database updates (by ORM repositories) will be committed or rollbacked.
        /// Note: Database updates done by SqlExecuter are always instantly committed.
        /// </param>
        /// <param name="rhetosServerFolder">
        /// If not set, the class will try to automatically locate Rhetos server, looking from current directory.
        /// </param>
        public RhetosTestContainer(bool commitChanges = false, string rhetosServerFolder = null)
        {
            _commitChanges = commitChanges;
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
                            Paths.InitializeRhetosServerRootPath(InitializeRhetosServerRootPath());
                            _iocContainer = InitializeIocContainer();
                        }
                }

                _lifetimeScope = _iocContainer.BeginLifetimeScope();
            }
        }

        protected virtual string InitializeRhetosServerRootPath()
        {
            var folder = new DirectoryInfo(Environment.CurrentDirectory);

            if (folder.Name == "Out") // Unit testing subfolder.
                folder = folder.Parent.Parent.Parent;

            if (folder.Name == "Debug") // Unit testing at project level, not at solution level. It depends on the way the testing has been started.
                folder = folder.Parent.Parent.Parent.Parent.Parent; // Climbing up CommonConcepts\CommonConceptsTest\CommonConcepts.Test\bin\Debug.

            if (folder.GetDirectories().Any(subDir => subDir.Name == "Source"))
                folder = new DirectoryInfo(Path.Combine(folder.FullName, @".\Source\Rhetos\"));

            if (folder.Name != "Rhetos")
                throw new ApplicationException("Cannot locate Rhetos folder from '" + Environment.CurrentDirectory + "'. Unexpected folder '" + folder.Name + "'.");

            return folder.FullName;
        }

        private IContainer InitializeIocContainer()
        {
            AppDomain.CurrentDomain.AssemblyResolve += SearchForAssembly;

            // Specific registrations and initialization:
            PluginsUtility.SetLogProvider(new ConsoleLogProvider());

            // Build the container:
            var builder = new ContainerBuilder();
            builder.RegisterModule(new DefaultAutofacConfiguration(generate: false));

            // Specific registrations override:
            builder.RegisterType<ProcessUserInfo>().As<IUserInfo>();
            builder.RegisterType<ConsoleLogProvider>().As<ILogProvider>();

            var sw = Stopwatch.StartNew();
            var container = builder.Build();
            _performanceLogger.Write(sw, "RhetosTestContainer: Built IoC container");
            return container;
        }

        protected virtual Assembly SearchForAssembly(object sender, ResolveEventArgs args)
        {
            foreach (var folder in new[] { Paths.PluginsFolder, Paths.GeneratedFolder, Paths.BinFolder })
            {
                string pluginAssemblyPath = Path.Combine(folder, new AssemblyName(args.Name).Name + ".dll");
                if (File.Exists(pluginAssemblyPath))
                    return Assembly.LoadFrom(pluginAssemblyPath);
            }
            return null;
        }
    }
}
