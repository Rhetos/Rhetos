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
        Dictionary<Type, Dictionary<Type, ITypeExtension>> _typeExtensions;

        IPluginsContainer<ITypeExtension> _plugins;

        public TypeExtensionProvider(IPluginsContainer<ITypeExtension> plugins)
        {
            _plugins = plugins;
            _typeExtensions = new Dictionary<Type, Dictionary<Type, ITypeExtension>>();
        }

        public T Get<T>(Type type) where T : ITypeExtension<IConceptInfo>
        {
            var genericTypeDefinition = typeof(T).GetGenericTypeDefinition();

            Dictionary<Type, ITypeExtension> _typeExtensionImplementation = null;
            if (!_typeExtensions.TryGetValue(genericTypeDefinition, out _typeExtensionImplementation))
            {
                _typeExtensionImplementation = GetTypeExtensionMappingForType(genericTypeDefinition);
                _typeExtensions.Add(genericTypeDefinition, _typeExtensionImplementation);
            }

            if (!_typeExtensionImplementation.ContainsKey(type))
            {
                var posibleBaseType = TryFindBaseType(type, _typeExtensionImplementation.Select(x => x.Key).ToList());
                if (posibleBaseType != null)
                {
                    _typeExtensionImplementation.Add(type, _typeExtensionImplementation[posibleBaseType]);
                }
                else
                {
                    throw new FrameworkException($@"There is no type extension of type {typeof(T).Name} for type {type.Name}");
                }
            }

            return (T)_typeExtensionImplementation[type];
        }

        private Dictionary<Type, ITypeExtension> GetTypeExtensionMappingForType(Type typeExtensionType)
        {
            var typeExtensionMapping = new Dictionary<Type, ITypeExtension>();

            foreach (var plugin in _plugins.GetPlugins().Where(x => IsAssignableToGenericType(x.GetType(), typeExtensionType)))
            {
                var type = plugin.GetType();
                var interfaceIplementationType = type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeExtensionType);
                var typeExtensionInterface = type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ITypeExtension<>));
                var typeExtensionGenericArgument = typeExtensionInterface.GetGenericArguments().Single();

                if (!typeExtensionMapping.ContainsKey(typeExtensionGenericArgument))
                {
                    typeExtensionMapping.Add(typeExtensionGenericArgument, plugin as ITypeExtension);
                }
                else
                {
                    throw new FrameworkException($@"There is already an implementation of {interfaceIplementationType.Name} for type {typeExtensionGenericArgument.Name}");
                }
            }

            return typeExtensionMapping;
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
            while (t.BaseType != typeof(object))
            {
                if (allowedTypes.Contains(baseType) && baseType.IsClass)
                    return baseType;

                baseType = baseType.BaseType;

                if (baseType == null)
                    return null;
            }

            return null;
        }
    }
}
