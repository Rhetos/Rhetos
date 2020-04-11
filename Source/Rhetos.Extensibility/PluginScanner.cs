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
        /// <param name="findAssemblies">The findAssemblies function should return a list of DLL file paths that will be searched for plugins when invoking the method <see cref="FindPlugins"/></param>
        public PluginScanner(Func<IEnumerable<string>> findAssemblies, string cacheFolder, ILogProvider logProvider, PluginScannerOptions pluginScannerOptions)
        {
            _pluginsByExport = new Lazy<MultiDictionary<string, PluginInfo>>(
                () => GetPluginsByExport(findAssemblies),
                LazyThreadSafetyMode.ExecutionAndPublication);
            _pluginScannerCache = new PluginScannerCache(cacheFolder, logProvider, new FilesUtility(logProvider));
            _performanceLogger = logProvider.GetLogger("Performance." + GetType().Name);
            _logger = logProvider.GetLogger(GetType().Name);

            var ignoreList = pluginScannerOptions.PredefinedIgnoreAssemblyFiles.Concat(pluginScannerOptions.IgnoreAssemblyFiles ?? Array.Empty<string>()).Distinct().ToList();
            _ignoreAssemblyFiles = new HashSet<string>(ignoreList.Where(name => !name.EndsWith("*")), StringComparer.OrdinalIgnoreCase);
            _ignoreAssemblyPrefixes = ignoreList.Where(name => name.EndsWith("*")).Select(name => name.Trim('*')).ToArray();
        }

        /// <summary>
        /// Finds the cache folder in either build-time or run-time configuration, since plugin scanner is used in both environments.
        /// </summary>
        public static string GetCacheFolder(IConfiguration configurationProvider)
        {
            var rhetosBuildEnvironment = configurationProvider.GetOptions<RhetosBuildEnvironment>();
            var rhetosAppEnvironment = configurationProvider.GetOptions<RhetosAppEnvironment>();
            string cacheFolder = rhetosBuildEnvironment?.CacheFolder ?? rhetosAppEnvironment?.AssemblyFolder;
            if (cacheFolder == null)
                throw new FrameworkException($"Missing configuration settings for build ({nameof(RhetosBuildEnvironment.CacheFolder)})" +
                    $" or runtime ({nameof(RhetosAppEnvironment.AssemblyFolder)}).");
            return cacheFolder;
        }

        /// <summary>
        /// Returns plugins that are registered for the given interface, sorted by dependencies (MefPovider.DependsOn).
        /// </summary>
        public IEnumerable<PluginInfo> FindPlugins(Type pluginInterface)
        {
            return _pluginsByExport.Value.Get(pluginInterface.FullName);
        }

        private MultiDictionary<string, PluginInfo> GetPluginsByExport(Func<IEnumerable<string>> findAssemblies)
        {
            var assemblies = ListAssemblies(findAssemblies);
            var resolver = CreateAssemblyResolveDelegate(assemblies);
            MultiDictionary<string, PluginInfo> plugins = null;
            try
            {
                AppDomain.CurrentDomain.AssemblyResolve += resolver;
                plugins = LoadPlugins(assemblies);
            }
            catch (Exception ex)
            {
                string typeLoadReport = CsUtility.ReportTypeLoadException(ex, "Cannot load plugins.", assemblies);
                if (typeLoadReport != null)
                    throw new FrameworkException(typeLoadReport, ex);
                else
                    ExceptionsUtility.Rethrow(ex);
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= resolver;
            }
            return plugins;
        }

        private ResolveEventHandler CreateAssemblyResolveDelegate(List<string> assemblies)
        {
            var byFilename = assemblies
                .GroupBy(Path.GetFileName)
                .Select(group => new {filename = group.Key, paths = group.OrderBy(path => path.Length).ThenBy(path => path).ToList()})
                .ToList();

            foreach (var duplicate in byFilename.Where(dll => dll.paths.Count > 1))
            {
                var otherPaths = string.Join(", ", duplicate.paths.Skip(1).Select(path => $"'{path}'"));
                _logger.Warning($"Multiple paths for '{duplicate.filename}' found. This causes ambiguous DLL loading and can cause type errors. Loaded: '{duplicate.paths.First()}', ignored: {otherPaths}.");
            }

            var namesToPaths = byFilename.ToDictionary(dll => dll.filename, dll => dll.paths.First(), StringComparer.InvariantCultureIgnoreCase);

            return (sender, args) => LoadAssemblyFromSpecifiedPaths(args, namesToPaths);
        }

        private Assembly LoadAssemblyFromSpecifiedPaths(ResolveEventArgs args, Dictionary<string, string> namesToPaths)
        {
            var filename = $"{new AssemblyName(args.Name).Name}.dll";
            if (namesToPaths.TryGetValue(filename, out var path))
            {
                _logger.Trace(() => $"Custom resolver found assembly '{args.Name}' at '{path}'.");
                return Assembly.LoadFrom(path);
            }

            return null;
        }

        private List<string> ListAssemblies(Func<IEnumerable<string>> findAssemblies)
        {
            var stopwatch = Stopwatch.StartNew();

            var assemblies = findAssemblies()
                .Select(path => (Path: path, Name: Path.GetFileName(path)))
                .Where(file => !_ignoreAssemblyFiles.Contains(file.Name) && !_ignoreAssemblyPrefixes.Any(prefix => file.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                .Select(file => Path.GetFullPath(file.Path))
                .Distinct().ToList();

            foreach (var assembly in assemblies)
                if (!File.Exists(assembly))
                    throw new FrameworkException($"{nameof(PluginScanner)}: The given assembly file path does not exist: '{assembly}'.");
                else
                    _logger.Trace(() => $"Searching for plugins in '{assembly}'");

            _performanceLogger.Write(stopwatch, $"Listed assemblies ({assemblies.Count}).");
            return assemblies;
        }

        private MultiDictionary<string, PluginInfo> LoadPlugins(List<string> assemblyPaths)
        {
            var stopwatch = Stopwatch.StartNew();

            lock (_pluginsCacheLock) // Reading and updating cache files should not be done in parallel.
            {
                var cache = _pluginScannerCache.LoadPluginsCacheData();

                bool cacheUpdated = false;
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
                        _logger.Trace($"Assembly '{assemblyPath}' is not cached. Scanning all types.");
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

                    var newCachedFileData = new CachedFileData()
                    {
                        ModifiedTime = assemblyModifiedToken,
                        TypesWithExports = exports.SelectMany(export => export.Value.Select(plugin => plugin.Type.AssemblyQualifiedName)).Distinct().ToList()
                    };
                    if (!cache.Assemblies.ContainsKey(assemblyPath)
                        || !cache.Assemblies[assemblyPath].Equals(newCachedFileData))
                    {
                        cache.Assemblies[assemblyPath] = newCachedFileData;
                        cacheUpdated = true;
                    }
                }

                _logger.Trace($"Used cached data for {cachedAssemblies} out of total {assemblyPaths.Count} assemblies. {pluginsCount} total plugins loaded.");

                if (cacheUpdated)
                    _pluginScannerCache.SavePluginsCacheData(cache);

                foreach (var pluginsGroup in pluginsByExport)
                    SortByDependency(pluginsGroup.Value);

                _performanceLogger.Write(stopwatch, $"Loaded plugins ({pluginsCount}).");

                return pluginsByExport;
            }
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
            var assemblyFilename = Path.GetFileNameWithoutExtension(assemblyPath);
            var assembly = Assembly.Load(assemblyFilename);
            ValidateAssembliesEquivalent(assemblyPath, assembly.Location);

            return assembly;
        }

        private void ValidateAssembliesEquivalent(string requestedPath, string actualPath)
        {
            if (requestedPath.Equals(actualPath, StringComparison.InvariantCultureIgnoreCase)) 
                return;

            var requestedFile = new FileInfo(requestedPath);
            var actualFile = new FileInfo(actualPath);

            if (requestedFile.Length != actualFile.Length || !requestedFile.LastWriteTimeUtc.Equals(actualFile.LastWriteTimeUtc))
                _logger.Warning($"Assembly at requested path '{requestedPath}' is not the same as loaded assembly at '{actualPath}'. This can cause issues with types.");
            else
                _logger.Trace($"Same assembly loaded from '{actualPath}' instead of '{requestedPath}'.");
        }

        private static Dictionary<Type, List<PluginInfo>> GetMefExportsForTypes(Type[] types)
        {
            var allAttributes = types
                .Select(type => new { type, customAttributeData = type.GetCustomAttributesData()})
                .Select(info => new
                {
                    info.type,
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
