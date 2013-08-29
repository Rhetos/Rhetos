<Query Kind="Program">
  <Reference Relative="bin\Iesi.Collections.dll">C:\Projects\Core\Rhetos\Source\Rhetos\bin\Iesi.Collections.dll</Reference>
  <Reference Relative="bin\NHibernate.dll">C:\Projects\Core\Rhetos\Source\Rhetos\bin\NHibernate.dll</Reference>
  <Reference Relative="bin\NLog.dll">C:\Projects\Core\Rhetos\Source\Rhetos\bin\NLog.dll</Reference>
  <Reference Relative="bin\Oracle.DataAccess.dll">C:\Projects\Core\Rhetos\Source\Rhetos\bin\Oracle.DataAccess.dll</Reference>
  <Reference Relative="bin\Plugins\Rhetos.Dom.DefaultConcepts.dll">C:\Projects\Core\Rhetos\Source\Rhetos\bin\Plugins\Rhetos.Dom.DefaultConcepts.dll</Reference>
  <Reference Relative="bin\Plugins\Rhetos.Dom.DefaultConcepts.Interfaces.dll">C:\Projects\Core\Rhetos\Source\Rhetos\bin\Plugins\Rhetos.Dom.DefaultConcepts.Interfaces.dll</Reference>
  <Reference Relative="bin\Rhetos.Dom.Interfaces.dll">C:\Projects\Core\Rhetos\Source\Rhetos\bin\Rhetos.Dom.Interfaces.dll</Reference>
  <Reference Relative="bin\Rhetos.Interfaces.dll">C:\Projects\Core\Rhetos\Source\Rhetos\bin\Rhetos.Interfaces.dll</Reference>
  <Reference Relative="bin\Rhetos.Logging.Interfaces.dll">C:\Projects\Core\Rhetos\Source\Rhetos\bin\Rhetos.Logging.Interfaces.dll</Reference>
  <Reference Relative="bin\Rhetos.Persistence.Interfaces.dll">C:\Projects\Core\Rhetos\Source\Rhetos\bin\Rhetos.Persistence.Interfaces.dll</Reference>
  <Reference Relative="bin\Rhetos.Persistence.NHibernate.dll">C:\Projects\Core\Rhetos\Source\Rhetos\bin\Rhetos.Persistence.NHibernate.dll</Reference>
  <Reference Relative="bin\Plugins\Rhetos.Persistence.NHibernateDefaultConcepts.dll">C:\Projects\Core\Rhetos\Source\Rhetos\bin\Plugins\Rhetos.Persistence.NHibernateDefaultConcepts.dll</Reference>
  <Reference Relative="bin\Plugins\Rhetos.Processing.DefaultCommands.Interfaces.dll">C:\Projects\Core\Rhetos\Source\Rhetos\bin\Plugins\Rhetos.Processing.DefaultCommands.Interfaces.dll</Reference>
  <Reference Relative="bin\Rhetos.Processing.Interfaces.dll">C:\Projects\Core\Rhetos\Source\Rhetos\bin\Rhetos.Processing.Interfaces.dll</Reference>
  <Reference Relative="bin\Rhetos.Security.Interfaces.dll">C:\Projects\Core\Rhetos\Source\Rhetos\bin\Rhetos.Security.Interfaces.dll</Reference>
  <Reference Relative="bin\Rhetos.Utilities.dll">C:\Projects\Core\Rhetos\Source\Rhetos\bin\Rhetos.Utilities.dll</Reference>
  <Reference Relative="bin\ServerDom.dll">C:\Projects\Core\Rhetos\Source\Rhetos\bin\ServerDom.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.DirectoryServices.AccountManagement.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.DirectoryServices.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\System.Runtime.Serialization.dll</Reference>
  <Namespace>NHibernate</Namespace>
  <Namespace>NHibernate.Cfg</Namespace>
  <Namespace>NHibernate.Tool.hbm2ddl</Namespace>
  <Namespace>Oracle.DataAccess.Client</Namespace>
  <Namespace>Rhetos.Dom</Namespace>
  <Namespace>Rhetos.Dom.DefaultConcepts</Namespace>
  <Namespace>Rhetos.Persistence</Namespace>
  <Namespace>Rhetos.Persistence.NHibernate</Namespace>
  <Namespace>Rhetos.Persistence.NHibernateDefaultConcepts</Namespace>
  <Namespace>Rhetos.Security</Namespace>
  <Namespace>Rhetos.Utilities</Namespace>
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
                try
                {
                    SqlUtility.LoadSpecificConnectionString(configFile);
                }
                catch (Exception ex)
                {
                    if (!ex.Message.Contains("Connection string is already loaded"))
                        throw;
                }

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