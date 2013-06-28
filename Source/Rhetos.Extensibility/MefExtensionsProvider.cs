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
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition;
using System.Reflection;
using System.IO;
using System.Diagnostics.Contracts;
using Rhetos.Utilities;

namespace Rhetos.Extensibility
{
    public class MefExtensionsProvider : IExtensionsProvider
    {
        /// <summary>
        /// Organizes plugins in groups by Type that each plugin is handling (MEF metadata "Implements").
        /// Extracts class type for each plugin (MEF metadata "ClassType" if class does not have default constructor).
        /// Sorts plugins by their explicitly given dependencies (MEF metadata "DependsOn").
        /// </summary>
        public Dictionary<Type, List<Type>> OrganizePlugins<TPluginInterface>(Lazy<TPluginInterface, Dictionary<string, object>>[] plugins)
        {
            Contract.Requires(plugins != null);

            var groups = new Dictionary<Type, List<Type>>();
            var dependencies = new List<Tuple<Type, Type>>();
            foreach (var plugin in plugins)
            {
                Type pluginType = LazyFindType(plugin);

                List<Type> list;
                Type commandName = (Type) plugin.Metadata[MefProvider.Implements];
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
            return groups;
        }

        private static Type LazyFindType<TPluginInterface>(Lazy<TPluginInterface, Dictionary<string, object>> implementation)
        {
            if (!implementation.Metadata.ContainsKey(MefProvider.ClassType))
            {
                try
                {
                    return implementation.Value.GetType();
                }
                catch (Exception ex)
                {
                    throw new FrameworkException("Class is not decorated with " + MefProvider.ClassType + " Mef metadata" + Environment.NewLine + ex.ToString());
                }
            }
            return (Type)implementation.Metadata[MefProvider.ClassType];
        }

        public Dictionary<string, KeyValuePair<Type, Dictionary<string, object>>> FindConcepts<TPluginInterface>(Lazy<TPluginInterface, Dictionary<string, object>>[] implementations)
        {
            var dict = new Dictionary<string, KeyValuePair<Type, Dictionary<string, object>>>();
            foreach (var impl in implementations)
            {                
                Type implType = LazyFindType(impl);
                var pair = new KeyValuePair<Type, Dictionary<string, object>>(implType, impl.Metadata);

                if (!dict.ContainsKey(implType.Name))
                {
                    dict.Add(implType.Name, pair);
                    dict.Add(implType.FullName, pair);
                }
                dict.Add(implType.AssemblyQualifiedName, pair);
            }
            return dict;
        }
    }
}
