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
using Autofac.Core;
using Rhetos.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.Extensibility
{
    /// <summary>
    /// Helper functions for plugins.
    /// </summary>
    public static class Plugins
    {
        #region Global initialization

        /// <summary>Plugins class is usually used before IoC container is built, so we use this
        /// improvised way of handling the logging, instead of using IoC registered components.</summary>
        public static void SetInitializationLogging(ILogProvider logProvider)
        {
            InitializationLogging.PerformanceLogger = logProvider.GetLogger("Performance");
            InitializationLogging.Logger = logProvider.GetLogger("Plugins");
        }

        /// <summary>Find and registers Autofac modules that are implemented as plugins.</summary>
        public static void FindAndRegisterModules(ContainerBuilder builder)
        {
            var modules = MefPluginScanner.FindPlugins(builder, typeof(Module));

            foreach (var module in modules)
            {
                InitializationLogging.Logger.Trace(() => "Registering module: " + module.Type.FullName);
                builder.RegisterModule((Module)Activator.CreateInstance(module.Type));
            }
        }

        /// <summary>Deletes the plugins cache to allow scanning of the new generated dlls.</summary>
        public static void ClearCache()
        {
            MefPluginScanner.ClearCache();
        }

        #endregion
        //================================================================
        #region Find and register plugins

        /// <summary>
        /// Scans for plugins that implement the given export type (it is usually the plugin's interface), and registers them.
        /// The function should be called from a plugin module initialization (from Autofac.Module implementation).
        /// </summary>
        public static void FindAndRegisterPlugins<TPluginInterface>(ContainerBuilder builder)
        {
            var matchingPlugins = MefPluginScanner.FindPlugins(builder, typeof(TPluginInterface));
            RegisterPlugins(builder, matchingPlugins, typeof(TPluginInterface));
        }

        /// <summary>
        /// Scans for plugins that implement the given export type (it is usually the plugin's interface), and registers them.
        /// The function should be called from a plugin module initialization (from Autofac.Module implementation).
        /// </summary>
        /// <param name="genericImplementationBase">
        /// The genericImplementationBase is a generic interface or a generic abstract class that the plugin implements.
        /// The concept type that the plugin handles will be automatically extracted from the generic argument of the genericImplementationBase.
        /// This is an alternative to using MefProvider.Implements in the plugin's ExportMetadata attribute.
        /// </param>
        public static void FindAndRegisterPlugins<TPluginInterface>(ContainerBuilder builder, Type genericImplementationBase)
        {
            var matchingPlugins = MefPluginScanner.FindPlugins(builder, typeof(TPluginInterface));

            foreach (var plugin in matchingPlugins)
                ExtractGenericPluginImplementsMetadata(plugin, genericImplementationBase);

            RegisterPlugins(builder, matchingPlugins, typeof(TPluginInterface));
        }

        /// <summary>
        /// Since the last registration is considered the active one, when overriding previous registrations
        /// use this function to verify if the previous plugins are already registered and will be overridden.
        /// 
        /// To force the specific registration order between modules (derivations of Autofac.Module)
        /// use [ExportMetadata(MefProvider.DependsOn, typeof(the other Autofac.Module derivation))] attribute
        /// on the module that registers the components that override registrations from the other module.
        /// </summary>
        public static void CheckOverride<TInterface, TImplementation>(ContainerBuilder builder, params Type[] expectedPreviousPlugins)
        {
            builder.RegisterCallback(componentRegistry =>
            {
                var existingService = new Autofac.Core.TypedService(typeof(TInterface));
                var existingRegistrations = componentRegistry.RegistrationsFor(existingService).Select(r => r.Activator.LimitType).ToList();

                var missingRegistration = expectedPreviousPlugins.Except(existingRegistrations).ToList();
                var excessRegistration = existingRegistrations.Except(expectedPreviousPlugins).ToList();

                if (missingRegistration.Count > 0 || excessRegistration.Count > 0)
                {
                    string error = "Unexpected plugins while overriding '" + typeof(TInterface).Name + "' with '" + typeof(TImplementation).Name + "'.";

                    if (missingRegistration.Count > 0)
                        error += " The following plugins should have been previous registered in order to be overridden: "
                            + string.Join(", ", missingRegistration.Select(r => r.Name)) + ".";

                    if (excessRegistration.Count > 0)
                        error += " The following plugins have been previous registered and would be unintentionally overridden: "
                            + string.Join(", ", excessRegistration.Select(r => r.Name)) + ".";

                    error += " Verify that the module registration is occurring in the right order: Use [ExportMetadata(MefProvider.DependsOn, typeof(other Autofac.Module implementation))], to make those registration will occur before this one.";

                    throw new FrameworkException(error);
                }
            });
        }

        private static void RegisterPlugins(ContainerBuilder builder, IEnumerable<PluginInfo> matchingPlugins, Type exportType)
        {
            if (matchingPlugins.Count() == 0)
                return;

            InitializationLogging.Logger.Trace(() => "Registering plugins: " + exportType.FullName + " (" + matchingPlugins.Count() + ")");

            foreach (var plugin in matchingPlugins)
            {
                var registration = builder.RegisterType(plugin.Type).As(new[] { exportType });

                foreach (var metadataElement in plugin.Metadata)
                {
                    registration = registration.WithMetadata(metadataElement.Key, metadataElement.Value);
                    if (metadataElement.Key == MefProvider.Implements)
                        registration = registration.Keyed(metadataElement.Value, exportType);
                }
            }
        }

        /// <summary>
        /// Updates the plugin's metadata (MefProvider.Implements) to match concept type that is given as the generic argument of the given interface or abstract class.
        /// </summary>
        private static void ExtractGenericPluginImplementsMetadata(PluginInfo plugin, Type genericImplementationBase)
        {
            var implementsTypes = plugin.Type.GetInterfaces().Concat(new[] { plugin.Type.BaseType })
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericImplementationBase)
                .Select(i => i.GetGenericArguments()[0])
                .ToList();

            if (implementsTypes.Count == 0)
                throw new FrameworkException(string.Format(
                    "Plugin {0} does not implement or inherit generic type {1}.",
                    plugin.Type.FullName,
                    genericImplementationBase.FullName));

            foreach (Type implements in implementsTypes)
                plugin.Metadata.Add(MefProvider.Implements, implements);
        }

        /// <summary>
        /// Suppresses the plugin when reading the plugins from IPluginsContainer and INamedPlugins.
        /// </summary>
        public static void SuppressPlugin<TPluginInterface, TPlugin>(ContainerBuilder builder)
            where TPlugin : TPluginInterface
        {
            builder.RegisterInstance(new SuppressPlugin(typeof(TPlugin))).Keyed<SuppressPlugin>(typeof(TPluginInterface));
        }

        #endregion
        //================================================================
        #region Log registration statistics

        public static void LogRegistrationStatistics(string title, IContainer container)
        {
            Func<string> generateReport = () =>
            {
                var registrations = container.ComponentRegistry.Registrations
                    .SelectMany(r => r.Services.Select(s => new { pluginInterface = GetServiceType(s), pluginType = r.Activator.LimitType, registration = r }))
                    .OrderBy(r => r.pluginInterface)
                    .ToList();

                var stats = registrations.GroupBy(r => r.pluginInterface)
                    .Select(g => new
                    {
                        pluginInterface = g.Key,
                        pluginsCountDistinct = g.Select(x => x.pluginType).Distinct().Count(),
                        pluginsCount = g.Select(x => x.pluginType).Count()
                    })
                    .OrderBy(stat => stat.pluginInterface)
                    .ToList();

                return title + ":" + string.Join("", stats.Select(stat => "\r\n"
                    + stat.pluginInterface + " " + stat.pluginsCountDistinct + " " + stat.pluginsCount));
            };

            InitializationLogging.Logger.Trace(generateReport);
        }

        private static string GetServiceType(Autofac.Core.Service service)
        {
            if (service is TypedService)
                return GetShortTypeName(((TypedService)service).ServiceType);
            if (service is KeyedService)
                return GetShortTypeName(((KeyedService)service).ServiceType);
            return service.Description;
        }

        private static string GetShortTypeName(Type type)
        {
            if (!type.IsGenericType)
                return type.Name;
            else
                return type.Name + "<" + string.Join(", ", type.GetGenericArguments().Select(ga => GetShortTypeName(ga))) + ">";
        }

        #endregion
    }
}
