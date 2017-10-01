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

using Rhetos.Persistence;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public static bool CleanUpSaveMethodArguments<TEntity>(ref IEnumerable<TEntity> insertedNew, ref IEnumerable<TEntity> updatedNew, ref IEnumerable<TEntity> deletedIds)
            where TEntity : IEntity, new()
        {
            MaterializeItemsToSave(ref insertedNew);
            MaterializeItemsToSave(ref updatedNew);
            MaterializeItemsToDelete(ref deletedIds);

            if (insertedNew.Count() == 0 && updatedNew.Count() == 0 && deletedIds.Count() == 0)
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
            if (items is IQueryable<IEntity>)
                // IQueryable Select will generate a better SQL query instead. IEnumerable Select would load all columns.
                items = ((IQueryable<IEntity>)items).Select(item => new TEntity { ID = item.ID }).ToList();
            else if (!(items is IList))
                items = items.Select(item => new TEntity { ID = item.ID }).ToList();
        }

        public enum SaveOperation { None, Insert, Update, Delete };

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

                if (saveException.Entries != null && saveException.Entries.Count() == 1)
                {
                    var entity = saveException.Entries.First().Entity as IEntity;
                    if (entity != null)
                        message += " ID=" + entity.ID.ToString();
                }

                if (checkUserPermissions)
                    throw new ClientException(message);
                else
                    throw new FrameworkException(message, saveException);
            }
        }
    }
}
