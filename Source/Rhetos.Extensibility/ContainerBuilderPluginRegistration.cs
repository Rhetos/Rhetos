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
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.Extensibility
{
    public class ContainerBuilderPluginRegistration
    {
        private readonly ILogger _logger;
        private readonly ContainerBuilder _builder;
        private readonly IPluginScanner _pluginScanner;

        /// <summary>
        /// It is important to understand that Autofac will provide a new instance of <see cref="ContainerBuilder"/>
        /// for each module registration, and this is why we need to create a fresh instance of ContainerBuilderPluginRegistration
        /// for each module, instead of reusing a single one during the initialization of DI container.
        /// </summary>
        public ContainerBuilderPluginRegistration(ContainerBuilder builder, ILogProvider logProvider, IPluginScanner pluginScanner)
        {
            _builder = builder;
            _logger = logProvider.GetLogger("Plugins");
            _pluginScanner = pluginScanner;
        }

        /// <summary>
        /// Find and registers Autofac modules that are implemented as plugins.
        /// </summary>
        public void FindAndRegisterPluginModules()
        {
            var modules = _pluginScanner.FindPlugins(typeof(Module));

            foreach (var module in modules)
            {
                _logger.Trace(() => "Registering module: " + module.Type.FullName);
                _builder.RegisterModule((Module)Activator.CreateInstance(module.Type));
            }
        }

        /// <summary>
        /// Scans for plugins that implement the given export type (it is usually the plugin's interface), and registers them.
        /// The function should be called from a plugin module initialization (from Autofac.Module implementation).
        /// </summary>
        public void FindAndRegisterPlugins<TPluginInterface>()
        {
            var matchingPlugins = _pluginScanner.FindPlugins(typeof(TPluginInterface));
            RegisterPlugins(matchingPlugins, typeof(TPluginInterface));
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
        public void FindAndRegisterPlugins<TPluginInterface>(Type genericImplementationBase)
        {
            var matchingPlugins = _pluginScanner.FindPlugins(typeof(TPluginInterface));

            foreach (var plugin in matchingPlugins)
                ExtractGenericPluginImplementsMetadata(plugin, genericImplementationBase);

            RegisterPlugins(matchingPlugins, typeof(TPluginInterface));
        }

        /// <summary>
        /// Suppresses the plugin when reading the plugins from IPluginsContainer and INamedPlugins.
        /// </summary>
        public void SuppressPlugin<TPluginInterface, TPlugin>()
            where TPlugin : TPluginInterface
        {
            _builder.RegisterInstance(new SuppressPlugin(typeof(TPlugin))).Keyed<SuppressPlugin>(typeof(TPluginInterface));
        }

        private void RegisterPlugins(IEnumerable<PluginInfo> matchingPlugins, Type exportType)
        {
            if (!matchingPlugins.Any())
                return;

            _logger.Trace(() => "Registering plugins: " + exportType.FullName + " (" + matchingPlugins.Count() + ")");

            foreach (var plugin in matchingPlugins)
            {
                var registration = _builder.RegisterType(plugin.Type).As(new[] { exportType });

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
        private void ExtractGenericPluginImplementsMetadata(PluginInfo plugin, Type genericImplementationBase)
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

        #region Log registration statistics

        public static void LogRegistrationStatistics(string title, IComponentContext scope, ILogProvider logProvider)
        {
            var logger = logProvider.GetLogger("Plugins");
            logger.Trace(() => title + ":\r\n" + string.Join("\r\n", GetRegistrationStatistics(scope)));
        }

        private static IEnumerable<string> GetRegistrationStatistics(IComponentContext scope)
        {
            var registrations = scope.ComponentRegistry.Registrations
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

            return stats.Select(stat => $"{stat.pluginInterface} {stat.pluginsCountDistinct} {stat.pluginsCount}");
        }

        private static string GetServiceType(Autofac.Core.Service service)
        {
            if (service is TypedService typedService)
                return CsUtility.GetShortTypeName(typedService.ServiceType);
            if (service is KeyedService keyedService)
                return CsUtility.GetShortTypeName(keyedService.ServiceType);
            return service.Description;
        }

        #endregion
    }
}
