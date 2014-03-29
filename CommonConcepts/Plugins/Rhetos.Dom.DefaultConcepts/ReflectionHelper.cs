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

using Rhetos.Processing.DefaultCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Rhetos.Dom.DefaultConcepts
{
    internal class ReflectionHelper<TEntityInterface>
        where TEntityInterface : class, IEntity
    {
        private readonly string _entityName;
        private readonly IDomainObjectModel _domainObjectModel;
        private readonly Lazy<IRepository> _repository;

        public ReflectionHelper(string entityName, IDomainObjectModel domainObjectModel, Lazy<IRepository> repository)
        {
            _entityName = entityName;
            _domainObjectModel = domainObjectModel;
            _repository = repository;
        }

        //===================================================
        #region Types

        private Type _entityType = null;
        public Type EntityType
        {
            get
            {
                if (_entityType == null)
                {
                    _entityType = _domainObjectModel.GetType(_entityName);
                    if (!typeof(TEntityInterface).IsAssignableFrom(_entityType))
                        throw new FrameworkException(string.Format(
                            "The given data structure's type {0} does not implement {1} interface.",
                            _entityType.FullName, typeof(TEntityInterface).FullName));
                }
                return _entityType;
            }
        }

        private Type _enumerableType = null;
        public Type EnumerableType
        {
            get
            {
                if (_enumerableType == null)
                    _enumerableType = typeof(IEnumerable<>).MakeGenericType(new[] { EntityType });
                return _enumerableType;
            }
        }

        private Type _listType = null;
        public Type ListType
        {
            get
            {
                if (_listType == null)
                    _listType = typeof(List<>).MakeGenericType(new[] { EntityType });
                return _listType;
            }
        }

        private Type _queryableType = null;
        public Type QueryableType
        {
            get
            {
                if (_queryableType == null)
                    _queryableType = typeof(IQueryable<>).MakeGenericType(new[] { EntityType });
                return _queryableType;
            }
        }

        private Type _repositoryType = null;
        public Type RepositoryType
        {
            get
            {
                if (_repositoryType == null)
                    _repositoryType = _repository.Value.GetType();
                return _repositoryType;
            }
        }

        public bool IsPredicateExpression(Type expressionType)
        {
            return IsPredicateExpression(expressionType, EntityType);
        }

        public static bool IsPredicateExpression(Type expressionType, Type acceptingArgument)
        {
            var parameterType = GetPredicateExpressionParameter(expressionType);
            return parameterType != null && parameterType.IsAssignableFrom(acceptingArgument);
        }

        public static Type GetPredicateExpressionParameter(Type expressionType)
        {
            if (!expressionType.IsGenericType) return null;
            if (expressionType.GetGenericTypeDefinition() != typeof(Expression<>)) return null;

            var funcType = expressionType.GetGenericArguments().First();
            if (!funcType.IsGenericType) return null;
            if (funcType.GetGenericTypeDefinition() != typeof(Func<,>)) return null;

            var funcArgs = funcType.GetGenericArguments();
            if (funcArgs.Length != 2) return null;
            if (funcArgs[1] != typeof(bool)) return null;
            return funcArgs[0];
        }

        #endregion
        //===================================================
        #region Methods

        /// <param name="predicateExpression">
        /// Expression&lt;Func&lt;parameter, result&gt;&gt; does not support contravariant parameter type,
        /// so a specific function type is needed for each parameter type.</param>
        private MethodInfo _queryableWhereMethod = null;
        public MethodInfo QueryableWhereMethod(Type parameterType)
        {
            if (_queryableWhereMethod == null)
                _queryableWhereMethod = typeof(Queryable).GetMethods()
                    .Where(m => m.Name == "Where" && m.GetParameters()[1].ParameterType.GetGenericArguments()[0].GetGenericArguments().Count() == 2)
                    .Single();

            return _queryableWhereMethod.MakeGenericMethod(parameterType);
        }
        /// <param name="predicateExpression">
        /// Expression&lt;Func&lt;parameter, result&gt;&gt; does not support contravariant parameter type,
        /// so the object is used for predicateExpression.</param>
        public IQueryable<TEntityInterface> Where(IQueryable<TEntityInterface> items, object predicateExpression)
        {
            Type predicateParameter = GetPredicateExpressionParameter(predicateExpression.GetType());
            MethodInfo whereMethod = QueryableWhereMethod(predicateParameter);
            object result = whereMethod.Invoke(null, new object[] { items, predicateExpression });
            return (IQueryable<TEntityInterface>)result;
        }

        private MethodInfo _addRangeMethod = null;
        public MethodInfo AddRangeMethod
        {
            get
            {
                if (_addRangeMethod == null)
                    _addRangeMethod = ListType.GetMethod("AddRange");
                return _addRangeMethod;
            }
        }
        public void AddRange(IEnumerable<TEntityInterface> list, IEnumerable<TEntityInterface> items)
        {
            AddRangeMethod.Invoke(list, new object[] { items });
        }

        private MethodInfo _castAsEntityMethod = null;
        public MethodInfo CastAsEntityMethod
        {
            get
            {
                if (_castAsEntityMethod == null)
                    _castAsEntityMethod = typeof(Enumerable).GetMethod("Cast", BindingFlags.Public | BindingFlags.Static)
                        .MakeGenericMethod(new[] { EntityType });
                return _castAsEntityMethod;
            }
        }
        public IEnumerable<TEntityInterface> CastAsEntity(IEnumerable<object> items)
        {
            return (IEnumerable<TEntityInterface>)CastAsEntityMethod.Invoke(null, new object[] { items });
        }

        private MethodInfo _asQueryableMethod = null;
        public MethodInfo AsQueryableMethod
        {
            get
            {
                if (_asQueryableMethod == null)
                    _asQueryableMethod = typeof(Queryable).GetMethod("AsQueryable", new[] { EnumerableType });
                return _asQueryableMethod;
            }
        }
        /// <summary>
        /// Casts items to the entity type before calling AsQueryable.
        /// </summary>
        public IQueryable<TEntityInterface> AsQueryable(IEnumerable<TEntityInterface> items)
        {
            var castItems = CastAsEntity(items);
            return (IQueryable<TEntityInterface>)AsQueryableMethod.Invoke(null, new object[] { castItems });
        }

        private MethodInfo _toListOfEntityMethod = null;
        public MethodInfo ToListOfEntityMethod
        {
            get
            {
                if (_toListOfEntityMethod == null)
                    _toListOfEntityMethod = typeof(Enumerable).GetMethod("ToList", BindingFlags.Public | BindingFlags.Static)
                        .MakeGenericMethod(new[] { EntityType });
                return _toListOfEntityMethod;
            }
        }
        /// <summary>
        /// Casts items to the entity type before calling ToList.
        /// </summary>
        public IEnumerable<TEntityInterface> ToListOfEntity(IEnumerable<TEntityInterface> items)
        {
            var castItems = CastAsEntity(items);
            return (IEnumerable<TEntityInterface>)ToListOfEntityMethod.Invoke(null, new object[] { castItems });
        }

        private MethodInfo _toArrayOfEntityMethod = null;
        public MethodInfo ToArrayOfEntityMethod
        {
            get
            {
                if (_toArrayOfEntityMethod == null)
                    _toArrayOfEntityMethod = typeof(Enumerable).GetMethod("ToArray", BindingFlags.Public | BindingFlags.Static)
                        .MakeGenericMethod(new[] { EntityType });
                return _toArrayOfEntityMethod;
            }
        }
        /// <summary>
        /// Casts items to the entity type before calling ToArray.
        /// </summary>
        public IEnumerable<TEntityInterface> ToArrayOfEntity(IEnumerable<TEntityInterface> items)
        {
            var castItems = CastAsEntity(items);
            return (IEnumerable<TEntityInterface>)ToArrayOfEntityMethod.Invoke(null, new object[] { castItems });
        }

        #endregion
        //===================================================
        #region Repository methods

        private MethodInfo _repositoryLoadMethod = null;
        public MethodInfo RepositoryLoadMethod
        {
            get
            {
                if (_repositoryLoadMethod == null)
                    _repositoryLoadMethod = RepositoryType.GetMethod("All", new Type[] { }); // TODO: Rename All to Load
                return _repositoryLoadMethod;
            }
        }

        private MethodInfo _repositoryQueryMethod = null;
        public MethodInfo RepositoryQueryMethod
        {
            get
            {
                if (_repositoryQueryMethod == null)
                    _repositoryQueryMethod = RepositoryType.GetMethod("Query", new Type[] { });
                return _repositoryQueryMethod;
            }
        }

        private Dictionary<Type, MethodInfo> _repositoryLoadWithParameterMethod = null;
        public MethodInfo RepositoryLoadWithParameterMethod(Type parameterType)
        {
            MethodInfo method = null;
            bool exists = false;

            if (_repositoryLoadWithParameterMethod == null)
                _repositoryLoadWithParameterMethod = new Dictionary<Type, MethodInfo>();
            else
                exists = _repositoryLoadWithParameterMethod.TryGetValue(parameterType, out method);

            if (!exists)
            {
                method = RepositoryType.GetMethod("Filter", new Type[] { parameterType }); // TODO: Rename Filter with a single argument to Load
                _repositoryLoadWithParameterMethod.Add(parameterType, method);
            }

            return method;
        }

        private Dictionary<Type, MethodInfo> _repositoryQueryWithParameterMethod = null;
        public MethodInfo RepositoryQueryWithParameterMethod(Type parameterType)
        {
            MethodInfo method = null;
            bool exists = false;

            if (_repositoryQueryWithParameterMethod == null)
                _repositoryQueryWithParameterMethod = new Dictionary<Type, MethodInfo>();
            else
                exists = _repositoryQueryWithParameterMethod.TryGetValue(parameterType, out method);

            if (!exists)
            {
                method = RepositoryType.GetMethod("Query", new Type[] { parameterType });
                _repositoryQueryWithParameterMethod.Add(parameterType, method);
            }

            return method;
        }

        private Dictionary<Type, MethodInfo> _repositoryEnumerableFilterMethod = null;
        public MethodInfo RepositoryEnumerableFilterMethod(Type parameterType)
        {
            MethodInfo method = null;
            bool exists = false;

            if (_repositoryEnumerableFilterMethod == null)
                _repositoryEnumerableFilterMethod = new Dictionary<Type, MethodInfo>();
            else
                exists = _repositoryEnumerableFilterMethod.TryGetValue(parameterType, out method);

            if (!exists)
            {
                method = RepositoryType.GetMethod("Filter", new Type[] { EnumerableType, parameterType });
                _repositoryEnumerableFilterMethod.Add(parameterType, method);
            }

            return method;
        }

        private Dictionary<Type, MethodInfo> _repositoryQueryableFilterMethod = null;
        public MethodInfo RepositoryQueryableFilterMethod(Type parameterType)
        {
            MethodInfo method = null;
            bool exists = false;

            if (_repositoryQueryableFilterMethod == null)
                _repositoryQueryableFilterMethod = new Dictionary<Type, MethodInfo>();
            else
                exists = _repositoryQueryableFilterMethod.TryGetValue(parameterType, out method);

            if (!exists)
            {
                method = RepositoryType.GetMethod("Filter", new Type[] { QueryableType, parameterType });
                if (method != null && method.GetParameters()[0].ParameterType.GetGenericTypeDefinition() != typeof(IQueryable<>))
                    method = null;
                _repositoryQueryableFilterMethod.Add(parameterType, method);
            }

            return method;
        }

        private MethodInfo _repositorySaveMethod = null;
        public MethodInfo RepositorySaveMethod
        {
            get
            {
                if (_repositorySaveMethod == null)
                    _repositorySaveMethod = RepositoryType.GetMethod("Save", new Type[] { EnumerableType, EnumerableType, EnumerableType, typeof(bool) });
                return _repositorySaveMethod;
            }
        }

        private MethodInfo _repositoryQueryDataSourceCommandMethod = null;
        public MethodInfo RepositoryQueryDataSourceCommandMethod
        {
            get
            {
                if (_repositoryQueryDataSourceCommandMethod == null)
                    _repositoryQueryDataSourceCommandMethod = RepositoryType.GetMethod("QueryData", new Type[] { typeof(QueryDataSourceCommandInfo) });
                return _repositoryQueryDataSourceCommandMethod;
            }
        }

        #endregion
    }
}
