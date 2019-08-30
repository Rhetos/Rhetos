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

using Autofac;
using Rhetos.Dsl;
using Rhetos.Extensibility;
using System.Diagnostics.Contracts;

namespace Rhetos.Configuration.Autofac
{
    public class DslModuleConfiguration : Module
    {
        private readonly bool _deploymentTime;
        private readonly bool _deployDatabaseOnly;

        public DslModuleConfiguration(bool deploymentTime, bool deployDatabaseOnly)
        {
            _deploymentTime = deploymentTime;
            _deployDatabaseOnly = deployDatabaseOnly;
        }

        protected override void Load(ContainerBuilder builder)
        {
            if (_deploymentTime)
            {
                builder.RegisterType<DiskDslScriptLoader>().As<IDslScriptsProvider>().SingleInstance();
                builder.RegisterType<Tokenizer>().SingleInstance();
                builder.RegisterType<DslModelFile>().As<IDslModelFile>().SingleInstance();
                builder.RegisterType<DslParser>().As<IDslParser>();
                builder.RegisterType<MacroOrderRepository>().As<IMacroOrderRepository>();
                builder.RegisterType<ConceptMetadata>().SingleInstance();
                builder.RegisterType<TypeExtensionProvider>().SingleInstance();
                builder.RegisterType<InitializationConcept>().As<IConceptInfo>(); // This plugin is registered manually because FindAndRegisterPlugins does not scan core Rhetos dlls.
                Plugins.FindAndRegisterPlugins<IConceptInfo>(builder);
                Plugins.FindAndRegisterPlugins<IConceptMacro>(builder, typeof(IConceptMacro<>));
                Plugins.FindAndRegisterPlugins<ITypeExtension>(builder);
            }

            if (_deploymentTime && !_deployDatabaseOnly)
                builder.RegisterType<DslModel>().As<IDslModel>().SingleInstance();
            else
                builder.RegisterType<DslModelFile>().As<IDslModel>().SingleInstance();

            builder.RegisterType<DslContainer>();
            Plugins.FindAndRegisterPlugins<IDslModelIndex>(builder);
            builder.RegisterType<DslModelIndexByType>().As<IDslModelIndex>(); // This plugin is registered manually because FindAndRegisterPlugins does not scan core Rhetos dlls.
            builder.RegisterType<DslModelIndexByReference>().As<IDslModelIndex>(); // This plugin is registered manually because FindAndRegisterPlugins does not scan core Rhetos dlls.

            base.Load(builder);
        }
    }
}
