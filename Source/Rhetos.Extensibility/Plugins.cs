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
using Rhetos.Logging;
using System;

namespace Rhetos.Extensibility
{
    [Obsolete("Use ContainerBuilderPluginRegistration instead. Resolve from ContainerBuilder with extension method builder.GetPluginRegistration().")]
    public static class Plugins
    {
        private static Func<ContainerBuilder, ContainerBuilderPluginRegistration> _pluginRegistrationFactory;

        public static void Initialize(Func<ContainerBuilder, ContainerBuilderPluginRegistration> pluginRegistrationFactory)
        {
            _pluginRegistrationFactory = pluginRegistrationFactory;
        }

        public static void FindAndRegisterModules(ContainerBuilder builder)
        {
            ThrowIfNotInitialized();
            _pluginRegistrationFactory(builder)
                .FindAndRegisterModules();
        }

        public static void FindAndRegisterPlugins<TPluginInterface>(ContainerBuilder builder)
        {
            ThrowIfNotInitialized();
            _pluginRegistrationFactory(builder)
                .FindAndRegisterPlugins<TPluginInterface>();
        }

        public static void FindAndRegisterPlugins<TPluginInterface>(ContainerBuilder builder, Type genericImplementationBase)
        {
            ThrowIfNotInitialized();
            _pluginRegistrationFactory(builder)
                .FindAndRegisterPlugins<TPluginInterface>(genericImplementationBase);
        }

        public static void CheckOverride<TInterface, TImplementation>(ContainerBuilder builder, params Type[] expectedPreviousPlugins)
        {
            ThrowIfNotInitialized();
            _pluginRegistrationFactory(builder)
                .CheckOverride<TInterface, TImplementation>(expectedPreviousPlugins);
        }

        public static void SuppressPlugin<TPluginInterface, TPlugin>(ContainerBuilder builder)
            where TPlugin : TPluginInterface
        {
            ThrowIfNotInitialized();
            _pluginRegistrationFactory(builder)
                .SuppressPlugin<TPluginInterface, TPlugin>();
        }

        public static void LogRegistrationStatistics(string title, IContainer container, ILogProvider logProvider)
        {
            var logger = logProvider.GetLogger("Plugins");
            logger.Trace(() => ContainerBuilderPluginRegistration.GetRegistrationStatistics(title, container));
        }

        private static void ThrowIfNotInitialized()
        {
            if (_pluginRegistrationFactory == null)
                throw new FrameworkException("Plugins legacy utility has not been initialized. Use Plugins.Initialize() to initialize or migrate to new convention.");
        }
    }
}
