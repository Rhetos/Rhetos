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
using System.IO;
using System.Linq;
using System.Reflection;

namespace Rhetos.Extensibility
{
    public static class AssemblyResolver
    {
        public static string[] GetRuntimeAssemblies(IConfiguration configuration)
        {
            var rhetosAppOptions = configuration.GetOptions<RhetosAppOptions>();

            if (string.IsNullOrEmpty(rhetosAppOptions.RhetosRuntimePath))
                throw new FrameworkException($"Run-time configuration option '{nameof(RhetosAppOptions)}.{nameof(RhetosAppOptions.RhetosRuntimePath)}' is not provided.");
            string runtimeAssemblyFolder = Path.GetDirectoryName(rhetosAppOptions.RhetosRuntimePath);

            IEnumerable<string> searchFolders;

            if (rhetosAppOptions.AssemblyFolders != null && rhetosAppOptions.AssemblyFolders.Any())
                searchFolders = rhetosAppOptions.AssemblyFolders;
            else
                searchFolders = new[] { runtimeAssemblyFolder };

            var foundAssemblies = searchFolders
                .Where(folder => Directory.Exists(folder))
                .SelectMany(folder => Directory.GetFiles(folder, "*.dll", SearchOption.TopDirectoryOnly));

            return new[] { rhetosAppOptions.RhetosRuntimePath }.Concat(foundAssemblies)
                .Distinct()
                .ToArray();
        }

        /// <param name="warningOnDuplicateFiles">Suppress the warnings to avoid spamming the application run-time log. The warnings should show on build.</param>
        public static ResolveEventHandler GetResolveEventHandler(IEnumerable<string> assemblies, ILogProvider logProvider, bool warningOnDuplicateFiles)
        {
            var logger = logProvider.GetLogger(nameof(AssemblyResolver));

            var byFilename = assemblies
                .GroupBy(Path.GetFileName)
                .Select(group => new { filename = group.Key, paths = group.OrderBy(path => path.Length).ThenBy(path => path).ToList() })
                .ToList();

            foreach (var duplicate in byFilename.Where(dll => dll.paths.Count > 1))
            {
                var otherPaths = string.Join(", ", duplicate.paths.Skip(1).Select(path => $"'{path}'"));
                logger.Write(
                    warningOnDuplicateFiles ? EventType.Warning : EventType.Trace,
                    $"Multiple files for '{duplicate.filename}' are provided. This can cause type errors. Loaded: '{duplicate.paths.First()}', ignored: {otherPaths}.");
            }

            var namesToPaths = byFilename.ToDictionary(dll => dll.filename, dll => dll.paths.First(), StringComparer.InvariantCultureIgnoreCase);

            return (sender, args) => LoadAssemblyFromSpecifiedPaths(args, namesToPaths, logger);
        }

        private static Assembly LoadAssemblyFromSpecifiedPaths(ResolveEventArgs args, Dictionary<string, string> namesToPaths, ILogger logger)
        {
            var filename = $"{new AssemblyName(args.Name).Name}.dll";
            if (namesToPaths.TryGetValue(filename, out var path))
            {
                logger.Trace(() => $"Custom resolver found assembly '{args.Name}' at '{path}'.");
                return Assembly.LoadFrom(path);
            }

            return null;
        }
    }
}
