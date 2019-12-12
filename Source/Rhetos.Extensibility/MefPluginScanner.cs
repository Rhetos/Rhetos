﻿/*
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
using System.Linq;

namespace Rhetos.Extensibility
{
    public class MefPluginScanner : IPluginScanner
    {
        /// <summary>
        /// The key is FullName of the plugin's export type (it is usually the interface it implements).
        /// </summary>
        private MultiDictionary<string, PluginInfo> _pluginsByExport = null;
        private object _pluginsLock = new object();
        private readonly ILogger _performanceLogger;
        private readonly List<string> _assemblyList;

        public MefPluginScanner(List<string> assemblyList, ILogProvider logProvider)
        {
            _performanceLogger = logProvider.GetLogger("Performance");
            _assemblyList = assemblyList;
        }

        /// <summary>
        /// Returns plugins that are registered for the given type, sorted by dependencies (MefPovider.DependsOn).
        /// </summary>
        public IEnumerable<PluginInfo> FindPlugins(Type pluginInterface)
        {
            return FindPlugins(pluginInterface.FullName);
        }

        /// <summary>
        /// Returns plugins that are registered for the given type, sorted by dependencies (MefPovider.DependsOn).
        /// </summary>
        public IEnumerable<PluginInfo> FindPlugins(string name)
        {
            lock (_pluginsLock)
            {
                if (_pluginsByExport == null)
                {
                    try
                    {
                        _pluginsByExport = LoadPlugins(_assemblyList);
                    }
                    catch (Exception ex)
                    {
                        string typeLoadReport = CsUtility.ReportTypeLoadException(ex, "Cannot load plugins.", _assemblyList);
                        if (typeLoadReport != null)
                            throw new FrameworkException(typeLoadReport, ex);
                        else
                            ExceptionsUtility.Rethrow(ex);
                    }
                }
                return _pluginsByExport.Get(name);
            }
        }

        private MultiDictionary<string, PluginInfo> LoadPlugins(List<string> assemblies)
        {
            var stopwatch = Stopwatch.StartNew();

            var assemblyCatalogs = assemblies.Select(name => new AssemblyCatalog(name));
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

            _performanceLogger.Write(stopwatch, "MefPluginScanner: Loaded plugins (" + pluginsCount + ").");
            return pluginsByExport;
        }

        private void SortByDependency(List<PluginInfo> plugins)
        {
            var dependencies = plugins
                .Where(plugin => plugin.Metadata.ContainsKey(MefProvider.DependsOn))
                .Select(plugin => Tuple.Create((Type)plugin.Metadata[MefProvider.DependsOn], plugin.Type))
                .ToList();

            var pluginTypes = plugins.Select(plugin => plugin.Type).ToList();
            Graph.TopologicalSort(pluginTypes, dependencies);
            Graph.SortByGivenOrder(plugins, pluginTypes, plugin => plugin.Type);
        }
    }
}
