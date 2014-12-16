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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;
using Rhetos.Extensibility;
using Rhetos.Utilities;
using Rhetos.Dom;
using Rhetos.Logging;
using Oracle.DataAccess.Client;
using Oracle.DataAccess.Types;
using NHibernate;
using NHibernate.Tool.hbm2ddl;

namespace Rhetos.Persistence.NHibernate
{
    public class NHibernatePersistenceEngine : IPersistenceEngine
    {
        private readonly ILogger _performanceLogger;
        private readonly INHibernateMapping _nHibernateMapping;
        private readonly IDomainObjectModel _domainObjectModel;
        private readonly ConnectionString _connectionString;
        private readonly IEnumerable<INHibernateConfigurationExtension> _nHibernateConfigurationExtensions;

        public NHibernatePersistenceEngine(
            ILogProvider logProvider,
            INHibernateMapping nHibernateMapping,
            IDomainObjectModel domainObjectModel,
            ConnectionString connectionString,
            IEnumerable<INHibernateConfigurationExtension> nHibernateConfigurationExtensions)
        {
            _performanceLogger = logProvider.GetLogger("Performance");
            _nHibernateMapping = nHibernateMapping;
            _domainObjectModel = domainObjectModel;
            _connectionString = connectionString;
            _nHibernateConfigurationExtensions = nHibernateConfigurationExtensions;
        }

        private ISessionFactory _sessionFactory;
        private readonly object _sessionFactoryLock = new object();

        public Tuple<ISession, ITransaction> BeginTransaction(IUserInfo userInfo)
        {
            if (_sessionFactory == null)
                _sessionFactory = PrepareNHSessionFactory();

            ISession session = _sessionFactory.OpenSession();
            ITransaction transaction = session.BeginTransaction();

            if (userInfo.IsUserRecognized)
            {
                if (SqlUtility.DatabaseLanguage == "MsSql")
                    ExecuteSqlInSession(session, MsSqlUtility.SetUserContextInfoQuery(userInfo));
                else if (SqlUtility.DatabaseLanguage == "Oracle")
                {
                    OracleSqlUtility.SetSqlUserInfo(((OracleConnection)session.Connection), userInfo);
                    ExecuteSqlInSession(session, OracleSqlUtility.SetNationalLanguageQuery());
                }
                else
                    throw new FrameworkException(DatabaseLanguageError);
            }

            return Tuple.Create(session, transaction);
        }

        private static void ExecuteSqlInSession(ISession session, string sql)
        {
            if (!string.IsNullOrEmpty(sql))
            {
                var nhQuery = session.CreateSQLQuery(sql);
                nhQuery.ExecuteUpdate();
            }
        }

        private static string DatabaseLanguageError
        {
            get
            {
                return "NHibernatePersistenceEngine does not support for database language '" + SqlUtility.DatabaseLanguage + "'."
                    + " Supported database languages are: 'MsSql', 'Oracle'.";
            }
        }

        void ForceLoadObjectModel()
        {
            if (_domainObjectModel.Assembly == null)
                throw new FrameworkException("Cannot load domain object model.");
        }

        private ISessionFactory PrepareNHSessionFactory()
        {
            lock (_sessionFactoryLock)
            {
                if (_sessionFactory != null)
                    return _sessionFactory;

                var sw = Stopwatch.StartNew();

                ForceLoadObjectModel(); // This is needed for "new Configuration()".
                var configuration = new global::NHibernate.Cfg.Configuration();
                configuration.SetProperty("connection.provider", "NHibernate.Connection.DriverConnectionProvider");
                configuration.SetProperty("connection.connection_string", _connectionString);

                // Set factory-level property:
                configuration.SetProperty("command_timeout", SqlUtility.SqlCommandTimeout.ToString());
                // Set system-level property:
                // Note: Using NHibernate.Cfg.Environment.Properties does not allow setting properties becase the public property returns a copy of the dictionary.
                var globalPropertiesField = typeof(global::NHibernate.Cfg.Environment).GetField("GlobalProperties", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                var globalProperties = (Dictionary<string, string>)globalPropertiesField.GetValue(null);
                globalProperties.Add("command_timeout", SqlUtility.SqlCommandTimeout.ToString());

                if (SqlUtility.DatabaseLanguage == "MsSql")
                {
                    configuration.SetProperty("dialect", "NHibernate.Dialect.MsSql2005Dialect");
                    configuration.SetProperty("connection.driver_class", "NHibernate.Driver.SqlClientDriver");
                }
                else if (SqlUtility.DatabaseLanguage == "Oracle")
                {
                    configuration.SetProperty("dialect", "NHibernate.Dialect.Oracle10gDialect");
                    configuration.SetProperty("connection.driver_class", "NHibernate.Driver.OracleDataClientDriver");
                }
                else
                    throw new FrameworkException(DatabaseLanguageError);

                ResolveEventHandler resolveAssembly = (s, args) => _domainObjectModel.Assembly;
                try
                {
                    AppDomain.CurrentDomain.AssemblyResolve += resolveAssembly;
                    configuration.AddXmlString(_nHibernateMapping.GetMapping());
                }
                finally
                {
                    AppDomain.CurrentDomain.AssemblyResolve -= resolveAssembly;
                }

                foreach (var configurationExtension in _nHibernateConfigurationExtensions)
                    configurationExtension.ExtendConfiguration(configuration);

                SchemaMetadataUpdater.QuoteTableAndColumns(configuration);
                var sessionFactory = configuration.BuildSessionFactory();

                _performanceLogger.Write(sw, "NHibernatePersistenceEngine.PrepareNHSessionFactory");
                return sessionFactory;
            }
        }
    }
}