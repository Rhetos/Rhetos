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
using System.Collections;
using System.Collections.Generic;

namespace Rhetos.Utilities
{
    /// <summary>
    /// Creates an IEnumerable that will load the provided data only if/when the IEnumerable is used.
    /// If the IEnumerable is used multiple times, the data will be loaded only once.
    /// </summary>
    public class LazyEnumerable<T> : IEnumerable<T>
    {
        private readonly Lazy<List<T>> _item;

        public LazyEnumerable(Func<List<T>> getItems)
        {
            _item = new(getItems);
        }

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator() => _item.Value.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public static class LazyEnumerable
    {
        /// <summary>
        /// Creates an IEnumerable that will load the provided data only if/when the IEnumerable is used.
        /// If the IEnumerable is used multiple times, the data will be loaded only once.
        /// </summary>
        public static LazyEnumerable<TItem> Create<TItem>(Func<List<TItem>> getItems) => new LazyEnumerable<TItem>(getItems);
    }
}
