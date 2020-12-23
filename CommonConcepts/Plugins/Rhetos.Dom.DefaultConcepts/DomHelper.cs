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
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;

namespace Rhetos.Dom.DefaultConcepts
{
    /// <summary>
    /// These methods are called from the generated source code.
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

        public enum SaveOperation { None, Insert, Update, Delete };

        public static void EntityFrameworkOptimizedSave<TEntity, TQueryableEntity>(
            IEnumerable<TEntity> insertedNew,
            IEnumerable<TEntity> updatedNew,
            IEnumerable<TEntity> deletedIds,
            Func<TEntity, TQueryableEntity> toNavigation,
            bool checkUserPermissions,
            DbContext dbContext,
            ISqlUtility sqlUtility,
            out SaveOperation saveOperation,
            out DbUpdateException saveException,
            out RhetosException interpretedException)
            where TEntity : class, IEntity
            where TQueryableEntity : class, IEntity, TEntity
        {
            saveOperation = SaveOperation.None;
            try
            {
                if (deletedIds.Any())
                {
                    saveOperation = SaveOperation.Delete;
                    dbContext.Configuration.AutoDetectChangesEnabled = false;
                    foreach (var item in deletedIds.Select(toNavigation))
                        dbContext.Entry(item).State = System.Data.Entity.EntityState.Deleted;
                    dbContext.Configuration.AutoDetectChangesEnabled = true;
                    dbContext.SaveChanges();
                }

                if (updatedNew.Any())
                {
                    saveOperation = SaveOperation.Update;
                    dbContext.Configuration.AutoDetectChangesEnabled = false;
                    foreach (var item in updatedNew.Select(toNavigation))
                        dbContext.Entry(item).State = System.Data.Entity.EntityState.Modified;
                    dbContext.Configuration.AutoDetectChangesEnabled = true;
                    dbContext.SaveChanges();
                }

                if (insertedNew.Any())
                {
                    saveOperation = SaveOperation.Insert;
                    dbContext.Set<TQueryableEntity>().AddRange(insertedNew.Select(toNavigation));
                    dbContext.SaveChanges();
                }

                saveOperation = SaveOperation.None;
                ((Rhetos.Persistence.IPersistenceCache)dbContext).ClearCache();

                saveException = null;
                interpretedException = null;
            }
            catch (DbUpdateException e)
            {
                saveException = e;
                ThrowIfSavingNonexistentId(saveException, checkUserPermissions, saveOperation);
                interpretedException = sqlUtility.InterpretSqlException(saveException);
            }
        }

        public static void ThrowIfSavingNonexistentId(DbUpdateException saveException, bool checkUserPermissions, SaveOperation saveOperation)
        {
            if (saveException.Message.StartsWith("Store update, insert, or delete statement affected an unexpected number of rows (0)."))
            {
                string message;
                if (saveOperation == SaveOperation.Update)
                    message = "Updating a record that does not exist in database.";
                else if (saveOperation == SaveOperation.Delete)
                    message = "Deleting a record that does not exist in database.";
                else
                    return;

                if (saveException.Entries != null && saveException.Entries.Count() == 1 && saveException.Entries.First().Entity is IEntity entity)
                    message += " ID=" + entity.ID.ToString();

                if (checkUserPermissions)
                    throw new ClientException(message);
                else
                    throw new FrameworkException(message, saveException);
            }
        }

        public static void ThrowInterpretedException(bool checkUserPermissions, DbUpdateException saveException, RhetosException interpretedException, ISqlUtility sqlUtility, string tableName)
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
