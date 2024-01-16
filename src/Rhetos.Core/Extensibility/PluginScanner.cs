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
using System.Threading;

namespace Rhetos.Extensibility
{
    /// <summary>
    /// Build-time plugin scanner.
    /// See <see cref="RuntimePluginScanner"/> for runtime plugins.
    /// </summary>
    public class PluginScanner : IPluginScanner
    {
        /// <summary>
        /// The key is FullName of the plugin's export type (it is usually the interface it implements).
        /// </summary>
        private readonly Lazy<MultiDictionary<string, PluginInfo>> _pluginsByExport;
        private readonly ILogger _logger;
        private readonly ILogger _performanceLogger;
        private readonly PluginScannerCache _pluginScannerCache;
        private static readonly object _pluginsCacheLock = new object();
        private readonly HashSet<string> _ignoreAssemblyFiles;
        private readonly string[] _ignoreAssemblyPrefixes;

        /// <summary>
        /// It searches for type implementations in the provided list of assemblies.
        /// </summary>
        /// <param name="pluginAssemblies">List of DLL file paths that will be searched for plugins when invoking the method <see cref="FindPlugins"/>.</param>
        public PluginScanner(IEnumerable<string> pluginAssemblies, RhetosBuildEnvironment buildEnvironment, ILogProvider logProvider, PluginScannerOptions pluginScannerOptions)
        {
            if (string.IsNullOrEmpty(buildEnvironment.CacheFolder))
                throw new ArgumentException($"Configuration setting '{OptionsAttribute.GetConfigurationPath<RhetosBuildEnvironment>()}:{nameof(RhetosBuildEnvironment.CacheFolder)}' in not provided.");

            _pluginsByExport = new Lazy<MultiDictionary<string, PluginInfo>>(() => GetPluginsByExport(pluginAssemblies), LazyThreadSafetyMode.ExecutionAndPublication);
            _pluginScannerCache = new PluginScannerCache(buildEnvironment.CacheFolder, logProvider, new FilesUtility(logProvider));
            _performanceLogger = logProvider.GetLogger("Performance." + GetType().Name);
            _logger = logProvider.GetLogger(GetType().Name);

            var ignoreList = pluginScannerOptions.PredefinedIgnoreAssemblyFiles.Concat(pluginScannerOptions.IgnoreAssemblyFiles ?? Array.Empty<string>()).Distinct().ToList();
            _ignoreAssemblyFiles = new HashSet<string>(ignoreList.Where(name => !name.EndsWith("*")), StringComparer.OrdinalIgnoreCase);
            _ignoreAssemblyPrefixes = ignoreList.Where(name => name.EndsWith("*")).Select(name => name.Trim('*')).ToArray();
        }

        /// <summary>
        /// Returns plugins that are registered for the given interface, sorted by dependencies (MefPovider.DependsOn).
        /// </summary>
        public IEnumerable<PluginInfo> FindPlugins(Type pluginInterface)
        {
            return _pluginsByExport.Value.Get(pluginInterface.FullName);
        }

        public IEnumerable<PluginInfo> FindAllPlugins()
        {
            return _pluginsByExport.Value.SelectMany(x => x.Value);
        }

        private MultiDictionary<string, PluginInfo> GetPluginsByExport(IEnumerable<string> pluginAssemblies)
        {
            List<string> assemblyPaths = pluginAssemblies.Select(path => Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path))).Distinct().ToList();

            foreach (var assembly in assemblyPaths)
                if (!File.Exists(assembly))
                    throw new FrameworkException($"{nameof(PluginScanner)}: The given assembly file path does not exist: '{assembly}'.");
                else
                    _logger.Trace(() => $"Searching for plugins in '{assembly}'");

            MultiDictionary<string, PluginInfo> plugins = null;
            try
            {
                plugins = LoadPlugins(assemblyPaths);
            }
            catch (Exception ex)
            {
                string typeLoadReport = CsUtility.ReportTypeLoadException(ex, "Cannot load plugins.", assemblyPaths);
                if (typeLoadReport != null)
                    throw new FrameworkException(typeLoadReport, ex);
                else
                    ExceptionsUtility.Rethrow(ex);
            }
            return plugins;
        }

        private MultiDictionary<string, PluginInfo> LoadPlugins(List<string> assemblyPaths)
        {
            var stopwatch = Stopwatch.StartNew();

            int ignoredFileCount = 0;
            assemblyPaths = assemblyPaths
                .Where(file =>
                {
                    string fileName = Path.GetFileName(file);
                    bool ignored = _ignoreAssemblyFiles.Contains(fileName)
                        || _ignoreAssemblyPrefixes.Any(prefix => fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
                    if (ignored)
                        ignoredFileCount++;
                    return !ignored;
                })
                .ToList();
            if (ignoredFileCount > 0)
                _logger.Trace($"Ignored {ignoredFileCount} assemblies based on {nameof(PluginScannerOptions)}.");

            lock (_pluginsCacheLock) // Reading and updating cache files should not be done in parallel.
            {
                var cache = _pluginScannerCache.LoadPluginsCacheData();

                bool cacheUpdated = false;
                var cachedAssemblies = 0;
                var pluginsByExport = new List<(Type ExportType, PluginInfo Plugin)>();

                foreach (var assemblyPath in assemblyPaths)
                {
                    var assemblyModifiedToken = new FileInfo(assemblyPath).LastWriteTimeUtc.ToString("O");
                    List<(Type ExportType, PluginInfo Plugin)> pluginsByExportForAssembly;
                    if (cache.Assemblies.TryGetValue(assemblyPath, out var cachedFileData) && cachedFileData.ModifiedTime == assemblyModifiedToken)
                    {
                        pluginsByExportForAssembly = GetMefExportsForAssembly(assemblyPath, cachedFileData.TypesWithExports);
                        cachedAssemblies++;
                    }
                    else
                    {
                        _logger.Trace($"Assembly '{assemblyPath}' is not cached. Scanning all types.");
                        pluginsByExportForAssembly = GetMefExportsForAssembly(assemblyPath);
                    }

                    pluginsByExport.AddRange(pluginsByExportForAssembly);
                    var newCachedFileData = new CachedFileData()
                    {
                        ModifiedTime = assemblyModifiedToken,
                        TypesWithExports = pluginsByExportForAssembly.Select(export => export.Plugin.Type.AssemblyQualifiedName).Distinct().ToList()
                    };
                    if (!cache.Assemblies.ContainsKey(assemblyPath)
                        || !cache.Assemblies[assemblyPath].Equals(newCachedFileData))
                    {
                        cache.Assemblies[assemblyPath] = newCachedFileData;
                        cacheUpdated = true;
                    }
                }

                if (cacheUpdated)
                    _pluginScannerCache.SavePluginsCacheData(cache);

                var loadedPlugins = GroupAndSortByDependency(pluginsByExport);
                var pluginsCount = loadedPlugins.Sum(x => x.Value.Count);

                _logger.Trace($"Used cached data for {cachedAssemblies} out of total {assemblyPaths.Count} assemblies. {pluginsCount} total plugins loaded.");

                _performanceLogger.Write(stopwatch, $"Loaded plugins ({pluginsCount}).");

                return loadedPlugins;
            }
        }

        private List<(Type ExportType, PluginInfo Plugin)> GetMefExportsForAssembly(string assemblyPath, List<string> typesToCheck = null)
        {
            if (typesToCheck != null && typesToCheck.Count == 0) return new List<(Type Type, PluginInfo Plugin)>();

            var assembly = LoadAssembly(assemblyPath);

            var types = typesToCheck == null
                ? assembly.GetTypes()
                : typesToCheck.Select(type => Type.GetType(type) ?? throw new FrameworkException($"Plugin type {type} not found. Check if correct version of the assembly is available.")).ToArray();

            return GetMefExportsForTypes(types);
        }

        private Assembly LoadAssembly(string assemblyPath)
        {
            var assemblyFilename = Path.GetFileNameWithoutExtension(assemblyPath);
            var assembly = Assembly.Load(assemblyFilename);
            ValidateAssembliesEquivalent(assemblyPath, assembly.Location);

            return assembly;
        }

        private void ValidateAssembliesEquivalent(string requestedPath, string actualPath)
        {
            if (requestedPath.Equals(actualPath, StringComparison.OrdinalIgnoreCase)) 
                return;

            var requestedFile = new FileInfo(requestedPath);
            var actualFile = new FileInfo(actualPath);

            if (requestedFile.Length != actualFile.Length) // Ignoring difference in LastWriteTime to avoid spamming with false negatives, because many tools that manage the assemblies unfortunately modify the time (NuGet restore and ASP.NET Shadow Copying, e.g.).
                _logger.Warning($"Assembly at requested path '{requestedPath}' is not the same as loaded assembly at '{actualPath}'. This can cause issues with types.");
            else
                _logger.Trace($"Same assembly loaded from '{actualPath}' instead of '{requestedPath}'.");
        }

        internal static MultiDictionary<string, PluginInfo> GroupAndSortByDependency(List<(Type ExportType, PluginInfo Plugin)> exports)
        {
            var pluginsByExport = new MultiDictionary<string, PluginInfo>();
            foreach (var export in exports)
            {
                pluginsByExport.Add(export.ExportType.FullName, export.Plugin);
            }

            foreach (var pluginsGroup in pluginsByExport)
                SortByDependency(pluginsGroup.Value);

            return pluginsByExport;
        }

        internal static List<(Type ExportType, PluginInfo Plugin)> GetMefExportsForTypes(Type[] types)
        {
            var allAttributes = types
                .Select(type => new { type, customAttributeData = type.GetCustomAttributesData() })
                .Select(info => new
                {
                    info.type,
                    exports = info.customAttributeData.Where(attr => attr.AttributeType == typeof(ExportAttribute)).ToList(),
                    metadata = info.customAttributeData.Where(attr => attr.AttributeType == typeof(ExportMetadataAttribute)).ToList()
                });

            return allAttributes.SelectMany(attr => attr.exports
                .Select(export => new ValueTuple<Type, PluginInfo>
                (
                    (Type)export.ConstructorArguments[0].Value,
                    new PluginInfo()
                    {
                        Type = attr.type,
                        Metadata = attr.metadata
                            .Select(metadata => new KeyValuePair<string, object>((string)metadata.ConstructorArguments[0].Value, metadata.ConstructorArguments[1].Value))
                            .Concat(new[] { new KeyValuePair<string, object>("ExportTypeIdentity", ((Type)export.ConstructorArguments[0].Value)?.FullName) })
                            .ToDictionary(pair => pair.Key, pair => pair.Value)
                    }
                ))).ToList();
        }

        internal static void SortByDependency(List<PluginInfo> plugins)
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
