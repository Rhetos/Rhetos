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
        private readonly ConcurrentDictionary<(string DataStuctureFullName, bool ExtendedSet), IEnumerable<DataStructureReadParameter>> _readParametersByDataStucture = new();

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
            new DataStructureReadParameter(
                "IEnumerable<Guid>", // Short type name format as specified in C# source. Exact type name with full namespace is supported by adding Type.ToString() later.
                typeof(IEnumerable<Guid>)),
        };

        /// <summary>
        /// Reusing string instances for standard filters names for many data structures.
        /// </summary>
        private static readonly Lazy<DataStructureReadParameter[]> _standardFilterTypesAlternativeNames = new(CreateStandardFilterTypesExtended);

        private static DataStructureReadParameter[] CreateStandardFilterTypesExtended()
        {
            var result = new HashSet<DataStructureReadParameter>(_standardFilterTypes);
            AddAlternativeFilterNames(result, null);
            return result.ToArray();
        }

        private IEnumerable<DataStructureReadParameter> CreateReadParametersList((string DataStuctureFullName, bool ExtendedSet) key)
        {
            if (!_repositoryReadParameters.TryGetValue(key.DataStuctureFullName, out var specificFilterTypes))
                return Array.Empty<DataStructureReadParameter>();

            int estimatedSize = !key.ExtendedSet
                ? specificFilterTypes.Length + _standardFilterTypes.Length
                : specificFilterTypes.Length * 3 + _standardFilterTypesAlternativeNames.Value.Length;
            var allFilterTypes = new HashSet<DataStructureReadParameter>(estimatedSize);

            allFilterTypes.UnionWith(specificFilterTypes.Select(filterType => new DataStructureReadParameter(filterType.Key, filterType.Value)));
            if (key.ExtendedSet)
                AddAlternativeFilterNames(allFilterTypes, key.DataStuctureFullName);

            allFilterTypes.UnionWith(key.ExtendedSet ? _standardFilterTypesAlternativeNames.Value : _standardFilterTypes);

            return allFilterTypes.ToArray();
        }

        private static void AddAlternativeFilterNames(ICollection<DataStructureReadParameter> allFilterTypes, string dataStuctureFullName = null)
        {
            AddSimplifiedTypeNames(allFilterTypes, dataStuctureFullName);
            AddTypeNamesFromReflection(allFilterTypes);
        }

        /// <summary>
        /// Includes Type.ToString().
        /// </summary>
        private static void AddTypeNamesFromReflection(ICollection<DataStructureReadParameter> allFilterTypes)
        {
            var distinctTypes = allFilterTypes.Select(t => t.Type).Distinct().ToList();
            foreach (var type in distinctTypes)
                allFilterTypes.Add(new DataStructureReadParameter(type.ToString(), type));
        }

        /// <summary>
        /// Heuristics that allows usage of simplified type name without default namespaces
        /// and by using array instead of IEnumerable, for the C# source type name format.
        /// </summary>
        private static void AddSimplifiedTypeNames(ICollection<DataStructureReadParameter> allFilterTypes, string dataStuctureFullName = null)
        {
            var removablePrefixes = new List<string>(_defaultNamespaces.Length + 1);
            removablePrefixes.AddRange(_defaultNamespaces);
            if (dataStuctureFullName != null)
            {
                var moduleEnd = dataStuctureFullName.IndexOf(".");
                if (moduleEnd > 0)
                    removablePrefixes.Add(dataStuctureFullName.Substring(0, moduleEnd + 1));
            }
            removablePrefixes = removablePrefixes.OrderByDescending(p => p.Length).ToList();

            // Namespace removal is useful only for C# type syntax (e.g., in C# code you can write "IEnumerable<Guid>" without namespace),
            // but there is no use to shorten reflection type names (e.g. Type.ToString() will never return "IEnumerable`1[Guid]" without namespace).
            var csFormatFilterTypes = allFilterTypes
                .Where(filterType => filterType.Name.IndexOfAny(_refectionTypeNameIndicators) == -1)
                .ToList();

            foreach (var filterType in csFormatFilterTypes)
            {
                string shortName = filterType.Name;

                foreach (var namePart in _namePartsRegex.Matches(shortName).Reverse()) // Reverse to avoid corrupting remaining matches after removing some prefixes.
                    shortName = TryRemovePrefix(shortName, namePart.Index, namePart.Length, removablePrefixes);

                if (shortName.StartsWith("IEnumerable<", StringComparison.Ordinal) && shortName.EndsWith(">", StringComparison.Ordinal))
                    shortName = string.Concat(shortName.AsSpan("IEnumerable<".Length, shortName.Length - "IEnumerable<".Length - 1), "[]");

                if (shortName != filterType.Name)
                    allFilterTypes.Add(new DataStructureReadParameter(shortName, filterType.Type));
            }
        }

        private static readonly char[] _refectionTypeNameIndicators = new[] { '`', '+' };

        private static readonly Regex _namePartsRegex = new Regex(@"[\w\.]+");

        private static string TryRemovePrefix(string filterName, int index, int length, List<string> removablePrefixes)
        {
            var removablePrefix = removablePrefixes.FirstOrDefault(prefix => filterName.AsSpan(index, length).StartsWith(prefix, StringComparison.Ordinal));
            if (removablePrefix != null)
                return string.Concat(filterName.AsSpan(0, index), filterName.AsSpan(index + removablePrefix.Length));
            return filterName;
        }
    }
}
