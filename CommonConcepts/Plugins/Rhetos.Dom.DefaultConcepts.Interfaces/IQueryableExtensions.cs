using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Rhetos.Dom.DefaultConcepts
{
    public static class IQueryableExtensions
    {
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
            Expression _left;
            Expression _right;

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
