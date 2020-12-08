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

using Rhetos.Extensibility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.Dsl
{
    public class ConceptMetadata
    {
        private readonly IPluginsContainer<IConceptMetadataExtension> _plugins;

        /// <summary>First key is the metadata interface type (inherits <see cref="IConceptMetadataExtension"/>), second key is the concept type (inherits <see cref="IConceptInfo"/>).</summary>
        private readonly ConcurrentDictionary<Type, Dictionary<Type, IConceptMetadataExtension>> _pluginsByType = new ConcurrentDictionary<Type, Dictionary<Type, IConceptMetadataExtension>>();

        /// <summary>First key is the metadata interface type (inherits <see cref="IConceptMetadataExtension"/>), second key is the concept type (inherits <see cref="IConceptInfo"/>).</summary>
        private readonly ConcurrentDictionary<(Type, Type), IConceptMetadataExtension> _pluginsByTypeOrDerivedConcept = new ConcurrentDictionary<(Type, Type), IConceptMetadataExtension>();

        public ConceptMetadata(IPluginsContainer<IConceptMetadataExtension> plugins)
        {
            _plugins = plugins;
        }

        public TMetadata Get<TMetadata>(Type conceptType)
            where TMetadata : IConceptMetadataExtension<IConceptInfo>
        {
            return (TMetadata)Get(typeof(TMetadata), conceptType);
        }

        public IConceptMetadataExtension Get(Type metadataInterface, Type conceptType)
        {
            return _pluginsByTypeOrDerivedConcept.GetOrAdd((metadataInterface, conceptType), GetConceptMetadataPlugin);
        }

        private IConceptMetadataExtension GetConceptMetadataPlugin((Type MetadataInterface, Type ConceptType) key)
        {
            var expectedMetadataInterface = typeof(IConceptMetadataExtension<IConceptInfo>);
            if (!expectedMetadataInterface.IsAssignableFrom(key.MetadataInterface))
                throw new FrameworkException($"{key.MetadataInterface} does not implement {expectedMetadataInterface}.");

            var pluginsByConcept = _pluginsByType.GetOrAdd(key.MetadataInterface, GetAllPluginsForMetadataInterface);

            return FindBaseConceptPlugin(key.ConceptType, pluginsByConcept)
                ?? throw new FrameworkException($@"There is no {nameof(IConceptMetadataExtension)} plugin of type {key.MetadataInterface} for concept {key.ConceptType}.");
        }

        private Dictionary<Type, IConceptMetadataExtension> GetAllPluginsForMetadataInterface(Type metadataInterface)
        {
            var pluginsByConcept = new Dictionary<Type, IConceptMetadataExtension>();

            foreach (var plugin in _plugins.GetPlugins().Where(x => metadataInterface.IsInstanceOfType(x)))
            {
                var pluginType = plugin.GetType();
                var pluginInterface = pluginType.GetInterfaces().Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConceptMetadataExtension<>));
                var conceptType = pluginInterface.GetGenericArguments().Single();

                if (pluginsByConcept.ContainsKey(conceptType))
                    throw new FrameworkException($"There are multiple implementations of {metadataInterface.Name} for type {conceptType.Name}:" +
                        $" '{pluginsByConcept[conceptType].GetType()}' and '{pluginType}'.");

                pluginsByConcept.Add(conceptType, plugin);
            }

            return pluginsByConcept;
        }

        private IConceptMetadataExtension FindBaseConceptPlugin(Type conceptType, Dictionary<Type, IConceptMetadataExtension> pluginsByConcept)
        {
            while (conceptType != null)
            {
                if (pluginsByConcept.TryGetValue(conceptType, out var value))
                    return value;

                conceptType = conceptType.BaseType;
            }

            return null;
        }
    }
}
