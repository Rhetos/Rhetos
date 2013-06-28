<Query Kind="Program">
  <Reference Relative="bin\Iesi.Collections.dll">C:\Projects\Core\Rhetos\Source\Rhetos\bin\Iesi.Collections.dll</Reference>
  <Reference Relative="bin\NHibernate.dll">C:\Projects\Core\Rhetos\Source\Rhetos\bin\NHibernate.dll</Reference>
  <Reference Relative="bin\NLog.dll">C:\Projects\Core\Rhetos\Source\Rhetos\bin\NLog.dll</Reference>
  <Reference Relative="bin\Oracle.DataAccess.dll">C:\Projects\Core\Rhetos\Source\Rhetos\bin\Oracle.DataAccess.dll</Reference>
  <Reference Relative="bin\Rhetos.Utilities.dll">C:\Projects\Core\Rhetos\Source\Rhetos\bin\Rhetos.Utilities.dll</Reference>
  <Reference Relative="bin\Plugins\Rhetos.Dom.DefaultConcepts.dll">C:\Projects\Core\Rhetos\Source\Rhetos\bin\Plugins\Rhetos.Dom.DefaultConcepts.dll</Reference>
  <Reference Relative="bin\Plugins\Rhetos.Dom.DefaultConcepts.Interfaces.dll">C:\Projects\Core\Rhetos\Source\Rhetos\bin\Plugins\Rhetos.Dom.DefaultConcepts.Interfaces.dll</Reference>
  <Reference Relative="bin\Rhetos.Logging.Interfaces.dll">C:\Projects\Core\Rhetos\Source\Rhetos\bin\Rhetos.Logging.Interfaces.dll</Reference>
  <Reference Relative="bin\Rhetos.Persistence.Interfaces.dll">C:\Projects\Core\Rhetos\Source\Rhetos\bin\Rhetos.Persistence.Interfaces.dll</Reference>
  <Reference Relative="bin\Rhetos.Persistence.NHibernate.dll">C:\Projects\Core\Rhetos\Source\Rhetos\bin\Rhetos.Persistence.NHibernate.dll</Reference>
  <Reference Relative="bin\Plugins\Rhetos.Persistence.NHibernateDefaultConcepts.dll">C:\Projects\Core\Rhetos\Source\Rhetos\bin\Plugins\Rhetos.Persistence.NHibernateDefaultConcepts.dll</Reference>
  <Reference Relative="bin\Plugins\Rhetos.Processing.DefaultCommands.Interfaces.dll">C:\Projects\Core\Rhetos\Source\Rhetos\bin\Plugins\Rhetos.Processing.DefaultCommands.Interfaces.dll</Reference>
  <Reference Relative="bin\Rhetos.Processing.Interfaces.dll">C:\Projects\Core\Rhetos\Source\Rhetos\bin\Rhetos.Processing.Interfaces.dll</Reference>
  <Reference Relative="bin\Rhetos.Security.Interfaces.dll">C:\Projects\Core\Rhetos\Source\Rhetos\bin\Rhetos.Security.Interfaces.dll</Reference>
  <Reference Relative="bin\Rhetos.Interfaces.dll">C:\Projects\Core\Rhetos\Source\Rhetos\bin\Rhetos.Interfaces.dll</Reference>
  <Reference Relative="bin\ServerDom.dll">C:\Projects\Core\Rhetos\Source\Rhetos\bin\ServerDom.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.DirectoryServices.AccountManagement.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.DirectoryServices.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Runtime.Serialization.dll</Reference>
  <Namespace>NHibernate</Namespace>
  <Namespace>NHibernate.Cfg</Namespace>
  <Namespace>NHibernate.Tool.hbm2ddl</Namespace>
  <Namespace>Oracle.DataAccess.Client</Namespace>
  <Namespace>Rhetos.Utilities</Namespace>
  <Namespace>Rhetos.Dom.DefaultConcepts</Namespace>
  <Namespace>Rhetos.Persistence</Namespace>
  <Namespace>Rhetos.Persistence.NHibernate</Namespace>
  <Namespace>Rhetos.Security</Namespace>
  <Namespace>System</Namespace>
  <Namespace>System.Collections.Generic</Namespace>
  <Namespace>System.DirectoryServices</Namespace>
  <Namespace>System.DirectoryServices.AccountManagement</Namespace>
  <Namespace>System.IO</Namespace>
  <Namespace>System.Linq</Namespace>
  <Namespace>System.Reflection</Namespace>
  <Namespace>System.Runtime.Serialization.Json</Namespace>
  <Namespace>System.Text</Namespace>
  <Namespace>System.Xml</Namespace>
  <Namespace>System.Xml.Serialization</Namespace>
</Query>

void Main()
{
	bool commitChanges = false;
	string rhetosServerPath = Path.GetDirectoryName(Util.CurrentQueryPath); // LinqPad script path
	using (var executionContext = new TestExecutionContext(commitChanges, rhetosServerPath))
	{
		var repository = new Common.DomRepository(executionContext);
		
		// PRINT 3 CLAIMS:
		var claimsAll = repository.Common.Claim.Query();
		claimsAll.Take(3).Dump();
		
		// ADD AND REMOVE A PRINCIPAL:
		var testUser = new Common.Principal { Name = "Test123ABC", ID = Guid.NewGuid() };
		repository.Common.Principal.Insert(new[] { testUser });
		repository.Common.Principal.Delete(new[] { testUser });
	
		// PRINT LAST 5 RECORDS IN SYSTEM LOG OF Common.Principal:
		repository.Common.Log.Query()
			.Where(log => log.TableName == "Common.Principal")
			.OrderByDescending(log => log.Created)
			.Take(5).Dump();
	}
}


//=================================================
// HELPER FUNCTIONS AND CLASSES:

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
            _nHibernateSession = new Lazy<ISession>(() => new NHibernatePersistenceTransaction(NhSession, NhTransaction, new ConsoleLogProvider()).NHibernateSession);
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
				try
				{
                	SqlUtility.LoadSpecificConnectionString(configFile);
				}
				catch (Exception ex)
				{
					if (ex.Message != "Cannot execute LoadSpecificConnectionString: Connection string is already loaded.")
						throw;
				}
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

        #region LAZY INITIALIZATION

        private Lazy<string> ConnectionString = new Lazy<string>(() => SqlUtility.ConnectionString);

        private static ISessionFactory _nhSessionFactory; // Static caching of NhSessionFactory to improve performance.
        private static string _nhSessionFactory_LastConnectionString;
        private static string _nhSessionFactory_LastNHibernateMappingFolder;
        private ISessionFactory NhSessionFactory
        {
            get
            {
                if (!ConnectionString.Value.Equals(_nhSessionFactory_LastConnectionString, StringComparison.OrdinalIgnoreCase)
                    || !_nHibernateMappingFolder.Equals(_nhSessionFactory_LastNHibernateMappingFolder, StringComparison.OrdinalIgnoreCase))
                {
                    _nhSessionFactory = CreateNHibernateSessionFactory(ConnectionString.Value, _nHibernateMappingFolder);
                    _nhSessionFactory_LastConnectionString = ConnectionString.Value;
                    _nhSessionFactory_LastNHibernateMappingFolder = _nHibernateMappingFolder;
                }
                return _nhSessionFactory;
            }
        }

        private ISession _nhSession;
        private ISession NhSession
        {
            get { return _nhSession ?? (_nhSession = NhSessionFactory.OpenSession()); }
        }

        private ITransaction _nhTransaction;
        private ITransaction NhTransaction
        {
            get
            {
                if (_nhTransaction == null)
                {
                    _nhTransaction = NhSession.BeginTransaction();
                    if (UserInfo.IsUserRecognized)
                    {
                        if (SqlUtility.DatabaseLanguage == "MsSql")
                        {
                            var userContextQuery = NhSession.CreateSQLQuery(MsSqlUtility.SetUserContextInfoQuery(UserInfo));
                            userContextQuery.ExecuteUpdate();
                        }
                        else if (SqlUtility.DatabaseLanguage == "Oracle")
                        {
                            ((OracleConnection)NhSession.Connection).ClientInfo = SqlUtility.UserContextInfoText(UserInfo);
                        }
                        else
                            throw new Rhetos.FrameworkException(DatabaseLanguageError);
                    }
                }
                return _nhTransaction;
            }
        }

        private static ISessionFactory CreateNHibernateSessionFactory(string connectionString, string nHibernateMappingFolder)
        {
            var configuration = new Configuration();
            configuration.SetProperty("connection.provider", "NHibernate.Connection.DriverConnectionProvider");
            configuration.SetProperty("connection.connection_string", connectionString);

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
                throw new Rhetos.FrameworkException(DatabaseLanguageError);

            var mappingFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, nHibernateMappingFolder, "ServerDomNHibernateMapping.xml");
            configuration.AddFile(mappingFile);

            var configurationExtension1 = new Rhetos.Persistence.NHibernateDefaultConcepts.CommonConceptsNHibernateConfigurationExtension();
            configurationExtension1.ExtendConfiguration(configuration);

            SchemaMetadataUpdater.QuoteTableAndColumns(configuration);
            return configuration.BuildSessionFactory();
        }

        private static string DatabaseLanguageError
        {
            get
            {
                return "NHibernatePersistenceEngine does not support for database language '" + SqlUtility.DatabaseLanguage + "'."
                    + " Supported database languages are: 'MsSql', 'Oracle'.";
            }
        }

        #endregion

        ~TestExecutionContext()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (_nhTransaction != null)
            {
                if (_commitChanges)
                    _nhTransaction.Commit();
                else
                    if (_nhTransaction.IsActive)
                        try { _nhTransaction.Rollback(); } catch {}
                _nhTransaction.Dispose();
                _nhTransaction = null;
            }
            if (_nhSession != null)
            {
                if (_nhSession.IsOpen)
                    try { _nhSession.Close(); } catch {}
                if (_nhSession.IsConnected)
                    try { _nhSession.Disconnect(); } catch {}
                _nhSession.Dispose();
                _nhSession = null;
            }
        }
    }
	
    public class TestUserInfo : IUserInfo
    {
        public TestUserInfo()
        {
            UserName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            Workstation = System.Environment.MachineName;
            IsUserRecognized = true;
        }

        public bool IsUserRecognized { get; private set; }
        public string UserName { get; private set; }
        public string Workstation { get; private set; }
    }