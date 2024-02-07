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

using System.Linq.Expressions;
using System.Reflection;

namespace Rhetos.Dom.DefaultConcepts
{
    /// <summary>
    /// Helper method for backward compatibility with EF6, which required certain optimizations for the 'contains' operation.
    /// </summary>
    public static class EFExpression
    {
        /// <summary>
        /// This method is available for backward compatibility with EF6, which required certain optimizations for the 'contains' operation.
        /// It simply returns a standard LINQ query: <c>query.Where(item => ids.Contains(memberSelector))</c>,
        /// since EF Core has internal optimizations for the 'contains' operation.
        /// </summary>
        public static IQueryable<T> WhereContains<T>(this IQueryable<T> query, List<Guid> ids, Expression<Func<T, Guid>> memberSelector)
        {
            Expression<Func<List<Guid>>> idsLambda = () => ids;
            var idsContainsExpression = (Expression<Func<T, bool>>)Expression.Lambda(
                Expression.Call(
                    idsLambda.Body,
                    ListOfGuidContainsMethod,
                    memberSelector.Body),
                memberSelector.Parameters.Single());
            return query.Where(idsContainsExpression);
        }

        static readonly MethodInfo ListOfGuidContainsMethod = typeof(List<Guid>).GetMethod("Contains");

        /// <summary>
        /// This method is available for backward compatibility with EF6, which required certain optimizations for the 'contains' operation.
        /// It simply returns the given expression,
        /// since EF Core has internal optimizations for the 'contains' operation.
        /// The expression is expected to be in format <c>item => ids.Contains(memberSelector)</c>.
        /// </summary>
        public static Expression<Func<T, bool>> OptimizeContains<T>(Expression<Func<T, bool>> expressionToOptimize)
        {
            return expressionToOptimize;
        }

        /// <summary>
        /// This method is available for backward compatibility with EF6, which required certain optimizations for the 'contains' operation.
        /// It simply returns the given expression,
        /// since EF Core has internal optimizations for the 'contains' operation.
        /// The expression is expected to be in format <c>item => ids.Contains(memberSelector)</c>.
        /// </summary>
        /// <remarks>
        /// This is an untyped version of <see cref="OptimizeContains{T}(Expression{Func{T, bool}})"/> method,
        /// for when it is not convenient the use the generic method.
        /// </remarks>
        public static Expression OptimizeContains(Expression expressionToOptimize)
        {
            return expressionToOptimize;
        }
    }
}
