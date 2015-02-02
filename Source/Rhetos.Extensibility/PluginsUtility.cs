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
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.ReflectionModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Rhetos.Extensibility
{
    public static class PluginsUtility // TODO: Rename to Plugins.
    {
        private static bool _initialized;
        private static ILogger _logger = new NullLogger();
        private static ILogger _performanceLogger = new NullLogger();

        private class NullLogger : ILogger
        {
            public void Write(EventType eventType, Func<string> logMessage) { }
        }

        /// <summary>PluginsUtility is usually used before IoC container is built, so we use this
        /// improvised way of handling the logging, instead of using IoC registered components.</summary>
        public static void SetLogProvider(ILogProvider logProvider)
        {
            _performanceLogger = logProvider.GetLogger("Performance");
            _logger = logProvider.GetLogger("PluginsUtility");
        }

        /// <summary>Should be called once, at system initialization time.</summary>
        public static void RegisterPluginModules(ContainerBuilder builder) // TODO: Rename to FindAndRegisterModules
        {
            if (!_initialized)
            {
                LoadNewAssemblies(builder);
                _initialized = true;
            }
        }

        /// <summary>Should be called after generating new dlls, to load new plugins from them.</summary>
        public static void DetectAndRegisterNewModulesAndPlugins(IContainer container) // TODO: Rename to FindAndRegisterNewModulesAndPlugins
        {
            var newBuilder = new ContainerBuilder();
            LoadNewAssemblies(newBuilder);

            var sw = Stopwatch.StartNew();
            newBuilder.Update(container);
            _performanceLogger.Write(sw, () => "PluginsUtility: Updated Autofac container");
        }

        public static void LogRegistrationStatistics(string title, IContainer container)
        {
            _logger.Trace(() => ReportRegistrationStatistics(title, container.ComponentRegistry));
        }

        private static string ReportRegistrationStatistics(string title, IComponentRegistry componentRegistry)
        {
            var registrations = componentRegistry.Registrations
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

        //================================================================

        private static readonly List<string> _loadedAssemblies = new List<string>();

        private static void LoadNewAssemblies(ContainerBuilder builder)
        {
            lock (_loadedAssemblies)
            {
                try
                {
                    var sw = Stopwatch.StartNew();

                    List<string> newAssemblies = new List<string>();
                    string[] pluginsPath = new[] { Paths.PluginsFolder, Paths.GeneratedFolder, Paths.DomAssemblyFile };

                    foreach (var path in pluginsPath)
                        if (File.Exists(path))
                            newAssemblies.Add(path);
                        else if (Directory.Exists(path))
                            newAssemblies.AddRange(Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories));
                    // If the path does not exist, it may be generated later (see DetectAndRegisterNewModulesAndPlugins).

                    newAssemblies = newAssemblies.Except(_loadedAssemblies).OrderBy(name => name).ToList();
                    _loadedAssemblies.AddRange(newAssemblies);

                    foreach (var assembly in newAssemblies)
                        _logger.Trace(() => "Found assembly: " + assembly);
                    _performanceLogger.Write(sw, "PluginsUtility: Found new assemblies");

                    LoadNewPlugins(builder, newAssemblies);
                    _performanceLogger.Write(sw, "PluginsUtility: Loaded and registered new plugins");

                    RegisterNewModules(builder);
                    _performanceLogger.Write(sw, "PluginsUtility: Registered new modules");
                }
                catch (System.Reflection.ReflectionTypeLoadException ex)
                {
                    throw new FrameworkException(ReportLoaderExceptions(ex), ex);
                }
            }
        }

        private static string ReportLoaderExceptions(System.Reflection.ReflectionTypeLoadException rtle)
        {
            var report = new StringBuilder();
            report.Append("Cannot load plugins. Check for missing assembly or unsupported assembly version:");
            bool first = true;
            foreach (var innerException in rtle.LoaderExceptions.Take(5))
            {
                report.AppendLine();
                report.Append(innerException.Message);

                if (first)
                {
                    var fileLoadException = innerException as FileLoadException;
                    if (fileLoadException != null && !string.IsNullOrEmpty(fileLoadException.FusionLog))
                    {
                        report.AppendLine();
                        report.Append(fileLoadException.FusionLog);
                    }
                }
                first = false;
            }
            return report.ToString();
        }

        //================================================================

        private class PluginInfo
        {
            public Type Type;
            public Dictionary<string, object> Metadata;
        }

        /// <summary>
        /// The key is FullName of the plugin's export type (it is usually the inteface it implements).
        /// </summary>
        private static readonly Dictionary<string, List<PluginInfo>> _loadedPluginsByExport = new Dictionary<string, List<PluginInfo>>();

        private static void LoadNewPlugins(ContainerBuilder builder, List<string> newAssemblies)
        {
            lock (_loadedPluginsByExport)
            {
                var sw = Stopwatch.StartNew();

                var assemblyCatalogs = newAssemblies.Select(a => new AssemblyCatalog(a));
                var container = new CompositionContainer(new AggregateCatalog(assemblyCatalogs));

                Dictionary<string, List<PluginInfo>> newPlugins = container.Catalog.Parts
                    .SelectMany(part => part.ExportDefinitions.Select(exportDefinition =>
                        new { Part = part, exportDefinition.ContractName, exportDefinition.Metadata }))
                    .GroupBy(ped => ped.ContractName)
                    .ToDictionary(g => g.Key, g => g.Select(
                        ped => new PluginInfo
                        {
                            Type = ReflectionModelServices.GetPartType(ped.Part).Value,
                            Metadata = ped.Metadata.ToDictionary(e => e.Key, e => e.Value)
                        }).ToList());

                _performanceLogger.Write(sw, "PluginsUtility: Loaded MEF plugins");

                foreach (var newPluginExport in newPlugins)
                {
                    if (!_loadedPluginsByExport.ContainsKey(newPluginExport.Key))
                        _loadedPluginsByExport.Add(newPluginExport.Key, new List<PluginInfo>());

                    _loadedPluginsByExport[newPluginExport.Key].AddRange(newPluginExport.Value);
                }

                RegisterNewPlugins(builder, newPlugins);
            }
        }

        //================================================================

        private static HashSet<Type> _registeredModules = new HashSet<Type>();

        private static void RegisterNewModules(ContainerBuilder builder)
        {
            var allModulesPluginInfo = _loadedPluginsByExport[typeof(Module).FullName];
            SortByDependency(allModulesPluginInfo);
            var allModules = allModulesPluginInfo.Select(p => p.Type);

            var newModules = allModules.Except(_registeredModules).ToList();
            foreach (var module in newModules)
            {
                _logger.Trace(() => "Registering module: " + module.FullName);
                builder.RegisterModule((Module)Activator.CreateInstance(module));
                _registeredModules.Add(module);
            }
        }

        private static void SortByDependency(List<PluginInfo> plugins)
        {
            var dependencies = plugins
                .Where(p => p.Metadata.ContainsKey(MefProvider.DependsOn))
                .Select(p => Tuple.Create((Type)p.Metadata[MefProvider.DependsOn], p.Type))
                .ToList();

            var pluginTypes = plugins.Select(p => p.Type).ToList();
            Graph.TopologicalSort(pluginTypes, dependencies);
            Graph.SortByGivenOrder(plugins, pluginTypes, p => p.Type);
        }

        //================================================================

        /// <summary>
        /// List of previous plugin registerations, used for rescanning when new assemblies are introduced.
        /// Key is the plugin (export) type. Value is optional generic implementation interface.
        /// </summary>
        private static Dictionary<Type, Type> _pluginRegistrations = new Dictionary<Type, Type>
            {
                { typeof(Module), null }
            };

        private static void RegisterNewPlugins(ContainerBuilder builder, Dictionary<string, List<PluginInfo>> newPlugins)
        {
            lock (_pluginRegistrations)
                foreach (var pluginRegistration in _pluginRegistrations)
                    RegisterPlugins(builder, newPlugins, pluginRegistration.Key, pluginRegistration.Value);
        }

        private static void RegisterPlugins(ContainerBuilder builder, Dictionary<string, List<PluginInfo>> pluginsByExport, Type exportType, Type genericImplementationInterface)
        {
            List<PluginInfo> matchingPlugins;
            if (pluginsByExport.TryGetValue(exportType.FullName, out matchingPlugins))
            {
                _logger.Trace(() => "Registering plugins: " + exportType.FullName + " (" + matchingPlugins.Count + ")");

                if (genericImplementationInterface != null)
                    foreach (var plugin in matchingPlugins)
                        ExtractGenericPluginImplementsMetadata(plugin, genericImplementationInterface);

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
        }

        private static void ExtractGenericPluginImplementsMetadata(PluginInfo plugin, Type genericImplementationInterface)
        {
            var implementsTypes = plugin.Type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericImplementationInterface)
                .Select(i => i.GetGenericArguments()[0])
                .ToList();

            if (implementsTypes.Count == 0)
                throw new FrameworkException(string.Format(
                    "Plugin {0} does not implement generic interface {1}.",
                    plugin.Type.FullName,
                    genericImplementationInterface.FullName));

            foreach (Type implements in implementsTypes)
                plugin.Metadata.Add(MefProvider.Implements, implements);
        }

        /// <summary>
        /// Scans for plugins that implement the given export type (it is usually the plugin's interface), and registers them.
        /// The function should be called from a plugin module initialization (from Autofac.Module implementation).
        /// </summary>
        public static void RegisterPlugins<TPlugin>(ContainerBuilder builder) // TODO: Rename to FindAndRegisterPlugins.
        {
            lock (_pluginRegistrations)
            {
                RegisterPlugins(builder, _loadedPluginsByExport, typeof(TPlugin), null);
                _pluginRegistrations.Add(typeof(TPlugin), null);
            }
        }

        /// <summary>
        /// Scans for plugins that implement the given export type (it is usually the plugin's interface), and registers them.
        /// The function should be called from a plugin module initialization (from Autofac.Module implementation).
        /// </summary>
        /// <param name="genericImplementationInterface">
        /// Argument type that the plugin handles is automatically extracted from the provided genericImplementationInterface parameter.
        /// This is an alternative to using MefProvider.Implements in the plugin's ExportMetadata attribute.
        /// </param>
        public static void RegisterPlugins<TPlugin>(ContainerBuilder builder, Type genericImplementationInterface) // TODO: Rename to FindAndRegisterPlugins.
        {
            lock (_pluginRegistrations)
            {
                RegisterPlugins(builder, _loadedPluginsByExport, typeof(TPlugin), genericImplementationInterface);
                _pluginRegistrations.Add(typeof(TPlugin), genericImplementationInterface);
            }
        }

        /// <summary>
        /// Since the last registration is considered the active one, when overriding previous registrations
        /// use this function to verify if the previous plugins are already registered and will be overriden.
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
                            error += " The following plugins should have been previous registered in order to be overriden: "
                                + string.Join(", ", missingRegistration.Select(r => r.Name)) + ".";

                        if (excessRegistration.Count > 0)
                            error += " The following plugins have been previous registered and whould be unintentionally overriden: "
                                + string.Join(", ", excessRegistration.Select(r => r.Name)) + ".";

                        error += " Verify that the module registration is occurring in the right order: Use [ExportMetadata(MefProvider.DependsOn, typeof(other Autofac.Module implementation))], to make those registration will occur before this one.";

                        throw new FrameworkException(error);
                    }
                });
        }
    }
}
