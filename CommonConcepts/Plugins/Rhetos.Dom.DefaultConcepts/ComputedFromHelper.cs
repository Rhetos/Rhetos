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

using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Rhetos.Dom.DefaultConcepts
{
    public static class ComputedFromHelper
    {
        /// <param name="sameRecord">Compare key properties, determining the records that should be inserted or deleted.
        /// If set to null, the items will be compared by the ID property.
        /// Typical implementation:
        /// <code>
        ///     class CompareName : IComparer&lt;ISomeEntity&gt;
        ///     {
        ///         public int Compare(ISomeEntity x, ISomeEntity y) { return string.Compare(x.Name, y.Name, StringComparison.InvariantCultureIgnoreCase); }
        ///     }
        /// </code></param>
        /// <param name="sameValue">Compare other properties, determining the records that should be updated.
        /// Comparison may also include key properties with stricter constraints (such as case sensitivity).
        /// Typical implementation:
        /// <code>(x, y) =&gt; x.Name == y.Name &amp;&amp; x.SomeValue == y.SomeValue;</code></param>
        /// <remarks>
        /// The diff result in <see cref="DiffResult{T}.ToUpdate"/> should be processed before saving to the database:
        /// 1. If the comparison key was not the ID property, then the saved item should have ID from the Old item and other properties from the New item.
        /// 2. Some properties may not be included in the ComputedFrom mapping, they should keep the old values instead of the new ones.
        /// </remarks>
        public static DiffResult<TEntity> Diff<TEntity, TSourceEntity>(
            IReadableRepository<TEntity> destinationRepository,
            IReadableRepository<TSourceEntity> sourceRepository,
            ILogProvider logProvider,
            object filterLoad,
            Func<TSourceEntity, TEntity> mapping,
            IComparer<TEntity> sameRecord,
            Func<TEntity, TEntity, bool> sameValue)
            where TEntity : class, IEntity
            where TSourceEntity : class, IEntity
        {
            var stopwatch = Stopwatch.StartNew();
            var performanceLogger = logProvider.GetLogger("Performance." + typeof(TEntity).FullName);

            string sourceShortName = typeof(TSourceEntity).Name;
            var sourceItems = sourceRepository.Load(filterLoad);
            var newItems = sourceItems.Select(mapping).ToList();
            performanceLogger.Write(stopwatch, () => $"DiffFrom{sourceShortName}: Load new items ({newItems.Count})");

            IEnumerable<TEntity> oldItems = destinationRepository.Load(filterLoad);
            performanceLogger.Write(stopwatch, () => $"DiffFrom{sourceShortName}: Load old items ({oldItems.Count()})");

            var result = Diff(oldItems, newItems, sameRecord, sameValue);
            performanceLogger.Write(stopwatch, () => $"DiffFrom{sourceShortName}: {newItems.Count} new items, {oldItems.Count()} old items," +
                $" {result.ToInsert.Count} to insert, {result.ToUpdate.Count} to update, {result.ToDelete.Count} to delete.");

            return result;
        }

        /// <summary>
        /// Compares the old dataset with the new dataset, and returns a list of changes that need to be applied to the old dataset to match the new one.
        /// </summary>
        /// <param name="sameRecord">Compare key properties, determining the records that should be inserted or deleted.
        /// If set to null, the items will be compared by the ID property.
        /// Typical implementation:
        /// <code>
        ///     class CompareName : IComparer&lt;ISomeEntity&gt;
        ///     {
        ///         public int Compare(ISomeEntity x, ISomeEntity y) { return string.Compare(x.Name, y.Name, StringComparison.InvariantCultureIgnoreCase); }
        ///     }
        /// </code></param>
        /// <param name="sameValue">Compare other properties, determining the records that should be updated.
        /// Comparison may also include key properties with stricter constraints (such as case sensitivity).
        /// Typical implementation:
        /// <code>(x, y) =&gt; x.Name == y.Name &amp;&amp; x.SomeValue == y.SomeValue;</code></param>
        /// <remarks>
        /// The diff result in <see cref="DiffResult{T}.ToUpdate"/> should be processed before saving to the database:
        /// 1. If the comparison key was not the ID property, then the saved item should have ID from the Old item and other properties from the New item.
        /// 2. Some properties may not be included in the ComputedFrom mapping, they should keep the old values instead of the new ones.
        /// </remarks>
        public static DiffResult<TEntity> Diff<TEntity>(
            IEnumerable<TEntity> oldItems,
            IEnumerable<TEntity> newItems,
            IComparer<TEntity> sameRecord,
            Func<TEntity, TEntity, bool> sameValue)
            where TEntity : class, IEntity
        {
            sameRecord ??= new EntityIdComparer();

            var toDelete = new List<TEntity>();
            var toInsert = new List<TEntity>();
            var toUpdate = new List<(TEntity Old, TEntity New)>();

            List<TEntity> newItemsList = newItems.OrderBy(item => item, sameRecord).ToList();
            List<TEntity> oldItemsList = oldItems.OrderBy(item => item, sameRecord).ToList();

            IEnumerator<TEntity> newEnum = newItemsList.GetEnumerator();
            IEnumerator<TEntity> oldEnum = oldItemsList.GetEnumerator();

            try
            {
                bool newExists = newEnum.MoveNext();
                bool oldExists = oldEnum.MoveNext();

                while (true)
                {
                    int keyDiff;

                    if (newExists)
                        if (oldExists)
                            keyDiff = sameRecord.Compare(newEnum.Current, oldEnum.Current);
                        else
                            keyDiff = -1;
                    else
                        if (oldExists)
                        keyDiff = 1;
                    else
                        break;

                    if (keyDiff == 0)
                    {
                        if (!sameValue(oldEnum.Current, newEnum.Current))
                            toUpdate.Add((oldEnum.Current, newEnum.Current));

                        newExists = newEnum.MoveNext();
                        oldExists = oldEnum.MoveNext();
                    }
                    else if (keyDiff < 0)
                    {
                        toInsert.Add(newEnum.Current);
                        newExists = newEnum.MoveNext();
                    }
                    else
                    {
                        toDelete.Add(oldEnum.Current);
                        oldExists = oldEnum.MoveNext();
                    }
                }
            }
            finally
            {
                newEnum.Dispose();
                oldEnum.Dispose();
            }

            return new DiffResult<TEntity>(newItems, oldItems, toInsert, toUpdate, toDelete);
        }

        /// <param name="assign">Typical implementation:
        /// <code>(destination, source) =&gt; {
        ///     destination.Property1 = source.Property1;
        ///     destination.Property2 = source.Property2; }</code></param>
        /// <remarks>
        /// This method updates the <see cref="DiffResult{T}.OldItems"/> instanced in the provided parameter <paramref name="diff"/> that are included in the <see cref="DiffResult{T}.ToUpdate"/> list.
        /// </remarks>
        public static void InsertOrUpdateOrDelete<TEntity>(
            IWritableRepository<TEntity> destinationRepository,
            ILogProvider logProvider,
            DiffResult<TEntity> diff,
            Action<TEntity, TEntity> assign,
            Func<IEnumerable<TEntity>, IEnumerable<TEntity>> filterSave = null)
            where TEntity : class, IEntity
        {
            var stopwatch = Stopwatch.StartNew();
            var performanceLogger = logProvider.GetLogger("Performance." + typeof(TEntity).FullName);

            (IEnumerable<TEntity> toInsert, IEnumerable<TEntity> toUpdate, IEnumerable<TEntity> toDelete) = diff.PrepareForSaving(assign);

            if (filterSave != null)
            {
                toInsert = filterSave(toInsert);
                toUpdate = filterSave(toUpdate);
                toDelete = filterSave(toDelete);
                CsUtility.Materialize(ref toInsert);
                CsUtility.Materialize(ref toUpdate);
                CsUtility.Materialize(ref toDelete);

                performanceLogger.Write(stopwatch, () => $"InsertOrUpdateOrDelete: FilterSave ({toInsert.Count()} to insert, {toUpdate.Count()} to update, {toDelete.Count()} to delete)");
            }

            destinationRepository.Save(toInsert, toUpdate, toDelete);
            performanceLogger.Write(stopwatch, () => $"InsertOrUpdateOrDelete: Save ({diff.NewItems.Count()} new items, {diff.OldItems.Count()} old items, {toInsert.Count()} to insert, {toUpdate.Count()} to update, {toDelete.Count()} to delete)");
        }
    }
}
