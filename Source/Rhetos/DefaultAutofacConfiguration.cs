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
using System.ServiceModel;
using Autofac;
using Rhetos.Utilities;
using Rhetos.Configuration.Autofac;
using System.Configuration;
using System.IO;
using Rhetos.Dsl;

namespace Rhetos
{
    public class DefaultAutofacConfiguration : Module
    {
        private static readonly string _rootPath = AppDomain.CurrentDomain.BaseDirectory;
        private const string _domAssemblyName = "ServerDom";
        private const string _nHibernateMappingFile = "bin\\ServerDomNHibernateMapping.xml";
        private static readonly string _dslScriptsFolder = Path.Combine(_rootPath, "DslScripts");

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(new ConnectionString(SqlUtility.ConnectionString));
            builder.RegisterInstance(new ResourcesFolder(ConfigurationManager.AppSettings["ResourcesDirectory"]));

            builder.RegisterType<WcfUserInfo>().As<IUserInfo>().As<WcfUserInfo>().InstancePerLifetimeScope();
            builder.RegisterType<RhetosService>().InstancePerDependency().As<RhetosService>().As<IServerApplication>();
            builder.RegisterInstance<IDslSource>(new DiskDslScriptProvider(_dslScriptsFolder));
            builder.RegisterType<GlobalErrorHandler>();

            builder.RegisterModule(new CommonModuleConfiguration());
            builder.RegisterModule(new ExtensibilityModuleConfiguration());
            builder.RegisterModule(new DslModuleConfiguration());
            builder.RegisterModule(new CompilerConfiguration());
            builder.RegisterModule(new FactoryModuleConfiguration());
            builder.RegisterModule(new DomModuleConfiguration(_domAssemblyName, DomAssemblyUsage.Load));
            builder.RegisterModule(new NHibernateModuleConfiguration(Path.Combine(_rootPath, _nHibernateMappingFile)));
            builder.RegisterModule(new ProcessingModuleConfiguration());
            builder.RegisterModule(new LoggingConfiguration());
            builder.RegisterModule(new SecurityModuleConfiguration());

            base.Load(builder);
        }

    }
}