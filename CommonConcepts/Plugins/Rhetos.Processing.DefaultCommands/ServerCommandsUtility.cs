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
    public class ServerCommandsUtility
    {
        private readonly ILogger _logger;

        public ServerCommandsUtility(
            ILogProvider logProvider)
        {
            _logger = logProvider.GetLogger(GetType().Name);
        }

        /// <summary>
        /// Checks if all items are within the given filter. Works with materialized items.
        /// </summary>
        public bool CheckAllItemsWithinFilter(object[] validateObjects, Type filterType, GenericRepository<IEntity> genericRepository)
        {
            if (validateObjects == null) return true;
            var validateItems = (IEntity[])validateObjects;

            var filterMethodInfo = genericRepository.Reflection.RepositoryQueryableFilterMethod(filterType); // TODO: After implementing repository metadata for available filter parameters (loader, query, filter, queryFilter), it should be used here instead of RepositoryQueryableFilterMethod.

            if (filterMethodInfo != null)
            {
                var itemsIds = validateItems.Select(item => item.ID).ToList();
                Guid? duplicateId = FindDuplicate(itemsIds); // Duplicates validation is necessary because CheckAllItemsWithinFilter works by counting filtered items.
                if (duplicateId != null)
                    throw new FrameworkException(string.Format(
                        "Error while checking {2}: Loaded items have duplicate IDs ({0}:{1}).",
                        genericRepository.EntityName, duplicateId, filterType.Name));

                var allowedGivenItemsFilter = new[] { new FilterCriteria(filterType), new FilterCriteria(itemsIds) };
                var allowedGivenItemsQuery = (IQueryable<IEntity>)genericRepository.Read(allowedGivenItemsFilter, preferQuery: true);
                int allowedItemsCount = allowedGivenItemsQuery.Count();

                _logger.Trace(() => string.Format("Filter validation {0} test; requested: {1}, allowed: {2}.", filterType.Name, itemsIds.Count, allowedItemsCount));

                if (allowedItemsCount < itemsIds.Count)
                    return false;
                else if (allowedItemsCount > itemsIds.Count)
                    throw new FrameworkException(
                        $"Row permissions filter error:"
                        + $" Filter {filterType.Name} on {genericRepository.EntityName} returned more items ({allowedItemsCount}) than requested ({itemsIds.Count})."
                        + $" Possible cause: {genericRepository.EntityName}, or any related view that is used for its row permissions, returns duplicate IDs.");
            }

            return true;
        }

        public bool MissingItemId(IEntity[] items, GenericRepository<IEntity> genericRepository, out Guid? missingId)
        {
            missingId = null;

            if (items != null && items.Length > 0)
            {
                var findIds = items.Select(searchItem => searchItem.ID).ToList();
                var existingIds = genericRepository.Query(findIds).Select(existingItem => existingItem.ID).ToList();
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
