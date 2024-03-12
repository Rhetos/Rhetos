﻿/*
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
using Rhetos.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace Rhetos.Dom.DefaultConcepts
{
    public class GenericRepositoryParameters
    {
        public IDomainObjectModel DomainObjectModel { get; set; }
        public Lazy<IIndex<string, IRepository>> Repositories { get; set; }
        public ILogProvider LogProvider { get; set; }
        public GenericFilterHelper GenericFilterHelper { get; set; }
        public IDelayedLogProvider DelayedLogProvider { get; set; }
        public IOrmUtility OrmUtility { get; set; }
    }

    /// <summary>
    /// GenericRepository is a helper for a type-safe reflection-based use of the repositories in the generated object model,
    /// without a need to directly reference the generated assembly.
    /// For the type-safe use of an entity with a GenericRepository, set the generic parameter to an interface that an entity implements (ImplementInterface concept).
    /// For the pure reflection-based use of an entity with a GenericRepository, provide the full entity name when creating the GenericRepository; the IEntity interface will be used for all methods.
    /// </summary>
    /// <remarks>
    /// The term "entity" in the contest of this class represents any identifiable
    /// data structure (implementation of IEntity). Not to be confused with Entity
    /// DSL concept, which generates only one kind of IEntity (common entity).
    /// Other IEntity implementations can also be handled by this class.
    /// </remarks>
    public class GenericRepository<TEntityInterface> : IWritableRepository<TEntityInterface>, IQueryableRepository<TEntityInterface, TEntityInterface>
        where TEntityInterface : class, IEntity
    {
        // ================================================================================
        #region Initialization

        private readonly ILogger _logger;
        private readonly ILogger _performanceLogger;
        private readonly IDelayedLogger _delayedLogger;
        private readonly GenericFilterHelper _genericFilterHelper;
        private readonly IOrmUtility _ormUtility;

        private readonly string _genericRepositoryName;
        private readonly Lazy<IRepository> _repository;

        public string EntityName { get; private set; }
        public IRepository EntityRepository { get { return _repository.Value; } }
        public ReflectionHelper<TEntityInterface> Reflection { get; private set; }

        public GenericRepository(GenericRepositoryParameters parameters, RegisteredInterfaceImplementations registeredInterfaceImplementations)
            : this(parameters, InitializeEntityName(registeredInterfaceImplementations))
        {
        }

        public GenericRepository(GenericRepositoryParameters parameters, string entityName)
        {
            EntityName = entityName;
            _genericRepositoryName = "GenericRepository(" + EntityName + ")";

            _logger = parameters.LogProvider.GetLogger(_genericRepositoryName);
            _performanceLogger = parameters.LogProvider.GetLogger("Performance." + _genericRepositoryName);
            _delayedLogger = parameters.DelayedLogProvider.GetLogger(_genericRepositoryName);
            _genericFilterHelper = parameters.GenericFilterHelper;
            _ormUtility = parameters.OrmUtility;

            _repository = new Lazy<IRepository>(() => InitializeRepository(parameters.Repositories));
            Reflection = new ReflectionHelper<TEntityInterface>(EntityName, parameters.DomainObjectModel, _repository);
        }

        private static string InitializeEntityName(RegisteredInterfaceImplementations registeredInterfaceImplementations)
        {
            if (typeof(TEntityInterface).IsInterface)
                return registeredInterfaceImplementations.GetValue(typeof(TEntityInterface),
                    "There is no registered implementation of " + typeof(TEntityInterface).FullName + " in domain object model."
                    + " Try using " + new RegisteredInterfaceImplementationHelperInfo().GetKeyword() + " DSL concept.");

            string fullName = typeof(TEntityInterface).FullName;

            const string queryablePrefix = "Common.Queryable.";
            if (fullName.StartsWith(queryablePrefix))
            {
                int separator = fullName.IndexOf('_');
                if (separator < 0)
                    throw new FrameworkException("Unexpected queryable type \"" + fullName + "\".");
                string module = fullName.Substring(queryablePrefix.Length, separator - queryablePrefix.Length);
                string dataStructure = fullName.Substring(separator + 1);
                return module + "." + dataStructure;
            }

            return fullName;
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
            return (TEntityInterface)Activator.CreateInstance(Reflection.EntityType);
        }

        /// <returns>Result is a List&lt;&gt; of the data structure type.
        /// The list is returned as IEnumerable&lt;&gt; of the interface type,
        /// to allow strongly-typed use of the list through TEntityInterface interface.
        /// Neither List&lt;&gt; or IList&lt;&gt; are covariant, so IEnumerable&lt;&gt; is used.</returns>
        public IEnumerable<TEntityInterface> CreateList(int size)
        {
            IEnumerable<object> instances = Enumerable.Range(1, size).Select(i => Activator.CreateInstance(Reflection.EntityType));
            var castInstances = Reflection.CastAsEntity(instances);

            var list = (IEnumerable<TEntityInterface>)Activator.CreateInstance(Reflection.ListType, [size]);
            Reflection.AddRange(list, castInstances);

            return list;
        }

        /// <returns>Result is a List&lt;&gt; of the data structure type.
        /// The list is returned as IEnumerable&lt;&gt; of the interface type,
        /// to allow strongly-typed use of the list through TEntityInterface interface.
        /// Neither List&lt;&gt; or IList&lt;&gt; are covariant, so IEnumerable&lt;&gt; is used.</returns>
        public IEnumerable<TEntityInterface> CreateList<TSource>(IEnumerable<TSource> source, Action<TSource, TEntityInterface> initializer)
        {
            var sourceCollection = CsUtility.Materialized(source);
            var newItems = CreateList(sourceCollection.Count);
            foreach (var pair in sourceCollection.Zip(newItems, (sourceItem, newItem) => new { sourceItem, newItem }))
                initializer(pair.sourceItem, pair.newItem);
            return newItems;
        }

        #endregion
        // ================================================================================
        #region Reading

        public IQueryable<TEntityInterface> Query(object parameter, Type parameterType)
        {
            var items = Read(parameter, parameterType, preferQuery: true);

            var query = items as IQueryable<TEntityInterface>;
            if (query == null && items != null)
                throw new FrameworkException($"{EntityName} does not implement a query method or a filter with parameter {parameterType} that returns an IQueryable. There is an IEnumerable loader of filter implemented, so try using the Load function instead of the Query function.");

            return query;
        }

        public IQueryable<TEntityInterface> Query<TParameter>()
        {
            return Query(null, typeof(TParameter));
        }

        public IEnumerable<TEntityInterface> Load(object parameter, Type parameterType)
        {
            using (_delayedLogger.PerformanceWarning(() => $"Loading with filter '{parameterType}'."))
            {
                var items = Read(parameter, parameterType, preferQuery: false);
                Reflection.MaterializeEntityList(ref items);
                return items;
            }
        }

        public IEnumerable<TEntityInterface> Load<TParameter>()
        {
            return Load(null, typeof(TParameter));
        }

        private delegate Func<IEnumerable<TEntityInterface>> ReadingOption();

        private class ReadingOptions : List<ReadingOption>
        {
            public Func<IEnumerable<TEntityInterface>> FirstOptionOrNull()
            {
                return this.Select(option => option()).FirstOrDefault(method => method != null);
            }
        }

        public IEnumerable<TEntityInterface> Read(object parameter, Type parameterType, bool preferQuery)
        {
            // Use Load(parameter), Query(parameter) or Filter(Query(), parameter), if any of the options exist.
            ReadingOption loaderWithParameter = () =>
                {
                    var reader = Reflection.RepositoryLoadWithParameterMethod(parameterType);
                    if (reader == null) return null;
                    return () =>
                    {
                        _logger.Trace(() => $"Reading using Load({reader.GetParameters()[0].ParameterType})");
                        return (IEnumerable<TEntityInterface>)reader.InvokeEx(_repository.Value, parameter);
                    };
                };

            ReadingOption queryWithParameter = () =>
                {
                    var reader = Reflection.RepositoryQueryWithParameterMethod(parameterType);
                    if (reader == null) return null;
                    return () =>
                    {
                        _logger.Trace(() => $"Reading using Query({reader.GetParameters()[0].ParameterType})");
                        return (IEnumerable<TEntityInterface>)reader.InvokeEx(_repository.Value, parameter);
                    };
                };

            ReadingOption queryThenQueryableFilter = () =>
                {
                    if (Reflection.RepositoryQueryMethod == null) return null;
                    var reader = Reflection.RepositoryQueryableFilterMethod(parameterType);
                    if (reader == null) return null;
                    return () =>
                    {
                        _logger.Trace(() => $"Reading using queryable Filter(Query(), {reader.GetParameters()[1].ParameterType})");
                        var query = Reflection.RepositoryQueryMethod.InvokeEx(_repository.Value);
                        return (IEnumerable<TEntityInterface>)reader.InvokeEx(_repository.Value, query, parameter);
                    };
                };

            ReadingOption queryAll = () =>
                {
                    if (!typeof(FilterAll).IsAssignableFrom(parameterType)) return null;
                    var reader = Reflection.RepositoryQueryMethod;
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
                    options = new ReadingOptions { loaderWithParameter, queryWithParameter, queryThenQueryableFilter };
                else
                    options = new ReadingOptions { queryWithParameter, queryThenQueryableFilter, queryAll, loaderWithParameter };

                var readingMethod = options.FirstOptionOrNull();
                if (readingMethod != null)
                    return readingMethod();
            }

            // If the parameter is FilterAll, unless explicitly implemented above, use Load() or Query() if any option exists
            if (typeof(FilterAll).IsAssignableFrom(parameterType))
            {
                var options = new ReadingOptions {
                    () => {
                        var reader = Reflection.RepositoryLoadMethod;
                        if (reader == null) return null;
                        return () => {
                            _logger.Trace(() => "Reading using Load()");
                            return (IEnumerable<TEntityInterface>)reader.InvokeEx(_repository.Value);
                        };
                    },
                    () => {
                        var reader = Reflection.RepositoryQueryMethod;
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
            if (parameterType == typeof(FilterCriteria))
            {
                _logger.Trace(() => "Reading using generic filter");
                return ExecuteGenericFilter(new[] { (FilterCriteria)parameter }, preferQuery);
            }
            if (typeof(IEnumerable<FilterCriteria>).IsAssignableFrom(parameterType))
            {
                _logger.Trace(() => "Reading using generic filters");
                return ExecuteGenericFilter((IEnumerable<FilterCriteria>)parameter, preferQuery);
            }

            // If the parameter is a generic property filter, unless explicitly implemented above, use Query().Where(property filter)
            if (Reflection.RepositoryQueryMethod != null && typeof(IEnumerable<PropertyFilter>).IsAssignableFrom(parameterType))
            {
                _logger.Trace(() => "Reading using Query().Where(property filter");
                var query = (IQueryable<TEntityInterface>)Reflection.RepositoryQueryMethod.InvokeEx(_repository.Value);
                var filterExpression = _genericFilterHelper.ToExpression((IEnumerable<PropertyFilter>)parameter, Reflection.EntityNavigationType);
                return Reflection.Where(query, filterExpression);
            }

            // If the parameter is a filter expression, unless explicitly implemented above, use Query().Where(parameter)
            if (Reflection.RepositoryQueryMethod != null && Reflection.IsPredicateExpression(parameterType))
            {
                _logger.Trace(() => "Reading using Query().Where(" + parameterType.Name + ")");
                var query = (IQueryable<TEntityInterface>)Reflection.RepositoryQueryMethod.InvokeEx(_repository.Value);
                return Reflection.Where(query, (Expression)parameter);
            }

            // If the parameter is a IEnumarable<Guid>, it will be interpreted as filter by IDs.
            if (Reflection.RepositoryQueryMethod != null && typeof(IEnumerable<Guid>).IsAssignableFrom(parameterType))
            {
                _logger.Trace(() => "Reading using Query().Where(item => guids.Contains(item.ID))");
                var parameterList = parameter is List<Guid> list ? list : ((IEnumerable<Guid>)parameter).ToList();
                var query = (IQueryable<TEntityInterface>)Reflection.RepositoryQueryMethod.InvokeEx(_repository.Value);

                // The query is built by reflection to avoid an obscure problem with complex query in NHibernate:
                // using generic parameter TEntityInterface or IEntity for a query parameter fails with exception on some complex scenarios.
                var filterPredicateParameter = Expression.Parameter(Reflection.EntityType, "item");
                var filterPredicate = Expression.Lambda(
                    Expression.Call(
                        _ormUtility.CreateContainsItemsExpression(parameterList),
                        typeof(List<Guid>).GetMethod("Contains"),
                        new[] { Expression.Property(filterPredicateParameter, "ID") }),
                    filterPredicateParameter);

                return Reflection.Where(query, _ormUtility.OptimizeContains(filterPredicate));
            }

            // It there is only enumerable filter available, use inefficient loader with in-memory filtering: Filter(Load(), parameter)
            {
                var reader = Reflection.RepositoryEnumerableFilterMethod(parameterType);
                if (reader != null)
                {
                    IEnumerable<TEntityInterface> items;
                    try
                    {
                        items = Read(null, typeof(FilterAll), preferQuery: false);
                    }
                    catch (FrameworkException)
                    {
                        items = null;
                    }

                    if (items != null)
                    {
                        _logger.Trace(() => "Reading using enumerable Filter(all, " + reader.GetParameters()[1].ParameterType.FullName + ")");
                        Reflection.MaterializeEntityList(ref items);
                        return (IEnumerable<TEntityInterface>)reader.InvokeEx(_repository.Value, items, parameter);
                    }
                }
            }

            throw new FrameworkException($"{EntityName} does not implement a load method, a query or a filter with parameter {parameterType}.");
        }

        private IEnumerable<TEntityInterface> ExecuteGenericFilter(IEnumerable<FilterCriteria> genericFilter, bool preferQuery, IEnumerable<TEntityInterface> items = null)
        {
            var filterObjects = _genericFilterHelper.ToFilterObjects(EntityName, genericFilter);

            foreach (var filter in filterObjects)
            {
                // When reading data, use 'Read' function on first filter parameter, and 'Filter' function on other filter parameters:

                if (items == null)
                    items = Read(filter.Parameter, filter.FilterType, preferQuery: filterObjects.Count > 1 || preferQuery);
                else
                    items = FilterOrQuery(items, filter.Parameter, filter.FilterType);

                if (items == null)
                    throw new FrameworkException($"{EntityName}'s loader or filter result is null. ParameterType = '{filter.FilterType}', Parameter.ToString = '{filter.Parameter}'.");
            }

            return items ?? Read(null, typeof(FilterAll), preferQuery: preferQuery);
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
            var filteredItems = FilterOrQuery(items, parameter, parameterType);
            Reflection.MaterializeEntityList(ref filteredItems);
            return filteredItems;
        }

        /// <summary>
        /// A more flexible filtering method than Filter(). Filter() will always return materialized items.
        /// </summary>
        public IEnumerable<TEntityInterface> FilterOrQuery<TParameter>(IEnumerable<TEntityInterface> items, TParameter parameter)
        {
            Type filterType = parameter != null ? parameter.GetType() : typeof(TParameter);
            return FilterOrQuery(items, parameter, filterType);
        }

        /// <summary>
        /// A more flexible filtering method than Filter(). Filter() will always return materialized items.
        /// </summary>
        public IEnumerable<TEntityInterface> FilterOrQuery(IEnumerable<TEntityInterface> items, object parameter, Type parameterType)
        {
            bool preferQuery = items is IQueryable;

            // If exists use Filter(IQueryable, TParameter) or Filter(IEnumerable, TParameter)
            {
                ReadingOption enumerableFilter = () =>
                    {
                        var reader = Reflection.RepositoryEnumerableFilterMethod(parameterType);
                        if (reader == null) return null;
                        return () =>
                        {
                            _logger.Trace(() => $"Filtering using enumerable Filter(items, {reader.GetParameters()[1].ParameterType})");
                            Reflection.MaterializeEntityList(ref items);
                            return (IEnumerable<TEntityInterface>)reader.InvokeEx(_repository.Value, items, parameter);
                        };
                    };

                ReadingOption queryableFilter = () =>
                    {
                        var reader = Reflection.RepositoryQueryableFilterMethod(parameterType);
                        if (reader == null) return null;
                        return () =>
                        {
                            _logger.Trace(() => $"Filtering using queryable Filter(items, {reader.GetParameters()[1].ParameterType})");
                            var query = Reflection.AsQueryable(items);
                            return (IEnumerable<TEntityInterface>)reader.InvokeEx(_repository.Value, query, parameter);
                        };
                    };

                ReadingOptions options;
                if (!preferQuery)
                    options = new ReadingOptions { enumerableFilter, queryableFilter };
                else
                    options = new ReadingOptions { queryableFilter, enumerableFilter };

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
            if (parameterType == typeof(FilterCriteria))
            {
                _logger.Trace(() => "Filtering using generic filter");
                return ExecuteGenericFilter(new[] { (FilterCriteria)parameter }, preferQuery, items);
            }
            if (typeof(IEnumerable<FilterCriteria>).IsAssignableFrom(parameterType))
            {
                _logger.Trace(() => "Filtering using generic filters");
                return ExecuteGenericFilter((IEnumerable<FilterCriteria>)parameter, preferQuery, items);
            }

            // If the parameter is a generic property filter, unless explicitly implemented above, use queryable items.Where(property filter)
            if (typeof(IEnumerable<PropertyFilter>).IsAssignableFrom(parameterType))
            {
                _logger.Trace(() => "Reading using items.AsQueryable().Where(property filter");

                // The filterExpression must use EntityType or EntityNavigationType, depending on the provided query.
                var itemType = items.GetType().GetInterface("IEnumerable`1").GetGenericArguments()[0];

                var filterExpression = _genericFilterHelper.ToExpression((IEnumerable<PropertyFilter>)parameter, itemType);
                if (Reflection.IsQueryable(items))
                {
                    var query = Reflection.AsQueryable(items);
                    return Reflection.Where(query, filterExpression);
                }
                else
                {
                    return Reflection.Where(items, filterExpression.Compile());
                }
            }

            // If the parameter is a filter expression, unless explicitly implemented above, use queryable items.Where(parameter)
            if (Reflection.IsPredicateExpression(parameterType))
            {
                _logger.Trace(() => "Filtering using items.AsQueryable().Where(" + parameterType.Name + ")");
                var query = Reflection.AsQueryable(items);
                return Reflection.Where(query, (Expression)parameter);
            }

            // If the parameter is a filter function, unless explicitly implemented above, use materialized items.Where(parameter)
            if (typeof(Func<TEntityInterface, bool>).IsAssignableFrom(parameterType))
            {
                _logger.Trace(() => "Filtering using items.Where(" + parameterType.Name + ")");
                var filterFunction = parameter as Func<TEntityInterface, bool>;
                Reflection.MaterializeEntityList(ref items);
                if (filterFunction != null)
                    return items.Where(filterFunction);
            }

            // If the parameter is a IEnumarable<Guid>, it will be interpreted as filter by IDs.
            if (typeof(IEnumerable<Guid>).IsAssignableFrom(parameterType))
            {
                _logger.Trace(() => "Filtering using items.Where(item => guids.Contains(item.ID))");
                var parameterList = parameter is List<Guid> list ? list : ((IEnumerable<Guid>)parameter).ToList();

                if (items is IQueryable<TEntityInterface>) // Use queryable Where function with bool expression instead of bool function.
                {
                    // The query is built by reflection to avoid an obscure problem with complex query in NHibernate:
                    // using generic parameter TEntityInterface or IEntity for a query parameter fails with exception on some complex scenarios.
                    var filterPredicateParameter = Expression.Parameter(Reflection.EntityType, "item");
                    var filterPredicate = Expression.Lambda(
                        Expression.Call(
                            _ormUtility.CreateContainsItemsExpression(parameterList),
                            typeof(List<Guid>).GetMethod("Contains"),
                            new[] { Expression.Property(filterPredicateParameter, "ID") }),
                        filterPredicateParameter);

                    return Reflection.Where((IQueryable<TEntityInterface>)items, _ormUtility.OptimizeContains(filterPredicate));
                }

                return items.Where(item => parameterList.Contains(item.ID));
            }

            string errorMessage = $"{EntityName} does not implement a filter method with parameter {parameterType}.";

            if (Reflection.RepositoryLoadWithParameterMethod(parameterType) != null)
            {
                errorMessage += $" There is a loader method with this parameter implemented: Try reordering filters to use the {parameterType.Name} first.";
                throw new ClientException(errorMessage);
            }
            else
                throw new FrameworkException(errorMessage);
        }

        #endregion
        // ================================================================================
        #region Writing

        /// <summary>
        /// Type casting helper. The type casting of performance-efficient; it will not generate a new list or array or instance.
        /// </summary>
        public void Save(IEnumerable<TEntityInterface> insertedNew, IEnumerable<TEntityInterface> updatedNew, IEnumerable<TEntityInterface> deletedIds, bool checkUserPermissions = false)
        {
            Reflection.MaterializeEntityList(ref insertedNew);
            Reflection.MaterializeEntityList(ref updatedNew);
            Reflection.MaterializeEntityList(ref deletedIds);

            if (Reflection.RepositorySaveMethod == null)
                throw new FrameworkException(EntityName + "'s repository does not implement the Save(IEnumerable<Entity>, ...) method.");

            using (_delayedLogger.PerformanceWarning(() => $"Saving: {insertedNew.Count()} to insert, {updatedNew.Count()} to update, {deletedIds.Count()} to delete."))
                Reflection.RepositorySaveMethod.InvokeEx(_repository.Value,
                    insertedNew != null ? Reflection.CastAsEntity(insertedNew) : null,
                    updatedNew != null ? Reflection.CastAsEntity(updatedNew) : null,
                    deletedIds != null ? Reflection.CastAsEntity(deletedIds) : null,
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
                Guid id = Reflection.Where(this.Query(), filterLoad).Select(e => e.ID).FirstOrDefault();

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
                TEntityInterface oldItem = Reflection.Where(this.Query(), filterOld).ToList().SingleOrDefault();

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
        /// <param name="assign">Typical implementation:
        /// <code>(destination, source) =&gt; {
        ///     destination.Property1 = source.Property1;
        ///     destination.Property2 = source.Property2; }</code></param>
        /// <remarks>
        /// This method updates the <paramref name="oldItems"/> instances that are returned in the <paramref name="toUpdate"/> list.
        /// To avoid this behavior, use one of the <see cref="Diff"/> methods from <see cref="ComputedFromHelper"/>, but note that it requires
        /// processing the result before saving it to the database by calling <see cref="DiffResult{T}.PrepareForSaving()"/> or <see cref="DiffResult{T}.PrepareForSaving(Action{T, T})"/>.
        /// </remarks>
        public void Diff(
            IEnumerable<TEntityInterface> oldItems, IEnumerable<TEntityInterface> newItems,
            IComparer<TEntityInterface> sameRecord, Func<TEntityInterface, TEntityInterface, bool> sameValue,
            Action<TEntityInterface, TEntityInterface> assign,
            out IReadOnlyCollection<TEntityInterface> toInsert, out IReadOnlyCollection<TEntityInterface> toUpdate, out IReadOnlyCollection<TEntityInterface> toDelete)
        {
            // TODO: Remove this method and use ComputedFromHelper.Diff instead, after refactoring GenericRepository class into a set of base repository class helpers. This legacy method is currently still needed because of usage of reflection to manage entity instances in GenericRepository methods.

            if (sameRecord == null)
                sameRecord = new EntityIdComparer();

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
                            assign(oldEnum.Current, newEnum.Current);
                            toUpdateList.Add(oldEnum.Current);
                        }

                        newExists = newEnum.MoveNext();
                        oldExists = oldEnum.MoveNext();
                    }
                    else if (keyDiff < 0)
                    {
                        toInsertList.Add(newEnum.Current);
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

            toDelete = (IReadOnlyCollection<TEntityInterface>)toDeleteList;
            toInsert = (IReadOnlyCollection<TEntityInterface>)toInsertList;
            toUpdate = (IReadOnlyCollection<TEntityInterface>)toUpdateList;
        }

        public delegate void BeforeSave(ref IEnumerable<TEntityInterface> toInsert, ref IEnumerable<TEntityInterface> toUpdate, ref IEnumerable<TEntityInterface> toDelete);

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
        /// <param name="filterLoad">For supported filters types see <see cref="Load"/> function.</param>
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

            var newItemsCollection = CsUtility.Materialized(newItems);
            _performanceLogger.Write(stopwatch, () => $"InsertOrUpdateOrDelete: Initialize new items ({newItemsCollection.Count})");

            // Load old items:

            var oldItems = CsUtility.Materialized(this.Load(filterLoad));
            _performanceLogger.Write(stopwatch, () => $"InsertOrUpdateOrDelete: Load old items ({oldItems.Count})");

            // Compare new and old items:

            Diff(oldItems, newItemsCollection, sameRecord, sameValue, assign, out var toInsert, out var toUpdate, out var toDelete);
            _performanceLogger.Write(stopwatch, () => $"InsertOrUpdateOrDelete: Diff ({newItemsCollection.Count} new items, {oldItems.Count} old items, {toInsert.Count} to insert, {toUpdate.Count} to update, {toDelete.Count} to delete)");

            // Save the new items, delete or update old items:

            if (beforeSave != null)
            {
                var toInsertEnum = (IEnumerable<TEntityInterface>)toInsert;
                var toUpdateEnum = (IEnumerable<TEntityInterface>)toUpdate;
                var toDeleteEnum = (IEnumerable<TEntityInterface>)toDelete;
                beforeSave(ref toInsertEnum, ref toUpdateEnum, ref toDeleteEnum);
                toInsert = CsUtility.Materialized(toInsertEnum);
                toUpdate = CsUtility.Materialized(toUpdateEnum);
                toDelete = CsUtility.Materialized(toDeleteEnum);
            }
            Save(toInsert, toUpdate, toDelete);
            _performanceLogger.Write(stopwatch, () => $"InsertOrUpdateOrDelete: Save ({newItemsCollection.Count} new items, {oldItems.Count} old items, {toInsert.Count} to insert, {toUpdate.Count} to update, {toDelete.Count} to delete)");
        }

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
        /// <param name="filterLoad">For supported filters types see <see cref="Load"/> function.</param>
        /// <param name="assign">Typical implementation:
        /// <code>(destination, source) =&gt; {
        ///     destination.Property1 == source.Property1;
        ///     destination.Property2 == source.Property2; }</code></param>
        /// <param name="filterDeactivateDeleted">A filter that selects items that should be deactivated instead of deleted.
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

            var newItemsCollection = CsUtility.Materialized(newItems);
            foreach (var newItem in newItemsCollection.Cast<IDeactivatable>())
                if (newItem.Active == null)
                    newItem.Active = true;
            _performanceLogger.Write(stopwatch, () => $"InsertOrUpdateOrDeleteOrDeactivate: Initialize new items ({newItemsCollection.Count})");

            // Load old items:

            var oldItems = CsUtility.Materialized(this.Load(filterLoad));
            _performanceLogger.Write(stopwatch, () => $"InsertOrUpdateOrDeleteOrDeactivate: Load old items ({oldItems.Count})");

            // Compare new and old items:

            Diff(oldItems, newItemsCollection, sameRecord, sameValue, assign, out var toInsert, out var toUpdate, out var toDelete);
            _performanceLogger.Write(stopwatch, () => $"InsertOrUpdateOrDeleteOrDeactivate: Diff ({newItemsCollection.Count} new items, {oldItems.Count} old items, {toInsert.Count} to insert, {toUpdate.Count} to update, {toDelete.Count} to delete)");

            // Deactivate some items instead of deleting:

            var toDeactivate = CsUtility.Materialized(Filter(toDelete, filterDeactivateDeleted));
            int activeToDeactivateCount = 0;
            if (toDeactivate.Count != 0)
            {
                int oldDeleteCount = toDelete.Count;

                // Don't delete items that should be deactivated:
                if (toDeactivate.Count == oldDeleteCount)
                    toDelete = CsUtility.Materialized(CreateList(0));
                else
                {
                    var toDeactivateIndex = new HashSet<TEntityInterface>(toDeactivate, ReferenceEqualityComparer.Instance);
                    toDelete = Reflection.ToListOfEntity(toDelete.Where(item => !toDeactivateIndex.Contains(item)));
                }
                if (toDelete.Count + toDeactivate.Count != oldDeleteCount)
                    throw new FrameworkException($"Invalid number of items to deactivate for '{_genericRepositoryName}'." +
                        $" Verify if the deactivation filter ({filterDeactivateDeleted.GetType()}) on that data structure returns a valid subset of the given items." +
                        $" {oldDeleteCount} items to remove: {toDeactivate.Count} items to deactivate and {toDelete.Count} items remaining to delete (should be {oldDeleteCount - toDeactivate.Count}).");

                // Update the items to deactivate (unless already deactivated):
                var activeToDeactivate = toDeactivate.Cast<IDeactivatable>().Where(item => item.Active == null || item.Active == true).ToList();
                foreach (var item in activeToDeactivate)
                    item.Active = false;
                Reflection.AddRange(toUpdate, Reflection.CastAsEntity(activeToDeactivate));
                activeToDeactivateCount = activeToDeactivate.Count;
            }
            _performanceLogger.Write(stopwatch, () => $"InsertOrUpdateOrDeleteOrDeactivate: Deactivate ({activeToDeactivateCount} to deactivate, {toDeactivate.Count - activeToDeactivateCount} already deactivated)");

            // Modify old items to match new items:

            if (beforeSave != null)
            {
                var toInsertEnum = (IEnumerable<TEntityInterface>)toInsert;
                var toUpdateEnum = (IEnumerable<TEntityInterface>)toUpdate;
                var toDeleteEnum = (IEnumerable<TEntityInterface>)toDelete;
                beforeSave(ref toInsertEnum, ref toUpdateEnum, ref toDeleteEnum);
                toInsert = CsUtility.Materialized(toInsertEnum);
                toUpdate = CsUtility.Materialized(toUpdateEnum);
                toDelete = CsUtility.Materialized(toDeleteEnum);
            }
            Save(toInsert, toUpdate, toDelete);
            _performanceLogger.Write(stopwatch, () => $"InsertOrUpdateOrDeleteOrDeactivate: Save ({newItemsCollection.Count} new items, {oldItems.Count} old items, {toInsert.Count} to insert, {toUpdate.Count} to update, {toDelete.Count} to delete)");
        }

        public DiffResult<TEntityInterface> DiffFrom(
            string source,
            object filterLoad = null)
        {
            var diffMethod = Reflection.RepositoryDiffFromMethod(source);

            if (diffMethod == null)
                throw new FrameworkException($"{EntityName}'s repository does not implement a ComputedFrom mapping from '{source}' ({Reflection.RepositoryDiffFromMethodName(source)}).");

            object newItems = diffMethod.InvokeEx(_repository.Value, filterLoad, null);
            return (DiffResult<TEntityInterface>)newItems;
        }

        public IEnumerable<TEntityInterface> RecomputeFrom(
            string source,
            object filterLoad = null)
        {
            var recomputeMethod = Reflection.RepositoryRecomputeFromMethod(source);

            if (recomputeMethod == null)
                throw new FrameworkException($"{EntityName}'s repository does not implement a ComputedFrom mapping from '{source}' ({Reflection.RepositoryRecomputeFromMethodName(source)}).");

            object newItems = recomputeMethod.InvokeEx(_repository.Value, filterLoad, null);
            return (IEnumerable<TEntityInterface>)newItems;
        }

        #endregion
    }
}