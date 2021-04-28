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
using System.Reflection;

namespace Rhetos.Dom.DefaultConcepts
{
    public class DataStructureReadParameters : IDataStructureReadParameters
    {
        private readonly INamedPlugins<IRepository> _repositories;

        /// <summary>
        /// This cache is not static, because <see cref="DataStructureReadParameters"/> is a singleton.
        /// </summary>
        private readonly ConcurrentDictionary<(string DataStuctureFullName, bool ExtendedSet), IEnumerable<DataStructureReadParameter>> _readParametersByDataStucture =
            new ConcurrentDictionary<(string DataStuctureFullName, bool ExtendedSet), IEnumerable<DataStructureReadParameter>>();

        public DataStructureReadParameters(INamedPlugins<IRepository> repositories)
        {
            _repositories = repositories;
        }

        public IEnumerable<DataStructureReadParameter> GetReadParameters(string dataStuctureFullName, bool extendedSet)
        {
            return _readParametersByDataStucture.GetOrAdd((dataStuctureFullName, extendedSet), CreateReadParametersList);
        }

        private static readonly string[] _defaultNamespaces = new string[]
        {
            "Common.", // TODO: Should be removed. Configurable for backward compatibility.
            "System.",
            "System.Collections.Generic.",
            "Rhetos.Dom.DefaultConcepts.",
        };

        private static readonly DataStructureReadParameter[] _standardFilterTypes = new DataStructureReadParameter[]
        {
            new DataStructureReadParameter("System.Collections.Generic.IEnumerable<System.Guid>", typeof(IEnumerable<Guid>)),
        };

        private IEnumerable<DataStructureReadParameter> CreateReadParametersList((string DataStuctureFullName, bool ExtendedSet) key)
        {
            var repository = _repositories.GetPlugin(key.DataStuctureFullName);
            var readParameterTypesProperty = repository.GetType().GetField("ReadParameterTypes", BindingFlags.Public | BindingFlags.Static);
            if (readParameterTypesProperty == null)
                return Array.Empty<DataStructureReadParameter>();
            var specificFilterTypes = (KeyValuePair<string, Type>[])readParameterTypesProperty.GetValue(null);

            var allFilterTypes = new List<DataStructureReadParameter>(specificFilterTypes.Length + _standardFilterTypes.Length);
            allFilterTypes.AddRange(specificFilterTypes.Select(filterType => new DataStructureReadParameter(filterType.Key, filterType.Value)));
            allFilterTypes.AddRange(_standardFilterTypes);

            if (key.ExtendedSet)
            {
                AddAlternativeArrayTypesForIEnumerable(allFilterTypes);
                AddAlternativeDefaultNamepaceTypeNames(allFilterTypes, key.DataStuctureFullName);
            }

            return allFilterTypes.Distinct().ToList();
        }

        private void AddAlternativeArrayTypesForIEnumerable(List<DataStructureReadParameter> allFilterTypes)
        {
            var enumerablePrefixes = new[] { "IEnumerable<", "System.Collections.Generic.IEnumerable<" };
            foreach (var filterType in allFilterTypes.ToList()) // Using a copy of the list in the foreach, to avoid modifying it while enumerating.
                if (filterType.Type.Name == "IEnumerable`1" && filterType.Name.EndsWith(">"))
                    foreach (string prefix in enumerablePrefixes)
                        if (filterType.Name.StartsWith(prefix))
                        {
                            var innerName = filterType.Name.Substring(prefix.Length, filterType.Name.Length - 1 - prefix.Length);
                            var elementType = filterType.Type.GetGenericArguments().Single();
                            allFilterTypes.Add(new DataStructureReadParameter(innerName + "[]", elementType.MakeArrayType()));
                            break;
                        }
        }

        private void AddAlternativeDefaultNamepaceTypeNames(List<DataStructureReadParameter> allFilterTypes, string dataStuctureFullName)
        {
            var removablePrefixes = new List<string>(_defaultNamespaces.Length + 1);
            removablePrefixes.AddRange(_defaultNamespaces);
            var moduleEnd = dataStuctureFullName.IndexOf(".");
            if (moduleEnd > 0)
                removablePrefixes.Add(dataStuctureFullName.Substring(0, moduleEnd + 1));
            removablePrefixes = removablePrefixes.OrderByDescending(p => p.Length).ToList();

            foreach (var filterType in allFilterTypes.ToList()) // Using a copy of the list in the foreach, to avoid modifying it while enumerating.
            {
                var removablePrefix = removablePrefixes.FirstOrDefault(prefix => filterType.Name.StartsWith(prefix));
                if (removablePrefix != null)
                    allFilterTypes.Add(new DataStructureReadParameter(filterType.Name.Substring(removablePrefix.Length), filterType.Type));
            }
        }
    }
}
