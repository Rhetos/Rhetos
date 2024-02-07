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
using System.Reflection;

namespace Rhetos.Dom.DefaultConcepts
{
    public interface IOrmUtility
    {
        MethodInfo CastToStringMethod { get; }
        MethodInfo ContainsCaseInsensitiveMethod { get; }
        MethodInfo EndsWithCaseInsensitiveMethod { get; }
        MethodInfo EqualsCaseInsensitiveMethod { get; }
        MethodInfo GuidIsGreaterThanMethod { get; }
        MethodInfo GuidIsGreaterThanOrEqualMethod { get; }
        MethodInfo GuidIsLessThanMethod { get; }
        MethodInfo GuidIsLessThanOrEqualMethod { get; }
        MethodInfo IsGreaterThanMethod { get; }
        MethodInfo IsGreaterThanOrEqualMethod { get; }
        MethodInfo IsLessThanMethod { get; }
        MethodInfo IsLessThanOrEqualMethod { get; }
        MethodInfo NotEqualsCaseInsensitiveMethod { get; }
        MethodInfo StartsWithCaseInsensitiveMethod { get; }

        /// <summary>
        /// Untyped version of <see cref="OptimizeContains{T}(Expression{Func{T, bool}})"/> method,
        /// for when it is not convenient the use the generic method.
        /// </summary>
        Expression OptimizeContains(Expression expressionToOptimize);

        /// <summary>
        /// Optimizes LINQ expression <c>item => ids.Contains(memberSelector)</c>.
        /// The same optimization is applied by <see cref="WhereContains{T}(IQueryable{T}, List{Guid}, Expression{Func{T, Guid}})"/> method, with a different syntax.
        /// </summary>
        /// <remarks>
        /// Entity Framework 6.1.3 has performance issues with Contains method: it does not cache the compiled SQL
        /// if the LINQ query has Contains method, and the compilation can take significant amount on time on complex queries.
        /// </remarks>
        Expression<Func<T, bool>> OptimizeContains<T>(Expression<Func<T, bool>> expressionToOptimize);

        /// <summary>
        /// Optimized alternative to LINQ operation: <c>query.Where(item => ids.Contains(memberSelector))</c>.
        /// The same optimization is applied by <see cref="OptimizeContains"/> method, with a different syntax.
        /// </summary>
        /// <remarks>
        /// Entity Framework 6.1.3 has performance issues with Contains method: it does not cache the compiled SQL
        /// if the LINQ query has Contains method, and the compilation can take significant amount on time on complex queries.
        /// </remarks>
        IQueryable<T> WhereContains<T>(IQueryable<T> query, List<Guid> ids, Expression<Func<T, Guid>> memberSelector);
    }
}