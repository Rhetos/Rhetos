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

namespace Rhetos.Dom.DefaultConcepts
{
    public static class QueryableExtensions
    {
        /// <summary>
        /// Optimized alternative to LINQ operation "Where(item => ids.Contains(predicate))".
        /// Entity Framework 6.1.3 has performance issues with Contains method: it does not cache the compiled SQL
        /// if the LINQ query has Contains method, and the compilation can take significant amount on time on complex queries.
        /// </summary>
        public static IQueryable<T> WhereContains<T>(this IQueryable<T> query, List<Guid> ids, Expression<Func<T, Guid>> predicate)
        {
            Expression <Func<List<Guid>>> idsLambda = () => ids;
            var finalExpression = (Expression<Func<T, bool>>)Expression.Lambda(
                Expression.Call(
                    idsLambda.Body,
                    typeof(List<Guid>).GetMethod("Contains"),
                    predicate.Body),
                predicate.Parameters.Single());
            return query.Where(EFExpression.OptimizeContains(finalExpression));
        }
    }
}
