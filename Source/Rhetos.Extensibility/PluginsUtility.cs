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
    public static class PluginsUtility
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
        public static void RegisterPluginModules(ContainerBuilder builder)
        {
            if (!_initialized)
            {
                LoadNewAssemblies(builder);
                _initialized = true;
            }
        }

        /// <summary>Should be called after generating new dlls, to load new plugins from them.</summary>
        public static void DetectAndRegisterNewModulesAndPlugins(IContainer container)
        {
            var newBuilder = new ContainerBuilder();
            LoadNewAssemblies(newBuilder);

            var sw = Stopwatch.StartNew();
            newBuilder.Update(container);
            _performanceLogger.Write(sw, () => "PluginsUtility: Updated Autofac container");
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

                    RegisterNewModules(builder, newAssemblies);
                    _performanceLogger.Write(sw, "PluginsUtility: Registered new modules");
                }
                catch (System.Reflection.ReflectionTypeLoadException ex)
                {
                    var firstFive = ex.LoaderExceptions.Take(5).Select(it => Environment.NewLine + it.Message);
                    throw new FrameworkException("Can't find MEF plugin dependencies:" + string.Concat(firstFive), ex);
                }
            }
        }

        //================================================================

        private class PluginInfo
        {
            public Type Type;
            public IDictionary<string, object> Metadata;
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
                            Metadata = ped.Metadata
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

        private static void RegisterNewModules(ContainerBuilder builder, List<string> newAssemblies)
        {
            var assemblyCatalogs = newAssemblies.Select(a => new AssemblyCatalog(a));
            var container = new CompositionContainer(new AggregateCatalog(assemblyCatalogs));
            var pluginModules = container.GetExports<Module>().ToList();
            foreach (var pluginModule in pluginModules)
            {
                _logger.Trace(() => "Registering module: " + pluginModule.Value.GetType().FullName);
                builder.RegisterModule(pluginModule.Value);
            }
        }

        //================================================================

        /// <summary>
        /// List of previous plugin registerations, used for rescanning when new assemblies are introduced.
        /// Key is the plugin (export) type. Value is optional generic implementation interface.
        /// </summary>
        private static Dictionary<Type, Type> _pluginRegistrations = new Dictionary<Type, Type>();

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

                foreach (var plugin in matchingPlugins)
                {
                    var registration = builder.RegisterType(plugin.Type).As(new[] { exportType });

                    foreach (var metadataElement in plugin.Metadata)
                    {
                        registration = registration.WithMetadata(metadataElement.Key, metadataElement.Value);
                        if (metadataElement.Key == MefProvider.Implements)
                            registration = registration.Keyed(metadataElement.Value, exportType);
                    }

                    if (genericImplementationInterface != null)
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
                            registration = registration.Keyed(implements, exportType);
                    }
                }
            }
        }

        /// <summary>
        /// Scans for plugins that implement the given export type (it is usually the plugin's interface), and registers them.
        /// The function should be called from a plugin module initialization (from Autofac.Module implementation).
        /// </summary>
        public static void RegisterPlugins<TPlugin>(ContainerBuilder builder)
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
        public static void RegisterPlugins<TPlugin>(ContainerBuilder builder, Type genericImplementationInterface)
        {
            lock (_pluginRegistrations)
            {
                RegisterPlugins(builder, _loadedPluginsByExport, typeof(TPlugin), genericImplementationInterface);
                _pluginRegistrations.Add(typeof(TPlugin), genericImplementationInterface);
            }
        }
    }
}
