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

namespace Rhetos.Configuration.Autofac
{
    /// <summary>
    /// RhetosTestContainer is a legacy wrapper around <see cref="ProcessContainer"/> and <see cref="TransactionScopeContainer"/>.
    /// For new projects use those classes directly.
    /// Inherit this class and override virtual functions to customize it.
    /// </summary>
    [Obsolete("Use Rhetos.ProcessContainer instead.")]
    public class RhetosTestContainer : IDisposable
    {
        // Global:
        private static object _containerInitializationLock = new object();
        private static ProcessContainer _processContainer;

        // Instance per test or session:
        protected bool _commitChanges;
        protected string _applicationFolder;
        protected TransactionScopeContainer _rhetosTransactionScope;
        public event Action<ContainerBuilder> InitializeSession;

        /// <param name="commitChanges">
        /// Whether database updates (by ORM repositories) will be committed or rolled back.
        /// </param>
        /// <param name="applicationFolder">
        /// Folder where the Rhetos configuration file is located (see <see cref="RhetosAppEnvironment.ConfigurationFileName"/>),
        /// or any subfolder.
        /// If not specified, the current application's base directory is used by default.
        /// </param>
        public RhetosTestContainer(bool commitChanges = false, string applicationFolder = null)
        {
            if (applicationFolder != null && !Directory.Exists(applicationFolder))
                throw new ArgumentException("The given folder does not exist: " + Path.GetFullPath(applicationFolder) + ".");

            _commitChanges = commitChanges;
            _applicationFolder = applicationFolder;
        }

        /// <summary>
        /// No need to call this method directly before calling Resolve().
        /// Calling Initialize() is needed only when directly accessing static (global) Rhetos properties before resolving any component.
        /// </summary>
        public void Initialize()
        {
            InitializeRhetosTransactionScopeContainer();
        }

        public T Resolve<T>()
        {
            InitializeRhetosTransactionScopeContainer();
            return _rhetosTransactionScope.Resolve<T>();
        }

        private bool disposed = false; // Standard IDisposable pattern to detect redundant calls.

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                _rhetosTransactionScope?.Dispose();
            }

            disposed = true;
        }

        private void InitializeRhetosTransactionScopeContainer()
        {
            if (_rhetosTransactionScope == null)
            {
                if (_processContainer == null)
                {
                    lock (_containerInitializationLock)
                        if (_processContainer == null)
                        {
                            _processContainer = new ProcessContainer(_applicationFolder, new ConsoleLogProvider(),
                                configurationBuilder => configurationBuilder.AddConfigurationManagerConfiguration());
                        }
                }

                _rhetosTransactionScope = _processContainer.CreateTransactionScopeContainer(InitializeSession);
                if (_commitChanges)
                    _rhetosTransactionScope.CommitChanges();
            }
        }
    }
}
