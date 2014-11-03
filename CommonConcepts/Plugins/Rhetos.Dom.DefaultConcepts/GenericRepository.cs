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

        private readonly string _repositoryName;
        private readonly Lazy<IRepository> _repository;
        private readonly ReflectionHelper<TEntityInterface> _reflection;
        private const string UnsupportedLoaderMessage = "{0} does not implement a loader or a filter with parameter {1}.";
        private const string UnsupportedFilterMessage = "{0} does not implement a filter with parameter {1}.";

        public string EntityName { get; private set; }
        public IRepository EntityRepository { get { return _repository.Value; } }

        public GenericRepository(
            IDomainObjectModel domainObjectModel,
            Lazy<IIndex<string, IRepository>> repositories,
            IRegisteredInterfaceImplementations registeredInterfaceImplementations,
            ILogProvider logProvider,
            IPersistenceTransaction persistenceTransaction,
            GenericFilterHelper genericFilterHelper)
            : this(domainObjectModel, repositories, InitializeEntityName(registeredInterfaceImplementations), logProvider, persistenceTransaction, genericFilterHelper)
        {
        }

        public GenericRepository(
            IDomainObjectModel domainObjectModel,
            Lazy<IIndex<string, IRepository>> repositories,
            string entityName,
            ILogProvider logProvider,
            IPersistenceTransaction persistenceTransaction,
            GenericFilterHelper genericFilterHelper)
        {
            EntityName = entityName;
            _repositoryName = "GenericRepository(" + EntityName + ")";

            _logger = logProvider.GetLogger(_repositoryName);
            _performanceLogger = logProvider.GetLogger("Performance");
            _persistenceTransaction = persistenceTransaction;
            _genericFilterHelper = genericFilterHelper;

            _repository = new Lazy<IRepository>(() => InitializeRepository(repositories));
            _reflection = new ReflectionHelper<TEntityInterface>(EntityName, domainObjectModel, _repository);
        }

        private static string InitializeEntityName(IRegisteredInterfaceImplementations registeredInterfaceImplementations)
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

        /// <returns>Result is a List<> of the data structure type.
        /// The list is returened as IEnumerable<> of the interface type,
        /// to allow strongly-typed use of the list through TEntityInterface interface.
        /// Neither List or IList are covariant, so IEnumerable is used.</returns>
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
                throw new FrameworkException(EntityName + "'s repostory does not implement the Query() method.");

            return (IQueryable<TEntityInterface>)_reflection.RepositoryQueryMethod.InvokeEx(_repository.Value);
        }

        public IEnumerable<TEntityInterface> Read()
        {
            return Read(new FilterAll());
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
            return Read(parameter, typeof(TParameter));
        }

        public IEnumerable<TEntityInterface> Read(object parameter, Type parameterType)
        {
            var items = ReadNonMaterialized(parameter, parameterType, false);
            MaterializeEntityList(ref items);
            return items;
        }

        public IEnumerable<TEntityInterface> ReadNonMaterialized<TParameter>(TParameter parameter, bool preferQuery)
        {
            return ReadNonMaterialized(parameter, typeof(TParameter), preferQuery);
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
                        items = ReadNonMaterialized(new FilterAll(), typeof(FilterAll), false);
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

        public void ValidateRowPermissions(ReadCommandInfo commandInfo, IEnumerable<TEntityInterface> rows)
        {
            string module = _reflection.EntityType.Namespace;
            string filterName = module + "." + RowPermissionsInfo.filterName;
            var RPType = _reflection.EntityType.Assembly.GetType(filterName);

            if (RPType != null)
            { 
                _logger.Trace(() => "Found row permissions filter, checking if all items are allowed.");
                var allowedItems = ReadNonMaterialized(null, RPType, true);
                bool allowed = rows.All(a => allowedItems.Contains(a));
                _logger.Trace(() => "Row permissions check result: " + allowed);
                if (!allowed)
                    throw new UserException("Insufficient permissions to access some or all of the data requested", "Row permission check failed on " + RPType.ToString());
            }
        }

        public ReadCommandResult ExecuteReadCommand(ReadCommandInfo commandInfo)
        {
            if (!commandInfo.ReadRecords && !commandInfo.ReadTotalCount)
                throw new ArgumentException("Invalid ReadCommand argument: At least one of the properties ReadRecords or ReadTotalCount should be set to true.");

            if (commandInfo.Top  < 0)
                throw new ArgumentException("Invalid ReadCommand argument: Top parameter must not be negative.");

            if (commandInfo.Skip < 0)
                throw new ArgumentException("Invalid ReadCommand argument: Skip parameter must not be negative.");

            var specificMethod = _reflection.RepositoryReadCommandMethod;
            if (specificMethod != null)
                return (ReadCommandResult)specificMethod.InvokeEx(_repository.Value, commandInfo);

            bool pagingIsUsed = commandInfo.Top > 0 || commandInfo.Skip > 0;

            IEnumerable<TEntityInterface> filtered = ReadNonMaterialized(commandInfo.Filters ?? new FilterCriteria[] { }, preferQuery: pagingIsUsed || !commandInfo.ReadRecords);
            ValidateRowPermissions(commandInfo, filtered);

            object[] resultRecords = null;
            int? totalCount = null;

            if (commandInfo.ReadRecords)
                resultRecords = (object[])_reflection.ToArrayOfEntity(_genericFilterHelper.SortAndPaginate(_reflection.AsQueryable(filtered), commandInfo));

            if (commandInfo.ReadTotalCount)
                if (pagingIsUsed)
                    totalCount = SmartCount(filtered);
                else
                    totalCount = resultRecords != null ? resultRecords.Length : SmartCount(filtered);

            return new ReadCommandResult
            {
                Records = resultRecords,
                TotalCount = totalCount
            };
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
            return Filter(items, parameter, typeof(TParameter));
        }

        public IEnumerable<TEntityInterface> Filter(IEnumerable<TEntityInterface> items, object parameter, Type parameterType)
        {
            var filteredItems = FilterNonMaterialized(items, parameter, parameterType);
            MaterializeEntityList(ref filteredItems);
            return filteredItems;
        }

        public IEnumerable<TEntityInterface> FilterNonMaterialized(IEnumerable<TEntityInterface> items, object parameter)
        {
            return FilterNonMaterialized(items, parameter, parameter.GetType());
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
                throw new FrameworkException(EntityName + "'s repostory does not implement the Save(IEnumerable<Entity>, ...) method.");

            _reflection.RepositorySaveMethod.InvokeEx(_repository.Value,
                insertedNew != null ? _reflection.CastAsEntity(insertedNew) : null,
                updatedNew != null ? _reflection.CastAsEntity(updatedNew) : null,
                deletedIds != null ? _reflection.CastAsEntity(deletedIds) : null,
                checkUserPermissions);
        }

        public void InsertOrReadId<TProperties>(
            TEntityInterface newItem,
            Expression<Func<TEntityInterface, TProperties>> propertiesSelector)
        {
            InsertOrReadId(new[] { newItem }, propertiesSelector);
        }

        public void InsertOrReadId<TProperties>(
            IEnumerable<TEntityInterface> newItems,
            Expression<Func<TEntityInterface, TProperties>> propertiesSelector)
        {
            var propertiesSelectorHandler = new ExpressionHelper<TEntityInterface, TProperties>(propertiesSelector);

            foreach (var newItem in newItems)
            {
                var filterLoad = propertiesSelectorHandler.BuildComparisonPredicate(newItem);
                Guid id = Query().Where(filterLoad).Select(e => e.ID).FirstOrDefault();

                if (id == default(Guid))
                {
                    _logger.Trace(() => "Creating " + EntityName + " " + propertiesSelectorHandler.ToString(newItem) + ".");
                    Save(new[] { newItem }, null, null);
                }
                else
                {
                    _logger.Trace(() => "Already exists " + EntityName + " " + propertiesSelectorHandler.ToString(newItem) + ".");
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
            var keyPropertiesHandler = new ExpressionHelper<TEntityInterface, TKeyProperties>(keySelector);
            var valuePropertiesHandler = new ExpressionHelper<TEntityInterface, TValueProperties>(valueSelector);

            foreach (var newItem in newItems)
            {
                var filterOld = keyPropertiesHandler.BuildComparisonPredicate(newItem);
                TEntityInterface oldItem = Query().Where(filterOld).ToList().SingleOrDefault();

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

        public void Diff(
            IEnumerable<TEntityInterface> oldItems, IEnumerable<TEntityInterface> newItems,
            IComparer<TEntityInterface> sameRecord, Func<TEntityInterface, TEntityInterface, bool> sameValue,
            Action<TEntityInterface, TEntityInterface> assign,
            out IEnumerable<TEntityInterface> toInsert, out IEnumerable<TEntityInterface> toUpdate, out IEnumerable<TEntityInterface> toDelete)
        {
            var toDeleteList = (IList)CreateList(0);
            var toInsertList = (IList)CreateList(0);
            var toUpdateList = (IList)CreateList(0);

            newItems = newItems.OrderBy(item => item, sameRecord).ToList();
            oldItems = oldItems.OrderBy(item => item, sameRecord).ToList();

            IEnumerator<TEntityInterface> newEnum = newItems.AsEnumerable().GetEnumerator();
            IEnumerator<TEntityInterface> oldEnum = oldItems.AsEnumerable().GetEnumerator();

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
        public void InsertOrUpdateOrDelete<TFilterLoad>(
            IEnumerable<TEntityInterface> newItems,
            IComparer<TEntityInterface> sameRecord,
            Func<TEntityInterface, TEntityInterface, bool> sameValue,
            TFilterLoad filterLoad,
            Action<TEntityInterface, TEntityInterface> assign,
            BeforeSave beforeSave = null)
        {
            var stopwatch = Stopwatch.StartNew();

            IEnumerable<TEntityInterface> oldItems = Read(filterLoad);

            _performanceLogger.Write(stopwatch, () => _repositoryName + ".InsertOrUpdateOrDeleteOrDeactivate: Read old items");

            IEnumerable<TEntityInterface> toInsert, toUpdate, toDelete;
            Diff(oldItems, newItems, sameRecord, sameValue, assign, out toInsert, out toUpdate, out toDelete);

            _performanceLogger.Write(stopwatch, () => _repositoryName + ".InsertOrUpdateOrDeleteOrDeactivate: Diff");

            if (beforeSave != null)
                beforeSave(ref toInsert, ref toUpdate, ref toDelete);
            Save(toInsert, toUpdate, toDelete);

            _performanceLogger.Write(stopwatch, () => _repositoryName + ".InsertOrUpdateOrDeleteOrDeactivate: Save");
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
        public void InsertOrUpdateOrDeleteOrDeactivate<TFilterLoad, TFilterDeactivateDeleted>(
            IEnumerable<TEntityInterface> newItems,
            IComparer<TEntityInterface> sameRecord,
            Func<TEntityInterface, TEntityInterface, bool> sameValue,
            TFilterLoad filterLoad,
            Action<TEntityInterface, TEntityInterface> assign,
            TFilterDeactivateDeleted deactivateDeleted,
            BeforeSave beforeSave = null)
        {
            var stopwatch = Stopwatch.StartNew();

            MaterializeQuick(ref newItems);
            foreach (var newItem in newItems.Cast<IDeactivatable>())
                if (newItem.Active == null)
                    newItem.Active = true;

            _performanceLogger.Write(stopwatch, () => _repositoryName + ".InsertOrUpdateOrDeleteOrDeactivate: Initialize new items");

            IEnumerable<TEntityInterface> oldItems = Read(filterLoad);

            _performanceLogger.Write(stopwatch, () => _repositoryName + ".InsertOrUpdateOrDeleteOrDeactivate: Read old items");

            IEnumerable<TEntityInterface> toInsert, toUpdate, toDelete;
            Diff(oldItems, newItems, sameRecord, sameValue, assign, out toInsert, out toUpdate, out toDelete);

            _performanceLogger.Write(stopwatch, () => _repositoryName + ".InsertOrUpdateOrDeleteOrDeactivate: Diff");

            IEnumerable<TEntityInterface> toDeactivate = Filter(toDelete, deactivateDeleted);
            if (toDeactivate.Count() > 0)
            {
                // Don't delete items that should be deactivated:
                if (toDeactivate.Count() == toDelete.Count())
                    toDelete = CreateList(0);
                else
                {
                    var toDeactivateIndex = new HashSet<TEntityInterface>(toDeactivate, new InstanceComparer());
                    toDelete = _reflection.ToListOfEntity(toDelete.Where(item => !toDeactivateIndex.Contains(item)));
                }

                // Update the items to deactivate (unless already deactivated):
                var activeToDeactivate = toDeactivate.Cast<IDeactivatable>().Where(item => item.Active == null || item.Active == true).ToList();
                foreach (var item in activeToDeactivate)
                    item.Active = false;
                _reflection.AddRange(toUpdate, _reflection.CastAsEntity(activeToDeactivate));
            }

            _performanceLogger.Write(stopwatch, () => _repositoryName + ".InsertOrUpdateOrDeleteOrDeactivate: Deactivate");

            if (beforeSave != null)
                beforeSave(ref toInsert, ref toUpdate, ref toDelete);
            Save(toInsert, toUpdate, toDelete);

            _performanceLogger.Write(stopwatch, () => _repositoryName + ".InsertOrUpdateOrDeleteOrDeactivate: Save");
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

        #endregion
    }
}