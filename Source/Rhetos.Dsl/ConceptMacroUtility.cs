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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Rhetos.Utilities;

namespace Rhetos.Dsl
{
    /// <summary>
    /// A helper for accessing IConceptMacro&lt;&gt; plugins.
    /// The plugins have generic interface to simplify plugin implementation.
    /// This class wraps a plugin and provides a non-generic interface, to simplify use of the plugin.
    /// </summary>
    public static class ConceptMacroUtility
    {
        public static IEnumerable<IConceptInfo> CreateNewConcepts(this IConceptMacro conceptMacro, IConceptInfo conceptInfo, IDslModel existingConcepts)
        {
            IEnumerable<IConceptInfo> newConcepts = null;

            foreach (var method in GetPluginMethods(conceptMacro, conceptInfo))
            {
                var pluginCreatedConcepts = (IEnumerable<IConceptInfo>)method.InvokeEx(conceptMacro, new object[] { conceptInfo, existingConcepts });

                if (newConcepts == null)
                    newConcepts = pluginCreatedConcepts;
                else if (pluginCreatedConcepts != null)
                    newConcepts = newConcepts.Concat(pluginCreatedConcepts);
            }

            return newConcepts;
        }

        private static List<MethodInfo> GetPluginMethods(IConceptMacro conceptMacro, IConceptInfo conceptInfo)
        {
            var methods = conceptMacro.GetType().GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConceptMacro<>)
                        && i.GetGenericArguments().Single().IsAssignableFrom(conceptInfo.GetType()))
                    .Select(i => i.GetMethod("CreateNewConcepts"))
                    .ToList();

            if (methods.Count == 0)
                throw new FrameworkException(string.Format(
                    "Plugin {0} does not implement generic interface {1} that accepts argument {2}.",
                    conceptMacro.GetType().FullName,
                    typeof(IConceptMacro<>).FullName,
                    conceptInfo.GetType().FullName));

            return methods;
        }

        /*IConceptMacro _pluginInstance;
        Lazy<List<MethodInfo>> _pluginMethods;

        public ConceptMacroGenericProxy(IConceptMacro pluginInstance)
        {
            _pluginInstance = pluginInstance;
            _pluginMethods = new Lazy<List<MethodInfo>>(
                () => pluginInstance.GetType().GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConceptMacro<>))
                    .Select(i => i.GetMethod("CreateNewConcepts"))
                    .ToList());
        }

        IEnumerable<IConceptInfo> CreateNewConcepts(IConceptInfo conceptInfo, IDslModel existingConcepts)
        {
            if (_pluginMethods.Value.Count == 0)
                throw new FrameworkException(string.Format(
                    "Plugin {0} does not implement interface {1}.",
                    _pluginInstance.GetType().FullName,
                    typeof(IConceptMacro<>).FullName));

            IEnumerable<IConceptInfo> newConcepts = null;

            foreach (var pluginMethod in _pluginMethods.Value)
            {
                var pluginCreatedConcepts = (IEnumerable<IConceptInfo>)pluginMethod.Invoke(_pluginInstance, new object[] { conceptInfo, existingConcepts });

                if (newConcepts == null)
                    newConcepts = pluginCreatedConcepts;
                else if (pluginCreatedConcepts != null)
                    newConcepts = newConcepts.Concat(pluginCreatedConcepts);
            }

            return newConcepts;
        }*/
    }
}
