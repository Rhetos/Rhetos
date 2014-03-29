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
using Rhetos.Logging;
using Rhetos.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Rhetos.Dom.DefaultConcepts
{
    /// <remarks>
    /// The term "entity" in the contest of this class representy any identifiable
    /// data structure (implementation of IEntity). Not to be confused with Entity
    /// DSL concept, which generates only one kind of IEntity (common entity).
    /// Other IEntity implementations can also be handled by this class.
    /// </remarks>
    public class GenericRepositories
    {
        // ================================================================================
        #region Initialization

        private readonly IDomainObjectModel _domainObjectModel;
        private readonly Lazy<IIndex<string, IRepository>> _repositories;
        private readonly IRegisteredInterfaceImplementations _registeredInterfaceImplementations;
        private readonly ILogProvider _logProvider;
        private readonly IPersistenceTransaction _persistenceTransaction;
        private readonly GenericFilterHelper _genericFilterHelper;

        public GenericRepositories(
            IDomainObjectModel domainObjectModel,
            Lazy<IIndex<string, IRepository>> repositories,
            IRegisteredInterfaceImplementations registeredInterfaceImplementations,
            ILogProvider logProvider,
            IPersistenceTransaction persistenceTransaction,
            GenericFilterHelper genericFilterHelper)
        {
            _domainObjectModel = domainObjectModel;
            _repositories = repositories;
            _registeredInterfaceImplementations = registeredInterfaceImplementations;
            _logProvider = logProvider;
            _persistenceTransaction = persistenceTransaction;
            _genericFilterHelper = genericFilterHelper;
        }

        public GenericRepository<TEntityInterface> GetGenericRepository<TEntityInterface>()
            where TEntityInterface : class, IEntity
        {
            return new GenericRepository<TEntityInterface>(
                _domainObjectModel,
                _repositories,
                _registeredInterfaceImplementations,
                _logProvider,
                _persistenceTransaction,
                _genericFilterHelper);
        }

        public GenericRepository<IEntity> GetGenericRepository(string entityName)
        {
            return new GenericRepository<IEntity>(
                _domainObjectModel,
                _repositories,
                entityName,
                _logProvider,
                _persistenceTransaction,
                _genericFilterHelper);
        }

        public string GetEntityName<TEntityInterface>()
            where TEntityInterface : class, IEntity
        {
            return GetGenericRepository<TEntityInterface>().EntityName;
        }

        public IRepository GetEntityRepository<TEntityInterface>()
            where TEntityInterface : class, IEntity
        {
            return GetGenericRepository<TEntityInterface>().EntityRepository;
        }

        public IRepository GetEntityRepository(string entityName)
        {
            return GetGenericRepository(entityName).EntityRepository;
        }

        #endregion
        // ================================================================================
        #region Instantiation

        public TEntityInterface CreateInstance<TEntityInterface>()
            where TEntityInterface : class, IEntity
        {
            return GetGenericRepository<TEntityInterface>().CreateInstance();
        }

        public IEnumerable<TEntityInterface> CreateList<TEntityInterface>(int size)
            where TEntityInterface : class, IEntity
        {
            return GetGenericRepository<TEntityInterface>().CreateList(size);
        }

        public IEnumerable<TEntityInterface> CreateList<TEntityInterface, TSource>(IEnumerable<TSource> source, Action<TSource, TEntityInterface> initializer)
            where TEntityInterface : class, IEntity
        {
            return GetGenericRepository<TEntityInterface>().CreateList(source, initializer);
        }

        #endregion
        // ================================================================================
        #region Reading

        public IQueryable<TEntityInterface> Query<TEntityInterface>()
            where TEntityInterface : class, IEntity
        {
            return GetGenericRepository<TEntityInterface>().Query();
        }

        public IEnumerable<TEntityInterface> Read<TEntityInterface>()
            where TEntityInterface : class, IEntity
        {
            return GetGenericRepository<TEntityInterface>().Read();
        }

        public IEnumerable<TEntityInterface> Read<TEntityInterface>(Expression<Func<TEntityInterface, bool>> filter)
            where TEntityInterface : class, IEntity
        {
            return GetGenericRepository<TEntityInterface>().Read(filter);
        }

        public IEnumerable<TEntityInterface> Read<TEntityInterface, TParameter>(TParameter parameter)
            where TEntityInterface : class, IEntity
        {
            return GetGenericRepository<TEntityInterface>().Read<TParameter>(parameter);
        }

        public IEnumerable<TEntityInterface> Read<TEntityInterface>(object parameter, Type parameterType)
            where TEntityInterface : class, IEntity
        {
            return GetGenericRepository<TEntityInterface>().Read(parameter, parameterType);
        }

        #endregion
        // ================================================================================
        #region Filtering

        public IEnumerable<TEntityInterface> Filter<TEntityInterface, TParameter>(IEnumerable<TEntityInterface> items, TParameter parameter)
            where TEntityInterface : class, IEntity
        {
            return GetGenericRepository<TEntityInterface>().Filter<TParameter>(items, parameter);
        }

        public IEnumerable<TEntityInterface> Filter<TEntityInterface>(IEnumerable<TEntityInterface> items, object parameter, Type parameterType)
            where TEntityInterface : class, IEntity
        {
            return GetGenericRepository<TEntityInterface>().Filter(items, parameter, parameterType);
        }

        #endregion
        // ================================================================================
        #region Writing

        public void Save<TEntityInterface>(IEnumerable<TEntityInterface> insertedNew, IEnumerable<TEntityInterface> updatedNew, IEnumerable<TEntityInterface> deletedIds, bool checkUserPermissions = false)
            where TEntityInterface : class, IEntity
        {
            GetGenericRepository<TEntityInterface>().Save(insertedNew, updatedNew, deletedIds, checkUserPermissions);
        }

        public void InsertOrReadId<TEntityInterface, TProperties>(TEntityInterface newItem, Expression<Func<TEntityInterface, TProperties>> propertiesSelector)
            where TEntityInterface : class, IEntity
        {
            GetGenericRepository<TEntityInterface>().InsertOrReadId(newItem, propertiesSelector);
        }

        public void InsertOrReadId<TEntityInterface, TProperties>(
            IEnumerable<TEntityInterface> newItems,
            Expression<Func<TEntityInterface, TProperties>> propertiesSelector)
            where TEntityInterface : class, IEntity
        {
            GetGenericRepository<TEntityInterface>().InsertOrReadId(newItems, propertiesSelector);
        }

        public void InsertOrUpdateReadId<TEntityInterface, TKeyProperties, TValueProperties>(
            TEntityInterface newItem,
            Expression<Func<TEntityInterface, TKeyProperties>> keySelector,
            Expression<Func<TEntityInterface, TValueProperties>> valueSelector)
            where TEntityInterface : class, IEntity
        {
            GetGenericRepository<TEntityInterface>().InsertOrUpdateReadId(newItem, keySelector, valueSelector);
        }

        public void InsertOrUpdateReadId<TEntityInterface, TKeyProperties, TValueProperties>(
            IEnumerable<TEntityInterface> newItems,
            Expression<Func<TEntityInterface, TKeyProperties>> keySelector,
            Expression<Func<TEntityInterface, TValueProperties>> valueSelector)
            where TEntityInterface : class, IEntity
        {
            GetGenericRepository<TEntityInterface>().InsertOrUpdateReadId(newItems, keySelector, valueSelector);
        }

        public void InsertOrUpdateOrDelete<TEntityInterface, TFilterLoad>(
            IEnumerable<TEntityInterface> newItems,
            IComparer<TEntityInterface> sameRecord,
            Func<TEntityInterface, TEntityInterface, bool> sameValue,
            TFilterLoad filterLoad,
            Action<TEntityInterface, TEntityInterface> assign)
            where TEntityInterface : class, IEntity
        {
            GetGenericRepository<TEntityInterface>().InsertOrUpdateOrDelete(newItems, sameRecord, sameValue, filterLoad, assign);
        }

        public void InsertOrUpdateOrDeleteOrDeactivate<TEntityInterface, TFilterLoad, TFilterDeactivateDeleted>(
            IEnumerable<TEntityInterface> newItems,
            IComparer<TEntityInterface> sameRecord,
            Func<TEntityInterface, TEntityInterface, bool> sameValue,
            TFilterLoad filterLoad,
            Action<TEntityInterface, TEntityInterface> assign,
            TFilterDeactivateDeleted deactivateDeleted)
            where TEntityInterface : class, IEntity
        {
            GetGenericRepository<TEntityInterface>().InsertOrUpdateOrDeleteOrDeactivate(newItems, sameRecord, sameValue, filterLoad, assign, deactivateDeleted);
        }

        #endregion
    }
}
