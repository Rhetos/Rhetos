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

using System.Collections.Generic;

namespace Rhetos.Dom.DefaultConcepts
{
    public class DiffResult<T> where T : class, IEntity
    {
        public DiffResult(IEnumerable<T> newItems, IEnumerable<T> oldItems, List<T> toInsert, List<(T Old, T New)> toUpdate, List<T> toDelete)
        {
            NewItems = newItems;
            OldItems = oldItems;
            ToInsert = toInsert;
            ToUpdate = toUpdate;
            ToDelete = toDelete;
        }

        public IEnumerable<T> NewItems { get; set; }

        public IEnumerable<T> OldItems { get; set; }

        public List<T> ToInsert { get; set; }

        /// <summary>
        /// Note that not all properties from the New instance should be applied when updating records in the database.
        /// The ComputedFrom mapping may cover only some of the properties, while other properties should keep the old values.
        /// </summary>
        public List<(T Old, T New)> ToUpdate { get; set; }

        public List<T> ToDelete { get; set; }
    }
}