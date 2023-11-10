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

using Rhetos;
using Rhetos.Configuration.Autofac.Modules;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Utilities;

namespace Autofac
{
    public static class ContainerBuilderExtensions
    {
        /// <summary>
        /// Returns the Rhetos <see cref="ExecutionStage"/> from the <see cref="ContainerBuilder"/> which is initialized with the <see cref="RhetosContainerBuilder.Create"/> method.
        /// </summary>
        public static ExecutionStage GetRhetosExecutionStage(this ContainerBuilder builder)
            => GetContainerBuilderProperty<ExecutionStage>(builder);

        /// <summary>
        /// Returns the Rhetos <see cref="ILogProvider"/> instance from the <see cref="ContainerBuilder"/> which is initialized with the <see cref="RhetosContainerBuilder.Create"/> method.
        /// </summary>
        public static ILogProvider GetRhetosLogProvider(this ContainerBuilder builder)
            => GetContainerBuilderProperty<ILogProvider>(builder);

        /// <summary>
        /// Returns the Rhetos <see cref="IConfiguration"/> instance from the <see cref="ContainerBuilder"/> which is initialized with the <see cref="RhetosContainerBuilder.Create"/> method.
        /// </summary>
        public static IConfiguration GetRhetosConfiguration(this ContainerBuilder builder)
            => GetContainerBuilderProperty<IConfiguration>(builder);

        /// <summary>
        /// Returns the Rhetos <see cref="IPluginScanner"/> instance from the <see cref="ContainerBuilder"/> which is initialized with the <see cref="RhetosContainerBuilder.Create"/> method.
        /// </summary>
        public static IPluginScanner GetRhetosPluginScanner(this ContainerBuilder builder)
            => GetContainerBuilderProperty<IPluginScanner>(builder);

        private static T GetContainerBuilderProperty<T>(ContainerBuilder builder)
        {
            var key = typeof(T).Name;
            if (builder.Properties.TryGetValue(key, out var propertyObject) && (propertyObject is T propertyValue))
                return propertyValue;
            else
                throw new FrameworkException($"{nameof(ContainerBuilder)} does not contain an entry for {key}. " +
                    $"This container was probably not created with the  {nameof(RhetosContainerBuilder)}.{nameof(RhetosContainerBuilder.Create)} method.");
        }

        /// <summary>
        /// Extension method which resolves new <see cref="ContainerBuilderPluginRegistration"/> instance from the <see cref="ContainerBuilder"/> which is initialized with the <see cref="RhetosContainerBuilder.Create"/> method.
        /// </summary>
        public static ContainerBuilderPluginRegistration GetRhetosPluginRegistration(this ContainerBuilder builder)
        {
            var pluginScanner = builder.GetRhetosPluginScanner();
            var logProvider = builder.GetRhetosLogProvider();

            return new ContainerBuilderPluginRegistration(
                builder, logProvider, pluginScanner);
        }

        /// <summary>
        /// Registration of Rhetos framework components required for run-time.
        /// Call this method <i>before</i> any specific components registration.
        /// </summary>
        public static ContainerBuilder AddRhetosRuntime(this ContainerBuilder builder)
        {
            builder.RegisterModule(new CoreModule());
            builder.RegisterModule(new CorePluginsModule());
            builder.RegisterModule(new RuntimeModule());
            return builder;
        }

        /// <summary>
        /// Registration of Rhetos plugin modules (implementations of <see cref="Module"/> with <see cref="System.ComponentModel.Composition.ExportAttribute"/>).
        /// Call this method before <i>after</i> specific components registration, to allow development of addition plugin that override and customize existing components.
        /// </summary>
        public static ContainerBuilder AddRhetosPluginModules(this ContainerBuilder builder)
        {
            builder.GetRhetosPluginRegistration().FindAndRegisterPluginModules();
            return builder;
        }
    }
}
