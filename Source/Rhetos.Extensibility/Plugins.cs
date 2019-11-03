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
using System;

namespace Rhetos.Extensibility
{
    public static class Plugins
    {
        private static ContainerBuilderPluginRegistration _pluginRegistration;

        public static void Initialize(ContainerBuilderPluginRegistration pluginRegistration)
        {
            _pluginRegistration = pluginRegistration;
        }

        public static void FindAndRegisterModules(ContainerBuilder builder)
        {
            ThrowIfNotInitializedOrOutOfScope(builder);
            _pluginRegistration.FindAndRegisterModules();
        }

        public static void ClearCache()
        {
            throw new FrameworkException($"Clearing the cache of Plugins is not longer supported, initialize with new {nameof(ContainerBuilderPluginRegistration)} instance instead.");
        }

        public static void FindAndRegisterPlugins<TPluginInterface>(ContainerBuilder builder)
        {
            ThrowIfNotInitializedOrOutOfScope(builder);
            _pluginRegistration.FindAndRegisterPlugins<TPluginInterface>();
        }

        public static void FindAndRegisterPlugins<TPluginInterface>(ContainerBuilder builder, Type genericImplementationBase)
        {
            ThrowIfNotInitializedOrOutOfScope(builder);
            _pluginRegistration.FindAndRegisterPlugins<TPluginInterface>(genericImplementationBase);
        }

        public static void CheckOverride<TInterface, TImplementation>(ContainerBuilder builder, params Type[] expectedPreviousPlugins)
        {
            ThrowIfNotInitializedOrOutOfScope(builder);
            _pluginRegistration.CheckOverride<TInterface, TImplementation>(expectedPreviousPlugins);
        }

        public static void SuppressPlugin<TPluginInterface, TPlugin>(ContainerBuilder builder)
            where TPlugin : TPluginInterface
        {
            ThrowIfNotInitializedOrOutOfScope(builder);
            _pluginRegistration.SuppressPlugin<TPluginInterface, TPlugin>();
        }

        public static void LogRegistrationStatistics(string title, IContainer container)
        {
            ThrowIfNotInitializedOrOutOfScope(null);
            _pluginRegistration.LogRegistrationStatistics(title, container);
        }

        private static void ThrowIfNotInitializedOrOutOfScope(ContainerBuilder builder)
        {
            if (_pluginRegistration == null)
                throw new FrameworkException("Plugins legacy utility has not been initialized. Use Plugins.Initialize() to initialize or migrate to new convention.");

            if (builder != null && builder != _pluginRegistration.Builder)
                throw new FrameworkException("Plugins legacy utility has been initialized with one instance of ContainerBuilder and method is called with another instance.");
        }
    }
}
