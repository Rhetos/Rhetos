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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(InitializationConcept))]
    public class DomInitializationCodeGenerator : IConceptCodeGenerator
    {
        public const string StandardNamespacesSnippet =
@"using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using NHibernate.Linq;
    using System.Linq.Expressions;
    using System.Runtime.Serialization;
    using Rhetos.Dom.DefaultConcepts;
    using Rhetos.Utilities;";

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (InitializationConcept)conceptInfo;

            codeBuilder.InsertCode(GenerateCommonClassesSnippet());
            // Types used in the preceding code snippet:
            codeBuilder.AddReferencesFromDependency(typeof(Autofac.Module)); // Includes a reference to Autofac.dll.
            codeBuilder.AddReferencesFromDependency(typeof(NHibernate.ISession));
            codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Utilities.IUserInfo));
            codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Utilities.ISqlExecuter));
            codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Security.IAuthorizationManager));
            codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Utilities.ResourcesFolder));
            codeBuilder.AddReferencesFromDependency(typeof(System.ComponentModel.Composition.ExportAttribute));
            codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Dom.DefaultConcepts.GenericRepositories));
            codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Persistence.NHibernate.INHibernateConfigurationExtension));
            codeBuilder.AddReferencesFromDependency(typeof(NHibernate.Cfg.Configuration));
            codeBuilder.AddReferencesFromDependency(typeof(NHibernate.Linq.Functions.DefaultLinqToHqlGeneratorsRegistry));
        }

        private static string GenerateCommonClassesSnippet()
        {
            return string.Format(
@"namespace Common
{{
    {0}

    using Autofac;
    {1}

    public class DomRepository
    {{
        private readonly ExecutionContext _executionContext;

        public DomRepository(ExecutionContext executionContext)
        {{
            _executionContext = executionContext;
        }}

        {2}
    }}

    public static class Infrastructure
    {{
        public static readonly RegisteredInterfaceImplementations RegisteredInterfaceImplementationName = new RegisteredInterfaceImplementations
        {{
            {7}
        }};

        public static readonly ApplyFiltersOnClientRead ApplyFiltersOnClientRead = new ApplyFiltersOnClientRead
        {{
            {10}
        }};

        {12}
    }}

    public class ExecutionContext
    {{
        protected Lazy<Rhetos.Persistence.IPersistenceTransaction> _persistenceTransaction;
        public NHibernate.ISession NHibernateSession {{ get {{ return _persistenceTransaction.Value.NHibernateSession; }} }}

        public Rhetos.Persistence.IPersistenceTransaction PersistenceTransaction {{ get {{ return _persistenceTransaction.Value; }} }}

        protected Lazy<Rhetos.Utilities.IUserInfo> _userInfo;
        public Rhetos.Utilities.IUserInfo UserInfo {{ get {{ return _userInfo.Value; }} }}

        protected Lazy<Rhetos.Utilities.ISqlExecuter> _sqlExecuter;
        public Rhetos.Utilities.ISqlExecuter SqlExecuter {{ get {{ return _sqlExecuter.Value; }} }}

        protected Lazy<Rhetos.Security.IAuthorizationManager> _authorizationManager;
        public Rhetos.Security.IAuthorizationManager AuthorizationManager {{ get {{ return _authorizationManager.Value; }} }}

        protected Lazy<Rhetos.Utilities.ResourcesFolder> _resourcesFolder;
        public Rhetos.Utilities.ResourcesFolder ResourcesFolder {{ get {{ return _resourcesFolder.Value; }} }}

        protected Lazy<Rhetos.Dom.DefaultConcepts.GenericRepositories> _genericRepositories;
        public Rhetos.Dom.DefaultConcepts.GenericRepositories GenericRepositories {{ get {{ return _genericRepositories.Value; }} }}

        public Rhetos.Dom.DefaultConcepts.GenericRepository<TEntity> GenericRepository<TEntity>() where TEntity : class, IEntity
        {{
            return GenericRepositories.GetGenericRepository<TEntity>();
        }}

        protected Lazy<Common.DomRepository> _repositories;
        public Common.DomRepository Repositories {{ get {{ return _repositories.Value; }} }}

        {4}

        // This constructor is used for automatic parameter injection with autofac.
        public ExecutionContext(
            Lazy<Rhetos.Persistence.IPersistenceTransaction> persistenceTransaction,
            Lazy<Rhetos.Utilities.IUserInfo> userInfo,
            Lazy<Rhetos.Utilities.ISqlExecuter> sqlExecuter,
            Lazy<Rhetos.Security.IAuthorizationManager> authorizationManager,
            Lazy<Rhetos.Utilities.ResourcesFolder> resourcesFolder,
            Lazy<Rhetos.Dom.DefaultConcepts.GenericRepositories> genericRepositories{5})
        {{
            _persistenceTransaction = persistenceTransaction;
            _userInfo = userInfo;
            _sqlExecuter = sqlExecuter;
            _authorizationManager = authorizationManager;
            _resourcesFolder = resourcesFolder;
            _genericRepositories = genericRepositories;
            _repositories = new Lazy<Common.DomRepository>(() => new Common.DomRepository(this));
            {6}
        }}

        // This constructor is used for manual context creation (unit testing)
        public ExecutionContext()
        {{
        }}
    }}

    [System.ComponentModel.Composition.Export(typeof(Autofac.Module))]
    public class AutofacConfiguration : Autofac.Module
    {{
        protected override void Load(Autofac.ContainerBuilder builder)
        {{
            builder.RegisterType<DomRepository>().InstancePerLifetimeScope();
            builder.RegisterType<ExecutionContext>().InstancePerLifetimeScope();
            builder.RegisterInstance(Infrastructure.RegisteredInterfaceImplementationName).ExternallyOwned();
            builder.RegisterInstance(Infrastructure.ApplyFiltersOnClientRead).ExternallyOwned();
            {3}

            base.Load(builder);
        }}
    }}

    namespace NHibernateConfiguration
    {{
        using NHibernate.Cfg;
        using NHibernate.Hql.Ast;
        using NHibernate.Linq;
        using NHibernate.Linq.Functions;
        using NHibernate.Linq.Visitors;

        [System.ComponentModel.Composition.Export(typeof(Rhetos.Persistence.NHibernate.INHibernateConfigurationExtension))]
        public sealed class NHibernateConfigurationExtension : Rhetos.Persistence.NHibernate.INHibernateConfigurationExtension
        {{
            public void ExtendConfiguration(NHibernate.Cfg.Configuration configuration)
            {{
                {8}
                configuration.LinqToHqlGeneratorsRegistry<LinqToHqlGeneratorsRegistry>();
            }}
        }}

        public sealed class LinqToHqlGeneratorsRegistry : NHibernate.Linq.Functions.DefaultLinqToHqlGeneratorsRegistry
        {{
            public LinqToHqlGeneratorsRegistry()
            {{
                {9}
            }}
        }}
    }}

    {11}
}}",
            StandardNamespacesSnippet,
            ModuleCodeGenerator.CommonUsingTag,
            ModuleCodeGenerator.CommonDomRepositoryMembersTag,
            ModuleCodeGenerator.CommonAutofacConfigurationMembersTag,
            ModuleCodeGenerator.ExecutionContextMemberTag,
            ModuleCodeGenerator.ExecutionContextConstructorArgumentTag,
            ModuleCodeGenerator.ExecutionContextConstructorAssignmentTag,
            ModuleCodeGenerator.RegisteredInterfaceImplementationNameTag,
            ModuleCodeGenerator.NHibernateConfigurationExtensionTag,
            ModuleCodeGenerator.LinqToHqlGeneratorsRegistryTag,
            ModuleCodeGenerator.ApplyFiltersOnClientReadTag,
            ModuleCodeGenerator.CommonNamespaceMembersTag,
            ModuleCodeGenerator.CommonInfrastructureMembersTag);
        }
    }
}
