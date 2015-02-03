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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Rhetos.Extensibility
{
    internal static class MefPluginScanner
    {
        /// <summary>
        /// The key is FullName of the plugin's export type (it is usually the interface it implements).
        /// </summary>
        private static MultiDictionary<string, PluginInfo> _pluginsByExport = null;
        private static object _pluginsLock = new object();

        /// <summary>
        /// Returns plugins that are registered for the given interface, sorted by dependencies (MefPovider.DependsOn).
        /// </summary>
        internal static IEnumerable<PluginInfo> FindPlugins(ContainerBuilder builder, Type pluginInterface)
        {
            try
            {
                lock (_pluginsLock)
                {
                    if (_pluginsByExport == null)
                    {
                        var assemblies = ListAssemblies();
                        _pluginsByExport = LoadPlugins(assemblies);
                    }

                    return _pluginsByExport.Get(pluginInterface.FullName);
                }
            }
            catch (System.Reflection.ReflectionTypeLoadException ex)
            {
                throw new FrameworkException(ReportLoaderExceptions(ex), ex);
            }
        }

        private static List<string> ListAssemblies()
        {
            var stopwatch = Stopwatch.StartNew();

            string[] pluginsPath = new[] { Paths.PluginsFolder, Paths.GeneratedFolder, Paths.DomAssemblyFile };

            List<string> assemblies = new List<string>();
            foreach (var path in pluginsPath)
                if (File.Exists(path))
                    assemblies.Add(Path.GetFullPath(path));
                else if (Directory.Exists(path))
                    assemblies.AddRange(Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories));
            // If the path does not exist, it may be generated later (see DetectAndRegisterNewModulesAndPlugins).

            assemblies.Sort();

            foreach (var assembly in assemblies)
                InitializationLogging.Logger.Trace(() => "Found assembly: " + assembly);

            InitializationLogging.PerformanceLogger.Write(stopwatch, "MefPluginScanner: Listed assemblies (" + assemblies.Count + ").");
            return assemblies;
        }

        private static MultiDictionary<string, PluginInfo> LoadPlugins(List<string> assemblies)
        {
            var stopwatch = Stopwatch.StartNew();

            var assemblyCatalogs = assemblies.Select(a => new AssemblyCatalog(a));
            var container = new CompositionContainer(new AggregateCatalog(assemblyCatalogs));
            var mefPlugins = container.Catalog.Parts
                .Select(part => new
                {
                    PluginType = ReflectionModelServices.GetPartType(part).Value,
                    part.ExportDefinitions
                })
                .SelectMany(part =>
                    part.ExportDefinitions.Select(exportDefinition => new
                    {
                        exportDefinition.ContractName,
                        exportDefinition.Metadata,
                        part.PluginType
                    }));

            var pluginsByExport = new MultiDictionary<string, PluginInfo>();
            int pluginsCount = 0;
            foreach (var mefPlugin in mefPlugins)
            {
                pluginsCount++;
                pluginsByExport.Add(
                    mefPlugin.ContractName,
                    new PluginInfo
                    {
                        Type = mefPlugin.PluginType,
                        Metadata = mefPlugin.Metadata.ToDictionary(m => m.Key, m => m.Value)
                    });
            }

            foreach (var pluginsGroup in pluginsByExport)
                SortByDependency(pluginsGroup.Value);

            InitializationLogging.PerformanceLogger.Write(stopwatch, "MefPluginScanner: Loaded plugins (" + pluginsCount + ").");
            return pluginsByExport;
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

        internal static void ClearCache()
        {
            lock (_pluginsLock)
                _pluginsByExport = null;
        }
    }
}
