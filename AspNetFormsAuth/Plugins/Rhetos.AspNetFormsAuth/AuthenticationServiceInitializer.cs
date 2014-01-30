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
using Autofac.Features.Indexed;
using Rhetos.Dom;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Processing;
using Rhetos.Security;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Description;
using System.Text;
using System.Web;
using System.Web.Routing;
using WebMatrix.WebData;

namespace Rhetos.AspNetFormsAuth
{
    [Export(typeof(Rhetos.IService))]
    public class AuthenticationServiceInitializer : Rhetos.IService
    {
        public static void InitializeDatabaseConnection(bool autoCreateTables)
        {
            WebSecurity.InitializeDatabaseConnection(SqlUtility.ConnectionString, SqlUtility.ProviderName, "aspnet_Principal", "AspNetUserId", "Name", autoCreateTables);
        }

        public void Initialize()
        {
            InitializeDatabaseConnection(autoCreateTables: true);
            RouteTable.Routes.Add(new ServiceRoute("Resources/AspNetFormsAuth/Authentication", new AuthenticationServiceHostFactory(), typeof(AuthenticationService)));
        }

        private static IHttpModule _cancelUnauthorizedClientRedirectionModule = new CancelUnauthorizedClientRedirection();

        public void InitializeApplicationInstance(HttpApplication context)
        {
            _cancelUnauthorizedClientRedirectionModule.Init(context);
        }
    }

	// Executes at deployment-time.
    [Export(typeof(Rhetos.Extensibility.IServerInitializer))]
    public class AuthenticationDatabaseInitializer : Rhetos.Extensibility.IServerInitializer
    {
        private readonly IIndex<string, IWritableRepository> _writableRepositories;
        private readonly IDomainObjectModel _domainObjectModel;
        private readonly IQueryableRepository<Rhetos.AspNetFormsAuth.IPrincipal> _principals;
        private readonly IQueryableRepository<Rhetos.AspNetFormsAuth.IRole> _roles;
        private readonly ILogger _logger;

        public AuthenticationDatabaseInitializer(
            IIndex<string, IWritableRepository> writableRepositories,
            IDomainObjectModel domainObjectModel,
            IQueryableRepository<Rhetos.AspNetFormsAuth.IPrincipal> principals,
            IQueryableRepository<Rhetos.AspNetFormsAuth.IRole> roles,
            ILogProvider logProvider)
        {
            _writableRepositories = writableRepositories;
            _domainObjectModel = domainObjectModel;
            _principals = principals;
            _roles = roles;
            _logger = logProvider.GetLogger("AuthenticationDatabaseInitializer");
        }

        const string adminUserName = "admin";
        const string adminRoleName = "SecurityAdministrator";

        public void Initialize()
        {
            Guid adminId = _principals.Query().Where(p => p.Name == adminUserName).Select(p => p.ID).SingleOrDefault();
            if (adminId == default(Guid))
            {
                _logger.Trace(() => "Creating user '" + adminUserName + "'.");
                adminId = Guid.NewGuid();

                var adminPrincipal = CreateEntity("Common.Principal");
                adminPrincipal.ID = adminId;
                adminPrincipal.Name = adminUserName;

                _writableRepositories["Common.Principal"].Save(new[] { adminPrincipal }, null, null);
            }
            else
            {
                _logger.Trace(() => "User '" + adminUserName + "' already exists.");
            }

            Guid adminRoleId = _roles.Query().
        }

        private dynamic CreateEntity(string typeName)
        {
            var t = _domainObjectModel.GetType(typeName);
            return Activator.CreateInstance(t);
        }

        public IEnumerable<string> Dependencies
        {
            get { return null; }
        }
    }

    [Export(typeof(IClaimProvider))]
    [ExportMetadata(MefProvider.Implements, typeof(DummyCommandInfo))]
    public class AuthenticationServiceClaims : IClaimProvider
    {
        public IList<Claim> GetRequiredClaims(ICommandInfo info)
        {
            return null;
        }

        public IList<Claim> GetAllClaims(Dsl.IDslModel dslModel)
        {
            return new[] { SetPasswordClaim };
        }

        public static readonly Claim SetPasswordClaim = new Claim("AspNetFormsAuth.AuthenticationService", "SetPassword");
    }

    public class DummyCommandInfo : ICommandInfo
    {
    }

    public class AuthenticationServiceHostFactory : Autofac.Integration.Wcf.AutofacServiceHostFactory
    {
        protected override ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
        {
            return new AuthenticationServiceHost(serviceType, baseAddresses);
        }
    }

    public class AuthenticationServiceHost : ServiceHost
    {
        private Type _serviceType;

        public AuthenticationServiceHost(Type serviceType, Uri[] baseAddresses)
            : base(serviceType, baseAddresses)
        {
            _serviceType = serviceType;
        }

        protected override void OnOpening()
        {
            base.OnOpening();

            this.AddServiceEndpoint(_serviceType, new WebHttpBinding("rhetosWebHttpBinding"), string.Empty);
            ((ServiceEndpoint)(Description.Endpoints.Where(e => e.Binding is WebHttpBinding).Single())).Behaviors.Add(new WebHttpBehavior());

            if (Description.Behaviors.Find<Rhetos.JsonErrorServiceBehavior>() == null)
                Description.Behaviors.Add(new Rhetos.JsonErrorServiceBehavior());
        }
    }
}
