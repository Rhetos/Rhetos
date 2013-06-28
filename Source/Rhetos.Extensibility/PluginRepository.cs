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

        public PluginRepository(
            IExtensionsProvider pluginProvider, 
            Lazy<TPlugin, Dictionary<string, object>>[] pluginsAndMetadata)
        {
            ConceptImplementationsDictionary = pluginProvider.OrganizePlugins<TPlugin>(pluginsAndMetadata);
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
