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

using Rhetos.Dom.DefaultConcepts;
using Rhetos.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.Processing.DefaultCommands
{
    /// <summary>
    /// Utility for implementing custom controllers or Rhetos server commands,
    /// with helper methods for checking row permissions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A custom controller or a Rhetos server command should request <see cref="ServerCommandsUtility"/> from dependency injection,
    /// then call <see cref="ServerCommandsUtility.ForEntity(string)"/> to get <see cref="EntityCommandsUtility"/>.
    /// </para>
    /// <para>
    /// "Entity" in this context represents any data structure that implements IEntity with ID property,
    /// including Browse, SqlQueryable and other data structures.
    /// </para>
    /// </remarks>
    public class EntityCommandsUtility
    {
        public GenericRepository<IEntity> GenericRepository { get; }

        private readonly ILogger _logger;

        public EntityCommandsUtility(ILogProvider logProvider, GenericRepository<IEntity> genericRepository)
        {
            _logger = logProvider.GetLogger(GetType().Name);
            GenericRepository = genericRepository;
        }

        /// <summary>
        /// Returns <see langword="true"/> if the current user has the read row permissions allowed for the given items,
        /// or if the row permissions are not implemented on the given data structure.
        /// </summary>
        /// <remarks>
        /// The method uses only provided items' IDs to check the permissions with a database query.
        /// </remarks>
        public bool UserHasReadRowPermissions(object[] items)
        {
            return CheckAllItemsWithinFilter(items, typeof(Common.RowPermissionsReadItems));
        }

        /// <summary>
        /// Returns <see langword="true"/> if the current user has the write row permissions allowed for the given items,
        /// or if the row permissions are not implemented on the given data structure.
        /// <para>
        /// Call both methods: <see cref="UserHasWriteRowPermissionsBeforeSave"/> before saving the data
        /// (to verify the old data) and <see cref="UserHasWriteRowPermissionsAfterSave"/> after saving (to verify the new data).
        /// </para>
        /// </summary>
        /// <remarks>
        /// The method uses only provided items' IDs to check the permissions with a database query.
        /// <para>
        /// The method throws a <see cref="ClientException"/> if the row permissions cannot be verified
        /// because the given records do not exist in the database.
        /// </para>
        /// </remarks>
        /// <param name="itemsToDelete">The value may be null if there is nothing to delete.</param>
        /// <param name="itemsToUpdate">The value may be null if there is nothing to update.</param>
        public bool UserHasWriteRowPermissionsBeforeSave(IEntity[] itemsToDelete, IEntity[] itemsToUpdate)
        {
            var updateDeleteItems = ConcatenateNullable(itemsToDelete, itemsToUpdate);
            if (updateDeleteItems != null)
                if (!CheckAllItemsWithinFilter(updateDeleteItems, typeof(Common.RowPermissionsWriteItems)))
                {
                    Guid? missingId;
                    if (MissingItemId(itemsToDelete, out missingId))
                        throw new ClientException($"Deleting a record that does not exist in database. DataStructure={GenericRepository.EntityName}, ID={missingId}");
                    else if (MissingItemId(itemsToUpdate, out missingId))
                        throw new ClientException($"Updating a record that does not exist in database. DataStructure={GenericRepository.EntityName}, ID={missingId}");
                    else
                        return false;
                }

            return true;
        }

        /// <summary>
        /// Returns <see langword="true"/> if the current user has the write row permissions allowed for the given items,
        /// or if the row permissions are not implemented on the given data structure.
        /// <para>
        /// Call both methods: <see cref="UserHasWriteRowPermissionsBeforeSave"/> before saving the data
        /// (to verify the old data) and <see cref="UserHasWriteRowPermissionsAfterSave"/> after saving (to verify the new data).
        /// </para>
        /// </summary>
        /// <remarks>
        /// The method uses only provided items' IDs to check the permissions with a database query.
        /// <para>
        /// The method throws a <see cref="ClientException"/> if the row permissions cannot be verified
        /// because the given records do not exist in the database.
        /// </para>
        /// </remarks>
        /// <param name="itemsToInsert">The value may be null if there is nothing to insert.</param>
        /// <param name="itemsToUpdate">The value may be null if there is nothing to update.</param>
        public bool UserHasWriteRowPermissionsAfterSave(IEntity[] itemsToInsert, IEntity[] itemsToUpdate)
        {
            var insertUpdateItems = ConcatenateNullable(itemsToInsert, itemsToUpdate);
            // We rely that this call will only use IDs of the items, because other data might be dirty.
            if (insertUpdateItems != null)
                if (!CheckAllItemsWithinFilter(insertUpdateItems, typeof(Common.RowPermissionsWriteItems)))
                    return false;

            return true;
        }

        private static IEntity[] ConcatenateNullable(IEntity[] a, IEntity[] b)
        {
            if (a != null && a.Length > 0)
                if (b != null && b.Length > 0)
                    return a.Concat(b).ToArray();
                else
                    return a;
            else
                if (b != null && b.Length > 0)
                return b;
            else
                return null;
        }

        /// <summary>
        /// Checks if all items are within the given filter.
        /// Works with materialized items.
        /// It the filter is not implemented on the given data structure, returns true.
        /// </summary>
        public bool CheckAllItemsWithinFilter(object[] validateObjects, Type filterType)
        {
            if (validateObjects == null) return true;
            var validateItems = (IEntity[])validateObjects;

            var filterMethodInfo = GenericRepository.Reflection.RepositoryQueryableFilterMethod(filterType); // TODO: After implementing repository metadata for available filter parameters (loader, query, filter, queryFilter), it should be used here instead of RepositoryQueryableFilterMethod.

            if (filterMethodInfo != null)
            {
                var itemsIds = validateItems.Select(item => item.ID).ToList();
                Guid? duplicateId = FindDuplicate(itemsIds); // Duplicates validation is necessary because CheckAllItemsWithinFilter works by counting filtered items.
                if (duplicateId != null)
                    throw new FrameworkException(string.Format(
                        "Error while checking {2}: Loaded items have duplicate IDs ({0}:{1}).",
                        GenericRepository.EntityName, duplicateId, filterType.Name));

                var allowedGivenItemsFilter = new[] { new FilterCriteria(filterType), new FilterCriteria(itemsIds) };
                var allowedGivenItemsQuery = (IQueryable<IEntity>)GenericRepository.Read(allowedGivenItemsFilter, preferQuery: true);
                int allowedItemsCount = allowedGivenItemsQuery.Count();

                _logger.Trace(() => string.Format("Filter validation {0} test; requested: {1}, allowed: {2}.", filterType.Name, itemsIds.Count, allowedItemsCount));

                if (allowedItemsCount < itemsIds.Count)
                    return false;
                else if (allowedItemsCount > itemsIds.Count)
                    throw new FrameworkException(
                        $"Row permissions filter error:"
                        + $" Filter {filterType.Name} on {GenericRepository.EntityName} returned more items ({allowedItemsCount}) than requested ({itemsIds.Count})."
                        + $" Possible cause: {GenericRepository.EntityName}, or any related view that is used for its row permissions, returns duplicate IDs.");
            }

            return true;
        }

        public bool MissingItemId(IEntity[] items, out Guid? missingId)
        {
            missingId = null;

            if (items != null && items.Length > 0)
            {
                var findIds = items.Select(searchItem => searchItem.ID).ToList();
                var existingIds = GenericRepository.Query(findIds).Select(existingItem => existingItem.ID).ToList();
                var missingIds = findIds.Except(existingIds).Take(1);
                if (missingIds.Any())
                    missingId = missingIds.First();
            }

            return missingId != null;
        }
        
        private static Guid? FindDuplicate(List<Guid> ids)
        {
            if (ids.Distinct().Count() != ids.Count)
                return ids.GroupBy(id => id).First(group => group.Count() > 1).Key;
            return null;
        }
    }
}
