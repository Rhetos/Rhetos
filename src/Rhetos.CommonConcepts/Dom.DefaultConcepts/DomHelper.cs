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

using Rhetos.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace Rhetos.Dom.DefaultConcepts
{
    /// <summary>
    /// These methods are called from the generated source code.
    /// </summary>
    public static class DomHelper
    {
        /// <summary>
        /// Initializes <paramref name="insertedNew"/> IDs, where not set.
        /// If a LINQ query is provided as an argument, materializes it to a list.
        /// Converts any null argument to an empty list.
        /// </summary>
        /// <returns>
        /// Returns true if there are any records to insert, update or delete.
        /// </returns>
        public static bool InitializeSaveMethodItems<TEntity>(ref IEnumerable<TEntity> insertedNew, ref IEnumerable<TEntity> updatedNew, ref IEnumerable<TEntity> deletedIds)
            where TEntity : IEntity, new()
        {
            MaterializeItemsToSave(ref insertedNew);
            MaterializeItemsToSave(ref updatedNew);
            MaterializeItemsToDelete(ref deletedIds);

#pragma warning disable CA1851 // Possible multiple enumerations of 'IEnumerable' collection

            if (!insertedNew.Any() && !updatedNew.Any() && !deletedIds.Any())
                return false;

            foreach (var item in insertedNew)
                if (item.ID == Guid.Empty)
                    item.ID = Guid.NewGuid();

#pragma warning restore CA1851 // Possible multiple enumerations of 'IEnumerable' collection

            return true;
        }

        public static void MaterializeItemsToSave<TEntity>(ref IEnumerable<TEntity> items)
            where TEntity : IEntity, new()
        {
            if (items == null)
                items = [];
            else if (items is IQueryable)
                throw new FrameworkException("The Save method for '" + typeof(TEntity).FullName + "' does not support the argument type '" + items.GetType().Name + "'. Use a List or an Array.");
            else if (items is not IList)
                items = items.ToList();
        }

        public static void MaterializeItemsToDelete<TEntity>(ref IEnumerable<TEntity> items)
            where TEntity : IEntity, new()
        {
            if (items == null)
                items = [];
            else if (items is IQueryable<IEntity> queryable)
                // IQueryable Select will generate a better SQL query instead. IEnumerable Select would load all columns.
                items = queryable.Select(item => new TEntity { ID = item.ID }).ToList();
            else if (items is not IList)
                items = items.Select(item => new TEntity { ID = item.ID }).ToList();
        }

        /// <param name="constraintErrorMetadata">For a given DB constraint name, and considering the current entity,
        /// returns the error metadata intended for the end user, that may be written to UserException.SystemMessage</param>
        public static void WriteToDatabase<TEntity>(
            IEnumerable<TEntity> insertedNew,
            IEnumerable<TEntity> updatedNew,
            IEnumerable<TEntity> deletedIds,
            IPersistenceStorage persistenceStorage,
            bool checkUserPermissions,
            ISqlUtility sqlUtility,
            ConstraintErrorMetadata constraintErrorMetadata)
            where TEntity : class, IEntity
        {
            try
            {
                persistenceStorage.Save(insertedNew, updatedNew, deletedIds);
            }
            catch (NonexistentRecordException nre)
            {
                if (checkUserPermissions)
                    throw new ClientException(nre.Message);
                else
                    ExceptionsUtility.Rethrow(nre);
            }
            catch (Exception e)
            {
                RhetosException interpretedException = sqlUtility.InterpretSqlException(e, checkUserPermissions, constraintErrorMetadata);
                if (interpretedException != null)
                    ExceptionsUtility.Rethrow(interpretedException);

                DbException sqlException = sqlUtility.ExtractSqlException(e);
                if (sqlException != null)
                    ExceptionsUtility.Rethrow(sqlException);

                ExceptionsUtility.Rethrow(e);
            }
        }

        /// <summary>
        /// Loads the given items from database, using the provided item's IDs.
        /// The resulting list has the same order of elements as the given list.
        /// The data is not loaded immediately. It will be loaded on the first usage of the resulting IEnumerable.
        /// </summary>
        public static IEnumerable<TQueryableEntity> LazyLoadData<TQueryableEntity>(IEnumerable<IEntity> items, IQueryableRepository<TQueryableEntity> repository)
            where TQueryableEntity : class, IEntity
        {
            var ids = items.Select(item => item.ID).ToList();
            if (ids.Count == 0)
                return [];

            return LazyEnumerable.Create(() =>
            {
                var loaded = repository.Query(ids).ToList();
                Graph.SortByGivenOrder(loaded, ids, item => item.ID);
                return loaded;
            });
        }
    }
}
