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
    /// <summary>
    /// Naming convention:
    /// Load() returns loaded (materialized) data in a List or an array.
    /// Query() returns a LINQ query.
    /// Read() returns loaded data or a linq query, depending on the available repository's functions and the preferQuery parameter.
    /// </summary>
    public interface IReadableRepository<out TEntity> : IRepository
        where TEntity : class, IEntity
    {
        /// <summary>
        /// This function returns loaded data (a List or an array), using available repository's Load, Query and Filter functions.
        /// </summary>
        /// <param name="parameterType">
        /// The parameterType is usually <code>parameter.GetType()</code>. Note that the parameter value may be null.
        /// </param>
        IEnumerable<TEntity> Load(object parameter, Type parameterType);

        /// <summary>
        /// This function returns a LINQ query (IQueryable) or loaded data (a List or an array), using available repository's Load, Query and Filter functions.
        /// If both Query and Load functions are available, the preferQuery argument will be used to determine which functions will be used and the result type.
        /// </summary>
        /// <param name="parameterType">
        /// The parameterType is usually <code>parameter.GetType()</code>. Note that the parameter value may be null.
        /// </param>
        IEnumerable<TEntity> Read(object parameter, Type parameterType, bool preferQuery);
    }

    public static class ReadableRepositoryExtensions
    {
        public static IEnumerable<TEntity> Load<TEntity>(this IReadableRepository<TEntity> repository)
            where TEntity : class, IEntity
        {
            return repository.Load(null, typeof(FilterAll));
        }

        public static IEnumerable<TEntity> Load<TEntity, TParameter>(this IReadableRepository<TEntity> repository, TParameter parameter)
            where TEntity : class, IEntity
        {
            Type filterType = parameter != null ? parameter.GetType() : typeof(TParameter);
            return repository.Load(parameter, filterType);
        }

        public static IEnumerable<TEntity> Read<TEntity, TParameter>(this IReadableRepository<TEntity> repository, TParameter parameter, bool preferQuery)
            where TEntity : class, IEntity
        {
            Type filterType = parameter != null ? parameter.GetType() : typeof(TParameter);
            return repository.Read(parameter, filterType, preferQuery);
        }
    }
}
