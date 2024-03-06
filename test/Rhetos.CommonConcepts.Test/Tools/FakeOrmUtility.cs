using Rhetos.Dom.DefaultConcepts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Rhetos.CommonConcepts.Test.Tools
{
    public class FakeOrmUtility : IOrmUtility
    {
        public MethodInfo CastToStringMethod => throw new NotImplementedException();

        public MethodInfo ContainsCaseInsensitiveMethod => throw new NotImplementedException();

        public MethodInfo EndsWithCaseInsensitiveMethod => throw new NotImplementedException();

        public MethodInfo EqualsCaseInsensitiveMethod => throw new NotImplementedException();

        public MethodInfo GuidIsGreaterThanMethod => throw new NotImplementedException();

        public MethodInfo GuidIsGreaterThanOrEqualMethod => throw new NotImplementedException();

        public MethodInfo GuidIsLessThanMethod => throw new NotImplementedException();

        public MethodInfo GuidIsLessThanOrEqualMethod => throw new NotImplementedException();

        public MethodInfo IsGreaterThanMethod => throw new NotImplementedException();

        public MethodInfo IsGreaterThanOrEqualMethod => throw new NotImplementedException();

        public MethodInfo IsLessThanMethod => throw new NotImplementedException();

        public MethodInfo IsLessThanOrEqualMethod => throw new NotImplementedException();

        public MethodInfo NotEqualsCaseInsensitiveMethod => throw new NotImplementedException();

        public MethodInfo StartsWithCaseInsensitiveMethod => throw new NotImplementedException();

        public Expression CreateContainsItemsExpression<TItems>(TItems items)
        {
            throw new NotImplementedException();
        }

        public Expression OptimizeContains(Expression expressionToOptimize)
        {
            throw new NotImplementedException();
        }

        public Expression<Func<T, bool>> OptimizeContains<T>(Expression<Func<T, bool>> expressionToOptimize)
        {
            throw new NotImplementedException();
        }

        public IQueryable<T> WhereContains<T>(IQueryable<T> query, List<Guid> ids, Expression<Func<T, Guid>> memberSelector)
        {
            throw new NotImplementedException();
        }
    }
}
