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
using System.ComponentModel.Composition.Hosting;
using System.IO;
using Autofac.Integration.Mef;
using Rhetos.Extensibility;
using System.Diagnostics.Contracts;

namespace Rhetos.Configuration.Autofac
{
    public class ExtensibilityModuleConfiguration : Module
    {
        private readonly IEnumerable<string> PluginsAssemblies;

        public ExtensibilityModuleConfiguration()
        {
            PluginsAssemblies = new string[] {};
        }

        /// <param name="pluginsAssemblies">A list of dll files. Plugins will be searched and loaded from those files.</param>
        public ExtensibilityModuleConfiguration(IEnumerable<string> pluginsAssemblies)
        {
            PluginsAssemblies = pluginsAssemblies;
        }

        protected override void Load(ContainerBuilder builder)
        {
            Contract.Requires(builder != null);

            var ci = new ComponentInterceptor();
            builder.RegisterInstance(ci).As<IComponentInterceptorRegistrator>();
            builder.RegisterModule(new ComponentInterceptionModule(ci));

            try
            {
                var assemblyCatalogs = PluginsAssemblies.Select(a => new AssemblyCatalog(a));
                builder.RegisterComposablePartCatalog(new AggregateCatalog(assemblyCatalogs));
            }
            catch (System.Reflection.ReflectionTypeLoadException rtle)
            {
                var firstFive = rtle.LoaderExceptions.Take(5).Select(it => Environment.NewLine + it.Message);
                throw new FrameworkException("Can't find MEF plugin dependencies:" + string.Concat(firstFive), rtle);
            }

            builder.RegisterType<MefExtensionsProvider>().As<IExtensionsProvider>().SingleInstance();
            builder.RegisterGeneric(typeof(PluginRepository<>)).As(typeof(IPluginRepository<>)).SingleInstance();
            builder.RegisterGeneric(typeof(ConceptRepository<>)).As(typeof(IConceptRepository<>)).SingleInstance();

            builder.RegisterGeneric(typeof(GenericContainer<,>)).As(typeof(IGenericContainer<,>)).SingleInstance();

            base.Load(builder);
        }
    }
}
