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
using System.Data.SqlClient;
using System.Linq;

namespace Rhetos.Dom.DefaultConcepts
{
    /// <summary>
    /// These methods are called from the generated ServerDom.*.cs.
    /// </summary>
    public static class DomHelper
    {
        /// <summary>
        /// Returns true if there are any records to save.
        /// </summary>
        public static bool InitializeSaveMethodItems<TEntity>(ref IEnumerable<TEntity> insertedNew, ref IEnumerable<TEntity> updatedNew, ref IEnumerable<TEntity> deletedIds)
            where TEntity : IEntity, new()
        {
            MaterializeItemsToSave(ref insertedNew);
            MaterializeItemsToSave(ref updatedNew);
            MaterializeItemsToDelete(ref deletedIds);

            if (!insertedNew.Any() && !updatedNew.Any() && !deletedIds.Any())
                return false;

            foreach (var item in insertedNew)
                if (item.ID == Guid.Empty)
                    item.ID = Guid.NewGuid();

            return true;
        }

        public static void MaterializeItemsToSave<TEntity>(ref IEnumerable<TEntity> items)
            where TEntity : IEntity, new()
        {
            if (items == null)
                items = Enumerable.Empty<TEntity>();
            else if (items is IQueryable)
                throw new FrameworkException("The Save method for '" + typeof(TEntity).FullName + "' does not support the argument type '" + items.GetType().Name + "'. Use a List or an Array.");
            else if (!(items is IList))
                items = items.ToList();
        }

        public static void MaterializeItemsToDelete<TEntity>(ref IEnumerable<TEntity> items)
            where TEntity : IEntity, new()
        {
            if (items == null)
                items = Enumerable.Empty<TEntity>();
            if (items is IQueryable<IEntity> queryable)
                // IQueryable Select will generate a better SQL query instead. IEnumerable Select would load all columns.
                items = queryable.Select(item => new TEntity { ID = item.ID }).ToList();
            else if (!(items is IList))
                items = items.Select(item => new TEntity { ID = item.ID }).ToList();
        }

        public static void EntityFrameworkOptimizedSave<TEntity>(
            IEnumerable<TEntity> insertedNew,
            IEnumerable<TEntity> updatedNew,
            IEnumerable<TEntity> deletedIds,
            IPersistanceStorage persistanceStorage,
            bool checkUserPermissions,
            ISqlUtility sqlUtility,
            out SqlException saveException,
            out RhetosException interpretedException)
            where TEntity : class, IEntity
        {
            try
            {
                persistanceStorage.Delete(deletedIds);
                persistanceStorage.Update(updatedNew);
                persistanceStorage.Insert(insertedNew);
                saveException = null;
                interpretedException = null;
            }
            catch (NonexistentRecordException nre)
            {
                saveException = null;
                interpretedException = null;
                if (checkUserPermissions)
                    throw new ClientException(nre.Message);
                else
                    ExceptionsUtility.Rethrow(nre);
            }
            catch (SqlException e)
            {
                saveException = e;
                interpretedException = sqlUtility.InterpretSqlException(saveException);
            }
        }

        public static void ThrowInterpretedException(bool checkUserPermissions, Exception saveException, RhetosException interpretedException, ISqlUtility sqlUtility, string tableName)
        {
            if (checkUserPermissions)
                MsSqlUtility.ThrowIfPrimaryKeyErrorOnInsert(interpretedException, tableName);
            if (interpretedException != null)
                ExceptionsUtility.Rethrow(interpretedException);
            var sqlException = sqlUtility.ExtractSqlException(saveException);
            if (sqlException != null)
                ExceptionsUtility.Rethrow(sqlException);
            ExceptionsUtility.Rethrow(saveException);
        }

        public static IEnumerable<TQueryableEntity> LoadOldDataWithNavigationProperties<TQueryableEntity>(IEnumerable<IEntity> items, IQueryableRepository<TQueryableEntity> repository)
            where TQueryableEntity : class, IEntity
        {
            var loaded = items.Any()
                ? repository.Query(items.Select(item => item.ID)).ToList()
                : new List<TQueryableEntity>();

            Graph.SortByGivenOrder(loaded, items.Select(item => item.ID), item => item.ID);
            return loaded;
        }
    }
}
