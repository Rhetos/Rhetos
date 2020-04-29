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
using Rhetos.Utilities;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace Rhetos.Configuration.Autofac
{
    /// <summary>
    /// Inherit this class and override virtual functions to customize it.
    /// </summary>
    public class RhetosTestContainer : IDisposable
    {

        // Global:
        private static object _containerInitializationLock = new object();
        private static RhetosProcessContainer _rhetosProcessContainer;

        // Instance per test or session:
        protected ILifetimeScope _lifetimeScope;
        protected bool _commitChanges;
        protected string _explicitRhetosServerFolder;
        protected RhetosTransactionScopeContainer _rhetosTransactionScope;
        public event Action<ContainerBuilder> InitializeSession;

        /// <param name="commitChanges">
        /// Whether database updates (by ORM repositories) will be committed or rollbacked.
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
            InitializeRhetosProcessContainer();
        }

        public T Resolve<T>()
        {
            InitializeRhetosProcessContainer();
            return _rhetosTransactionScope.Resolve<T>();
        }

        public void Dispose()
        {
            if (_rhetosTransactionScope != null)
                _rhetosTransactionScope.Dispose();
        }

        private void InitializeRhetosProcessContainer()
        {
            if (_rhetosTransactionScope == null)
            {
                if (_rhetosProcessContainer == null)
                {
                    lock (_containerInitializationLock)
                        if (_rhetosProcessContainer == null)
                        {
                            _rhetosProcessContainer = new RhetosProcessContainer(SearchForRhetosServerRootFolder, new ConsoleLogProvider(),
                                configurationBuilder => configurationBuilder.AddConfigurationManagerConfiguration());
                            var configuration = _rhetosProcessContainer.CreateTransactionScope(false).Resolve<IConfiguration>();
                            AppDomain.CurrentDomain.AssemblyResolve += new AssemblyResolver(configuration).SearchForAssembly;
                        }
                }

                _rhetosTransactionScope = _rhetosProcessContainer.CreateTransactionScope(_commitChanges);
                _rhetosTransactionScope.ScopeInitialization += (containerBuilder) => InitializeSession?.Invoke(containerBuilder);
            }
        }

        private static bool IsValidRhetosServerDirectory(string path)
        {
            return
                File.Exists(Path.Combine(path, @"web.config"))
                && File.Exists(Path.Combine(path, @"bin\Rhetos.Utilities.dll"));
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

            throw new FrameworkException("Cannot locate a valid Rhetos server's folder from '" + Environment.CurrentDirectory + "'. Unexpected folder '" + folder.FullName + "'.");
        }

        private class AssemblyResolver
        {
            private readonly List<string> _searchFolders;

            public AssemblyResolver(IConfiguration configuration)
            {
                var rhetosAppOptins = configuration.GetOptions<RhetosAppOptions>();
                var legacyPaths = configuration.GetOptions<LegacyPathsOptions>();
                _searchFolders = new[]
                {
                    legacyPaths.BinFolder ?? rhetosAppOptins.GetAssemblyFolder(),
                    legacyPaths.PluginsFolder,
                    rhetosAppOptins.AssetsFolder,
                }
                    .Where(folder => !string.IsNullOrEmpty(folder))
                    .Distinct()
                    .ToList();
            }

            public Assembly SearchForAssembly(object sender, ResolveEventArgs args)
            {
                foreach (var folder in _searchFolders)
                {
                    string pluginAssemblyPath = Path.Combine(folder, new AssemblyName(args.Name).Name + ".dll");
                    if (File.Exists(pluginAssemblyPath))
                        return Assembly.LoadFrom(pluginAssemblyPath);
                }
                return null;
            }
        }
    }
}
