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

using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Extensibility;
using System.ComponentModel.Composition;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(InitializationConcept))]
    public class DomInitializationCodeGenerator : IConceptCodeGenerator
    {
        public static readonly string EntityFrameworkContextMembersTag = "/*EntityFrameworkContextMembers*/";
        public static readonly string EntityFrameworkContextInitializeTag = "/*EntityFrameworkContextInitialize*/";
        public static readonly string EntityFrameworkConfigurationTag = "/*EntityFrameworkConfiguration*/";
        public static readonly string QueryExtensionsMembersTag = "/*QueryExtensionsMembers*/";
        public static readonly string RepositoryBaseMembersTag = "/*RepositoryBaseMembers*/";
        public static readonly string ReadableRepositoryBaseMembersTag = "/*ReadableRepositoryBaseMembers*/";
        public static readonly string QueryableRepositoryBaseMembersTag = "/*QueryableRepositoryBaseMembers*/";
        public static readonly string OrmRepositoryBaseMembersTag = "/*OrmRepositoryBaseMembers*/";

        private readonly CommonConceptsOptions _commonConceptsOptions;
        private readonly CommonConceptsDatabaseSettings _databaseSettings;

        public DomInitializationCodeGenerator(CommonConceptsOptions commonConceptsOptions, CommonConceptsDatabaseSettings databaseSettings)
        {
            _commonConceptsOptions = commonConceptsOptions;
            _databaseSettings = databaseSettings;
        }

        public static readonly string StandardNamespacesSnippet =
@"using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Linq.Expressions;
    using System.Runtime.Serialization;
    using Rhetos.Dom.DefaultConcepts;
    using Rhetos.Utilities;";

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            codeBuilder.InsertCodeToFile(GetModelSnippet(), $"{GeneratedSourceDirectories.Model}\\QueryExtensions");
            codeBuilder.InsertCodeToFile(GetOrmSnippet(), "EntityFrameworkContext");
            codeBuilder.InsertCodeToFile(GetRepositoriesSnippet(), GeneratedSourceDirectories.Repositories.ToString());

            codeBuilder.InsertCode("this.Configuration.UseDatabaseNullSemantics = _rhetosAppOptions.EntityFrameworkUseDatabaseNullSemantics;\r\n            ", EntityFrameworkContextInitializeTag);
        }

        public static string DisableWarnings(CommonConceptsOptions commonConceptsOptions)
        {
            return commonConceptsOptions.CompilerWarningsInGeneratedCode ? "" : $"#pragma warning disable // See configuration setting {ConfigurationProvider.GetKey((CommonConceptsOptions o) => o.CompilerWarningsInGeneratedCode)}.\r\n\r\n    ";
        }

        public static string RestoreWarnings(CommonConceptsOptions commonConceptsOptions)
        {
            return commonConceptsOptions.CompilerWarningsInGeneratedCode ? "" : $"\r\n\r\n    #pragma warning restore // See configuration setting {ConfigurationProvider.GetKey((CommonConceptsOptions o) => o.CompilerWarningsInGeneratedCode)}.";
        }

        private string GetModelSnippet() =>
$@"namespace System.Linq
{{
    {DisableWarnings(_commonConceptsOptions)}{StandardNamespacesSnippet}

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
            if (items is IQueryable<IQueryableEntity<TEntity>> query)
                items = query.GenericToSimple<TEntity>().ToList(); // The IQueryable function allows ORM optimizations.
            else if (items is IEnumerable<IQueryableEntity<TEntity>> navigationItems)
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
    }}{RestoreWarnings(_commonConceptsOptions)}
}}
";

        private string GetOrmSnippet() =>
$@"namespace Common
{{
    {DisableWarnings(_commonConceptsOptions)}{StandardNamespacesSnippet}
    using Autofac;
    {ModuleCodeGenerator.CommonUsingTag}

    public class EntityFrameworkContext : System.Data.Entity.DbContext, Rhetos.Persistence.IPersistenceCache
    {{
        private readonly Rhetos.Utilities.RhetosAppOptions _rhetosAppOptions;

        public EntityFrameworkContext(
            Rhetos.Persistence.IPersistenceTransaction persistenceTransaction,
            Rhetos.Dom.DefaultConcepts.Persistence.EntityFrameworkMetadata metadata,
            System.Data.Entity.DbConfiguration entityFrameworkConfiguration, // EntityFrameworkConfiguration is provided as an IoC dependency for EntityFrameworkContext in order to initialize the global DbConfiguration before using DbContext.
            Rhetos.Utilities.RhetosAppOptions rhetosAppOptions)
            : base(new System.Data.Entity.Core.EntityClient.EntityConnection(metadata.MetadataWorkspace, persistenceTransaction.Connection), false)
        {{
            _rhetosAppOptions = rhetosAppOptions;
            Initialize();
            Database.UseTransaction(persistenceTransaction.Transaction);
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
    }}{RestoreWarnings(_commonConceptsOptions)}
}}
";

        private string GetRepositoriesSnippet() =>
$@"namespace Common
{{
    {DisableWarnings(_commonConceptsOptions)}{StandardNamespacesSnippet}
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
        public static readonly RegisteredInterfaceImplementations RegisteredInterfaceImplementations = new RegisteredInterfaceImplementations
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
            Rhetos.Logging.ILogProvider logProvider{ModuleCodeGenerator.ExecutionContextConstructorArgumentTag},
            EntityFrameworkContext entityFrameworkContext)
        {{
            _persistenceTransaction = persistenceTransaction;
            _userInfo = userInfo;
            _sqlExecuter = sqlExecuter;
            _authorizationManager = authorizationManager;
            _genericRepositories = genericRepositories;
            _repository = repository;
            LogProvider = logProvider;
            EntityFrameworkContext = entityFrameworkContext;
            {ModuleCodeGenerator.ExecutionContextConstructorAssignmentTag}
        }}

        // This constructor is used for manual context creation (unit testing)
        public ExecutionContext()
        {{
        }}
    }}

    [System.ComponentModel.Composition.Export(typeof(Autofac.Module))]
    [System.ComponentModel.Composition.ExportMetadata(Rhetos.Extensibility.MefProvider.DependsOn, typeof(Rhetos.Dom.DefaultConcepts.AutofacModuleConfiguration))] // Overrides some registrations from that class.
    public class AutofacModuleConfiguration : Autofac.Module
    {{
        protected override void Load(Autofac.ContainerBuilder builder)
        {{
            builder.RegisterType<DomRepository>().InstancePerLifetimeScope();
            builder.RegisterType<EntityFrameworkConfiguration>()
                .As<System.Data.Entity.DbConfiguration>()
                .SingleInstance();
            builder.RegisterType<EntityFrameworkContext>()
                .As<EntityFrameworkContext>()
                .As<System.Data.Entity.DbContext>()
                .As<Rhetos.Persistence.IPersistenceCache>()
                .InstancePerLifetimeScope();
            builder.RegisterType<ExecutionContext>().InstancePerLifetimeScope();
            builder.RegisterInstance(Infrastructure.RegisteredInterfaceImplementations).ExternallyOwned();
            builder.RegisterInstance(Infrastructure.ApplyFiltersOnClientRead).ExternallyOwned();
            builder.RegisterInstance(new Rhetos.Dom.DefaultConcepts.CommonConceptsDatabaseSettings
            {{
                UseLegacyMsSqlDateTime = {_databaseSettings.UseLegacyMsSqlDateTime.ToString().ToLowerInvariant()},
                DateTimePrecision = {_databaseSettings.DateTimePrecision},
            }});
            builder.Register<CommonConceptsOptions>(context => throw new NotImplementedException($""{{nameof(CommonConceptsOptions)}} is a build-time configuration, not available at run-time.""));
            
            {ModuleCodeGenerator.CommonAutofacConfigurationMembersTag}

            base.Load(builder);
        }}
    }}

    public abstract class RepositoryBase : IRepository
    {{
        protected Common.DomRepository _domRepository;
        protected Common.ExecutionContext _executionContext;

        {RepositoryBaseMembersTag}
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

        public abstract TEntity[] Load();

        [Obsolete(""Use Load() or Query() method."")]
        public TEntity[] All()
        {{
            return Load();
        }}

        public TEntity[] Load(FilterAll filterAll)
        {{
            return Load();
        }}

        [Obsolete(""Use Load(parameter) method instead."")]
        public TEntity[] Filter<T>(T parameter)
        {{
            var items = Load(parameter, typeof(T));
            if (items is TEntity[] itemsArray)
                return itemsArray;
            else
                return items.ToArray();
        }}

        {ReadableRepositoryBaseMembersTag}
    }}

    public abstract class QueryableRepositoryBase<TQueryableEntity, TEntity> : ReadableRepositoryBase<TEntity>, IQueryableRepository<TQueryableEntity, TEntity>
        where TEntity : class, IEntity
        where TQueryableEntity : class, IEntity, TEntity, IQueryableEntity<TEntity>
    {{
        public TEntity[] Load(IEnumerable<Guid> ids)
        {{
            if (!(ids is System.Collections.IList))
                ids = ids.ToList();
            const int BufferSize = 500; // EF 6.1.3 LINQ query has O(n^2) time complexity. Batch size of 500 results with optimal total time on the test system.
            int n = ids.Count();
            var result = new List<TEntity>(n);
            for (int i = 0; i < (n + BufferSize - 1) / BufferSize; i++)
            {{
                Guid[] idBuffer = ids.Skip(i * BufferSize).Take(BufferSize).ToArray();
                List<TEntity> itemBuffer;
                if (idBuffer.Length == 1) // EF 6.1.3. does not use parametrized SQL query for Contains() function. The equality comparer is used instead, to reuse cached execution plans.
                {{
                    Guid id = idBuffer.Single();
                    itemBuffer = Query().Where(item => item.ID == id).GenericToSimple<TEntity>().ToList();
                }}
                else if(!idBuffer.Any())
                {{
                    itemBuffer = new List<TEntity>();  
                }}
                else
                    itemBuffer = Query().WhereContains(idBuffer.ToList(), item => item.ID).GenericToSimple<TEntity>().ToList();
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

        public override TEntity[] Load()
        {{
            return Query().GenericToSimple<TEntity>().ToArray();
        }}

        {QueryableRepositoryBaseMembersTag}
    }}

    public abstract class OrmRepositoryBase<TQueryableEntity, TEntity> : QueryableRepositoryBase<TQueryableEntity, TEntity>
        where TEntity : class, IEntity
        where TQueryableEntity : class, IEntity, TEntity, IQueryableEntity<TEntity>
    {{
        public override IQueryable<TQueryableEntity> Query()
        {{
            return _executionContext.EntityFrameworkContext.Set<TQueryableEntity>().AsNoTracking();
        }}

        public IQueryable<TQueryableEntity> Filter(IQueryable<TQueryableEntity> query, IEnumerable<Guid> ids)
        {{
            if (!(ids is System.Collections.IList))
                ids = ids.ToList();

            if (ids.Count() == 1) // EF 6.1.3. does not use parametrized SQL query for Contains() function. The equality comparer is used instead, to reuse cached execution plans.
            {{
                Guid id = ids.Single();
                return query.Where(item => item.ID == id);
            }}
            else if (!ids.Any())
            {{
                return Array.Empty<TQueryableEntity>().AsQueryable();
            }}
            else
            {{
                // Depending on the ids count, this method will return the list of IDs, or insert the ids to the database and return an SQL query that selects the ids.
                var idsQuery = _domRepository.Common.FilterId.CreateQueryableFilterIds(ids);

                if (idsQuery is IList<Guid>)
                    return query.WhereContains(idsQuery.ToList(), item => item.ID);
                else
                    return query.Where(item => idsQuery.Contains(item.ID));
            }}
        }}

        {OrmRepositoryBaseMembersTag}
    }}

    {ModuleCodeGenerator.CommonNamespaceMembersTag}{RestoreWarnings(_commonConceptsOptions)}
}}
";
    }
}
