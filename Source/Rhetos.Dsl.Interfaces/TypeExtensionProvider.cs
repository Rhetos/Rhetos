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
    public class TypeExtensionProvider
    {
        /// <summary>
        /// First key is the extension interface type, second key is the concept type.
        /// </summary>
        private readonly Dictionary<Type, Dictionary<Type, ITypeExtension>> _typeExtensions;

        private readonly IPluginsContainer<ITypeExtension> _plugins;

        public TypeExtensionProvider(IPluginsContainer<ITypeExtension> plugins)
        {
            _plugins = plugins;
            _typeExtensions = new Dictionary<Type, Dictionary<Type, ITypeExtension>>();
        }

        public TExtension Get<TExtension>(Type conceptType) where TExtension : ITypeExtension<IConceptInfo>
        {
            return (TExtension)Get(typeof(TExtension), conceptType);
        }

        public ITypeExtension Get(Type extensionInterface, Type conceptType)
        {
            Type expectedExtensionType = typeof(ITypeExtension<IConceptInfo>);
            if (!expectedExtensionType.IsAssignableFrom(extensionInterface))
                throw new FrameworkException($"{extensionInterface} does not implement {expectedExtensionType}.");

            var extensionGenericInterface = extensionInterface.GetGenericTypeDefinition();

            Dictionary<Type, ITypeExtension> extensionsByConceptType;
            if (!_typeExtensions.TryGetValue(extensionGenericInterface, out extensionsByConceptType))
            {
                extensionsByConceptType = GetTypeExtensionMappingForType(extensionGenericInterface);
                _typeExtensions.Add(extensionGenericInterface, extensionsByConceptType);
            }

            if (!extensionsByConceptType.ContainsKey(conceptType))
            {
                var conceptBaseType = TryFindBaseType(conceptType, extensionsByConceptType.Select(x => x.Key).ToList());
                if (conceptBaseType != null)
                {
                    extensionsByConceptType.Add(conceptType, extensionsByConceptType[conceptBaseType]);
                }
                else
                {
                    throw new FrameworkException($@"There is no {nameof(ITypeExtension)} plugin of type {extensionInterface} for concept {conceptType}.");
                }
            }

            return extensionsByConceptType[conceptType];
        }

        private Dictionary<Type, ITypeExtension> GetTypeExtensionMappingForType(Type extensionGenericInterface)
        {
            var extensionsByConceptType = new Dictionary<Type, ITypeExtension>();

            foreach (var plugin in _plugins.GetPlugins().Where(x => IsAssignableToGenericType(x.GetType(), extensionGenericInterface)))
            {
                var type = plugin.GetType();
                var interfaceIplementationType = type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == extensionGenericInterface);
                var typeExtensionInterface = type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ITypeExtension<>));
                var conceptType = typeExtensionInterface.GetGenericArguments().Single();

                if (!extensionsByConceptType.ContainsKey(conceptType))
                {
                    extensionsByConceptType.Add(conceptType, plugin);
                }
                else
                {
                    throw new FrameworkException($@"There is already an implementation of {interfaceIplementationType} for type {conceptType}.");
                }
            }

            return extensionsByConceptType;
        }

        private static bool IsAssignableToGenericType(Type givenType, Type genericType)
        {
            var interfaceTypes = givenType.GetInterfaces();

            foreach (var it in interfaceTypes)
            {
                if (it.IsGenericType && it.GetGenericTypeDefinition() == genericType)
                    return true;
            }

            if (givenType.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
                return true;

            Type baseType = givenType.BaseType;
            if (baseType == null) return false;

            return IsAssignableToGenericType(baseType, genericType);
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
