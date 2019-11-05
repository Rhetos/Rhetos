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
        /// Optimized alternative to LINQ operation "Where(item => ids.Contains(memberSelector))".
        /// Entity Framework 6.1.3 has performance issues with Contains method: it does not cache the compiled SQL
        /// if the LINQ query has Contains method, and the compilation can take significant amount on time on complex queries.
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

        static readonly MethodInfo EnumerableOfGuidContainsMethod = typeof(Enumerable).GetMethods().Single(m => m.Name == "Contains" && m.GetParameters().Count() == 2).MakeGenericMethod(typeof(Guid));

        static readonly MethodInfo EnumerableOfNullableGuidContainsMethod = typeof(Enumerable).GetMethods().Single(m => m.Name == "Contains" && m.GetParameters().Count() == 2).MakeGenericMethod(typeof(Guid?));

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
                if (node.Object != null && node.Object.NodeType == ExpressionType.MemberAccess)
                {
                    FieldInfo innerField = (node.Object as MemberExpression )?.Member as FieldInfo;
                    ConstantExpression ce = (node.Object as MemberExpression)?.Expression as ConstantExpression;

                    if (innerField == null || ce == null)
                        return base.VisitMethodCall(node);

                    if (node.Method == ListOfGuidContainsMethod && innerField.GetValue(ce.Value) is List<Guid> listOfGuid)
                        return CreateOptimizeExpressionForListOfGuid(listOfGuid, node.Arguments[0]);

                    if (node.Method == ListOfNullableGuidContainsMethod && innerField.GetValue(ce.Value) is List<Guid?> listOfNullableGuid)
                        return CreateOptimizeExpressionForListOfNullableGuid(listOfNullableGuid, node.Arguments[0]);
                }
                else if (node.Object == null && node.Arguments.Count == 2 && node.Arguments[0].NodeType == ExpressionType.Constant)
                {
                    ConstantExpression ce = node.Arguments[0] as ConstantExpression;

                    if (ce == null)
                        return base.VisitMethodCall(node);

                    if (node.Method == EnumerableOfGuidContainsMethod && ce.Value is IList<Guid> listOfGuid)
                        return CreateOptimizeExpressionForListOfGuid(listOfGuid, node.Arguments[1]);

                    if (node.Method == EnumerableOfNullableGuidContainsMethod && ce.Value is IList<Guid?> listOfNullableGuid)
                        return CreateOptimizeExpressionForListOfNullableGuid(listOfNullableGuid, node.Arguments[1]);
                }
                else if(node.Object == null && node.Arguments.Count == 2 && node.Arguments[0].NodeType == ExpressionType.MemberAccess)
                {
                    FieldInfo innerField = (node.Arguments[0] as MemberExpression)?.Member as FieldInfo;
                    ConstantExpression ce = (node.Arguments[0] as MemberExpression)?.Expression as ConstantExpression;

                    if (innerField == null || ce == null)
                        return base.VisitMethodCall(node);

                    if (node.Method == EnumerableOfGuidContainsMethod && innerField.GetValue(ce.Value) is IList<Guid> listOfGuid)
                        return CreateOptimizeExpressionForListOfGuid(listOfGuid, node.Arguments[1]);

                    if (node.Method == EnumerableOfNullableGuidContainsMethod && innerField.GetValue(ce.Value) is IList<Guid?> listOfNullableGuid)
                        return CreateOptimizeExpressionForListOfNullableGuid(listOfNullableGuid, node.Arguments[1]);
                }

                return base.VisitMethodCall(node);
            }

            Expression CreateOptimizeExpressionForListOfGuid(IList<Guid> guids, Expression value)
            {
                var concatenatedIds = string.Join(",", guids.Distinct().Select(x => x.ToString()));
                Expression<Func<string>> idsLambda = () => concatenatedIds;

                return Expression.Call(ContainsIdsMethod, value, idsLambda.Body);
            }

            Expression CreateOptimizeExpressionForListOfNullableGuid(IList<Guid?> guids, Expression value)
            {
                var concatenatedIds = string.Join(",", guids.Where(x => x != null).Distinct().Select(x => x.ToString()));
                Expression<Func<string>> idsLambda = () => concatenatedIds;

                Expression optimizedContainsExpression = Expression.Call(ContainsIdNullableMethod, value, idsLambda.Body);

                // EF would where add here "AND argument IS NOT NULL", if UseDatabaseNullSemantics=false,
                // but we have removed that condition because:
                // 1. it does not change the result in the database, and
                // 2. there is no clean way to use the system configuration here (from static methods WhereContains and OptimizeContains).

                if (guids.Any(x => x == null))
                    optimizedContainsExpression = Expression.Or(
                        optimizedContainsExpression,
                        Expression.Equal(value, Expression.Constant(null)));

                return optimizedContainsExpression;
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
