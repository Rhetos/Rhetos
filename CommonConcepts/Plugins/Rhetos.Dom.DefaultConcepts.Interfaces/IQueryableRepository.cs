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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Rhetos.Dom.DefaultConcepts
{
    public interface IQueryableRepository<out TQueryableEntity> : IRepository
        where TQueryableEntity : class, IEntity
    {
        IQueryable<TQueryableEntity> Query(object parameter, Type parameterType);
    }

    public interface IQueryableRepository<out TQueryableEntity, out TEntity> :
        IQueryableRepository<TQueryableEntity>,
        IReadableRepository<TEntity>
        where TEntity : class, IEntity
        where TQueryableEntity : class, IEntity, TEntity
    {
    }

    public static class QueryableRepositoryExtensions
    {
        public static IQueryable<TQueryableEntity> Query<TQueryableEntity>(this IQueryableRepository<TQueryableEntity> repository)
            where TQueryableEntity : class, IEntity
        {
            return repository.Query(null, typeof(FilterAll));
        }

        public static IQueryable<TQueryableEntity> Query<TQueryableEntity>(this IQueryableRepository<TQueryableEntity> repository, Expression<Func<TQueryableEntity, bool>> filter)
            where TQueryableEntity : class, IEntity
        {
            return repository.Query(null, typeof(FilterAll)).Where(filter);
        }

        public static IQueryable<TQueryableEntity> Query<TQueryableEntity, TParameter>(this IQueryableRepository<TQueryableEntity> repository, TParameter parameter)
            where TQueryableEntity : class, IEntity
        {
            Type filterType = parameter != null ? parameter.GetType() : typeof(TParameter);
            return repository.Query(parameter, filterType);
        }

        public static IEnumerable<TEntity> Load<TQueryableEntity, TEntity>(this IQueryableRepository<TQueryableEntity, TEntity> repository, Expression<Func<TQueryableEntity, bool>> filter)
            where TEntity : class, IEntity
            where TQueryableEntity : class, IEntity, TEntity
        {
            return repository.Load(filter, filter.GetType());
        }
    }
}
