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
        public void Initialize()
        {
            WebSecurity.InitializeDatabaseConnection(SqlUtility.ConnectionString, SqlUtility.ProviderName, "aspnet_Principal", "AspNetUserId", "Name", autoCreateTables: true);
            RouteTable.Routes.Add(new ServiceRoute("Resources/AspNetFormsAuth/Authentication", new AuthenticationServiceHostFactory(), typeof(AuthenticationService)));
        }

        private static IHttpModule _cancelUnauthorizedClientRedirectionModule = new CancelUnauthorizedClientRedirection();

        public void InitializeApplicationInstance(HttpApplication context)
        {
            _cancelUnauthorizedClientRedirectionModule.Init(context);
        }
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
