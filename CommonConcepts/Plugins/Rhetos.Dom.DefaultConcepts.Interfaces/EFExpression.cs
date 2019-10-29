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
        public static Expression<Func<T, bool>> OptimizeContains<T>(Expression<Func<T, bool>> expressionToOptimize)
        {
            return (Expression<Func<T, bool>>)new ReplaceContainsVisitor().Visit(expressionToOptimize);
        }

        public static Expression OptimizeContains(Expression expressionToOptimize)
        {
            return new ReplaceContainsVisitor().Visit(expressionToOptimize);
        }

        private class ReplaceContainsVisitor : ExpressionVisitor
        {
            static readonly MethodInfo ListOfGuidContainsMethod = typeof(List<Guid>).GetMethod(
                    "Contains",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    CallingConventions.Any,
                    new Type[] { typeof(Guid) },
                    null
                );

            static readonly MethodInfo ListOfNullableGuidContainsMethod = typeof(List<Guid?>).GetMethod(
                        "Contains",
                        BindingFlags.Public | BindingFlags.Instance,
                        null,
                        CallingConventions.Any,
                        new Type[] { typeof(Guid?) },
                        null
                );

            public override Expression Visit(Expression node)
            {
                if (node == null)
                    return base.Visit(node);

                if (node.NodeType != ExpressionType.Call)
                    return base.Visit(node);

                var methodCallExpression = node as MethodCallExpression;

                if (methodCallExpression.Object == null)
                    return base.Visit(node);

                if (methodCallExpression.Object.NodeType != ExpressionType.MemberAccess)
                    return base.Visit(node);

                FieldInfo innerField = (FieldInfo)((MemberExpression)methodCallExpression.Object)?.Member;
                ConstantExpression ce = (ConstantExpression)((MemberExpression)methodCallExpression.Object)?.Expression;

                if (innerField == null || ce == null)
                    return base.Visit(node);

                if (methodCallExpression.Method == ListOfGuidContainsMethod)
                {
                    var outerObj = innerField.GetValue(ce.Value) as List<Guid>;

                    var concatenatedIds = string.Join(",", outerObj.Distinct().Select(x => x.ToString()));
                    Expression<Func<string>> idsLambda = () => concatenatedIds;

                    return Expression.Call(typeof(EFExpression).GetMethod(
                        nameof(ContainsIds),
                        BindingFlags.NonPublic | BindingFlags.Static,
                        null,
                        CallingConventions.Any,
                        new Type[] { typeof(Guid), typeof(string) },
                        null
                    ), methodCallExpression.Arguments[0], idsLambda.Body);
                }

                if (methodCallExpression.Method == ListOfNullableGuidContainsMethod)
                {
                    var outerObj = innerField.GetValue(ce.Value) as List<Guid?>;

                    var concatenatedIds = string.Join(",", outerObj.Where(x => x != null).Distinct().Select(x => x.ToString()));
                    Expression<Func<string>> idsLambda = () => concatenatedIds;

                    var optimizedContainsExpression = Expression.And(
                            Expression.Call(typeof(EFExpression).GetMethod(
                                nameof(ContainsIds),
                                BindingFlags.NonPublic | BindingFlags.Static,
                                null,
                                CallingConventions.Any,
                                new Type[] { typeof(Guid?), typeof(string) },
                                null
                            ), methodCallExpression.Arguments[0], idsLambda.Body),
                            Expression.NotEqual(methodCallExpression.Arguments[0], Expression.Constant(null)));

                    if (outerObj.Any(x => x == null))
                    {
                        optimizedContainsExpression = Expression.Or(
                            optimizedContainsExpression,
                            Expression.Equal(methodCallExpression.Arguments[0], Expression.Constant(null)));
                    }

                    return optimizedContainsExpression;
                }

                return base.Visit(node);
            }
        }

        public const string ContainsIdsFunction = "InterceptContainsIds";

        [DbFunction(EntityFrameworkMapping.StorageModelNamespace, ContainsIdsFunction)]
        private static bool ContainsIds(Guid id, string guids)
        {
            throw new NotImplementedException();
        }

        [DbFunction(EntityFrameworkMapping.StorageModelNamespace, ContainsIdsFunction)]
        private static bool ContainsIds(Guid? id, string guids)
        {
            throw new NotImplementedException();
        }
    }
}
