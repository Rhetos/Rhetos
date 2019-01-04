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
using System.Text;

namespace Rhetos.Utilities
{
    /// <summary>
    /// May contain multiple values with the same key.
    /// </summary>
    public class MultiDictionary<TKey, TValue> : Dictionary<TKey, List<TValue>>
    {
        public MultiDictionary()
        {
        }

        public MultiDictionary(IEqualityComparer<TKey> comparer)
            : base(comparer)
        {
        }

        public void Add(TKey key, TValue value)
        {
            List<TValue> list;
            if (!TryGetValue(key, out list))
            {
                list = new List<TValue>();
                Add(key, list);
            }
            list.Add(value);
        }

        public void AddKey(TKey key)
        {
            if (!ContainsKey(key))
                Add(key, new List<TValue>());
        }

        private static TValue[] EmptyArray = new TValue[] { };

        /// <summary>
        /// Returns empty list is the given key does not exist.
        /// </summary>
        public IEnumerable<TValue> Get(TKey key)
        {
            List<TValue> list;
            if (!TryGetValue(key, out list))
                return EmptyArray;
            return list;
        }
    }

    public static class MultiDictionaryLinqExtensions
    {
        public static MultiDictionary<TKey, TElement> ToMultiDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> valueSelector)
        {
            var multyDictionary = new MultiDictionary<TKey, TElement>();
            foreach (var element in source)
                multyDictionary.Add(keySelector(element), valueSelector(element));
            return multyDictionary;
        }

        public static MultiDictionary<TKey, TElement> ToMultiDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> valueSelector, IEqualityComparer<TKey> comparer)
        {
            var multyDictionary = new MultiDictionary<TKey, TElement>(comparer);
            foreach (var element in source)
                multyDictionary.Add(keySelector(element), valueSelector(element));
            return multyDictionary;
        }
    }
}
