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

using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Rhetos.Extensibility
{
    public class RuntimePluginScanner : IPluginScanner
    {
        private readonly Lazy<MultiDictionary<string, PluginInfo>> _pluginsByExport;
        private readonly ILogger _performanceLogger;

        /// <summary>
        /// It searches for type implementations in the provided list of assemblies and types.
        /// </summary>
        /// <param name="pluginAssemblies">List of DLL file paths that will be searched for plugins when invoking the method <see cref="FindPlugins"/>.</param>
        /// <param name="pluginTypes">List of additional types that will be used for plugins search.</param>
        public RuntimePluginScanner(IEnumerable<Assembly> pluginAssemblies, IEnumerable<Type> pluginTypes, ILogProvider logProvider)
        {
            _pluginsByExport = new Lazy<MultiDictionary<string, PluginInfo>>(() => GetPluginsByExport(pluginAssemblies, pluginTypes), LazyThreadSafetyMode.ExecutionAndPublication);
            _performanceLogger = logProvider.GetLogger("Performance." + GetType().Name);
        }

        /// <summary>
        /// Returns plugins that are registered for the given interface, sorted by dependencies (MefPovider.DependsOn).
        /// </summary>
        public IEnumerable<PluginInfo> FindPlugins(Type pluginInterface)
        {
            return _pluginsByExport.Value.Get(pluginInterface.FullName);
        }

        private MultiDictionary<string, PluginInfo> GetPluginsByExport(IEnumerable<Assembly> assemblies, IEnumerable<Type> types)
        {
            var stopwatch = Stopwatch.StartNew();

            var typesToCheck = assemblies.SelectMany(x => x.GetTypes()).Concat(types).ToArray();
            var loadedPlugins = PluginScanner.GroupAndSortByDependency(PluginScanner.GetMefExportsForTypes(typesToCheck));
            var pluginsCount = loadedPlugins.Sum(x => x.Value.Count);

            _performanceLogger.Write(stopwatch, $"Loaded plugins ({pluginsCount}).");

            return loadedPlugins;
        }
    }
}
