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
    public class GenericRepositoryParameters
    {
        public IDomainObjectModel DomainObjectModel { get; set; }
        public Lazy<IIndex<string, IRepository>> Repositories { get; set; }
        public ILogProvider LogProvider { get; set; }
        public IPersistenceTransaction PersistenceTransaction { get; set; }
        public GenericFilterHelper GenericFilterHelper { get; set; }
        public Lazy<GenericRepository<ICommonFilterId>> FilterIdRepository { get; set; }
    }

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
        private readonly Lazy<GenericRepository<ICommonFilterId>> _filterIdRepository;

        private readonly string _repositoryName;
        private readonly Lazy<IRepository> _repository;
        private const string UnsupportedLoaderMessage = "{0} does not implement a loader, a query or a filter with parameter {1}.";
        private const string UnsupportedFilterMessage = "{0} does not implement a filter with parameter {1}.";

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
            _repositoryName = "GenericRepository(" + EntityName + ")";

            _logger = parameters.LogProvider.GetLogger(_repositoryName);
            _performanceLogger = parameters.LogProvider.GetLogger("Performance");
            _persistenceTransaction = parameters.PersistenceTransaction;
            _genericFilterHelper = parameters.GenericFilterHelper;
            _filterIdRepository = parameters.FilterIdRepository;

            _repository = new Lazy<IRepository>(() => InitializeRepository(parameters.Repositories));
            Reflection = new ReflectionHelper<TEntityInterface>(EntityName, parameters.DomainObjectModel, _repository);
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
            return (TEntityInterface)Activator.CreateInstance(Reflection.EntityType);
        }

        /// <returns>Result is a List&lt;&gt; of the data structure type.
        /// The list is returened as IEnumerable&lt;&gt; of the interface type,
        /// to allow strongly-typed use of the list through TEntityInterface interface.
        /// Neither List&lt;&gt; or IList&lt;&gt; are covariant, so IEnumerable&lt;&gt; is used.</returns>
        public IEnumerable<TEntityInterface> CreateList(int size)
        {
            IEnumerable<object> instances = Enumerable.Range(1, size).Select(i => Activator.CreateInstance(Reflection.EntityType));
            var castInstances = Reflection.CastAsEntity(instances);

            var list = (IEnumerable<TEntityInterface>)Activator.CreateInstance(Reflection.ListType, new object[] { size });
            Reflection.AddRange(list, castInstances);

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
                items = Reflection.ToListOfEntity(items);
        }

        #endregion
        // ================================================================================
        #region Reading

        public IQueryable<TEntityInterface> Query()
        {
            if (Reflection.RepositoryQueryMethod == null)
                throw new FrameworkException(EntityName + "'s repository does not implement the Query() method.");

            return (IQueryable<TEntityInterface>)Reflection.RepositoryQueryMethod.InvokeEx(_repository.Value);
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
            var items = ReadOrQuery(parameter, parameterType, true);

            var query = items as IQueryable<TEntityInterface>;
            if (query == null && items != null)
                throw new FrameworkException(string.Format(
                    "{0} does not implement a query method or a filter with parameter {1} than returns an IQueryable. There is an IEnumerable loader of filter implemented, so try using the Read function instead of the Query function.",
                    EntityName,
                    parameterType.FullName));

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
            var items = ReadOrQuery(parameter, parameterType, false);
            MaterializeEntityList(ref items);
            return items;
        }

        /// <summary>
        /// A more flexible reading method than Read() or Query(). Read() will always return materialized items, Query() will always return IQueryable.
        /// </summary>
        public IEnumerable<TEntityInterface> ReadOrQuery<TParameter>(TParameter parameter, bool preferQuery)
        {
            Type filterType = parameter != null ? parameter.GetType() : typeof(TParameter);
            return ReadOrQuery(parameter, filterType, preferQuery);
        }

        private delegate Func<IEnumerable<TEntityInterface>> ReadingOption();

        private class ReadingOptions : List<ReadingOption>
        {
            public Func<IEnumerable<TEntityInterface>> FirstOptionOrNull()
            {
                return this.Select(option => option()).FirstOrDefault(method => method != null);
            }
        }

        /// <summary>
        /// A more flexible reading method than Read() or Query(). Read() will always return materialized items, Query() will always return IQueryable.
        /// </summary>
        /// <param name="parameterType">
        /// It is usually <code>parameter.GetType()</code>, but be careful how to specify the filter if the parameter may be null.
        /// </param>
        public IEnumerable<TEntityInterface> ReadOrQuery(object parameter, Type parameterType, bool preferQuery)
        {
            // Use Filter(parameter), Query(parameter) or Filter(Query(), parameter), if any option exists
            ReadingOption loaderWithParameter = () =>
                {
                    var reader = Reflection.RepositoryLoadWithParameterMethod(parameterType);
                    if (reader == null) return null;
                    return () =>
                    {
                        _logger.Trace(() => "Reading using Filter(" + reader.GetParameters()[0].ParameterType.FullName + ")");
                        return (IEnumerable<TEntityInterface>)reader.InvokeEx(_repository.Value, parameter);
                    };
                };

            ReadingOption queryWithParameter = () =>
                {
                    var reader = Reflection.RepositoryQueryWithParameterMethod(parameterType);
                    if (reader == null) return null;
                    return () =>
                    {
                        _logger.Trace(() => "Reading using Query(" + reader.GetParameters()[0].ParameterType.FullName + ")");
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
                        _logger.Trace(() => "Reading using queryable Filter(Query(), " + reader.GetParameters()[1].ParameterType.FullName + ")");
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

            // If the parameter is FilterAll, unless explicitly implemented above, use All() or Query() if any option exists
            if (typeof(FilterAll).IsAssignableFrom(parameterType))
            {
                var options = new ReadingOptions {
                    () => {
                        var reader = Reflection.RepositoryLoadMethod;
                        if (reader == null) return null;
                        return () => {
                            _logger.Trace(() => "Reading using All()");
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
            if (typeof(IEnumerable<FilterCriteria>).IsAssignableFrom(parameterType))
            {
                _logger.Trace(() => "Reading using generic filter");
                return ExecuteGenericFilter((IEnumerable<FilterCriteria>)parameter, preferQuery);
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
                if (!(parameter is List<Guid>))
                    parameter = ((IEnumerable<Guid>)parameter).ToList();
                var query = (IQueryable<TEntityInterface>)Reflection.RepositoryQueryMethod.InvokeEx(_repository.Value);

                // The query is built by reflection to avoid an obscure problem with complex query in NHibernate:
                // using generic parameter TEntityInterface or IEntity for a query parameter fails with exception on some complex scenarios.
                var filterPredicateParameter = Expression.Parameter(Reflection.EntityType, "item");
                var filterPredicate = Expression.Lambda(
                    Expression.Call(
                        Expression.Constant(parameter),
                        typeof(List<Guid>).GetMethod("Contains"),
                        new[] { Expression.Property(filterPredicateParameter, "ID") }),
                    filterPredicateParameter);

                return Reflection.Where(query, filterPredicate);
            }

            // It there is only enumerable filter available, use inefficient loader with in-memory filtering: Filter(All(), parameter)
            {
                var reader = Reflection.RepositoryEnumerableFilterMethod(parameterType);
                if (reader != null)
                {
                    IEnumerable<TEntityInterface> items;
                    try
                    {
                        items = ReadOrQuery(new FilterAll(), false);
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
            var filterObjects = _genericFilterHelper.ToFilterObjects(genericFilter, Reflection.EntityType);

            foreach (var filter in filterObjects)
            {
                // When reading data, use 'Read' function on first filter parameter, and 'Filter' function on other filter parameters:

                if (items == null)
                    items = ReadOrQuery(filter.Parameter, filter.FilterType, preferQuery: filterObjects.Count > 1 || preferQuery);
                else
                    items = FilterOrQuery(items, filter.Parameter, filter.FilterType);

                if (items == null)
                    throw new FrameworkException(string.Format("{0}'s loader or filter result is null. ParameterType = '{1}', Parameter.ToString = '{2}'.",
                        EntityName, filter.FilterType.FullName, filter.Parameter.ToString()));
            }

            return items ?? ReadOrQuery(new FilterAll(), preferQuery: preferQuery);
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
            MaterializeEntityList(ref filteredItems);
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
                            _logger.Trace(() => "Filtering using enumerable Filter(items, " + reader.GetParameters()[1].ParameterType.FullName + ")");
                            MaterializeEntityList(ref items);
                            return (IEnumerable<TEntityInterface>)reader.InvokeEx(_repository.Value, items, parameter);
                        };
                    };

                ReadingOption queryableFilter = () =>
                    {
                        var reader = Reflection.RepositoryQueryableFilterMethod(parameterType);
                        if (reader == null) return null;
                        return () =>
                        {
                            _logger.Trace(() => "Filtering using queryable Filter(items, " + reader.GetParameters()[1].ParameterType.FullName + ")");
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
            if (typeof(IEnumerable<FilterCriteria>).IsAssignableFrom(parameterType))
            {
                _logger.Trace(() => "Filtering using generic filter");
                return ExecuteGenericFilter((IEnumerable<FilterCriteria>)parameter, preferQuery, items);
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
                MaterializeEntityList(ref items);
                if (filterFunction != null)
                    return items.Where(filterFunction);
            }

            // If the parameter is a IEnumarable<Guid>, it will be interpreted as filter by IDs.
            if (typeof(IEnumerable<Guid>).IsAssignableFrom(parameterType))
            {
                _logger.Trace(() => "Filtering using items.Where(item => guids.Contains(item.ID))");
                if (!(parameter is List<Guid>))
                    parameter = ((IEnumerable<Guid>)parameter).ToList();

                if (items is IQueryable<TEntityInterface>) // Use queryable Where function with bool expression instead of bool function.
                {
                    // The query is built by reflection to avoid an obscure problem with complex query in NHibernate:
                    // using generic parameter TEntityInterface or IEntity for a query parameter fails with exception on some complex scenarios.
                    var filterPredicateParameter = Expression.Parameter(Reflection.EntityType, "item");
                    var filterPredicate = Expression.Lambda(
                        Expression.Call(
                            Expression.Constant(parameter),
                            typeof(List<Guid>).GetMethod("Contains"),
                            new[] { Expression.Property(filterPredicateParameter, "ID") }),
                        filterPredicateParameter);

                    return Reflection.Where((IQueryable<TEntityInterface>)items, filterPredicate);
                }

                return items.Where(item => ((List<Guid>)parameter).Contains(item.ID));
            }

            throw new FrameworkException(string.Format(UnsupportedFilterMessage, EntityName, parameterType.FullName));
        }

        #endregion
        // ================================================================================
        #region Writing

        /// <summary>
        /// Type casting helper. The type casting of performance-efficient; it will not generate a new list or array or instance.
        /// </summary>
        public void Save(IEnumerable<TEntityInterface> insertNew, IEnumerable<TEntityInterface> updateNew, IEnumerable<TEntityInterface> deleteIds, bool checkUserPermissions = false)
        {
            MaterializeEntityList(ref insertNew);
            MaterializeEntityList(ref updateNew);
            MaterializeEntityList(ref deleteIds);

            if (Reflection.RepositorySaveMethod == null)
                throw new FrameworkException(EntityName + "'s repository does not implement the Save(IEnumerable<Entity>, ...) method.");

            Reflection.RepositorySaveMethod.InvokeEx(_repository.Value,
                insertNew != null ? Reflection.CastAsEntity(insertNew) : null,
                updateNew != null ? Reflection.CastAsEntity(updateNew) : null,
                deleteIds != null ? Reflection.CastAsEntity(deleteIds) : null,
                checkUserPermissions);
        }

        public void Insert(IEnumerable<TEntityInterface> insertNew, bool checkUserPermissions = false)
        {
            Save(insertNew, null, null, checkUserPermissions);
        }

        public void Update(IEnumerable<TEntityInterface> updateNew, bool checkUserPermissions = false)
        {
            Save(null, updateNew, null, checkUserPermissions);
        }

        public void Delete(IEnumerable<TEntityInterface> deleteIds, bool checkUserPermissions = false)
        {
            Save(null, null, deleteIds, checkUserPermissions);
        }

        /// <summary>checkUserPermissions is set to false.</summary>
        public void Insert(params TEntityInterface[] insertNew)
        {
            Save(insertNew, null, null, false);
        }

        /// <summary>checkUserPermissions is set to false.</summary>
        public void Update(params TEntityInterface[] updateNew)
        {
            Save(null, updateNew, null, false);
        }

        /// <summary>checkUserPermissions is set to false.</summary>
        public void Delete(params TEntityInterface[] deleteIds)
        {
            Save(null, null, deleteIds, false);
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
                Guid id = Reflection.Where(Query(), filterLoad).Select(e => e.ID).FirstOrDefault();

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
                TEntityInterface oldItem = Reflection.Where(Query(), filterOld).ToList().SingleOrDefault();

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
                    toDelete = Reflection.ToListOfEntity(toDelete.Where(item => !toDeactivateIndex.Contains(item)));
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
                Reflection.AddRange(toUpdate, Reflection.CastAsEntity(activeToDeactivate));
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
            var recomputeMethod = Reflection.RepositoryRecomputeFromMethod(source);

            if (recomputeMethod == null)
                throw new FrameworkException(string.Format(
                    "{0}'s repository does not implement the method to recompute from {1} ({2}).",
                    EntityName, source, Reflection.RepositoryRecomputeFromMethodName(source)));

            recomputeMethod.InvokeEx(_repository.Value, filterLoad, null);
        }

        #endregion
    }
}