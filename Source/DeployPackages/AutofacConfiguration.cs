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
using System.IO;
using Autofac;
using Rhetos.Utilities;
using Rhetos.Configuration.Autofac;
using Rhetos.Deployment;
using Rhetos.Dsl;
using Rhetos.Security;

namespace DeployPackages
{
    public class AutofacConfiguration : Module
    {
        private static readonly string _rootPath = AppDomain.CurrentDomain.BaseDirectory;

        public static readonly string DslScriptsFolder = Path.Combine(_rootPath, "..\\DslScripts");
        public static readonly string DataMigrationScriptsFolder = Path.Combine(_rootPath, "..\\DataMigration");
        public static readonly string DomAssemblyName = "ServerDom";
        public static readonly string NHibernateMappingFile = Path.Combine(_rootPath, "ServerDomNHibernateMapping.xml");
        public static readonly string RhetosServerWebConfigPath = Path.Combine(_rootPath, "..\\Web.config");

        private readonly string _connectionString;

        public AutofacConfiguration(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(new ConnectionString(_connectionString));
            builder.RegisterInstance(new ResourcesFolder(""));
            builder.RegisterModule(new UtilitiesModuleConfiguration());
            builder.RegisterModule(new ExtensibilityModuleConfiguration());
            builder.RegisterModule(new DslModuleConfiguration());
            builder.RegisterInstance<IDslSource>(new DiskDslScriptProvider(DslScriptsFolder));
            builder.RegisterModule(new CompilerConfiguration());
            builder.RegisterModule(new DatabaseGeneratorModuleConfiguration());
            builder.RegisterModule(new DomModuleConfiguration(DomAssemblyName, DomAssemblyUsage.Generate));
            builder.RegisterModule(new NHibernateModuleConfiguration(null));
            builder.RegisterModule(new LoggingConfiguration());
            builder.RegisterModule(new SecurityModuleConfiguration());

            builder.RegisterType<DataMigration>();
            builder.RegisterType<DatabaseCleaner>();

            builder.RegisterType<ProcessUserInfo>().As<IUserInfo>();

            base.Load(builder);
        }
    }
}