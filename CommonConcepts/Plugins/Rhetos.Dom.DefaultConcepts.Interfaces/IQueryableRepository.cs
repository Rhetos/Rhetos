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
    public interface IQueryableRepository<out TEntity> : IRepository
        where TEntity : class
    {
        IQueryable<TEntity> Query(object parameter, Type parameterType);
    }

    public static class QueryableRepositoryExtensions
    {
        public static IQueryable<TEntity> Query<TEntity>(this IQueryableRepository<TEntity> repository)
            where TEntity : class
        {
            return repository.Query(null, typeof(FilterAll));
        }

        public static IQueryable<TEntity> Query<TEntity>(this IQueryableRepository<TEntity> repository, Expression<Func<TEntity, bool>> filter)
            where TEntity : class
        {
            return repository.Query(null, typeof(FilterAll)).Where(filter);
        }

        public static IQueryable<TEntity> Query<TEntity, TParameter>(this IQueryableRepository<TEntity> repository, TParameter parameter)
            where TEntity : class
        {
            Type filterType = parameter != null ? parameter.GetType() : typeof(TParameter);
            return repository.Query(parameter, filterType);
        }
    }
}
