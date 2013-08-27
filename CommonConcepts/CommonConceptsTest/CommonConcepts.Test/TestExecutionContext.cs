/*
    Copyright (C) 2013 Omega software d.o.o.

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

namespace CommonConcepts.Test
{
    public class TestExecutionContext : Common.ExecutionContext, IDisposable
    {
        private readonly bool _commitChanges;
        private readonly string _nHibernateMappingFolder;

        public TestExecutionContext(bool commitChanges = false, string rhetosServerPath = "")
        {
            InitializeConnectionStringConfig(rhetosServerPath);
            RegisterPluginsFolderAssembies(rhetosServerPath);

            _commitChanges = commitChanges;
            _nHibernateMappingFolder = Path.Combine(rhetosServerPath, "bin");

            // Standard members of ExecutionContext:
            _persistenceTransaction = new Lazy<IPersistenceTransaction>(() => CreateNHPT());
            _userInfo = new Lazy<IUserInfo>(() => new TestUserInfo());
            _sqlExecuter = new Lazy<ISqlExecuter>(() => new MsSqlExecuter(ConnectionString.Value, new ConsoleLogProvider(), UserInfo));
            _authorizationManager = new Lazy<IAuthorizationManager>(() => { throw new NotImplementedException(); });
            _resourcesFolder = new Lazy<ResourcesFolder>(() => Path.Combine(rhetosServerPath, "Resources"));
        }

        private static string _initializedConnectionStringApplicationPath = null;
        private static void InitializeConnectionStringConfig(string rhetosServerPath)
        {
            if (_initializedConnectionStringApplicationPath == null)
            {
                var configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, rhetosServerPath, @"bin\ConnectionStrings.config");
                SqlUtility.LoadSpecificConnectionString(configFile);
                _initializedConnectionStringApplicationPath = rhetosServerPath;
            }
            else if (_initializedConnectionStringApplicationPath != rhetosServerPath)
                throw new ApplicationException(string.Format(
                    "Cannot use different rhetosServerPath in same session.\r\nOld:'{0}'\r\nNew:'{1}'",
                    _initializedConnectionStringApplicationPath, rhetosServerPath));
        }

        private static HashSet<string> _registeredAssemblyResolverForRhetosServerPlugins = new HashSet<string>();
        private static void RegisterPluginsFolderAssembies(string rhetosServerPath)
        {
            if (!_registeredAssemblyResolverForRhetosServerPlugins.Contains(rhetosServerPath))
            {
                _registeredAssemblyResolverForRhetosServerPlugins.Add(rhetosServerPath);
                AppDomain.CurrentDomain.AssemblyResolve += (object sender, ResolveEventArgs args) =>
                {
                    string pluginsFolder = Path.Combine(rhetosServerPath, @"bin\Plugins\");
                    string pluginAssembly = Path.Combine(pluginsFolder, new AssemblyName(args.Name).Name + ".dll");
                    if (File.Exists(pluginAssembly) == false) return null;
                    Assembly assembly = Assembly.LoadFrom(pluginAssembly);
                    return assembly;
                };
            }
        }

        private Lazy<string> ConnectionString = new Lazy<string>(() => SqlUtility.ConnectionString);

        private Lazy<NHibernatePersistenceEngine> NHPE = new Lazy<NHibernatePersistenceEngine>(
            () => new NHibernatePersistenceEngine(
                new ConsoleLogProvider(),
                new NHibernateMappingLoader(,
                ,
                ConnectionString.Value,
                ,
                new NullUserInfo());

        private IPersistenceTransaction CreateNHPT()
        {
            return new NHibernatePersistenceTransaction(
                
        }

        ~TestExecutionContext()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (_persistenceTransaction.IsValueCreated)
                _persistenceTransaction.Value.Dispose();
        }
    }
}
