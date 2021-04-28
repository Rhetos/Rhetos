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
using System;

namespace Autofac
{
    public static class ContainerBuilderExtensions
    {
        /// <summary>
        /// Extension method which resolves <see cref="ILogProvider"/> instance from properly initialized <see cref="RhetosContainerBuilder"/>.
        /// </summary>
        public static ILogProvider GetRhetosLogProvider(this ContainerBuilder builder)
        {
            var key = nameof(ILogProvider);
            if (builder.Properties.TryGetValue(key, out var logProvider) && (logProvider is ILogProvider iLogProvider))
                return iLogProvider;

            throw new FrameworkException($"{nameof(ContainerBuilder)} does not contain an entry for {nameof(ILogProvider)}. " +
                $"This container was probably not created as {nameof(RhetosContainerBuilder)}.");
        }

        /// <summary>
        /// Extension method which resolves <see cref="ILogProvider"/> instance from properly initialized <see cref="RhetosContainerBuilder"/>.
        /// </summary>
        [Obsolete("Use " + nameof(GetRhetosLogProvider) + " instead.")] 
        public static ILogProvider GetLogProvider(this ContainerBuilder builder) => GetRhetosLogProvider(builder);

        /// <summary>
        /// Extension method which resolves Rhetos <see cref="IConfiguration"/> instance from properly initialized <see cref="RhetosContainerBuilder"/>.
        /// </summary>
        public static IConfiguration GetRhetosConfiguration(this ContainerBuilder builder)
        {
            var key = nameof(IConfiguration);
            if (builder.Properties.TryGetValue(key, out var configuration) && (configuration is IConfiguration iConfiguration))
                return iConfiguration;

            throw new FrameworkException($"{nameof(ContainerBuilder)} does not contain an entry for {nameof(IConfiguration)}. " +
                $"This container was probably not created as {nameof(RhetosContainerBuilder)}.");
        }

        /// <summary>
        /// Extension method which resolves <see cref="IPluginScanner"/> instance from properly initialized <see cref="RhetosContainerBuilder"/>.
        /// </summary>
        public static IPluginScanner GetRhetosPluginScanner(this ContainerBuilder builder)
        {
            var key = nameof(IPluginScanner);
            if (builder.Properties.TryGetValue(key, out var pluginScanner) && (pluginScanner is IPluginScanner iPluginScanner))
                return iPluginScanner;

            throw new FrameworkException($"{nameof(ContainerBuilder)} does not contain an entry for {nameof(IPluginScanner)}. " +
                $"This container was probably not created as {nameof(RhetosContainerBuilder)}.");
        }

        /// <summary>
        /// Extension method which resolves <see cref="IPluginScanner"/> instance from properly initialized <see cref="RhetosContainerBuilder"/>.
        /// </summary>
        [Obsolete("Use " + nameof(GetRhetosPluginScanner) + " instead.")]
        public static IPluginScanner GetPluginScanner(this ContainerBuilder builder) => GetRhetosPluginScanner(builder);

        /// <summary>
        /// Extension method which resolves new <see cref="ContainerBuilderPluginRegistration"/> instance from properly initialized <see cref="RhetosContainerBuilder"/>.
        /// </summary>
        public static ContainerBuilderPluginRegistration GetRhetosPluginRegistration(this ContainerBuilder builder)
        {
            var pluginScanner = builder.GetRhetosPluginScanner();
            var logProvider = builder.GetRhetosLogProvider();

            return new ContainerBuilderPluginRegistration(
                builder, logProvider, pluginScanner);
        }

        /// <summary>
        /// Extension method which resolves new <see cref="ContainerBuilderPluginRegistration"/> instance from properly initialized <see cref="RhetosContainerBuilder"/>.
        /// </summary>
        [Obsolete("Use " + nameof(GetRhetosPluginRegistration) + " instead.")]
        public static ContainerBuilderPluginRegistration GetPluginRegistration(this ContainerBuilder builder) => GetRhetosPluginRegistration(builder);

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

        /// <summary>
        /// Registration of Rhetos plugin modules (implementations of <see cref="Module"/> with <see cref="System.ComponentModel.Composition.ExportAttribute"/>).
        /// Call this method before <i>after</i> specific components registration, to allow development of addition plugin that override and customize existing components.
        /// </summary>
        [Obsolete("Use " + nameof(AddRhetosPluginModules) + " instead.")]
        public static ContainerBuilder AddPluginModules(this ContainerBuilder builder) => AddRhetosPluginModules(builder);
    }
}
