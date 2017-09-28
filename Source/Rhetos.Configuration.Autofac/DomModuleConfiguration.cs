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
using System.Linq;
using System.Text;
using Autofac;
using Rhetos.Dom;
using System.Diagnostics.Contracts;

namespace Rhetos.Configuration.Autofac
{
    public class DomModuleConfiguration : Module
    {
        private readonly bool _deploymentTime;

        public DomModuleConfiguration(bool deploymentTime)
        {
            _deploymentTime = deploymentTime;
        }

        protected override void Load(ContainerBuilder builder)
        {
            if (_deploymentTime)
            {
                builder.RegisterType<DomGeneratorOptions>().SingleInstance();
                builder.RegisterType<DomGenerator>().As<IDomainObjectModel>().SingleInstance();
            }
            else
                builder.RegisterType<DomLoader>().As<IDomainObjectModel>().SingleInstance();

            base.Load(builder);
        }
    }
}
