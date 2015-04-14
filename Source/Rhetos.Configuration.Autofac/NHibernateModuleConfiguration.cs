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
using Rhetos.Compiler;
using Rhetos.Dsl;
using Autofac;
using Rhetos.Utilities;
using Rhetos.Persistence.NHibernate;
using Rhetos.Persistence;
using System.Diagnostics.Contracts;
using Rhetos.DatabaseGenerator;
using Rhetos.Extensibility;
using Autofac.Builder;
using Autofac.Core;
using Rhetos.Dom;

namespace Rhetos.Configuration.Autofac
{
    public class NHibernateModuleConfiguration : Module
    {
        private readonly bool _deploymentTime;

        public NHibernateModuleConfiguration(bool deploymentTime)
        {
            _deploymentTime = deploymentTime;
        }

        protected override void Load(ContainerBuilder builder)
        {
            if (_deploymentTime)
            {
                builder.RegisterType<NHibernateMappingGenerator>().As<INHibernateMapping>().SingleInstance();
                Plugins.FindAndRegisterPlugins<IConceptMappingCodeGenerator>(builder);
            }
            else
            {
                builder.RegisterType<NHibernateMappingLoader>().WithParameter("nHibernateMappingFile", Paths.NHibernateMappingFile).As<INHibernateMapping>().SingleInstance();
                builder.RegisterType<NHibernatePersistenceEngine>().As<IPersistenceEngine>().SingleInstance();
                Plugins.FindAndRegisterPlugins<INHibernateConfigurationExtension>(builder);
            }

            base.Load(builder);
        }
    }
}
