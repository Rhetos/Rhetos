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

using Rhetos.Dom;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Rhetos.Processing.DefaultCommands
{
    public class ServerCommandsUtility
    {
        private readonly ILogger _logger;
        private readonly ApplyFiltersOnClientRead _applyFiltersOnClientRead;
        private readonly IDomainObjectModel _domainObjectModel;

        public ServerCommandsUtility(
            ILogProvider logProvider,
            ApplyFiltersOnClientRead applyFiltersOnClientRead,
            IDomainObjectModel domainObjectModel)
        {
            _logger = logProvider.GetLogger(GetType().Name);
            _applyFiltersOnClientRead = applyFiltersOnClientRead;
            _domainObjectModel = domainObjectModel;
        }

        /// <summary>
        /// Checks if all items are within filter 'filterName'. Works with materialized items.
        /// </summary>
        public bool CheckAllItemsWithinFilter(object[] validateObjects, string filterName, GenericRepository<IEntity> genericRepository)
        {
            if (validateObjects == null) return true;
            var validateItems = (IEntity[])validateObjects;
            var filterType = _domainObjectModel.GetType(filterName);
            var filterMethodInfo = genericRepository.Reflection.RepositoryQueryableFilterMethod(filterType);

            if (filterMethodInfo != null)
            {
                var itemsIds = validateItems.Select(item => item.ID).ToList();
                Guid? duplicateId = FindDuplicate(itemsIds); // Duplicates validation is necessary because CheckAllItemsWithinFilter works by counting filtered items.
                if (duplicateId != null)
                    throw new FrameworkException(string.Format(
                        "Error while checking {2}: Loaded items have duplicate IDs ({0}:{1}).",
                        genericRepository.EntityName, duplicateId, filterType.Name));

                var allowedGivenItemsFilter = new[] { new FilterCriteria { Filter = filterName }, new FilterCriteria(itemsIds) };
                var allowedGivenItemsQuery = (IQueryable<IEntity>)genericRepository.Read(allowedGivenItemsFilter, preferQuery: true);
                int allowedItemsCount = allowedGivenItemsQuery.Count();

                _logger.Trace(() => string.Format("Filter validation {0} test; requested: {1}, allowed: {2}.", filterType.Name, itemsIds.Count, allowedItemsCount));

                if (allowedItemsCount < itemsIds.Count)
                    return false;
                else if (allowedItemsCount > itemsIds.Count)
                    throw new FrameworkException(
                        $"Row permissions filter error:"
                        + $" Filter {filterType.Name} on {genericRepository.EntityName} returned more items ({allowedItemsCount}) then requested ({itemsIds.Count})."
                        + $" Possible cause: {genericRepository.EntityName}, or any related view that is uses for its row permissions, returns duplicate IDs.");
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

        public ReadCommandResult ExecuteReadCommand(ReadCommandInfo commandInfo, GenericRepository<IEntity> genericRepository)
        {
            if (!commandInfo.ReadRecords && !commandInfo.ReadTotalCount)
                throw new ClientException("Invalid ReadCommand argument: At least one of the properties ReadRecords or ReadTotalCount should be set to true.");

            if (commandInfo.Top < 0)
                throw new ClientException("Invalid ReadCommand argument: Top parameter must not be negative.");

            if (commandInfo.Skip < 0)
                throw new ClientException("Invalid ReadCommand argument: Skip parameter must not be negative.");

            if (commandInfo.DataSource != genericRepository.EntityName)
                throw new FrameworkException(string.Format(
                    "Invalid ExecuteReadCommand arguments: The given ReadCommandInfo ('{0}') does not match the GenericRepository ('{1}').",
                    commandInfo.DataSource, genericRepository.EntityName));

            AutoApplyFilters(commandInfo);

            ReadCommandResult result;

            var specificMethod = genericRepository.Reflection.RepositoryReadCommandMethod;
            if (specificMethod != null)
                result = (ReadCommandResult)specificMethod.InvokeEx(genericRepository.EntityRepository, commandInfo);
            else
            {
                bool pagingIsUsed = commandInfo.Top > 0 || commandInfo.Skip > 0;

                IEnumerable<IEntity> filtered = genericRepository.Read(commandInfo.Filters ?? new FilterCriteria[] { }, preferQuery: pagingIsUsed || !commandInfo.ReadRecords);

                IEntity[] resultRecords = null;
                int? totalCount = null;

                if (commandInfo.ReadRecords)
                {
                    var sortedAndPaginated = GenericFilterHelper.SortAndPaginate(genericRepository.Reflection.AsQueryable(filtered), commandInfo);
                    resultRecords = (IEntity[])genericRepository.Reflection.ToArrayOfEntity(sortedAndPaginated);
                }

                if (commandInfo.ReadTotalCount)
                    if (pagingIsUsed)
                        totalCount = SmartCount(filtered);
                    else
                        totalCount = resultRecords != null ? resultRecords.Length : SmartCount(filtered);

                result = new ReadCommandResult
                {
                    Records = resultRecords,
                    TotalCount = totalCount
                };
            }

            return result;
        }

        private void AutoApplyFilters(ReadCommandInfo commandInfo)
        {
            List<ApplyFilterWhere> applyFilters;
            if (_applyFiltersOnClientRead.TryGetValue(commandInfo.DataSource, out applyFilters))
            {
                commandInfo.Filters = commandInfo.Filters ?? new FilterCriteria[] { };

                var newFilters = applyFilters
                    .Where(applyFilter => applyFilter.Where == null || applyFilter.Where(commandInfo))
                    .Where(applyFilter => !commandInfo.Filters.Any(existingFilter => GenericFilterHelper.EqualsSimpleFilter(existingFilter, applyFilter.FilterName)))
                    .Select(applyFilter => new FilterCriteria { Filter = applyFilter.FilterName })
                    .ToList();

                _logger.Trace(() => "AutoApplyFilters: " + string.Join(", ", newFilters.Select(f => f.Filter)));

                commandInfo.Filters = commandInfo.Filters.Concat(newFilters).ToArray();
            }
        }

        private static int SmartCount(IEnumerable<IEntity> items)
        {
            var query = items as IQueryable<IEntity>;
            return query != null ? query.Count() : items.Count();
        }

        private static IEnumerable<T> GetSubset<T>(T[] source, int startIndex, int endIndex)
        {
            while (startIndex < endIndex) yield return source[startIndex++];
        }

        private static IEnumerable<IEnumerable<T>> GetChunks<T>(T[] source, int chunkSize)
        {
            int start = 0;
            while (start < source.Length)
            {
                int end = Math.Min(start + chunkSize, source.Length);
                yield return GetSubset(source, start, end);
                start += chunkSize;
            }
        }

        private Guid? FindDuplicate(List<Guid> ids)
        {
            if (ids.Distinct().Count() != ids.Count())
                return ids.GroupBy(id => id).Where(group => group.Count() > 1).First().Key;
            return null;
        }

        //================================================================
        #region Sorting and paging

        private IQueryable<T> SortAndPaginate<T>(IQueryable<T> query, ReadCommandInfo commandInfo)
        {
            bool pagingIsUsed = commandInfo.Top > 0 || commandInfo.Skip > 0;

            if (pagingIsUsed && (commandInfo.OrderByProperties == null || commandInfo.OrderByProperties.Length == 0))
                throw new ClientException("Invalid ReadCommand argument: Sort order must be set if paging is used (Top or Skip).");

            if (commandInfo.OrderByProperties != null)
                foreach (var order in commandInfo.OrderByProperties)
                    query = Sort(query, order.Property, ascending: !order.Descending);

            if (commandInfo.Skip > 0)
                query = query.Skip(commandInfo.Skip);

            if (commandInfo.Top > 0)
                query = query.Take(commandInfo.Top);

            return query;
        }

        private IQueryable<T> Sort<T>(IQueryable<T> source, string orderByProperty, bool ascending = true)
        {
            if (string.IsNullOrEmpty(orderByProperty))
                return source;

            Type itemType = source.GetType().GetGenericArguments().Single();

            ParameterExpression parameter = Expression.Parameter(itemType, "posting");
            Expression property = Expression.Property(parameter, orderByProperty);
            LambdaExpression propertySelector = Expression.Lambda(property, new[] { parameter });

            MethodInfo orderMethod = ascending ? OrderByAscendingMethod : OrderByDescendingMethod;
            MethodInfo genericOrderMethod = orderMethod.MakeGenericMethod(new[] { itemType, property.Type });

            return (IQueryable<T>)genericOrderMethod.InvokeEx(null, source, propertySelector);
        }

        private static readonly MethodInfo OrderByAscendingMethod =
            typeof(Queryable).GetMethods()
                .Where(method => method.Name == "OrderBy")
                .Where(method => method.GetParameters().Length == 2)
                .Single();

        private static readonly MethodInfo OrderByDescendingMethod =
            typeof(Queryable).GetMethods()
                .Where(method => method.Name == "OrderByDescending")
                .Where(method => method.GetParameters().Length == 2)
                .Single();

        #endregion
    }
}
