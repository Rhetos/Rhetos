/*
    Copyright (C) 2013 Omega software d.o.o.

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
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.ReflectionModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;

namespace Rhetos.Extensibility
{
    public static class PluginsUtility
    {
        private static readonly string _rootPath = AppDomain.CurrentDomain.BaseDirectory;

        public static List<string> DeployPackagesAdditionalAssemblies = new List<string>(); // TODO: Remove this hack after ServerDom.dll is moved to the bin\Generated subfolder.

        public static string[] ListPluginsAssemblies()
        {
            List<string> pluginsAssemblies = new List<string>();
            if (ConfigurationManager.AppSettings["PluginsSearch"] != null)
            {
                IEnumerable<string> pluginsPath = ConfigurationManager.AppSettings["PluginsSearch"].Split(new[] { ',' })
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrEmpty(p))
                    .Select(p => Path.Combine(_rootPath, p));

                foreach (var path in pluginsPath)
                    if (File.Exists(path))
                        pluginsAssemblies.Add(path);
                    else if (Directory.Exists(path))
                        pluginsAssemblies.AddRange(Directory.GetFiles(path, "*.dll", SearchOption.AllDirectories));
                    else
                        throw new FrameworkException(String.Format("PluginsSearch file or folder \"{0}\" does not exist.", path));
            }

            var additionalAssemblies = DeployPackagesAdditionalAssemblies
                .Select(path => Path.Combine(_rootPath, path))
                .Where(path => File.Exists(path))
                .ToArray();

            return pluginsAssemblies.Concat(additionalAssemblies).Distinct().ToArray();
        }

        private static List<string> _registeredModulesFromAssemblies = new List<string>();

        /// <summary>
        /// Allows DSL packages and other plugins to register their own specific types to Autofac.
        /// </summary>
        public static void RegisterPluginModules(ContainerBuilder builder, IEnumerable<string> ignoreAssemblies = null)
        {
            string[] assemblies = ListPluginsAssemblies();
            if (ignoreAssemblies != null)
                assemblies = assemblies.Except(ignoreAssemblies).ToArray();

            var assemblyCatalogs = assemblies.Select(a => new AssemblyCatalog(a));
            var container = new CompositionContainer(new AggregateCatalog(assemblyCatalogs));
            foreach (var pluginModule in container.GetExports<Module>())
                builder.RegisterModule(pluginModule.Value);

            _registeredModulesFromAssemblies.AddRange(assemblies);
        }

        private static Dictionary<Type, List<string>> _registeredPluginsFromAssemblies = new Dictionary<Type, List<string>>();

        public static void RegisterPlugins<T>(ContainerBuilder builder)
        {
            RegisterPlugins(builder, typeof(T));
        }

        private static void RegisterPlugins(ContainerBuilder builder, Type pluginType, IEnumerable<string> ignoreAssemblies = null)
        {
            try
            {
                // Enumerate plugins using MEF:

                string[] assemblies = ListPluginsAssemblies();
                if (ignoreAssemblies != null)
                    assemblies = assemblies.Except(ignoreAssemblies).ToArray();

                var assemblyCatalogs = assemblies.Select(a => new AssemblyCatalog(a));
                var container = new CompositionContainer(new AggregateCatalog(assemblyCatalogs));

                Tuple<Type, IDictionary<string, object>>[] generatorImplementations = container.Catalog.Parts
                    .SelectMany(part => part.ExportDefinitions.Select(ped => new { part, ped }))
                    .Where(partEd => partEd.ped.ContractName == pluginType.FullName)
                    .Select(partEd => Tuple.Create(
                                ReflectionModelServices.GetPartType(partEd.part).Value,
                                partEd.ped.Metadata
                           ))
                    .ToArray();

                // Register the plugins to Autofac, including their metadata:
 
                foreach (var generatorImplementation in generatorImplementations)
                {
                    var registration = builder.RegisterType(generatorImplementation.Item1).As(new[] { pluginType });
                    foreach (var metadataElement in generatorImplementation.Item2)
                    {
                        registration = registration.WithMetadata(metadataElement.Key, metadataElement.Value);
                        if (metadataElement.Key == MefProvider.Implements)
                            registration = registration.Keyed(metadataElement.Value, pluginType);
                    }
                }

                if (!_registeredPluginsFromAssemblies.ContainsKey(pluginType))
                    _registeredPluginsFromAssemblies.Add(pluginType, new List<string>());
                _registeredPluginsFromAssemblies[pluginType].AddRange(assemblies);
            }
            catch (System.Reflection.ReflectionTypeLoadException rtle)
            {
                var firstFive = rtle.LoaderExceptions.Take(5).Select(it => Environment.NewLine + it.Message);
                throw new FrameworkException("Can't find MEF plugin dependencies:" + string.Concat(firstFive), rtle);
            }
        }

        public static void DetectAndRegisterNewModulesAndPlugins(IContainer container)
        {
            var newBuilder = new ContainerBuilder();

            RegisterPluginModules(newBuilder, _registeredModulesFromAssemblies);

            foreach (var registeredPlugins in _registeredPluginsFromAssemblies)
                RegisterPlugins(newBuilder, registeredPlugins.Key, registeredPlugins.Value);

            newBuilder.Update(container);
        }
    }
}
