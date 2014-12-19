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
    public class DictionaryOfLists<TKey, TValue> : Dictionary<TKey, List<TValue>>
    {
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

        /// <summary>
        /// Returns empty list is the given key does not exist.
        /// </summary>
        public List<TValue> Get(TKey key)
        {
            List<TValue> list;
            if (!TryGetValue(key, out list))
                list = new List<TValue>();
            return list;
        }
    }
}
