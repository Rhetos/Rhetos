using System;
using System.Collections.Generic;
using System.Text;

namespace Rhetos.Utilities
{
    public static class TypeExtensions
    {
        /// <summary>
        /// Returns the underlying genric type with concrete type arguments.
        /// </summary>
        public static Type GetUnderlyingGenericType(this Type type, Type genericType)
        {
            if(genericType.IsInterface)
                throw new ArgumentException("Interfaces are not supported.");

            if (!genericType.IsGenericType)
                throw new ArgumentException("The type must be a generic type.");

            if (genericType.GenericTypeArguments.Length != 0)
                throw new ArgumentException("The generic type should not have any type arguments.");

            while (type != null && type != typeof(object))
            {
                var cur = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
                if (genericType == cur)
                    return type;
                type = type.BaseType;
            }

            return null;
        }
    }
}
