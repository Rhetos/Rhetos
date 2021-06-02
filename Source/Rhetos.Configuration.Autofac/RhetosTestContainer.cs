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

namespace Rhetos.Configuration.Autofac
{
    /// <summary>
    /// RhetosTestContainer is a legacy wrapper around <see cref="RhetosHost"/> and <see cref="UnitOfWorkScope"/>.
    /// For new projects use those classes directly.
    /// Inherit this class and override virtual functions to customize it.
    /// </summary>
    [Obsolete("Use Rhetos.RhetosHost instead.")]
    public class RhetosTestContainer : IDisposable
    {
        // Global:
        private static object _rhetosHostInitializationLock = new object();
        private static RhetosHost _rhetosHost;

        // Instance per test or session:
        protected bool _commitChanges;
        protected string _rhetosAppAssemblyPath;
        protected UnitOfWorkScope _transactionScope;
        public event Action<ContainerBuilder> InitializeSession;

        /// <param name="commitChanges">
        /// Whether database updates (by ORM repositories) will be committed or rolled back.
        /// </param>
        /// <param name="rhetosAppAssemblyPath">
        /// Path to assembly where the CreateRhetosHostBuilder method is located.
        /// </param>
        public RhetosTestContainer(string rhetosAppAssemblyPath, bool commitChanges = false)
        {
            _commitChanges = commitChanges;
            _rhetosAppAssemblyPath = rhetosAppAssemblyPath;
        }

        /// <summary>
        /// No need to call this method directly before calling Resolve().
        /// Calling Initialize() is needed only when directly accessing static (global) Rhetos properties before resolving any component.
        /// </summary>
        public void Initialize()
        {
            InitializeUnitOfWorkScope();
        }

        public T Resolve<T>()
        {
            InitializeUnitOfWorkScope();
            return _transactionScope.Resolve<T>();
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
                if(_commitChanges)
                    _transactionScope?.CommitAndClose();
                else
                    _transactionScope?.RollbackAndClose();
            }

            disposed = true;
        }

        private void InitializeUnitOfWorkScope()
        {
            if (_transactionScope == null)
            {
                if (_rhetosHost == null)
                {
                    lock (_rhetosHostInitializationLock)
                        if (_rhetosHost == null)
                        {
                            _rhetosHost = RhetosHost.Find(_rhetosAppAssemblyPath, rhetosHostBuilder => {
                                rhetosHostBuilder.UseBuilderLogProvider(new ConsoleLogProvider())
                                    .ConfigureConfiguration(configurationBuilder => configurationBuilder.AddConfigurationManagerConfiguration());
                            });
                        }
                }

                _transactionScope = _rhetosHost.CreateScope(InitializeSession);
            }
        }
    }
}
