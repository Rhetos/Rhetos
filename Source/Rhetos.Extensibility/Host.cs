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
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.IO;
using System.Linq;

namespace Rhetos
{
    public static class Host
    {
        /// <summary>
        /// Helper method that bundles Rhetos runtime location (<see cref="Host.Find"/>),
        /// configuration (<see cref="IRhetosRuntime.BuildConfiguration"/>)
        /// and DI registration (<see cref="IRhetosRuntime.BuildContainer"/>).
        /// </summary>
        /// <param name="assemblyFolder">If not specified, using current application base directory by default.</param>
        /// <param name="logProvider">If not specified, using ConsoleLogProvider by default.</param>
        /// <returns>DI container for Rhetos runtime.</returns>
        public static IContainer Initialize(string assemblyFolder = null, ILogProvider logProvider = null,
            Action<IConfigurationBuilder> addConfiguration = null, Action<ContainerBuilder> registerComponents = null)
        {
            assemblyFolder = assemblyFolder ?? AppDomain.CurrentDomain.BaseDirectory;
            logProvider = logProvider ?? new ConsoleLogProvider();

            var rhetosRuntome = Find(assemblyFolder, logProvider);
            var configurationProvider = rhetosRuntome.BuildConfiguration(logProvider, assemblyFolder, addConfiguration);
            return rhetosRuntome.BuildContainer(logProvider, configurationProvider, registerComponents);
        }

        public static IRhetosRuntime Find(string assemblyFolder, ILogProvider logProvider)
        {
            var supportedExtensions = new[] { ".dll", ".exe" };
            var hostAssemblies = Directory.GetFiles(assemblyFolder).Where(x => supportedExtensions.Contains(Path.GetExtension(x)));

            var pluginScanner = new PluginScanner(
                    () => hostAssemblies,
                    new Utilities.BuildOptions(),
                    new Utilities.RhetosAppEnvironment { AssemblyFolder = assemblyFolder },
                    logProvider, new PluginScannerOptions());

            var rhetosRuntimeTypes = pluginScanner.FindPlugins(typeof(IRhetosRuntime)).Select(x => x.Type).ToList();

            if (rhetosRuntimeTypes.Count == 0)
                throw new FrameworkException($"No implementation of interface {nameof(IRhetosRuntime)} found with Export attribute.");

            if (rhetosRuntimeTypes.Count > 1)
                throw new FrameworkException($"Found multiple implementation of the type {nameof(IRhetosRuntime)}.");

            var rhetosRuntimeInstance = Activator.CreateInstance(rhetosRuntimeTypes.First()) as IRhetosRuntime;

            return rhetosRuntimeInstance;
        }
    }
}
