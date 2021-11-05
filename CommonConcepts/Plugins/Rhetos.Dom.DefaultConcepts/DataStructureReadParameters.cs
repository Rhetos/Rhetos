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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Rhetos.Dom.DefaultConcepts
{
    public class DataStructureReadParameters : IDataStructureReadParameters
    {
        private readonly Dictionary<string, KeyValuePair<string, Type>[]> _repositoryReadParameters;

        /// <summary>
        /// This cache is not static, because <see cref="DataStructureReadParameters"/> is a singleton.
        /// </summary>
        private readonly ConcurrentDictionary<(string DataStuctureFullName, bool ExtendedSet), IEnumerable<DataStructureReadParameter>> _readParametersByDataStucture =
            new ConcurrentDictionary<(string DataStuctureFullName, bool ExtendedSet), IEnumerable<DataStructureReadParameter>>();

        public DataStructureReadParameters(Dictionary<string, KeyValuePair<string, Type>[]> repositoryReadParameters)
        {
            _repositoryReadParameters = repositoryReadParameters;
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
            if (!_repositoryReadParameters.TryGetValue(key.DataStuctureFullName, out var specificFilterTypes))
                return Array.Empty<DataStructureReadParameter>();

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

        /// <summary>
        /// Heuristics that allows providing array instead of IEnumerable parameter (covariance).
        /// </summary>
        private void AddAlternativeArrayTypesForIEnumerable(List<DataStructureReadParameter> allFilterTypes)
        {
            var enumerablePrefixes = new[] { "IEnumerable<", "System.Collections.Generic.IEnumerable<" };
            foreach (var filterType in allFilterTypes.ToList()) // Using a copy of the list in the foreach, to avoid modifying it while enumerating.
                if (filterType.Type.Name == "IEnumerable`1")
                    foreach (string prefix in enumerablePrefixes)
                        if (filterType.Name.StartsWith(prefix) && filterType.Name.EndsWith(">"))
                        {
                            var innerName = filterType.Name.Substring(prefix.Length, filterType.Name.Length - 1 - prefix.Length);
                            var elementType = filterType.Type.GetGenericArguments().Single();
                            allFilterTypes.Add(new DataStructureReadParameter(innerName + "[]", elementType.MakeArrayType()));
                            break;
                        }
        }

        /// <summary>
        /// Heuristics that allows some common simplified type descriptions without specifying default namespaces.
        /// </summary>
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
                string shortName = filterType.Name;

                foreach (var namePart in NamePartsRegex.Matches(shortName).Reverse()) // Reverse to avoid corrupting remaining matches after removing some prefixes.
                    shortName = TryRemovePrefix(shortName, namePart.Index, namePart.Length, removablePrefixes);

                if (shortName != filterType.Name)
                    allFilterTypes.Add(new DataStructureReadParameter(shortName, filterType.Type));
            }
        }

        private static readonly Regex NamePartsRegex = new Regex(@"[\w.]+");

        private static string TryRemovePrefix(string filterName, int index, int length, List<string> removablePrefixes)
        {
            var removablePrefix = removablePrefixes.FirstOrDefault(prefix => filterName.AsSpan(index, length).StartsWith(prefix));
            if (removablePrefix != null)
                return filterName.Substring(0, index) + filterName.Substring(index + removablePrefix.Length);
            return filterName;
        }
    }
}
