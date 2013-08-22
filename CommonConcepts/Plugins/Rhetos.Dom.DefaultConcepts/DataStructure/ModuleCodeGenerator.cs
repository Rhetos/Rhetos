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
using System.Linq;
using System.Text;
using System.Globalization;
using System.ComponentModel.Composition;
using Microsoft.CSharp.RuntimeBinder;
using Rhetos.Utilities;
using Rhetos.Compiler;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Dsl;
using Rhetos.Extensibility;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(ModuleInfo))]
    public class ModuleCodeGenerator : IConceptCodeGenerator
    {
        public static readonly CsTag<ModuleInfo> UsingTag = "Using";
        public static readonly CsTag<ModuleInfo> NamespaceMembersTag = "Body";
        public static readonly CsTag<ModuleInfo> RepositoryMembersTag = "RepositoryMembers";
        public static readonly CsTag<ModuleInfo> HelperNamespaceMembersTag = "HelperNamespaceMembers";

        private const string StandardNamespacesSnippet =
@"using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using NHibernate.Linq;
    using System.Linq.Expressions;
    using System.Runtime.Serialization;
    using Rhetos.Dom.DefaultConcepts;
    using Rhetos.Utilities;";

        private static string GenerateNamespaceSnippet(ModuleInfo info)
        {
            return string.Format(
                @"
namespace {0}
{{
    {1}

    {2}

    {3}
}}

namespace {0}._Helper
{{
    {1}

    {2}

    public class _ModuleRepository
    {{
        private readonly Common.DomRepository _domRepository;
        private readonly Common.ExecutionContext _executionContext;

        public _ModuleRepository(Common.DomRepository domRepository, Common.ExecutionContext executionContext)
        {{
            _domRepository = domRepository;
            _executionContext = executionContext;
        }}
        {4}
    }}
    {5}
}}",
                info.Name,
                StandardNamespacesSnippet,
                UsingTag.Evaluate(info),
                NamespaceMembersTag.Evaluate(info),
                RepositoryMembersTag.Evaluate(info),
                HelperNamespaceMembersTag.Evaluate(info));
        }

        private static string ModuleRepositoryInCommonRepositorySnippet(ModuleInfo info)
        {
            return string.Format(
                @"private {0}._Helper._ModuleRepository _{0};
        public {0}._Helper._ModuleRepository {0} {{ get {{ return _{0} ?? (_{0} = new {0}._Helper._ModuleRepository(this, _executionContext)); }} }}

        ",
                info.Name);
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            ModuleInfo info = (ModuleInfo)conceptInfo;

            if (!CommonClassesCreated)
            {
                codeBuilder.InsertCode(GenerateCommonClassesSnippet());
                // Types used in the preceding code snippet:
                codeBuilder.AddReferencesFromDependency(typeof(Autofac.Module)); // Includes a reference to Autofac.dll.
                codeBuilder.AddReferencesFromDependency(typeof(NHibernate.ISession));
                codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Utilities.IUserInfo));
                codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Utilities.ISqlExecuter));
                codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Security.IAuthorizationManager));
                codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Utilities.ResourcesFolder));
                codeBuilder.AddReferencesFromDependency(typeof(System.ComponentModel.Composition.ExportAttribute));

                CommonClassesCreated = true;
            }

            codeBuilder.InsertCode(GenerateNamespaceSnippet(info));
            // Default .NET framework asseblies:
            codeBuilder.AddReferencesFromDependency(typeof(int)); // Includes reference to mscorlib.dll
            codeBuilder.AddReferencesFromDependency(typeof(Enumerable)); // Includes reference to System.Core.
            codeBuilder.AddReferencesFromDependency(typeof(ISet<>)); // Includes reference to System.
            codeBuilder.AddReferencesFromDependency(typeof(RuntimeBinderException)); // Includes reference to Microsoft.CSharp.
            codeBuilder.AddReferencesFromDependency(typeof(System.Data.Common.DbDataReader)); // Includes reference to System.Data.
            codeBuilder.AddReferencesFromDependency(typeof(System.Data.DataRowExtensions)); // Includes reference to System.Data.DataSetExtensions.
            codeBuilder.AddReferencesFromDependency(typeof(System.Xml.Serialization.XmlSerializer)); // Includes reference to System.Xml.
            codeBuilder.AddReferencesFromDependency(typeof(System.Xml.Linq.XElement)); // Includes reference to System.Xml.Linq.
            // Commonly used Rhetos classes:
            codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Dom.DefaultConcepts.FilterAll)); // Includes reference to Rhetos.Dom.DefaultConcepts.dll
            // Other classes used in domain object model:
            codeBuilder.AddReferencesFromDependency(typeof(NHibernate.Linq.LinqExtensionMethods)); // Includes reference to NHibernate.dll.
            codeBuilder.AddReferencesFromDependency(typeof(System.Runtime.Serialization.DataContractAttribute)); // Includes reference to System.Runtime.Serialization.dll.

            codeBuilder.InsertCode(ModuleRepositoryInCommonRepositorySnippet(info), CommonDomRepositoryMembersTag);
        }

        public static bool CommonClassesCreated = false;
        public const string CommonUsingTag = "/*CommonUsing*/";
        public const string CommonDomRepositoryMembersTag = "/*CommonDomRepositoryMembers*/";
        public const string CommonAutofacConfigurationMembersTag = "/*CommonAutofacConfigurationMembers*/";
        public const string ExecutionContextMemberTag = "/*ExecutionContextMember*/";
        public const string ExecutionContextConstructorArgumentTag = "/*ExecutionContextConstructorArgument*/";
        public const string ExecutionContextConstructorAssignmentTag = "/*ExecutionContextConstructorAssignment*/";

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

    public class ExecutionContext
    {{
        protected Lazy<NHibernate.ISession> _nHibernateSession;
        public NHibernate.ISession NHibernateSession {{ get {{ return _nHibernateSession.Value; }} }}

        protected Lazy<Rhetos.Utilities.IUserInfo> _userInfo;
        public Rhetos.Utilities.IUserInfo UserInfo {{ get {{ return _userInfo.Value; }} }}

        protected Lazy<Rhetos.Utilities.ISqlExecuter> _sqlExecuter;
        public Rhetos.Utilities.ISqlExecuter SqlExecuter {{ get {{ return _sqlExecuter.Value; }} }}

        protected Lazy<Rhetos.Security.IAuthorizationManager> _authorizationManager;
        public Rhetos.Security.IAuthorizationManager AuthorizationManager {{ get {{ return _authorizationManager.Value; }} }}

        protected Lazy<Rhetos.Utilities.ResourcesFolder> _resourcesFolder;
        public Rhetos.Utilities.ResourcesFolder ResourcesFolder {{ get {{ return _resourcesFolder.Value; }} }}
        {4}

        // This constructor is used for automatic parameter injection with autofac.
        public ExecutionContext(
            Lazy<NHibernate.ISession> nHibernateSession,
            Lazy<Rhetos.Utilities.IUserInfo> userInfo,
            Lazy<Rhetos.Utilities.ISqlExecuter> sqlExecuter,
            Lazy<Rhetos.Security.IAuthorizationManager> authorizationManager,
            Lazy<Rhetos.Utilities.ResourcesFolder> resourcesFolder{5})
        {{
            _nHibernateSession = nHibernateSession;
            _userInfo = userInfo;
            _sqlExecuter = sqlExecuter;
            _authorizationManager = authorizationManager;
            _resourcesFolder = resourcesFolder;
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
            {3}

            base.Load(builder);
        }}
    }}
}}",
            StandardNamespacesSnippet,
            CommonUsingTag,
            CommonDomRepositoryMembersTag,
            CommonAutofacConfigurationMembersTag,
            ExecutionContextMemberTag,
            ExecutionContextConstructorArgumentTag,
            ExecutionContextConstructorAssignmentTag);
        }
    }
}
