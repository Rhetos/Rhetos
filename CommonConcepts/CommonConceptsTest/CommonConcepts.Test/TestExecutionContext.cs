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
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;
using Rhetos.Utilities;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Persistence;
using Rhetos.Persistence.NHibernate;
using Rhetos.Security;
using Rhetos.TestCommon;
using System.Reflection;
using Oracle.DataAccess.Client;
using Rhetos.Dom;
using Rhetos.Persistence.NHibernateDefaultConcepts;

namespace CommonConcepts.Test
{
    public class TestExecutionContext : Common.ExecutionContext, IDisposable
    {
        private readonly bool _commitChanges;
        private static string _rhetosServerPath;

        public TestExecutionContext(bool commitChanges = false, string rhetosServerPath = "")
        {
            _commitChanges = commitChanges;

            if (_rhetosServerPath == null)
                _rhetosServerPath = rhetosServerPath;
            else
                if (_rhetosServerPath != rhetosServerPath)
                    throw new ApplicationException(string.Format(
                        "Cannot use different rhetosServerPath in same session.\r\nOld:'{0}'\r\nNew:'{1}'",
                        _rhetosServerPath, rhetosServerPath));

            Initialize();

            // Standard members of ExecutionContext:
            _userInfo = new Lazy<IUserInfo>(() => new TestUserInfo());
            _persistenceTransaction = new Lazy<IPersistenceTransaction>(() => new NHibernatePersistenceTransaction(NHPE.Value, new ConsoleLogProvider(), _userInfo.Value));
            _sqlExecuter = new Lazy<ISqlExecuter>(() => new MsSqlExecuter(ConnectionString.Value, new ConsoleLogProvider(), UserInfo));
            _authorizationManager = new Lazy<IAuthorizationManager>(() => { throw new NotImplementedException(); });
            _resourcesFolder = new Lazy<ResourcesFolder>(() => Path.Combine(rhetosServerPath, "Resources"));
        }

        private static bool _initialized;
        private static void Initialize()
        {
            if (!_initialized)
            {
                _initialized = true;

                // Initialize connection string:
                var configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _rhetosServerPath, @"bin\ConnectionStrings.config");
                SqlUtility.LoadSpecificConnectionString(configFile);

                // Register plugins folder assemblies:
                AppDomain.CurrentDomain.AssemblyResolve += (object sender, ResolveEventArgs args) =>
                {
                    string pluginsFolder = Path.Combine(_rhetosServerPath, @"bin\Plugins\");
                    string pluginAssembly = Path.Combine(pluginsFolder, new AssemblyName(args.Name).Name + ".dll");
                    if (File.Exists(pluginAssembly) == false) return null;
                    Assembly assembly = Assembly.LoadFrom(pluginAssembly);
                    return assembly;
                };
            }
        }

        private static Lazy<string> ConnectionString = new Lazy<string>(() => SqlUtility.ConnectionString);

        private static Lazy<NHibernatePersistenceEngine> NHPE = new Lazy<NHibernatePersistenceEngine>(() =>
            new NHibernatePersistenceEngine(
                new ConsoleLogProvider(),
                new NHibernateMappingLoader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _rhetosServerPath, "bin", "ServerDomNHibernateMapping.xml")),
                new TestDomainObjectModel(),
                ConnectionString.Value,
                new[] { new CommonConceptsNHibernateConfigurationExtension() }));

        ~TestExecutionContext()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (_persistenceTransaction != null && _persistenceTransaction.IsValueCreated)
            {
                if (!_commitChanges)
                    _persistenceTransaction.Value.DiscardChanges();
                _persistenceTransaction.Value.Dispose();
            }
        }

        class TestDomainObjectModel : IDomainObjectModel
        {
            public Assembly ObjectModel
            {
                get { return Assembly.GetAssembly(typeof(Common.ExecutionContext)) ; }
            }
        }
    }
}
