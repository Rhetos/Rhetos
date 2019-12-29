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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace Rhetos.Extensibility
{
    public class PluginScanner : IPluginScanner
    {
        /// <summary>
        /// The key is FullName of the plugin's export type (it is usually the interface it implements).
        /// </summary>
        private MultiDictionary<string, PluginInfo> _pluginsByExport = null;
        private object _pluginsLock = new object();
        private readonly ILogger _logger;
        private readonly FilesUtility _filesUtility;
        private readonly ILogger _performanceLogger;
        private readonly Func<IEnumerable<string>> _findAssemblies;
        private readonly PluginScannerOptions _options;

        /// <summary>
        /// It searches for type implementations in the provided list of assemblies.
        /// </summary>
        /// <param name="findAssemblies">The findAssemblies function should return a list of assembly file paths that will be searched for plugins when invoking the method <see cref="PluginScanner.FindPlugins"/></param>
        public PluginScanner(Func<IEnumerable<string>> findAssemblies, PluginScannerOptions options, ILogProvider logProvider)
        {
            _findAssemblies = findAssemblies;
            _options = options;
            _performanceLogger = logProvider.GetLogger("Performance");
            _logger = logProvider.GetLogger("Plugins");
            _filesUtility = new FilesUtility(logProvider);
        }

        /// <summary>
        /// Returns plugins that are registered for the given interface, sorted by dependencies (MefPovider.DependsOn).
        /// </summary>
        public IEnumerable<PluginInfo> FindPlugins(Type pluginInterface)
        {
            lock (_pluginsLock)
            {
                if (_pluginsByExport == null)
                {
                    var assemblies = ListAssemblies();

                    try
                    {
                        _pluginsByExport = LoadPlugins(assemblies);
                    }
                    catch (Exception ex)
                    {
                        string typeLoadReport = CsUtility.ReportTypeLoadException(ex, "Cannot load plugins.", assemblies);
                        if (typeLoadReport != null)
                            throw new FrameworkException(typeLoadReport, ex);
                        else
                            ExceptionsUtility.Rethrow(ex);
                    }
                }
                return _pluginsByExport.Get(pluginInterface.FullName);
            }
        }

        private List<string> ListAssemblies()
        {
            var stopwatch = Stopwatch.StartNew();

            var assemblies = _findAssemblies().ToList();

            foreach (var assembly in assemblies)
                if (!File.Exists(assembly))
                    throw new FrameworkException($"{nameof(PluginScanner)}: The given assembly file path does not exist: '{assembly}'.");
                else
                    _logger.Trace(() => $"Searching for plugins in '{assembly}'");

            _performanceLogger.Write(stopwatch, $"{nameof(PluginScanner)}: Listed assemblies ({assemblies.Count}).");
            return assemblies;
        }

        private MultiDictionary<string, PluginInfo> LoadPlugins(List<string> assemblyPaths)
        {
            var stopwatch = Stopwatch.StartNew();

            var cacheFilename = Path.Combine(_options.GeneratedFilesCacheFolder, _options.PluginScannerCacheFilename);
            var cacheContents = Directory.Exists(_options.GeneratedFilesCacheFolder) && File.Exists(cacheFilename) ? File.ReadAllText(cacheFilename) : null;
            var cache = cacheContents == null ? new PluginsCacheData() : JsonConvert.DeserializeObject<PluginsCacheData>(cacheContents);

            var newCache = new PluginsCacheData();
            var cachedAssemblies = 0;
            var pluginsByExport = new MultiDictionary<string, PluginInfo>();
            var pluginsCount = 0;
            
            foreach (var assemblyPath in assemblyPaths)
            {
                var assemblyModifiedToken = new FileInfo(assemblyPath).LastWriteTimeUtc.ToString("O");
                Dictionary<Type, List<PluginInfo>> exports;
                if (cache.Assemblies.TryGetValue(assemblyPath, out var cachedFileData) && cachedFileData.ModifiedTime == assemblyModifiedToken)
                {
                    exports = GetMefExportsForAssembly(assemblyPath, cachedFileData.TypesWithExports);
                    cachedAssemblies++;
                }
                else
                {
                    exports = GetMefExportsForAssembly(assemblyPath);
                }

                foreach (var export in exports)
                {
                    foreach (var plugin in export.Value)
                    {
                        pluginsByExport.Add(export.Key.FullName, plugin);
                        pluginsCount++;
                    }
                }

                newCache.Assemblies.Add(assemblyPath, new CachedFileData()
                {
                    ModifiedTime = assemblyModifiedToken,
                    TypesWithExports = exports.SelectMany(export => export.Value.Select(plugin => plugin.Type.AssemblyQualifiedName)).Distinct().ToList()
                });
            }

            _logger.Trace($"Used cached data for {cachedAssemblies} out of total {assemblyPaths.Count} assemblies.");

            var newCacheContents = JsonConvert.SerializeObject(newCache, Formatting.Indented);
            var cacheValid = newCacheContents == cacheContents;
            if (!cacheValid)
            {
                _filesUtility.SafeCreateDirectory(_options.GeneratedFilesCacheFolder);
                File.WriteAllText(cacheFilename, newCacheContents);
            }

            _logger.Trace($"CacheValid={cacheValid} for '{cacheFilename}'.{(cacheValid ? "" : " Saving new data.")}");

            foreach (var pluginsGroup in pluginsByExport)
                SortByDependency(pluginsGroup.Value);

            _performanceLogger.Write(stopwatch, $"{nameof(PluginScanner)}: Loaded plugins ({pluginsCount}).");

            return pluginsByExport;
        }

        private Dictionary<Type, List<PluginInfo>> GetMefExportsForAssembly(string assemblyPath, List<string> typesToCheck = null)
        {
            if (typesToCheck != null && typesToCheck.Count == 0) return new Dictionary<Type, List<PluginInfo>>();

            var assembly = LoadAssembly(assemblyPath);
            var types = typesToCheck == null
                ? assembly.GetTypes()
                : typesToCheck.Select(type => Type.GetType(type)).ToArray();

            return GetMefExportsForTypes(types);
        }

        private Assembly LoadAssembly(string assemblyPath)
        {
            Assembly assembly = null;
            var assemblyFilename = Path.GetFileNameWithoutExtension(assemblyPath);

            try
            {
                assembly = Assembly.Load(assemblyFilename);
                _logger.Trace($"'{assemblyFilename}' loaded from '{assembly.Location}'.");
            }
            catch (Exception e)
            {
                _logger.Trace($"'{assemblyFilename}' could not by loaded from probing paths. ({e.GetType().Name}: {e.Message})");
            }

            if (assembly == null)
            {
                try
                {
                    assembly = Assembly.LoadFrom(assemblyPath);
                    _logger.Trace($"'{assemblyFilename}' loaded from '{assembly.Location}' via explicit LoadFrom.");
                }
                catch (Exception e)
                {
                    _logger.Trace($"'{assemblyFilename}' could not by loaded from '{assemblyPath}'. ({e.GetType().Name}: {e.Message})");
                }
            }

            if (assembly == null)
                throw new FrameworkException($"Failed to load requested assembly '{assemblyFilename}' either from application probing paths or from '{assemblyPath}'.");

            ValidateAssembliesEquivalent(assemblyPath, assembly.Location);
            return assembly;
        }

        private void ValidateAssembliesEquivalent(string requestedPath, string actualPath)
        {
            if (requestedPath.Equals(actualPath, StringComparison.InvariantCultureIgnoreCase)) 
                return;

            var requestedFile = new FileInfo(requestedPath);
            var actualFile = new FileInfo(actualPath);

            if (requestedFile.Length != actualFile.Length || requestedFile.LastWriteTimeUtc != actualFile.LastWriteTimeUtc)
                _logger.Info($"Assembly at requested path '{requestedPath}' is not the same as loaded assembly at '{actualPath}'. This can cause issues with types.");
        }

        private static Dictionary<Type, List<PluginInfo>> GetMefExportsForTypes(Type[] types)
        {
            var allAttributes = types
                .Select(type => new { type, customAttributeData = type.GetCustomAttributesData()})
                .Select(info => new
                {
                    type = info.type,
                    exports = info.customAttributeData.Where(attr => attr.AttributeType == typeof(ExportAttribute)).ToList(),
                    metadata = info.customAttributeData.Where(attr => attr.AttributeType == typeof(ExportMetadataAttribute)).ToList()
                });

            var byExport = allAttributes.SelectMany(attr => attr.exports
                .Select(export => new
                {
                    exportType = (Type)export.ConstructorArguments[0].Value,
                    pluginInfo = new PluginInfo()
                    {
                        Type = attr.type,
                        Metadata = attr.metadata
                            .Select(metadata => new KeyValuePair<string, object>((string)metadata.ConstructorArguments[0].Value, metadata.ConstructorArguments[1].Value))
                            .Concat(new[] { new KeyValuePair<string, object>("ExportTypeIdentity", ((Type)export.ConstructorArguments[0].Value)?.FullName) })
                            .ToDictionary(pair => pair.Key, pair => pair.Value)
                    }
                }));

            var groupedInfo = byExport
                .GroupBy(info => info.exportType)
                .ToDictionary(group => group.Key, group => group.Select(plugin => plugin.pluginInfo).ToList());

            return groupedInfo;
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
