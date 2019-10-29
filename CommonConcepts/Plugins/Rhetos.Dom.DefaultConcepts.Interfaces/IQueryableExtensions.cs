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
    public static class IQueryableExtensions
    {
        /// <summary>
        /// Optimized alternative to LINQ operation "Where(item => ids.Contains(predicate))".
        /// Entity Framework 6.1.3 has performance issues with Contains method: it does not cache the compiled SQL
        /// if the LINQ query has Contains method, and the compilation can take significant amount on time on complex queries.
        /// </summary>
        public static IQueryable<T> WhereContains<T>(this IQueryable<T> query, List<Guid> ids, Expression<Func<T, Guid>> predicate)
        {
            Expression<Func<List<Guid>>> idsLambda = () => ids;
            ParameterExpression parameter = Expression.Parameter(typeof(T), "p");
            var predicateReplacement = (Expression<Func<T, Guid>>)(new ReplaceParameterVisitor(predicate.Parameters.Single(), parameter)).Visit(predicate);
            var finalExpression = (Expression<Func<T, bool>>)Expression.Lambda(
                Expression.Call(
                    idsLambda.Body,
                    typeof(List<Guid>).GetMethod(
                        "Contains",
                        BindingFlags.Public | BindingFlags.Instance,
                        null,
                        CallingConventions.Any,
                        new Type[] { typeof(Guid) },
                        null
                    ),
                predicateReplacement.Body), parameter);
            return query.Where(EFExpression.OptimizeContains(finalExpression));
        }

        private class ReplaceParameterVisitor : ExpressionVisitor
        {
            readonly Expression _left;
            readonly Expression _right;

            public ReplaceParameterVisitor(Expression left, Expression right)
            {
                _left = left;
                _right = right;
            }

            public override Expression Visit(Expression node)
            {
                if (node.Equals(_left))
                {
                    return _right;
                }

                return base.Visit(node);
            }
        }
    }
}
