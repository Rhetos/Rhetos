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

using Rhetos.Persistence;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Rhetos.Dom.DefaultConcepts
{
    public static class EFExpression
    {
        /// <summary>
        /// Optimized alternative to LINQ operation "Where(item => ids.Contains(predicate))".
        /// Entity Framework 6.1.3 has performance issues with Contains method: it does not cache the compiled SQL
        /// if the LINQ query has Contains method, and the compilation can take significant amount on time on complex queries.
        /// </summary>
        public static IQueryable<T> WhereContains<T>(this IQueryable<T> query, List<Guid> ids, Expression<Func<T, Guid>> predicate)
        {
            Expression<Func<List<Guid>>> idsLambda = () => ids;
            var idsContainsExpression = (Expression<Func<T, bool>>)Expression.Lambda(
                Expression.Call(
                    idsLambda.Body,
                    ListOfGuidContainsMethod,
                    predicate.Body),
                predicate.Parameters.Single());
            return query.Where(OptimizeContains(idsContainsExpression));
        }

        public static Expression<Func<T, bool>> OptimizeContains<T>(Expression<Func<T, bool>> expressionToOptimize)
        {
            return (Expression<Func<T, bool>>)new ReplaceContainsVisitor().Visit(expressionToOptimize);
        }

        public static Expression OptimizeContains(Expression expressionToOptimize)
        {
            return new ReplaceContainsVisitor().Visit(expressionToOptimize);
        }

        static readonly MethodInfo ListOfGuidContainsMethod = typeof(List<Guid>).GetMethod("Contains");

        static readonly MethodInfo ListOfNullableGuidContainsMethod = typeof(List<Guid?>).GetMethod("Contains");

        private class ReplaceContainsVisitor : ExpressionVisitor
        {
            static readonly MethodInfo ContainsIdsMethod = typeof(EFExpression).GetMethod(
                nameof(ContainsIds),
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new Type[] { typeof(Guid), typeof(string) },
                null);

            static readonly MethodInfo ContainsIdNullableMethod = typeof(EFExpression).GetMethod(
                nameof(ContainsIds),
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new Type[] { typeof(Guid?), typeof(string) },
                null);

            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (node.Object == null)
                    return base.VisitMethodCall(node);

                if (node.Object.NodeType != ExpressionType.MemberAccess)
                    return base.VisitMethodCall(node);

                FieldInfo innerField = (FieldInfo)((MemberExpression)node.Object)?.Member;
                ConstantExpression ce = (ConstantExpression)((MemberExpression)node.Object)?.Expression;

                if (innerField == null || ce == null)
                    return base.VisitMethodCall(node);

                if (node.Method == ListOfGuidContainsMethod)
                {
                    var outerObj = (List<Guid>)innerField.GetValue(ce.Value);

                    var concatenatedIds = string.Join(",", outerObj.Distinct().Select(x => x.ToString()));
                    Expression<Func<string>> idsLambda = () => concatenatedIds;

                    return Expression.Call(ContainsIdsMethod, node.Arguments[0], idsLambda.Body);
                }

                if (node.Method == ListOfNullableGuidContainsMethod)
                {
                    var outerObj = (List<Guid?>)innerField.GetValue(ce.Value);

                    var concatenatedIds = string.Join(",", outerObj.Where(x => x != null).Distinct().Select(x => x.ToString()));
                    Expression<Func<string>> idsLambda = () => concatenatedIds;

                    Expression optimizedContainsExpression = Expression.Call(ContainsIdNullableMethod, node.Arguments[0], idsLambda.Body);

                    optimizedContainsExpression = Expression.And(
                            optimizedContainsExpression,
                            Expression.NotEqual(node.Arguments[0], Expression.Constant(null)));

                    if (outerObj.Any(x => x == null))
                    {
                        optimizedContainsExpression = Expression.Or(
                            optimizedContainsExpression,
                            Expression.Equal(node.Arguments[0], Expression.Constant(null)));
                    }

                    return optimizedContainsExpression;
                }

                return base.VisitMethodCall(node);
            }
        }

        public const string ContainsIdsFunction = "InterceptContainsIds";

        [DbFunction(EntityFrameworkMapping.StorageModelNamespace, ContainsIdsFunction)]
        private static bool ContainsIds(Guid id, string guids)
        {
            return !string.IsNullOrEmpty(guids)
                && guids.Split(',').Select(guid => new Guid(guid)).Contains(id);
        }

        [DbFunction(EntityFrameworkMapping.StorageModelNamespace, ContainsIdsFunction)]
        private static bool ContainsIds(Guid? id, string guids)
        {
            return id != null
                && !string.IsNullOrEmpty(guids)
                && guids.Split(',').Select(guid => new Guid(guid)).Contains(id.Value);
        }
    }
}
