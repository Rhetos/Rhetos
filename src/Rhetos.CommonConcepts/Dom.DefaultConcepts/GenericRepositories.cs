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
using Rhetos.Dom.DefaultConcepts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Rhetos.Dom.DefaultConcepts
{
    /// <remarks>
    /// The term "entity" in the contest of this class represents any identifiable
    /// data structure (implementation of IEntity). Not to be confused with Entity
    /// DSL concept, which generates only one kind of IEntity (common entity).
    /// Other IEntity implementations can also be handled by this class.
    /// </remarks>
    public class GenericRepositories
    {
        // ================================================================================
        #region Initialization

        private readonly GenericRepositoryParameters _parameters;
        private readonly RegisteredInterfaceImplementations _registeredInterfaceImplementations;

        public GenericRepositories(GenericRepositoryParameters parameters, RegisteredInterfaceImplementations registeredInterfaceImplementations)
        {
            _parameters = parameters;
            _registeredInterfaceImplementations = registeredInterfaceImplementations;
        }

        public GenericRepository<TEntityInterface> GetGenericRepository<TEntityInterface>()
            where TEntityInterface : class, IEntity
        {
            return new GenericRepository<TEntityInterface>(_parameters, _registeredInterfaceImplementations);
        }

        public GenericRepository<IEntity> GetGenericRepository(string entityName)
        {
            return new GenericRepository<IEntity>(_parameters, entityName);
        }

        public GenericRepository<TEntityInterface> GetGenericRepository<TEntityInterface>(string entityName)
            where TEntityInterface : class, IEntity
        {
            return new GenericRepository<TEntityInterface>(_parameters, entityName);
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

        public IEnumerable<TEntityInterface> Load<TEntityInterface>()
            where TEntityInterface : class, IEntity
        {
            return GetGenericRepository<TEntityInterface>().Load();
        }

        public IEnumerable<TEntityInterface> Load<TEntityInterface>(Expression<Func<TEntityInterface, bool>> filter)
            where TEntityInterface : class, IEntity
        {
            return GetGenericRepository<TEntityInterface>().Load(filter);
        }

        public IEnumerable<TEntityInterface> Load<TEntityInterface, TParameter>(TParameter parameter)
            where TEntityInterface : class, IEntity
        {
            return GetGenericRepository<TEntityInterface>().Load<TEntityInterface, TParameter>(parameter);
        }

        public IEnumerable<TEntityInterface> Load<TEntityInterface>(object parameter, Type parameterType)
            where TEntityInterface : class, IEntity
        {
            return GetGenericRepository<TEntityInterface>().Load(parameter, parameterType);
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

        public void Insert<TEntityInterface>(IEnumerable<TEntityInterface> insertNew, bool checkUserPermissions = false)
            where TEntityInterface : class, IEntity
        {
            GetGenericRepository<TEntityInterface>().Save(insertNew, null, null, checkUserPermissions);
        }

        public void Update<TEntityInterface>(IEnumerable<TEntityInterface> updateNew, bool checkUserPermissions = false)
            where TEntityInterface : class, IEntity
        {
            GetGenericRepository<TEntityInterface>().Save(null, updateNew, null, checkUserPermissions);
        }

        public void Delete<TEntityInterface>(IEnumerable<TEntityInterface> deleteIds, bool checkUserPermissions = false)
            where TEntityInterface : class, IEntity
        {
            GetGenericRepository<TEntityInterface>().Save(null, null, deleteIds, checkUserPermissions);
        }

        /// <summary>checkUserPermissions is set to false.</summary>
        public void Insert<TEntityInterface>(params TEntityInterface[] insertNew)
            where TEntityInterface : class, IEntity
        {
            GetGenericRepository<TEntityInterface>().Save(insertNew, null, null, false);
        }

        /// <summary>checkUserPermissions is set to false.</summary>
        public void Update<TEntityInterface>(params TEntityInterface[] updateNew)
            where TEntityInterface : class, IEntity
        {
            GetGenericRepository<TEntityInterface>().Save(null, updateNew, null, false);
        }

        /// <summary>checkUserPermissions is set to false.</summary>
        public void Delete<TEntityInterface>(params TEntityInterface[] deleteIds)
            where TEntityInterface : class, IEntity
        {
            GetGenericRepository<TEntityInterface>().Save(null, null, deleteIds, false);
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
            Action<TEntityInterface, TEntityInterface> assign,
            GenericRepository<TEntityInterface>.BeforeSave beforeSave = null)
            where TEntityInterface : class, IEntity
        {
            GetGenericRepository<TEntityInterface>().InsertOrUpdateOrDelete(newItems, sameRecord, sameValue, filterLoad, assign, beforeSave);
        }

        public void InsertOrUpdateOrDeleteOrDeactivate<TEntityInterface, TFilterLoad, TFilterDeactivateDeleted>(
            IEnumerable<TEntityInterface> newItems,
            IComparer<TEntityInterface> sameRecord,
            Func<TEntityInterface, TEntityInterface, bool> sameValue,
            TFilterLoad filterLoad,
            Action<TEntityInterface, TEntityInterface> assign,
            TFilterDeactivateDeleted deactivateDeleted,
            GenericRepository<TEntityInterface>.BeforeSave beforeSave = null)
            where TEntityInterface : class, IEntity
        {
            GetGenericRepository<TEntityInterface>().InsertOrUpdateOrDeleteOrDeactivate(newItems, sameRecord, sameValue, filterLoad, assign, deactivateDeleted, beforeSave);
        }

        #endregion
    }
}
