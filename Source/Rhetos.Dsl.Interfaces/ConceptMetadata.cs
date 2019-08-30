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
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.Dsl
{
    public class ConceptMetadata
    {
        /// <summary>
        /// First key is the metadata interface type (inherits IConceptMetadata), second key is the concept type (inherits IConceptInfo).
        /// </summary>
        private readonly Dictionary<Type, Dictionary<Type, IConceptMetadataExtension>> _metadata;

        private readonly IPluginsContainer<IConceptMetadataExtension> _plugins;

        public ConceptMetadata(IPluginsContainer<IConceptMetadataExtension> plugins)
        {
            _plugins = plugins;
            _metadata = new Dictionary<Type, Dictionary<Type, IConceptMetadataExtension>>();
        }

        public TMetadata Get<TMetadata>(Type conceptType) where TMetadata : IConceptMetadataExtension<IConceptInfo>
        {
            return (TMetadata)Get(typeof(TMetadata), conceptType);
        }

        public IConceptMetadataExtension Get(Type metadataInterface, Type conceptType)
        {
            Type expectedMetadataType = typeof(IConceptMetadataExtension<IConceptInfo>);
            if (!expectedMetadataType.IsAssignableFrom(metadataInterface))
                throw new FrameworkException($"{metadataInterface} does not implement {expectedMetadataType}.");

            var metadataGenericInterface = metadataInterface.GetGenericTypeDefinition();

            Dictionary<Type, IConceptMetadataExtension> metadataByConceptType;
            if (!_metadata.TryGetValue(metadataGenericInterface, out metadataByConceptType))
            {
                metadataByConceptType = GetPluginsForMetadataType(metadataInterface, metadataGenericInterface);
                _metadata.Add(metadataGenericInterface, metadataByConceptType);
            }

            if (!metadataByConceptType.ContainsKey(conceptType))
            {
                var conceptBaseType = TryFindBaseType(conceptType, metadataByConceptType.Select(x => x.Key).ToList());
                if (conceptBaseType == null)
                    throw new FrameworkException($@"There is no {nameof(IConceptMetadataExtension)} plugin of type {metadataInterface} for concept {conceptType}.");

                metadataByConceptType.Add(conceptType, metadataByConceptType[conceptBaseType]);
            }

            return metadataByConceptType[conceptType];
        }

        private Dictionary<Type, IConceptMetadataExtension> GetPluginsForMetadataType(Type metadataInterface, Type metadataGenericInterface)
        {
            var metadataByConceptType = new Dictionary<Type, IConceptMetadataExtension>();

            foreach (var plugin in _plugins.GetPlugins().Where(x => metadataInterface.IsInstanceOfType(x)))
            {
                var pluginType = plugin.GetType();
                var pluginInterface = pluginType.GetInterfaces().Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConceptMetadataExtension<>));
                var conceptType = pluginInterface.GetGenericArguments().Single();

                if (metadataByConceptType.ContainsKey(conceptType))
                    throw new FrameworkException($"There are multiple implementations of {metadataGenericInterface.Name} for type {conceptType.Name}:" +
                        $" '{metadataByConceptType[conceptType].GetType()}' and '{pluginType}'.");

                metadataByConceptType.Add(conceptType, plugin);
            }

            return metadataByConceptType;
        }

        private static Type TryFindBaseType(Type t, List<Type> allowedTypes)
        {
            var baseType = t.BaseType;

            while (baseType != null && baseType.IsClass)
            {
                if (allowedTypes.Contains(baseType))
                    return baseType;

                baseType = baseType.BaseType;
            }

            return null;
        }
    }
}
