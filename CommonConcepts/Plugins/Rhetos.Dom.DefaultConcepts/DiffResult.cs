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

        /// <remarks>
        /// These items may be modifies by <see cref="PrepareForSaving()"/>, see the method's documentation.
        /// </remarks>
        public IEnumerable<T> NewItems { get; set; }

        /// <remarks>
        /// These items may be modifies by <see cref="PrepareForSaving(Action{T, T})"/>, see the method's documentation.
        /// </remarks>
        public IEnumerable<T> OldItems { get; set; }

        public List<T> ToInsert { get; set; }

        /// <summary>
        /// The diff result in <see cref="ToUpdate"/> should be processed before saving to the database:
        /// 1. If the comparison key was not the ID property, then the saved item should have ID from the Old item and other properties from the New item.
        /// 2. Some properties may not be included in the ComputedFrom mapping, they should keep the old values instead of the new ones.
        /// See <see cref="PrepareForSaving()"/> and <see cref="PrepareForSaving(Action{T, T})"/>.
        /// </summary>
        public List<(T Old, T New)> ToUpdate { get; set; }

        public List<T> ToDelete { get; set; }

        /// <summary>
        /// Returns modified <b>new</b> items from <see cref="ToUpdate"/>, prepared for saving to the database.
        /// Items in <see cref="ToInsert"/> and <see cref="ToDelete"/> are not modified.
        /// </summary>
        /// <remarks>
        /// This method modifies items in <see cref="NewItems"/> list:
        /// It modifies IDs in <see cref="NewItems"/> to match the values from <see cref="OldItems"/> if the comparison key was not ID.
        /// </remarks>
        public (List<T> ToInsert, List<T> ToUpdate, List<T> ToDelete) PrepareForSaving()
        {
            foreach (var update in ToUpdate)
                update.New.ID = update.Old.ID;
            return (ToInsert, ToUpdate.Select(update => update.New).ToList(), ToDelete);
        }

        /// <summary>
        /// Returns modified <b>old</b> items from <see cref="ToUpdate"/>, prepared for saving to the database.
        /// Items in <see cref="ToInsert"/> and <see cref="ToDelete"/> are not modified.
        /// </summary>
        /// <remarks>
        /// This method modifies items in <see cref="OldItems"/> list to match the <see cref="NewItems"/>.
        /// Not all properties from the New instance are applied when updating records in the database.
        /// The ComputedFrom mapping may cover only some of the properties, while other properties will keep the old values.
        /// </remarks>
        public (List<T> ToInsert, List<T> ToUpdate, List<T> ToDelete) PrepareForSaving(Action<T, T> assign)
        {
            foreach (var update in ToUpdate)
                assign(update.Old, update.New);
            return (ToInsert, ToUpdate.Select(update => update.Old).ToList(), ToDelete);
        }
    }
}