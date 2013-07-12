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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhetos.Utilities;

namespace Rhetos.Extensibility
{
    public class PluginRepository<TPlugin> : IPluginRepository<TPlugin>
    {
        private readonly Dictionary<Type, List<Type>> ConceptImplementationsDictionary;

        /// <summary>
        /// Organizes plugins in groups by Type that each plugin is handling (MEF metadata "Implements").
        /// Sorts plugins by their explicitly given dependencies (MEF metadata "DependsOn").
        /// </summary>
        public PluginRepository(Lazy<TPlugin, Dictionary<string, object>>[] pluginsAndMetadata)
        {
            var groups = new Dictionary<Type, List<Type>>();
            var dependencies = new List<Tuple<Type, Type>>();
            foreach (var plugin in pluginsAndMetadata)
            {
                Type pluginType = plugin.Value.GetType();

                List<Type> list;
                Type commandName = (Type)plugin.Metadata[MefProvider.Implements];
                if (!groups.TryGetValue(commandName, out list))
                {
                    list = new List<Type>();
                    groups.Add(commandName, list);
                }
                list.Add(pluginType);

                object dependantObject;
                plugin.Metadata.TryGetValue(MefProvider.DependsOn, out dependantObject);
                if (dependantObject != null)
                {
                    Type dependantValue = dependantObject as Type;
                    if (dependantValue == null)
                        throw new ApplicationException("Plugin " + pluginType + " has " + MefProvider.DependsOn + " attribute set to a " + dependantObject.GetType().Name + " instead of a Type.");
                    dependencies.Add(Tuple.Create(dependantValue, pluginType));
                }
            }

            foreach (List<Type> pluginsInGroup in groups.Values)
                DirectedGraph.TopologicalSort(pluginsInGroup, dependencies);
            ConceptImplementationsDictionary = groups;
        }

        public IEnumerable<Type> GetImplementations(Type pluginType)
        {
            var typeHierarchy = GetTypeHierarchy(pluginType);
            var typeHierarchyWithImplementations = typeHierarchy.Where(type => ConceptImplementationsDictionary.ContainsKey(type));
            var result = typeHierarchyWithImplementations.SelectMany(type => ConceptImplementationsDictionary[type]);
            return result;
        }

        public IEnumerable<Type> GetImplementations()
        {
            return ConceptImplementationsDictionary.Values.SelectMany(i => i);
        }

        public static List<Type> GetTypeHierarchy(Type type)
        {
            var types = new List<Type>();
            while (type != typeof(object))
            {
                types.Add(type);
                type = type.BaseType;
            }
            types.Reverse();
            return types;
        }
    }
}
