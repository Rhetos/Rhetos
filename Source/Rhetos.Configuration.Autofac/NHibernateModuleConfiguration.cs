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
using Rhetos.Compiler;
using Rhetos.Factory;
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
        private readonly string _loadNhMappingFromFile;

        /// <summary>
        /// If loadNhMappingFromFile is not null, the NHibernate mapping configuration will be loaded from the file.
        /// If loadNhMappingFromFile is null, the NHibernate mapping configuration will be generated from IConceptMappingCodeGenerator plugins.
        /// </summary>
        public NHibernateModuleConfiguration(string loadNhMappingFromFile)
        {
            _loadNhMappingFromFile = loadNhMappingFromFile;
        }

        protected override void Load(ContainerBuilder builder)
        {
            if (string.IsNullOrEmpty(_loadNhMappingFromFile))
                builder.RegisterType<NHibernateMappingGenerator>().As<INHibernateMapping>().SingleInstance();
            else
                builder.RegisterType<NHibernateMappingLoader>().WithParameter("nHibernateMappingFile", _loadNhMappingFromFile).As<INHibernateMapping>().SingleInstance();

            builder.RegisterType<NHibernatePersistenceTransaction>();
            builder.RegisterGeneratedFactory<NHibernatePersistenceTransaction.Factory>();
            builder.RegisterType<NHibernatePersistenceEngine>().As<IPersistenceEngine>().SingleInstance();
            PluginsUtility.RegisterPlugins<IConceptMappingCodeGenerator>(builder);
            PluginsUtility.RegisterPlugins<INHibernateConfigurationExtension>(builder);

            base.Load(builder);
        }
    }
}
