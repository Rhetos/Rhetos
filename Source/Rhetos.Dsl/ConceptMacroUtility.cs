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

using Rhetos.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Rhetos.Dsl
{
    /// <summary>
    /// A helper for accessing IConceptMacro&lt;&gt; plugins.
    /// The plugins have generic interface to simplify plugin implementation.
    /// This class wraps a plugin and provides a non-generic interface, to simplify use of the plugin.
    /// </summary>
    public static class ConceptMacroUtility
    {
        // TODO: Remove this utility after implementing new design as described in IConceptMacro.

        private static readonly ConcurrentDictionary<(Type ConceptMacroType, Type ConceptInfoType), List<MethodInfo>> _methodCache
            = new ConcurrentDictionary<(Type ConceptMacroType, Type ConceptInfoType), List<MethodInfo>>();

        public static IEnumerable<IConceptInfo> CreateNewConcepts(this IConceptMacro conceptMacro, IConceptInfo conceptInfo, IDslModel existingConcepts)
        {
            IEnumerable<IConceptInfo> newConcepts = null;

            var methods = _methodCache.GetOrAdd((conceptMacro.GetType(), conceptInfo.GetType()), GetPluginMethods);
            foreach (var method in methods)
            {
                var pluginCreatedConcepts = (IEnumerable<IConceptInfo>)method.InvokeEx(conceptMacro, new object[] { conceptInfo, existingConcepts });

                if (newConcepts == null)
                    newConcepts = pluginCreatedConcepts;
                else if (pluginCreatedConcepts != null)
                    newConcepts = newConcepts.Concat(pluginCreatedConcepts);
            }

            return newConcepts;
        }

        private static List<MethodInfo> GetPluginMethods((Type ConceptMacroType, Type ConceptInfoType) key)
        {
            var methods = key.ConceptMacroType.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConceptMacro<>)
                        && i.GetGenericArguments().Single().IsAssignableFrom(key.ConceptInfoType))
                    .Select(i => i.GetMethod("CreateNewConcepts"))
                    .ToList();

            if (methods.Count == 0)
                throw new FrameworkException(string.Format(
                    "Plugin {0} does not implement generic interface {1} that accepts argument {2}.",
                    key.ConceptMacroType.FullName,
                    typeof(IConceptMacro<>).FullName,
                    key.ConceptInfoType.FullName));

            return methods;
        }
    }
}
