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

using Microsoft.CSharp.RuntimeBinder;
using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using Rhetos.Processing;
using Rhetos.Utilities;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(InitializationConcept))]
    public class DomInitializationCodeGenerator : IConceptCodeGenerator
    {
        public static readonly string EntityFrameworkContextMembersTag = "/*EntityFrameworkContextMembers*/";
        public static readonly string EntityFrameworkContextInitializeTag = "/*EntityFrameworkContextInitialize*/";
        public static readonly string EntityFrameworkOnModelCreatingTag = "/*EntityFrameworkOnModelCreating*/";
        public static readonly string EntityFrameworkConfigurationTag = "/*EntityFrameworkConfiguration*/";
        public static readonly string CommonQueryableMemebersTag = "/*CommonQueryableMemebers*/";
        public static readonly string QueryExtensionsMembersTag = "/*QueryExtensionsMembers*/";
        public static readonly string SimpleClassesTag = "/*SimpleClasses*/";
        public static readonly string RepositoryClassesTag = "/*RepositoryClasses*/";

        public static readonly string StandardNamespacesSnippet =
@"using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Linq.Expressions;
    using System.Runtime.Serialization;
    using Rhetos.Dom.DefaultConcepts;
    using Rhetos.Utilities;";

        private readonly IConfiguration _configuration;

        public DomInitializationCodeGenerator(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (InitializationConcept)conceptInfo;

            codeBuilder.InsertCode(GenerateCommonClassesSnippet());

            codeBuilder.InsertCode("this.Configuration.UseDatabaseNullSemantics = _configuration.GetBool(\"EntityFramework.UseDatabaseNullSemantics\", false).Value;\r\n            ", EntityFrameworkContextInitializeTag);

            // Types used in the preceding code snippet:
            codeBuilder.AddReferencesFromDependency(typeof(Autofac.Module)); // Includes a reference to Autofac.dll.
            codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Extensibility.INamedPlugins<>));
            codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Utilities.IUserInfo));
            codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Utilities.ISqlExecuter));
            codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Security.IAuthorizationManager));
            codeBuilder.AddReferencesFromDependency(typeof(System.ComponentModel.Composition.ExportAttribute));
            codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Dom.DefaultConcepts.GenericRepositories));
            codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Logging.ILogProvider));
            codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Security.IWindowsSecurity));
            codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Utilities.SqlUtility));
            codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Utilities.ExceptionsUtility));
            codeBuilder.AddReferencesFromDependency(typeof(System.Data.Entity.DbContext));
            codeBuilder.AddReferencesFromDependency(typeof(System.Data.Entity.SqlServer.SqlProviderServices));
            codeBuilder.AddReferencesFromDependency(typeof(System.Data.Entity.Core.EntityClient.EntityConnection));
            codeBuilder.AddReferencesFromDependency(typeof(System.Data.Entity.Core.Metadata.Edm.MetadataWorkspace));
            codeBuilder.AddReferencesFromDependency(typeof(System.Data.Entity.Core.Objects.ObjectStateEntry));
            codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Persistence.IPersistenceCache));
            codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Persistence.IPersistenceTransaction));
            codeBuilder.AddReferencesFromDependency(typeof(ApplyFiltersOnClientRead));
            codeBuilder.AddReferencesFromDependency(typeof(ICommandInfo)); // Used from ApplyFiltersOnClientRead.
        }

        private static string GenerateCommonClassesSnippet()
        {
            return $@"
{DomGeneratorOptions.FileSplitterPrefix}{DomAssemblies.Model}{DomGeneratorOptions.FileSplitterSuffix}

{SimpleClassesTag}

namespace Common.Queryable
{{
    {StandardNamespacesSnippet}

    {CommonQueryableMemebersTag}
}}

namespace Rhetos.Dom.DefaultConcepts
{{
    {StandardNamespacesSnippet}

    public static class QueryExtensions
    {{
        {QueryExtensionsMembersTag}

        /// <summary>
        /// A specific overload of the 'ToSimple' method cannot be targeted from a generic method using generic type.
        /// This method uses reflection instead to find the specific 'ToSimple' method.
        /// </summary>
        public static IQueryable<TEntity> GenericToSimple<TEntity>(this IQueryable<IEntity> i)
            where TEntity : class, IEntity
	    {{
            var method = typeof(QueryExtensions).GetMethod(""ToSimple"", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public, null, new Type[] {{ i.GetType() }}, null);
            if (method == null)
                throw new Rhetos.FrameworkException(""Cannot find 'ToSimple' method for argument type '"" + i.GetType().ToString() + ""'."");
            return (IQueryable<TEntity>)Rhetos.Utilities.ExceptionsUtility.InvokeEx(method, null, new object[] {{ i }});
        }}

        /// <summary>Converts the objects to simple object and the IEnumerable to List or Array, if not already.</summary>
        public static void LoadSimpleObjects<TEntity>(ref IEnumerable<TEntity> items)
            where TEntity: class, IEntity
        {{
            var query = items as IQueryable<IQueryableEntity<TEntity>>;
            var navigationItems = items as IEnumerable<IQueryableEntity<TEntity>>;

            if (query != null)
                items = query.GenericToSimple<TEntity>().ToList(); // The IQueryable function allows ORM optimizations.
            else if (navigationItems != null)
                items = navigationItems.Select(item => item.ToSimple()).ToList();
            else
            {{
                Rhetos.Utilities.CsUtility.Materialize(ref items);
                var itemsList = (IList<TEntity>)items;
                for (int i = 0; i < itemsList.Count(); i++)
                {{
                    var navigationItem = itemsList[i] as IQueryableEntity<TEntity>;
                    if (navigationItem != null)
                        itemsList[i] = navigationItem.ToSimple();
                }}
            }}
        }}
    }}
}}

{DomGeneratorOptions.FileSplitterPrefix}{DomAssemblies.Orm}{DomGeneratorOptions.FileSplitterSuffix}

namespace Common
{{
    {StandardNamespacesSnippet}
    using Autofac;
    {ModuleCodeGenerator.CommonUsingTag}

    public class EntityFrameworkContext : System.Data.Entity.DbContext, Rhetos.Persistence.IPersistenceCache
    {{
        private readonly Rhetos.Utilities.IConfiguration _configuration;

        public EntityFrameworkContext(
            Rhetos.Persistence.IPersistenceTransaction persistenceTransaction,
            Rhetos.Dom.DefaultConcepts.Persistence.EntityFrameworkMetadata metadata,
            EntityFrameworkConfiguration entityFrameworkConfiguration, // EntityFrameworkConfiguration is provided as an IoC dependency for EntityFrameworkContext in order to initialize the global DbConfiguration before using DbContext.
            Rhetos.Utilities.IConfiguration configuration)
            : base(new System.Data.Entity.Core.EntityClient.EntityConnection(metadata.MetadataWorkspace, persistenceTransaction.Connection), false)
        {{
            _configuration = configuration;
            Initialize();
            Database.UseTransaction(persistenceTransaction.Transaction);
        }}

        /// <summary>
        /// This constructor is used at deployment-time to create slow EntityFrameworkContext instance before the metadata files are generated.
        /// The instance is used by EntityFrameworkGenerateMetadataFiles to generate the metadata files.
        /// </summary>
        protected EntityFrameworkContext(
            System.Data.Common.DbConnection connection,
            EntityFrameworkConfiguration entityFrameworkConfiguration, // EntityFrameworkConfiguration is provided as an IoC dependency for EntityFrameworkContext in order to initialize the global DbConfiguration before using DbContext.
            Rhetos.Utilities.IConfiguration configuration)
            : base(connection, true)
        {{
            _configuration = configuration;
            Initialize();
        }}

        private void Initialize()
        {{
            System.Data.Entity.Database.SetInitializer<EntityFrameworkContext>(null); // Prevent EF from creating database objects.

            {EntityFrameworkContextInitializeTag}

            this.Database.CommandTimeout = Rhetos.Utilities.SqlUtility.SqlCommandTimeout;
        }}

        public void ClearCache()
        {{
            SetDetaching(true);
            try
            {{
                Configuration.AutoDetectChangesEnabled = false;
                var trackedItems = ChangeTracker.Entries().ToList();
                foreach (var item in trackedItems)
                    Entry(item.Entity).State = System.Data.Entity.EntityState.Detached;
                Configuration.AutoDetectChangesEnabled = true;
            }}
            finally
            {{
                SetDetaching(false);
            }}
        }}

        private void SetDetaching(bool detaching)
        {{
            foreach (var item in ChangeTracker.Entries().Select(entry => entry.Entity).OfType<IDetachOverride>())
                item.Detaching = detaching;
        }}

        protected override void OnModelCreating(System.Data.Entity.DbModelBuilder modelBuilder)
        {{
            {EntityFrameworkOnModelCreatingTag}
        }}

        {EntityFrameworkContextMembersTag}
    }}

    public class EntityFrameworkConfiguration : System.Data.Entity.DbConfiguration
    {{
        public EntityFrameworkConfiguration()
        {{
            SetProviderServices(""System.Data.SqlClient"", System.Data.Entity.SqlServer.SqlProviderServices.Instance);

            {EntityFrameworkConfigurationTag}

            System.Data.Entity.DbConfiguration.SetConfiguration(this);
        }}
    }}
}}

{DomGeneratorOptions.FileSplitterPrefix}{DomAssemblies.Repositories}{DomGeneratorOptions.FileSplitterSuffix}

namespace Common
{{
    {StandardNamespacesSnippet}
    using Autofac;
    {ModuleCodeGenerator.CommonUsingTag}

    public class DomRepository
    {{
        private readonly Rhetos.Extensibility.INamedPlugins<IRepository> _repositories;

        public DomRepository(Rhetos.Extensibility.INamedPlugins<IRepository> repositories)
        {{
            _repositories = repositories;
        }}

        {ModuleCodeGenerator.CommonDomRepositoryMembersTag}
    }}

    public static class Infrastructure
    {{
        public static readonly RegisteredInterfaceImplementations RegisteredInterfaceImplementationName = new RegisteredInterfaceImplementations
        {{
            {ModuleCodeGenerator.RegisteredInterfaceImplementationNameTag}
        }};

        public static readonly ApplyFiltersOnClientRead ApplyFiltersOnClientRead = new ApplyFiltersOnClientRead
        {{
            {ModuleCodeGenerator.ApplyFiltersOnClientReadTag}
        }};

        {ModuleCodeGenerator.CommonInfrastructureMembersTag}
    }}

    public class ExecutionContext
    {{
        protected Lazy<Rhetos.Persistence.IPersistenceTransaction> _persistenceTransaction;
        public Rhetos.Persistence.IPersistenceTransaction PersistenceTransaction {{ get {{ return _persistenceTransaction.Value; }} }}

        protected Lazy<Rhetos.Utilities.IUserInfo> _userInfo;
        public Rhetos.Utilities.IUserInfo UserInfo {{ get {{ return _userInfo.Value; }} }}

        protected Lazy<Rhetos.Utilities.ISqlExecuter> _sqlExecuter;
        public Rhetos.Utilities.ISqlExecuter SqlExecuter {{ get {{ return _sqlExecuter.Value; }} }}

        protected Lazy<Rhetos.Security.IAuthorizationManager> _authorizationManager;
        public Rhetos.Security.IAuthorizationManager AuthorizationManager {{ get {{ return _authorizationManager.Value; }} }}

        protected Lazy<Rhetos.Dom.DefaultConcepts.GenericRepositories> _genericRepositories;
        public Rhetos.Dom.DefaultConcepts.GenericRepositories GenericRepositories {{ get {{ return _genericRepositories.Value; }} }}

        public Rhetos.Dom.DefaultConcepts.GenericRepository<TEntity> GenericRepository<TEntity>() where TEntity : class, IEntity
        {{
            return GenericRepositories.GetGenericRepository<TEntity>();
        }}

        public Rhetos.Dom.DefaultConcepts.GenericRepository<TEntity> GenericRepository<TEntity>(string entityName) where TEntity : class, IEntity
        {{
            return GenericRepositories.GetGenericRepository<TEntity>(entityName);
        }}

        public Rhetos.Dom.DefaultConcepts.GenericRepository<IEntity> GenericRepository(string entityName)
        {{
            return GenericRepositories.GetGenericRepository(entityName);
        }}

        protected Lazy<Common.DomRepository> _repository;
        public Common.DomRepository Repository {{ get {{ return _repository.Value; }} }}

        public Rhetos.Logging.ILogProvider LogProvider {{ get; private set; }}

        protected Lazy<Rhetos.Security.IWindowsSecurity> _windowsSecurity;
        public Rhetos.Security.IWindowsSecurity WindowsSecurity {{ get {{ return _windowsSecurity.Value; }} }}

        public EntityFrameworkContext EntityFrameworkContext {{ get; private set; }}

        {ModuleCodeGenerator.ExecutionContextMemberTag}

        // This constructor is used for automatic parameter injection with autofac.
        public ExecutionContext(
            Lazy<Rhetos.Persistence.IPersistenceTransaction> persistenceTransaction,
            Lazy<Rhetos.Utilities.IUserInfo> userInfo,
            Lazy<Rhetos.Utilities.ISqlExecuter> sqlExecuter,
            Lazy<Rhetos.Security.IAuthorizationManager> authorizationManager,
            Lazy<Rhetos.Dom.DefaultConcepts.GenericRepositories> genericRepositories,
            Lazy<Common.DomRepository> repository,
            Rhetos.Logging.ILogProvider logProvider,
            Lazy<Rhetos.Security.IWindowsSecurity> windowsSecurity{ModuleCodeGenerator.ExecutionContextConstructorArgumentTag},
            EntityFrameworkContext entityFrameworkContext)
        {{
            _persistenceTransaction = persistenceTransaction;
            _userInfo = userInfo;
            _sqlExecuter = sqlExecuter;
            _authorizationManager = authorizationManager;
            _genericRepositories = genericRepositories;
            _repository = repository;
            LogProvider = logProvider;
            _windowsSecurity = windowsSecurity;
            EntityFrameworkContext = entityFrameworkContext;
            {ModuleCodeGenerator.ExecutionContextConstructorAssignmentTag}
        }}

        // This constructor is used for manual context creation (unit testing)
        public ExecutionContext()
        {{
        }}
    }}

    [System.ComponentModel.Composition.Export(typeof(Autofac.Module))]
    public class AutofacModuleConfiguration : Autofac.Module
    {{
        protected override void Load(Autofac.ContainerBuilder builder)
        {{
            builder.RegisterType<DomRepository>().InstancePerLifetimeScope();
            builder.RegisterType<EntityFrameworkConfiguration>().SingleInstance();
            builder.RegisterType<EntityFrameworkContext>()
                .As<EntityFrameworkContext>()
                .As<System.Data.Entity.DbContext>()
                .As<Rhetos.Persistence.IPersistenceCache>()
                .InstancePerLifetimeScope();
            builder.RegisterType<ExecutionContext>().InstancePerLifetimeScope();
            builder.RegisterInstance(Infrastructure.RegisteredInterfaceImplementationName).ExternallyOwned();
            builder.RegisterInstance(Infrastructure.ApplyFiltersOnClientRead).ExternallyOwned();
            {ModuleCodeGenerator.CommonAutofacConfigurationMembersTag}

            base.Load(builder);
        }}
    }}

    public abstract class RepositoryBase : IRepository
    {{
        protected Common.DomRepository _domRepository;
        protected Common.ExecutionContext _executionContext;
    }}

    public abstract class ReadableRepositoryBase<TEntity> : RepositoryBase, IReadableRepository<TEntity>
        where TEntity : class, IEntity
    {{
        public IEnumerable<TEntity> Load(object parameter, Type parameterType)
        {{
            var items = _executionContext.GenericRepository(typeof(TEntity).FullName)
                .Load(parameter, parameterType);
            return (IEnumerable<TEntity>)items;
        }}

        public IEnumerable<TEntity> Read(object parameter, Type parameterType, bool preferQuery)
        {{
            var items = _executionContext.GenericRepository(typeof(TEntity).FullName)
                .Read(parameter, parameterType, preferQuery);
            return (IEnumerable<TEntity>)items;
        }}

        [Obsolete(""Use Load() or Query() method."")]
        public abstract TEntity[] All();

        [Obsolete(""Use Load() or Query() method."")]
        public TEntity[] Filter(FilterAll filterAll)
        {{
            return All();
        }}
    }}

    public abstract class QueryableRepositoryBase<TQueryableEntity, TEntity> : ReadableRepositoryBase<TEntity>, IQueryableRepository<TQueryableEntity, TEntity>
        where TEntity : class, IEntity
        where TQueryableEntity : class, IEntity, TEntity, IQueryableEntity<TEntity>
    {{
        [Obsolete(""Use Load(identifiers) or Query(identifiers) method."")]
        public TEntity[] Filter(IEnumerable<Guid> identifiers)
        {{
            const int BufferSize = 500; // EF 6.1.3 LINQ query has O(n^2) time complexity. Batch size of 500 results with optimal total time on the test system.
            int n = identifiers.Count();
            var result = new List<TEntity>(n);
            for (int i = 0; i < (n + BufferSize - 1) / BufferSize; i++)
            {{
                Guid[] idBuffer = identifiers.Skip(i * BufferSize).Take(BufferSize).ToArray();
                List<TEntity> itemBuffer;
                if (idBuffer.Count() == 1) // EF 6.1.3. does not use parametrized SQL query for Contains() function. The equality comparer is used instead, to reuse cached execution plans.
                {{
                    Guid id = idBuffer.Single();
                    itemBuffer = Query().Where(item => item.ID == id).GenericToSimple<TEntity>().ToList();
                }}
                else
                    itemBuffer = Query().Where(item => idBuffer.Contains(item.ID)).GenericToSimple<TEntity>().ToList();
                result.AddRange(itemBuffer);
            }}
            return result.ToArray();
        }}

        public abstract IQueryable<TQueryableEntity> Query();

        // LINQ to Entity does not support Query() method in subqueries.
        public IQueryable<TQueryableEntity> Subquery {{ get {{ return Query(); }} }}

        public IQueryable<TQueryableEntity> Query(object parameter, Type parameterType)
        {{
            var query = _executionContext.GenericRepository(typeof(TEntity).FullName).Query(parameter, parameterType);
            return (IQueryable<TQueryableEntity>)query;
        }}
    }}

    public abstract class OrmRepositoryBase<TQueryableEntity, TEntity> : QueryableRepositoryBase<TQueryableEntity, TEntity>
        where TEntity : class, IEntity
        where TQueryableEntity : class, IEntity, TEntity, IQueryableEntity<TEntity>
    {{
        public IQueryable<TQueryableEntity> Filter(IQueryable<TQueryableEntity> items, IEnumerable<Guid> ids)
        {{
            if (!(ids is System.Collections.IList))
                ids = ids.ToList();

            if (ids.Count() == 1) // EF 6.1.3. does not use parametrized SQL query for Contains() function. The equality comparer is used instead, to reuse cached execution plans.
            {{
                Guid id = ids.Single();
                return items.Where(item => item.ID == id);
            }}
            else
            {{
                // Depending on the ids count, this method will return the list of IDs, or insert the ids to the database and return an SQL query that selects the ids.
                var idsQuery = _domRepository.Common.FilterId.CreateQueryableFilterIds(ids);
                return items.Where(item => idsQuery.Contains(item.ID));
            }}
        }}
    }}

    {ModuleCodeGenerator.CommonNamespaceMembersTag}
}}

{RepositoryClassesTag}";
        }
    }
}
