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

using Autofac.Features.Indexed;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Logging;
using Rhetos.Persistence;
using Rhetos.Processing.DefaultCommands;
using Rhetos.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Rhetos.Dom.DefaultConcepts
{
    /// <remarks>
    /// The term "entity" in the contest of this class representy any identifiable
    /// data structure (implementation of IEntity). Not to be confused with Entity
    /// DSL concept, which generates only one kind of IEntity (common entity).
    /// Other IEntity implementations can also be handled by this class.
    /// </remarks>
    public class GenericRepository<TEntityInterface>
        where TEntityInterface : class, IEntity
    {
        // ================================================================================
        #region Initialization

        private readonly ILogger _logger;
        private readonly ILogger _performanceLogger;
        private readonly IPersistenceTransaction _persistenceTransaction;
        private readonly GenericFilterHelper _genericFilterHelper;
        private readonly IDomainObjectModel _domainObjectModel;
        private readonly ApplyFiltersOnClientRead _applyFiltersOnClientRead;

        private readonly string _repositoryName;
        private readonly Lazy<IRepository> _repository;
        private readonly ReflectionHelper<TEntityInterface> _reflection;
        private const string UnsupportedLoaderMessage = "{0} does not implement a loader, a query or a filter with parameter {1}.";
        private const string UnsupportedFilterMessage = "{0} does not implement a filter with parameter {1}.";
        private const string UnsupportedQueryMessage = "{0} does not implement a query method or a filter with parameter {1} than will return an IQueryable. Try using Read function instead of the Query function.";

        public string EntityName { get; private set; }
        public IRepository EntityRepository { get { return _repository.Value; } }

        public GenericRepository(
            IDomainObjectModel domainObjectModel,
            Lazy<IIndex<string, IRepository>> repositories,
            RegisteredInterfaceImplementations registeredInterfaceImplementations,
            ILogProvider logProvider,
            IPersistenceTransaction persistenceTransaction,
            GenericFilterHelper genericFilterHelper,
            ApplyFiltersOnClientRead applyFiltersOnClientRead)
            : this(domainObjectModel, repositories, InitializeEntityName(registeredInterfaceImplementations), logProvider, persistenceTransaction, genericFilterHelper, applyFiltersOnClientRead)
        {
        }

        public GenericRepository(
            IDomainObjectModel domainObjectModel,
            Lazy<IIndex<string, IRepository>> repositories,
            string entityName,
            ILogProvider logProvider,
            IPersistenceTransaction persistenceTransaction,
            GenericFilterHelper genericFilterHelper,
            ApplyFiltersOnClientRead applyFiltersOnClientRead)
        {
            EntityName = entityName;
            _repositoryName = "GenericRepository(" + EntityName + ")";

            _logger = logProvider.GetLogger(_repositoryName);
            _performanceLogger = logProvider.GetLogger("Performance");
            _persistenceTransaction = persistenceTransaction;
            _genericFilterHelper = genericFilterHelper;
            _domainObjectModel = domainObjectModel;
            _applyFiltersOnClientRead = applyFiltersOnClientRead;

            _repository = new Lazy<IRepository>(() => InitializeRepository(repositories));
            _reflection = new ReflectionHelper<TEntityInterface>(EntityName, domainObjectModel, _repository);
        }

        private static string InitializeEntityName(RegisteredInterfaceImplementations registeredInterfaceImplementations)
        {
            if (typeof(TEntityInterface).IsInterface)
                return registeredInterfaceImplementations.GetValue(typeof(TEntityInterface),
                    "There is no registered implementation of " + typeof(TEntityInterface).FullName + " in domain object model."
                    + " Try using " + new RegisteredInterfaceImplementationHelperInfo().GetKeyword() + " DSL concept.");

            return typeof(TEntityInterface).FullName;
        }

        private IRepository InitializeRepository(Lazy<IIndex<string, IRepository>> repositories)
        {
            IRepository repository;
            if (!repositories.Value.TryGetValue(EntityName, out repository))
                throw new FrameworkException("There is no registered repository for " + EntityName + " in domain object model.");
            return repository;
        }

        #endregion
        // ================================================================================
        #region Instantiation

        public TEntityInterface CreateInstance()
        {
            return (TEntityInterface)Activator.CreateInstance(_reflection.EntityType);
        }

        /// <returns>Result is a List&lt;&gt; of the data structure type.
        /// The list is returened as IEnumerable&lt;&gt; of the interface type,
        /// to allow strongly-typed use of the list through TEntityInterface interface.
        /// Neither List&lt;&gt; or IList&lt;&gt; are covariant, so IEnumerable&lt;&gt; is used.</returns>
        public IEnumerable<TEntityInterface> CreateList(int size)
        {
            IEnumerable<object> instances = Enumerable.Range(1, size).Select(i => Activator.CreateInstance(_reflection.EntityType));
            var castInstances = _reflection.CastAsEntity(instances);

            var list = (IEnumerable<TEntityInterface>)Activator.CreateInstance(_reflection.ListType, new object[] { size });
            _reflection.AddRange(list, castInstances);

            return list;
        }

        /// <returns>Result is a List&lt;&gt; of the data structure type.
        /// The list is returened as IEnumerable&lt;&gt; of the interface type,
        /// to allow strongly-typed use of the list through TEntityInterface interface.
        /// Neither List&lt;&gt; or IList&lt;&gt; are covariant, so IEnumerable&lt;&gt; is used.</returns>
        public IEnumerable<TEntityInterface> CreateList<TSource>(IEnumerable<TSource> source, Action<TSource, TEntityInterface> initializer)
        {
            MaterializeQuick(ref source);
            var newItems = CreateList(source.Count());
            foreach (var pair in source.Zip(newItems, (sourceItem, newItem) => new { sourceItem, newItem}))
                initializer(pair.sourceItem, pair.newItem);
            return newItems;
        }

        /// <summary>
        /// If <paramref name="items"/> is not a list or an array, converts it to a list of <typeparam name="T"></typeparam>.
        /// Use this function to make sure that the <paramref name="items"/> is not a LINQ query,
        /// before using it multiple times, to aviod query evaulation every time
        /// (sometimes it means reading data from the database on every evaluation).
        /// </summary>
        private void MaterializeQuick<T>(ref IEnumerable<T> items)
        {
            if (items != null && !(items is IList))
                items = items.ToList();
        }

        /// <summary>
        /// If <paramref name="items"/> is not a list or an array, converts it to a list of entity type.
        /// </summary>
        private void MaterializeEntityList(ref IEnumerable<TEntityInterface> items)
        {
            if (items != null && !(items is IList))
                items = _reflection.ToListOfEntity(items);
        }

        #endregion
        // ================================================================================
        #region Reading

        public IQueryable<TEntityInterface> Query()
        {
            if (_reflection.RepositoryQueryMethod == null)
                throw new FrameworkException(EntityName + "'s repository does not implement the Query() method.");

            return (IQueryable<TEntityInterface>)_reflection.RepositoryQueryMethod.InvokeEx(_repository.Value);
        }

        public IQueryable<TEntityInterface> Query<TParameter>()
        {
            return Query(null, typeof(TParameter));
        }

        public IQueryable<TEntityInterface> Query(Expression<Func<TEntityInterface, bool>> filter)
        {
            return Query().Where(filter);
        }

        public IQueryable<TEntityInterface> Query<TParameter>(TParameter parameter)
        {
            Type filterType = parameter != null ? parameter.GetType() : typeof(TParameter);
            return Query(parameter, filterType);
        }

        public IQueryable<TEntityInterface> Query(object parameter, Type parameterType)
        {
            var items = ReadNonMaterialized(parameter, parameterType, true);

            var query = items as IQueryable<TEntityInterface>;
            if (query == null && items != null)
                throw new FrameworkException(string.Format(UnsupportedQueryMessage, EntityName, parameterType.FullName));

            return query;
        }

        public IEnumerable<TEntityInterface> Read()
        {
            return Read(new FilterAll());
        }

        public IEnumerable<TEntityInterface> Read<TParameter>()
        {
            return Read(null, typeof(TParameter));
        }

        public IEnumerable<TEntityInterface> Read(Expression<Func<TEntityInterface, bool>> filter)
        {
            return Read(parameter: filter);
        }

        /// <summary>
        /// Find suitable functions for reading from the TEntityInterface's repository, according to the naming convention.
        /// Only efficient functions will be used (it will not use a parameterless function to loading all data, then filter it in memory).
        /// </summary>
        public IEnumerable<TEntityInterface> Read<TParameter>(TParameter parameter)
        {
            Type filterType = parameter != null ? parameter.GetType() : typeof(TParameter);
            return Read(parameter, filterType);
        }

        public IEnumerable<TEntityInterface> Read(object parameter, Type parameterType)
        {
            var items = ReadNonMaterialized(parameter, parameterType, false);
            MaterializeEntityList(ref items);
            return items;
        }

        public IEnumerable<TEntityInterface> ReadNonMaterialized<TParameter>(TParameter parameter, bool preferQuery)
        {
            Type filterType = parameter != null ? parameter.GetType() : typeof(TParameter);
            return ReadNonMaterialized(parameter, filterType, preferQuery);
        }

        private delegate Func<IEnumerable<TEntityInterface>> ReadingOption();

        private class ReadingOptions : List<ReadingOption>
        {
            public Func<IEnumerable<TEntityInterface>> FirstOptionOrNull()
            {
                return this.Select(option => option()).FirstOrDefault(method => method != null);
            }
        }

        /// <param name="parameterType">
        /// It is usually <code>parameter.GetType()</code>, but be careful how to specify the filter if the parameter may be null.
        /// </param>
        public IEnumerable<TEntityInterface> ReadNonMaterialized(object parameter, Type parameterType, bool preferQuery)
        {
            // Use Filter(parameter), Query(parameter) or Filter(Query(), parameter), if any option exists
            ReadingOption filterWithParameter = () =>
            {
                var reader = _reflection.RepositoryLoadWithParameterMethod(parameterType);
                if (reader == null) return null;
                return () =>
                {
                    _logger.Trace(() => "Reading using Filter(" + reader.GetParameters()[0].ParameterType.FullName + ")");
                    return (IEnumerable<TEntityInterface>)reader.InvokeEx(_repository.Value, parameter);
                };
            };

            ReadingOption queryWithParameter = () =>
            {
                var reader = _reflection.RepositoryQueryWithParameterMethod(parameterType);
                if (reader == null) return null;
                return () =>
                {
                    _logger.Trace(() => "Reading using Query(" + reader.GetParameters()[0].ParameterType.FullName + ")");
                    return (IEnumerable<TEntityInterface>)reader.InvokeEx(_repository.Value, parameter);
                };
            };

            ReadingOption queryThenQueryableFilter = () =>
            {
                if (_reflection.RepositoryQueryMethod == null) return null;
                var reader = _reflection.RepositoryQueryableFilterMethod(parameterType);
                if (reader == null) return null;
                return () =>
                {
                    _logger.Trace(() => "Reading using queryable Filter(Query(), " + reader.GetParameters()[1].ParameterType.FullName + ")");
                    var query = _reflection.RepositoryQueryMethod.InvokeEx(_repository.Value);
                    return (IEnumerable<TEntityInterface>)reader.InvokeEx(_repository.Value, query, parameter);
                };
            };

            ReadingOption queryAll = () =>
            {
                var reader = _reflection.RepositoryQueryMethod;
                if (reader == null) return null;
                return () =>
                {
                    _logger.Trace(() => "Reading using Query()");
                    return (IEnumerable<TEntityInterface>)reader.InvokeEx(_repository.Value);
                };
            };


            {
                ReadingOptions options;
                if (!preferQuery)
                    options = new ReadingOptions { filterWithParameter, queryWithParameter, queryThenQueryableFilter };
                else if (typeof(FilterAll).IsAssignableFrom(parameterType))
                    options = new ReadingOptions { queryWithParameter, queryThenQueryableFilter, queryAll, filterWithParameter };
                else
                    options = new ReadingOptions { queryWithParameter, queryThenQueryableFilter, filterWithParameter };

                var readingMethod = options.FirstOptionOrNull();
                if (readingMethod != null)
                    return readingMethod();
            }

            // If the parameter is FilterAll, unless explicitly implemented above, use All() or Query() if any option exists
            if (typeof(FilterAll).IsAssignableFrom(parameterType))
            {
                var options = new ReadingOptions {
                    () => {
                        var reader = _reflection.RepositoryLoadMethod;
                        if (reader == null) return null;
                        return () => {
                            _logger.Trace(() => "Reading using All()");
                            return (IEnumerable<TEntityInterface>)reader.InvokeEx(_repository.Value);
                        };
                    },
                    () => {
                        var reader = _reflection.RepositoryQueryMethod;
                        if (reader == null) return null;
                        return () => {
                            _logger.Trace(() => "Reading using Query()");
                            return (IEnumerable<TEntityInterface>)reader.InvokeEx(_repository.Value);
                        };
                    }
                };

                if (preferQuery) options.Reverse();

                var readingMethod = options.FirstOptionOrNull();
                if (readingMethod != null)
                    return readingMethod();
            }

            // If the parameter is a generic filter, unless explicitly implemented above, execute it
            if (typeof(IEnumerable<FilterCriteria>).IsAssignableFrom(parameterType))
            {
                _logger.Trace(() => "Reading using generic filter");
                return ExecuteGenericFilter((IEnumerable<FilterCriteria>)parameter, preferQuery);
            }

            // If the parameter is a filter expression, unless explicitly implemented above, use Query().Where(parameter)
            if (_reflection.RepositoryQueryMethod != null && _reflection.IsPredicateExpression(parameterType))
            {
                _logger.Trace(() => "Reading using Query().Where(" + parameterType.Name + ")");
                var query = (IQueryable<TEntityInterface>)_reflection.RepositoryQueryMethod.InvokeEx(_repository.Value);
                return _reflection.Where(query, parameter);
            }

            // It there is only enumerable filter available, use inefficient loader with in-memory filtering: Filter(All(), parameter)
            {
                var reader = _reflection.RepositoryEnumerableFilterMethod(parameterType);
                if (reader != null)
                {
                    IEnumerable<TEntityInterface> items;
                    try
                    {
                        items = ReadNonMaterialized(new FilterAll(), false);
                    }
                    catch (FrameworkException)
                    {
                        items = null;
                    }

                    if (items != null)
                    {
                        _logger.Trace(() => "Reading using enumerable Filter(all, " + reader.GetParameters()[1].ParameterType.FullName + ")");
                        MaterializeEntityList(ref items);
                        return (IEnumerable<TEntityInterface>)reader.InvokeEx(_repository.Value, items, parameter);
                    }
                }
            }

            throw new FrameworkException(string.Format(UnsupportedLoaderMessage, EntityName, parameterType.FullName));
        }

        private IEnumerable<TEntityInterface> ExecuteGenericFilter(IEnumerable<FilterCriteria> genericFilter, bool preferQuery, IEnumerable<TEntityInterface> items = null)
        {
            var filterObjects = _genericFilterHelper.ToFilterObjects(genericFilter, _reflection.EntityType);

            foreach (var filter in filterObjects)
            {
                // When reading data, use 'Read' function on first filter parameter, and 'Filter' function on other filter parameters:

                if (items == null)
                    items = ReadNonMaterialized(filter.Parameter, filter.FilterType, preferQuery: filterObjects.Count > 1 || preferQuery);
                else
                    items = FilterNonMaterialized(items, filter.Parameter, filter.FilterType);

                if (items == null)
                    throw new FrameworkException(string.Format("{0}'s loader or filter result is null. ParameterType = '{1}', Parameter.ToString = '{2}'.",
                        EntityName, filter.FilterType.FullName, filter.Parameter.ToString()));
            }

            return items ?? ReadNonMaterialized(new FilterAll(), preferQuery: preferQuery);
        }

        private IEnumerable<T> GetSubset<T>(T[] source, int startIndex, int endIndex)
        {
            while (startIndex < endIndex) yield return source[startIndex++];
        }

        private IEnumerable<IEnumerable<T>> GetChunks<T>(T[] source, int chunkSize)
        {
            int start = 0;
            while (start < source.Length)
            {
                int end = Math.Min(start + chunkSize, source.Length);
                yield return GetSubset(source, start, end);
                start += chunkSize;
            }
        }

        /// <summary>
        /// Checks if all items are within filter 'filterName'. Works with materialized items.
        /// </summary>
        /// <param name="readItems"></param>
        public bool CheckAllItemsWithinFilter(object[] validateObjects, string filterName)
        {
            if (validateObjects == null) return true;
            var validateItems = (TEntityInterface[])validateObjects;
            var filterType = _domainObjectModel.Assembly.GetType(filterName);
            var filterMethodInfo = _reflection.RepositoryQueryableFilterMethod(filterType);

            if (filterMethodInfo != null)
            {
                Guid? duplicateId = FindDuplicate(validateItems.Select(a => a.ID).ToList());
                if (duplicateId != null)
                    throw new FrameworkException(string.Format(
                        "Error while checking {2}: Loaded items have duplicate IDs ({0}:{1}).",
                        EntityName, duplicateId, filterType.Name));

                var allowedItemsQuery = (IQueryable<TEntityInterface>)ReadNonMaterialized(null, filterType, true);

                const int batchSize = 2000; // true NHibernate/SQL limit is probably 2100
                _logger.Trace(() => string.Format("Found validation filter {0}, checking if all items are allowed (with batchSize = {1}).", filterType.Name, batchSize));
                
                var itemsBatches = GetChunks(validateItems, batchSize);
                foreach (var itemsBatch in itemsBatches)
                {
                    var itemsIds = itemsBatch.Select(item => item.ID).ToList();

                    // The following query is equivalent to: int allowedItemsCount = allowedItemsQuery.Where(allowedItem => readItemsIds.Contains(allowedItem.ID)).Count();
                    // The query is built by reflection to avoid an obscure problem with complex query in NHibernate: using generic parameter TEntityInterface for a query parameter
                    // breaks on some complex scenarios, so row permissions would not work on browse data structures, see unit test CommonConcepts.Test.RowPermissionsTest.Browse).
                    var allowedItemPredicateParameter = Expression.Parameter(_reflection.EntityType, "allowedItem");
                    var allowedItemPredicate = Expression.Lambda(
                        Expression.Call(
                            Expression.Constant(itemsIds),
                            typeof(List<Guid>).GetMethod("Contains"),
                            new[] { Expression.Property(allowedItemPredicateParameter, "ID") }),
                        allowedItemPredicateParameter);
                    int allowedItemsCount = _reflection.Where(allowedItemsQuery, allowedItemPredicate).Count();
                    _logger.Trace(() => string.Format("Filter validation {0} batch test; distinct requested: {1}, distinct allowed: {2}.", filterType.Name, itemsIds.Count, allowedItemsCount));

                    if (allowedItemsCount < itemsIds.Count)
                        return false;
                    else if (allowedItemsCount > itemsIds.Count)
                        throw new FrameworkException(string.Format(
                            "Invalid filter validation result: More items allowed ({0}) then items read ({1}). Check if the {2} filter on {3} returns duplicate IDs.",
                            allowedItemsCount, itemsIds.Count, filterType.Name, EntityName));
                }
            }

            return true;
        }

        private Guid? FindDuplicate(List<Guid> ids)
        {
            if (ids.Distinct().Count() != ids.Count())
                return ids.GroupBy(id => id).Where(group => group.Count() > 1).First().Key;
            return null;
        }

        public ReadCommandResult ExecuteReadCommand(ReadCommandInfo commandInfo)
        {
            if (!commandInfo.ReadRecords && !commandInfo.ReadTotalCount)
                throw new ClientException("Invalid ReadCommand argument: At least one of the properties ReadRecords or ReadTotalCount should be set to true.");

            if (commandInfo.Top  < 0)
                throw new ClientException("Invalid ReadCommand argument: Top parameter must not be negative.");

            if (commandInfo.Skip < 0)
                throw new ClientException("Invalid ReadCommand argument: Skip parameter must not be negative.");

            AutoApplyFilters(commandInfo);

            ReadCommandResult result;

            var specificMethod = _reflection.RepositoryReadCommandMethod;
            if (specificMethod != null)
                result = (ReadCommandResult)specificMethod.InvokeEx(_repository.Value, commandInfo);
            else
            {
                bool pagingIsUsed = commandInfo.Top > 0 || commandInfo.Skip > 0;

                IEnumerable<TEntityInterface> filtered = ReadNonMaterialized(commandInfo.Filters ?? new FilterCriteria[] { }, preferQuery: pagingIsUsed || !commandInfo.ReadRecords);

                TEntityInterface[] resultRecords = null;
                int? totalCount = null;

                if (commandInfo.ReadRecords)
                    resultRecords = (TEntityInterface[])_reflection.ToArrayOfEntity(_genericFilterHelper.SortAndPaginate(_reflection.AsQueryable(filtered), commandInfo));

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
            List<string> filterNames;
            if (_applyFiltersOnClientRead.TryGetValue(EntityName, out filterNames))
            {
                commandInfo.Filters = commandInfo.Filters ?? new FilterCriteria[] { };

                var newFilters = filterNames
                    .Where(name => !commandInfo.Filters.Any(existingFilter => GenericFilterHelper.EqualsSimpleFilter(existingFilter, name)))
                    .Select(name => new FilterCriteria { Filter = name })
                    .ToList();

                _logger.Trace(() => "AutoApplyFilters: " + string.Join(", ", newFilters.Select(f => f.Filter)));

                commandInfo.Filters = commandInfo.Filters.Concat(newFilters).ToArray();
            }
        }


        private static int SmartCount(IEnumerable<TEntityInterface> items)
        {
            var query = items as IQueryable<TEntityInterface>;
            return query != null ? query.Count() : items.Count();
        }

        #endregion
        // ================================================================================
        #region Filtering

        public IEnumerable<TEntityInterface> Filter<TParameter>(IEnumerable<TEntityInterface> items, TParameter parameter)
        {
            Type filterType = parameter != null ? parameter.GetType() : typeof(TParameter);
            return Filter(items, parameter, filterType);
        }

        public IEnumerable<TEntityInterface> Filter(IEnumerable<TEntityInterface> items, object parameter, Type parameterType)
        {
            var filteredItems = FilterNonMaterialized(items, parameter, parameterType);
            MaterializeEntityList(ref filteredItems);
            return filteredItems;
        }

        public IEnumerable<TEntityInterface> FilterNonMaterialized<TParameter>(IEnumerable<TEntityInterface> items, TParameter parameter)
        {
            Type filterType = parameter != null ? parameter.GetType() : typeof(TParameter);
            return FilterNonMaterialized(items, parameter, filterType);
        }

        public IEnumerable<TEntityInterface> FilterNonMaterialized(IEnumerable<TEntityInterface> items, object parameter, Type parameterType)
        {
            bool preferQuery = items is IQueryable;

            // If exists use Filter(IQueryable, TParameter) or Filter(IEnumerable, TParameter)
            {
                var options = new ReadingOptions {
                    () => {
                        var reader = _reflection.RepositoryEnumerableFilterMethod(parameterType);
                        if (reader == null) return null;
                        return () =>
                        {
                            _logger.Trace(() => "Filtering using enumerable Filter(items, " + reader.GetParameters()[1].ParameterType.FullName + ")");
                            MaterializeEntityList(ref items);
                            return (IEnumerable<TEntityInterface>)reader.InvokeEx(_repository.Value, items, parameter);
                        };
                    },
                    () => {
                        var reader = _reflection.RepositoryQueryableFilterMethod(parameterType);
                        if (reader == null) return null;
                        return () =>
                        {
                            _logger.Trace(() => "Filtering using queryable Filter(items, " + reader.GetParameters()[1].ParameterType.FullName + ")");
                            var query = _reflection.AsQueryable(items);
                            return (IEnumerable<TEntityInterface>)reader.InvokeEx(_repository.Value, query, parameter);
                        };
                    }
                };

                if (preferQuery) options.Reverse();

                var readingMethod = options.FirstOptionOrNull();
                if (readingMethod != null)
                    return readingMethod();
            }

            // If the parameter is FilterAll, unless explicitly implemented above, return all
            if (typeof(FilterAll).IsAssignableFrom(parameterType))
            {
                _logger.Trace(() => "Filtering all items returned.");
                return items;
            }

            // If the parameter is a generic filter, unless explicitly implemented above, execute it
            if (typeof(IEnumerable<FilterCriteria>).IsAssignableFrom(parameterType))
            {
                _logger.Trace(() => "Filtering using generic filter");
                return ExecuteGenericFilter((IEnumerable<FilterCriteria>)parameter, preferQuery, items);
            }

            // If the parameter is a filter expression, unless explicitly implemented above, use queryable items.Where(parameter)
            if (_reflection.IsPredicateExpression(parameterType))
            {
                _logger.Trace(() => "Filtering using items.AsQueryable().Where(" + parameterType.Name + ")");
                var query = _reflection.AsQueryable(items);
                return _reflection.Where(query, parameter);
            }

            // If the parameter is a filter function, unless explicitly implemented above, use materialized items.Where(parameter)
            if (typeof(Func<TEntityInterface, bool>).IsAssignableFrom(parameterType))
            {
                _logger.Trace(() => "Filtering using items.Where(" + parameterType.Name + ")");
                var filterFunction = parameter as Func<TEntityInterface, bool>;
                MaterializeEntityList(ref items);
                if (filterFunction != null)
                    return items.Where(filterFunction);
            }

            throw new FrameworkException(string.Format(UnsupportedFilterMessage, EntityName, parameterType.FullName));
        }

        #endregion
        // ================================================================================
        #region Writing

        /// <summary>
        /// Type casting helper. The type casting of performance-efficient; it will not generate a new list or array or instance.
        /// </summary>
        public void Save(IEnumerable<TEntityInterface> insertedNew, IEnumerable<TEntityInterface> updatedNew, IEnumerable<TEntityInterface> deletedIds, bool checkUserPermissions = false)
        {
            MaterializeEntityList(ref insertedNew);
            MaterializeEntityList(ref updatedNew);
            MaterializeEntityList(ref deletedIds);

            if (_reflection.RepositorySaveMethod == null)
                throw new FrameworkException(EntityName + "'s repository does not implement the Save(IEnumerable<Entity>, ...) method.");

            _reflection.RepositorySaveMethod.InvokeEx(_repository.Value,
                insertedNew != null ? _reflection.CastAsEntity(insertedNew) : null,
                updatedNew != null ? _reflection.CastAsEntity(updatedNew) : null,
                deletedIds != null ? _reflection.CastAsEntity(deletedIds) : null,
                checkUserPermissions);
        }

        public void InsertOrReadId<TProperties>(
            TEntityInterface newItem,
            Expression<Func<TEntityInterface, TProperties>> keySelector)
        {
            InsertOrReadId(new[] { newItem }, keySelector);
        }

        public void InsertOrReadId<TProperties>(
            IEnumerable<TEntityInterface> newItems,
            Expression<Func<TEntityInterface, TProperties>> keySelector)
        {
            var keySelectorHandler = new PropertySelectorExpression<TEntityInterface, TProperties>(keySelector);

            foreach (var newItem in newItems)
            {
                var filterLoad = keySelectorHandler.BuildComparisonPredicate(newItem);
                Guid id = _reflection.Where(Query(), filterLoad).Select(e => e.ID).FirstOrDefault();

                if (id == default(Guid))
                {
                    _logger.Trace(() => "Creating " + EntityName + " " + keySelectorHandler.ToString(newItem) + ".");
                    Save(new[] { newItem }, null, null);
                }
                else
                {
                    _logger.Trace(() => "Already exists " + EntityName + " " + keySelectorHandler.ToString(newItem) + ".");
                    newItem.ID = id;
                }
            }
        }

        public void InsertOrUpdateReadId<TKeyProperties, TValueProperties>(
            TEntityInterface newItem,
            Expression<Func<TEntityInterface, TKeyProperties>> keySelector,
            Expression<Func<TEntityInterface, TValueProperties>> valueSelector)
        {
            InsertOrUpdateReadId(new[] { newItem }, keySelector, valueSelector);
        }

        public void InsertOrUpdateReadId<TKeyProperties, TValueProperties>(
            IEnumerable<TEntityInterface> newItems,
            Expression<Func<TEntityInterface, TKeyProperties>> keySelector,
            Expression<Func<TEntityInterface, TValueProperties>> valueSelector)
        {
            var keyPropertiesHandler = new PropertySelectorExpression<TEntityInterface, TKeyProperties>(keySelector);
            var valuePropertiesHandler = new PropertySelectorExpression<TEntityInterface, TValueProperties>(valueSelector);

            foreach (var newItem in newItems)
            {
                var filterOld = keyPropertiesHandler.BuildComparisonPredicate(newItem);
                TEntityInterface oldItem = _reflection.Where(Query(), filterOld).ToList().SingleOrDefault();

                if (oldItem == null)
                {
                    _logger.Trace(() => "Creating " + EntityName + " " + keyPropertiesHandler.ToString(newItem) + ".");
                    Save(new[] { newItem }, null, null);
                }
                else
                {
                    newItem.ID = oldItem.ID;

                    var compareValue = valuePropertiesHandler.BuildComparisonPredicate(newItem);
                    bool same = compareValue.Compile().Invoke(oldItem);
                    if (!same)
                    {
                        // Copy value from newItem to oldItem (instead of simply saving newItem) to keep the old value of properties that are not covered with the given interfaces.
                        valuePropertiesHandler.Assign(oldItem, newItem);
                        Save(null, new[] { oldItem }, null);
                        _logger.Trace(() => "Updating " + EntityName + " " + keyPropertiesHandler.ToString(newItem) + ".");
                    }
                    else
                        _logger.Trace(() => "Already exists " + EntityName + " " + keyPropertiesHandler.ToString(newItem) + ".");
                }
            }
        }

        /// <param name="sameRecord">Compare key properties, determining the records that should be inserted or deleted.
        /// Typical implementation:
        /// <code>
        ///     class CompareName : IComparer&lt;ISomeEntity&gt;
        ///     {
        ///         public int Compare(ISomeEntity x, ISomeEntity y) { return string.Compare(x.Name, y.Name, StringComparison.InvariantCultureIgnoreCase); }
        ///     }
        /// </code></param>
        /// <param name="sameValue">Compare other properties, determining the records that should be updated.
        /// Comparison may also include key properties with stricter constaints (such as case sensitivity).
        /// Typical implementation:
        /// <code>(x, y) =&gt; x.Name == y.Name &amp;&amp; x.SomeValue == y.SomeValue;</code></param>
        /// <param name="assign">Typical implementation:
        /// <code>(destination, source) =&gt; {
        ///     destination.Property1 = source.Property1;
        ///     destination.Property2 = source.Property2; }</code></param>
        public void Diff(
            IEnumerable<TEntityInterface> oldItems, IEnumerable<TEntityInterface> newItems,
            IComparer<TEntityInterface> sameRecord, Func<TEntityInterface, TEntityInterface, bool> sameValue,
            Action<TEntityInterface, TEntityInterface> assign,
            out IEnumerable<TEntityInterface> toInsert, out IEnumerable<TEntityInterface> toUpdate, out IEnumerable<TEntityInterface> toDelete)
        {
            var toDeleteList = (IList)CreateList(0);
            var toInsertList = (IList)CreateList(0);
            var toUpdateList = (IList)CreateList(0);

            List<TEntityInterface> newItemsList = newItems.OrderBy(item => item, sameRecord).ToList();
            List<TEntityInterface> oldItemsList = oldItems.OrderBy(item => item, sameRecord).ToList();

            IEnumerator<TEntityInterface> newEnum = newItemsList.GetEnumerator();
            IEnumerator<TEntityInterface> oldEnum = oldItemsList.GetEnumerator();

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
                        {
                            _persistenceTransaction.NHibernateSession.Evict(oldEnum.Current);
                            assign(oldEnum.Current, newEnum.Current);
                            toUpdateList.Add(oldEnum.Current);
                        }

                        newExists = newEnum.MoveNext();
                        oldExists = oldEnum.MoveNext();
                    }
                    else if (keyDiff < 0)
                    {
                        // In some scenarios it is not enough to Evict the newEnum.Current before saving it (a problem with NHibernate lazy references?)
                        // TODO: After NHibernate lazy objects are removed, check if there is a need for copying newEnum.Current
                        var newItemFullyLoadedWithoutOrmBinding = CreateInstance();
                        newItemFullyLoadedWithoutOrmBinding.ID = newEnum.Current.ID;
                        assign(newItemFullyLoadedWithoutOrmBinding, newEnum.Current);
                        toInsertList.Add(newItemFullyLoadedWithoutOrmBinding);

                        newExists = newEnum.MoveNext();
                    }
                    else
                    {
                        toDeleteList.Add(oldEnum.Current);
                        oldExists = oldEnum.MoveNext();
                    }
                }
            }
            finally
            {
                newEnum.Dispose();
                oldEnum.Dispose();
            }

            toDelete = (IEnumerable<TEntityInterface>)toDeleteList;
            toInsert = (IEnumerable<TEntityInterface>)toInsertList;
            toUpdate = (IEnumerable<TEntityInterface>)toUpdateList;
        }

        public delegate void BeforeSave(ref IEnumerable<TEntityInterface> toInsert, ref IEnumerable<TEntityInterface> toUpdate, ref IEnumerable<TEntityInterface> toDelete);

        /// <param name="sameRecord">Compare key properties, determining the records that should be inserted or deleted.
        /// Typical implementation:
        /// <code>
        ///     class CompareName : IComparer&lt;ISomeEntity&gt;
        ///     {
        ///         public int Compare(ISomeEntity x, ISomeEntity y) { return string.Compare(x.Name, y.Name, StringComparison.InvariantCultureIgnoreCase); }
        ///     }
        /// </code></param>
        /// <param name="sameValue">Compare other properties, determining the records that should be updated.
        /// Comparison may also include key properties with stricter constaints (such as case sensitivity).
        /// Typical implementation:
        /// <code>(x, y) =&gt; x.Name == y.Name &amp;&amp; x.SomeValue == y.SomeValue;</code></param>
        /// <param name="filterLoad">For supported filters types see <see cref="Load{TParameter}(TParameter)"/> function.</param>
        /// <param name="assign">Typical implementation:
        /// <code>(destination, source) =&gt; {
        ///     destination.Property1 = source.Property1;
        ///     destination.Property2 = source.Property2; }</code></param>
        /// <param name="beforeSave"><code>(toInsert, toUpdate, toDelete) => { some code; } </code></param>
        public void InsertOrUpdateOrDelete(
            IEnumerable<TEntityInterface> newItems,
            IComparer<TEntityInterface> sameRecord,
            Func<TEntityInterface, TEntityInterface, bool> sameValue,
            object filterLoad,
            Action<TEntityInterface, TEntityInterface> assign,
            BeforeSave beforeSave = null)
        {
            var stopwatch = Stopwatch.StartNew();

            // Initialize new items:

            MaterializeQuick(ref newItems);
            _performanceLogger.Write(stopwatch, () => string.Format("{0}.InsertOrUpdateOrDelete: Initialize new items ({1})",
                _repositoryName, newItems.Count()));

            // Read old items:

            IEnumerable<TEntityInterface> oldItems = Read(filterLoad);
            _performanceLogger.Write(stopwatch, () => string.Format("{0}.InsertOrUpdateOrDelete: Read old items ({1})",
                _repositoryName, oldItems.Count()));

            // Compare new and old items:

            IEnumerable<TEntityInterface> toInsert, toUpdate, toDelete;
            Diff(oldItems, newItems, sameRecord, sameValue, assign, out toInsert, out toUpdate, out toDelete);
            _performanceLogger.Write(stopwatch, () => string.Format("{0}.InsertOrUpdateOrDelete: Diff ({1} new items, {2} old items, {3} to insert, {4} to update, {5} to delete)",
                _repositoryName, newItems.Count(), oldItems.Count(), toInsert.Count(), toUpdate.Count(), toDelete.Count()));

            // Modify old items to match new items:

            if (beforeSave != null)
                beforeSave(ref toInsert, ref toUpdate, ref toDelete);
            Save(toInsert, toUpdate, toDelete);
            _performanceLogger.Write(stopwatch, () => string.Format("{0}.InsertOrUpdateOrDelete: Save ({1} new items, {2} old items, {3} to insert, {4} to update, {5} to delete)",
                _repositoryName, newItems.Count(), oldItems.Count(), toInsert.Count(), toUpdate.Count(), toDelete.Count()));
        }

        /// <param name="sameRecord">Compare key properties, determining the records that should be inserted or deleted.
        /// Typical implementation:
        /// <code>
        ///     class CompareName : IComparer&lt;ISomeEntity&gt;
        ///     {
        ///         public int Compare(ISomeEntity x, ISomeEntity y) { return string.Compare(x.Name, y.Name, StringComparison.InvariantCultureIgnoreCase); }
        ///     }
        /// </code></param>
        /// <param name="sameValue">Compare other properties, determining the records that should be updated.
        /// Comparison may also include key properties with stricter constaints (such as case sensitivity).
        /// Typical implementation:
        /// <code>(x, y) =&gt; x.Name == y.Name &amp;&amp; x.SomeValue == y.SomeValue;</code></param>
        /// <param name="filterLoad">For supported filters types see <see cref="Load{TParameter}(TParameter)"/> function.</param>
        /// <param name="assign">Typical implementation:
        /// <code>(destination, source) =&gt; {
        ///     destination.Property1 == source.Property1;
        ///     destination.Property2 == source.Property2; }</code></param>
        /// <param name="deactivateDeleted">A filter that selects items that should be deactivated instead of deleted.
        /// Typical implementation:
        /// <code>(Func&lt;IEntity, bool&gt;)(item =&gt; ItemsInUseHashSet.Contains(item.ID))</code>
        /// <br/>For supported filters types see <see cref="Filter"/> function.
        /// </param>
        /// <param name="beforeSave"><code>(toInsert, toUpdate, toDelete) => { some code; } </code></param>
        public void InsertOrUpdateOrDeleteOrDeactivate(
            IEnumerable<TEntityInterface> newItems,
            IComparer<TEntityInterface> sameRecord,
            Func<TEntityInterface, TEntityInterface, bool> sameValue,
            object filterLoad,
            Action<TEntityInterface, TEntityInterface> assign,
            object filterDeactivateDeleted,
            BeforeSave beforeSave = null)
        {
            var stopwatch = Stopwatch.StartNew();

            // Initialize new items:

            MaterializeQuick(ref newItems);
            foreach (var newItem in newItems.Cast<IDeactivatable>())
                if (newItem.Active == null)
                    newItem.Active = true;
            _performanceLogger.Write(stopwatch, () => string.Format("{0}.InsertOrUpdateOrDeleteOrDeactivate: Initialize new items ({1})",
                _repositoryName, newItems.Count()));

            // Read old items:

            IEnumerable<TEntityInterface> oldItems = Read(filterLoad);
            _performanceLogger.Write(stopwatch, () => string.Format("{0}.InsertOrUpdateOrDeleteOrDeactivate: Read old items ({1})",
                _repositoryName, oldItems.Count()));

            // Compare new and old items:

            IEnumerable<TEntityInterface> toInsert, toUpdate, toDelete;
            Diff(oldItems, newItems, sameRecord, sameValue, assign, out toInsert, out toUpdate, out toDelete);
            _performanceLogger.Write(stopwatch, () => string.Format("{0}.InsertOrUpdateOrDeleteOrDeactivate: Diff ({1} new items, {2} old items, {3} to insert, {4} to update, {5} to delete)",
                _repositoryName, newItems.Count(), oldItems.Count(), toInsert.Count(), toUpdate.Count(), toDelete.Count()));

            // Deactivate some items instead of deleting:

            IEnumerable<TEntityInterface> toDeactivate = Filter(toDelete, filterDeactivateDeleted);
            int activeToDeactivateCount = 0;
            if (toDeactivate.Count() > 0)
            {
                int oldDeleteCount = toDelete.Count();

                // Don't delete items that should be deactivated:
                if (toDeactivate.Count() == oldDeleteCount)
                    toDelete = CreateList(0);
                else
                {
                    var toDeactivateIndex = new HashSet<TEntityInterface>(toDeactivate, new InstanceComparer());
                    toDelete = _reflection.ToListOfEntity(toDelete.Where(item => !toDeactivateIndex.Contains(item)));
                }
                if (toDelete.Count() + toDeactivate.Count() != oldDeleteCount)
                    throw new FrameworkException(string.Format(
                        "Invalid number of items to deactivate for '{0}'."
                            + " Verify if the deactivation filter ({1}) on that data structure retuns a valid subset of the given items."
                            + " {2} items to remove: {3} items to deactivate and {4} items remainig to delete (should be {5}).",
                        _repositoryName, filterDeactivateDeleted.GetType().FullName,
                        oldDeleteCount, toDeactivate.Count(), toDelete.Count(), oldDeleteCount - toDeactivate.Count()));

                // Update the items to deactivate (unless already deactivated):
                var activeToDeactivate = toDeactivate.Cast<IDeactivatable>().Where(item => item.Active == null || item.Active == true).ToList();
                foreach (var item in activeToDeactivate)
                    item.Active = false;
                _reflection.AddRange(toUpdate, _reflection.CastAsEntity(activeToDeactivate));
                activeToDeactivateCount = activeToDeactivate.Count;
            }
            _performanceLogger.Write(stopwatch, () => string.Format("{0}.InsertOrUpdateOrDeleteOrDeactivate: Deactivate ({1} to deactivate, {2} already deactivated)",
                _repositoryName, activeToDeactivateCount, toDeactivate.Count() - activeToDeactivateCount));

            // Modify old items to match new items:

            if (beforeSave != null)
                beforeSave(ref toInsert, ref toUpdate, ref toDelete);
            Save(toInsert, toUpdate, toDelete);
            _performanceLogger.Write(stopwatch, () => string.Format("{0}.InsertOrUpdateOrDeleteOrDeactivate: Save ({1} new items, {2} old items, {3} to insert, {4} to update, {5} to delete)",
                _repositoryName, newItems.Count(), oldItems.Count(), toInsert.Count(), toUpdate.Count(), toDelete.Count()));
        }

        private class InstanceComparer : IEqualityComparer<TEntityInterface>
        {
            public bool Equals(TEntityInterface x, TEntityInterface y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(TEntityInterface obj)
            {
                return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
            }
        }

        public void RecomputeFrom(
            string source,
            object filterLoad = null)
        {
            var recomputeMethod = _reflection.RepositoryRecomputeFromMethod(source);

            if (recomputeMethod == null)
                throw new FrameworkException(string.Format(
                    "{0}'s repository does not implement the method to recompute from {1} ({2}).",
                    EntityName, source, _reflection.RepositoryRecomputeFromMethodName(source)));

            recomputeMethod.InvokeEx(_repository.Value, filterLoad, null);
        }

        #endregion
    }
}