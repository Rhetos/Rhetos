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
using Autofac;
using Autofac.Core;
using System.Diagnostics.Contracts;

namespace Rhetos.Extensibility
{
    public class ComponentInterceptionModule : Module
    {
        private readonly IComponentInterceptorExecutor Executor;

        public ComponentInterceptionModule(IComponentInterceptorExecutor executor)
        {
            Contract.Requires(executor != null);

            this.Executor = executor;
        }

        protected override void AttachToComponentRegistration(IComponentRegistry componentRegistry, IComponentRegistration registration)
        {
            Contract.Requires(registration != null);

            base.AttachToComponentRegistration(componentRegistry, registration);

            registration.Activated += new EventHandler<ActivatedEventArgs<object>>(registration_Activated);
            registration.Activating += new EventHandler<ActivatingEventArgs<object>>(registration_Activating);

        }

        private void registration_Activating(object sender, ActivatingEventArgs<object> e)
        {
            Contract.Requires(e != null);
            Contract.Requires(e.Component != null);
            Contract.Requires(e.Component.Services != null);

            TypedService typeServ;
            foreach (var serv in e.Component.Services)
                if ((typeServ = serv as TypedService) != null)
                    Executor.RunActivatingActions(typeServ.ServiceType, e);
        }

        private void registration_Activated(object sender, ActivatedEventArgs<object> e)
        {
            Contract.Requires(e != null);
            Contract.Requires(e.Component != null);
            Contract.Requires(e.Component.Services != null);

            TypedService typeServ;
            foreach (var serv in e.Component.Services)
                if ((typeServ = serv as TypedService) != null)
                    Executor.RunActivatedActions(typeServ.ServiceType, e);
        }
    }
}
