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
