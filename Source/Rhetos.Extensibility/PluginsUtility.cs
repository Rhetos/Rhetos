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

        public static IEnumerable<string> ListPluginsAssemblies()
        {
            List<string> pluginsAssemblies = new List<string>();
            if (ConfigurationManager.AppSettings["PluginsDirectory"] != null)
            {
                string pluginsDirectory = Path.Combine(_rootPath, ConfigurationManager.AppSettings["PluginsDirectory"]);
                if (!Directory.Exists(pluginsDirectory))
                    throw new ApplicationException(String.Format("PluginsDirectory folder \"{0}\" does not exist.", pluginsDirectory));

                pluginsAssemblies.AddRange(Directory.EnumerateFiles(pluginsDirectory, "*.dll", SearchOption.AllDirectories));
            }
            if (ConfigurationManager.AppSettings["PluginsAssemblies"] != null)
            {
                IEnumerable<string> pluginsFiles = ConfigurationManager.AppSettings["PluginsAssemblies"].Split(new[] { ',' })
                    .Select(pluginsFile => pluginsFile.Trim()).Where(pluginsFile => !string.IsNullOrEmpty(pluginsFile))
                    .Select(pluginsFile => Path.Combine(_rootPath, pluginsFile));

                var invalid = pluginsFiles.FirstOrDefault(pluginsFile => !File.Exists(pluginsFile));
                if (invalid != null)
                    throw new ApplicationException(String.Format("PluginsAssemblies file \"{0}\" does not exist.", invalid));

                pluginsAssemblies.AddRange(pluginsFiles);
            }
            return pluginsAssemblies;
        }

        public static void RegisterPlugins<T>(ContainerBuilder builder)
        {
            try
            {
                // Enumerate plugins using MEF:

                var assemblyCatalogs = ListPluginsAssemblies().Select(a => new AssemblyCatalog(a));
                var container = new CompositionContainer(new AggregateCatalog(assemblyCatalogs));

                Tuple<Type, IDictionary<string, object>>[] generatorImplementations = container.Catalog.Parts
                    .SelectMany(part => part.ExportDefinitions.Select(ped => new { part, ped }))
                    .Where(partEd => partEd.ped.ContractName == typeof(T).FullName)
                    .Select(partEd => Tuple.Create(
                                ReflectionModelServices.GetPartType(partEd.part).Value,
                                partEd.ped.Metadata
                           ))
                    .ToArray();

                // Register the plugins to Autofac, including their metadata:
 
                foreach (var generatorImplementation in generatorImplementations)
                {
                    var registration = builder.RegisterType(generatorImplementation.Item1).As<T>();
                    foreach (var metadataElement in generatorImplementation.Item2)
                        registration = registration.WithMetadata(metadataElement.Key, metadataElement.Value);
                }
            }
            catch (System.Reflection.ReflectionTypeLoadException rtle)
            {
                var firstFive = rtle.LoaderExceptions.Take(5).Select(it => Environment.NewLine + it.Message);
                throw new FrameworkException("Can't find MEF plugin dependencies:" + string.Concat(firstFive), rtle);
            }
        }
    }
}
