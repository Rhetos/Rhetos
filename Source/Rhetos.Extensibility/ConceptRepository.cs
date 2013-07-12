/*
    Copyright (C) 2013 Omega software d.o.o.

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
using System.Text;
using System.Diagnostics.Contracts;
using Rhetos.Extensibility;

namespace Rhetos.Extensibility
{
    public class ConceptRepository<TConcept> : IConceptRepository<TConcept>
    {
        private readonly Dictionary<string, KeyValuePair<Type, Dictionary<string, object>>> ConceptDictionary;

        public ConceptRepository(Lazy<TConcept, Dictionary<string, object>>[] conceptInfos)
        {
            ConceptDictionary = FindConcepts<TConcept>(conceptInfos);
        }

        public static Dictionary<string, KeyValuePair<Type, Dictionary<string, object>>> FindConcepts<TPluginInterface>(Lazy<TPluginInterface, Dictionary<string, object>>[] implementations)
        {
            var dict = new Dictionary<string, KeyValuePair<Type, Dictionary<string, object>>>();
            foreach (var impl in implementations)
            {
                Type implType = impl.Value.GetType();
                var pair = new KeyValuePair<Type, Dictionary<string, object>>(implType, impl.Metadata);

                if (!dict.ContainsKey(implType.Name))
                {
                    dict.Add(implType.Name, pair);
                    dict.Add(implType.FullName, pair);
                }
                dict.Add(implType.AssemblyQualifiedName, pair);
            }
            return dict;
        }

        public Type FindConcept(string name)
        {
            if (ConceptDictionary.ContainsKey(name))
                return ConceptDictionary[name].Key;

            return null;
        }

        public Dictionary<string, object> GetMetadata(string name)
        {
            if (ConceptDictionary.ContainsKey(name))
                return ConceptDictionary[name].Value;

            return null;
        }

    }
}
