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
    public class Ef6OrmUtility : IOrmUtility
    {
        public MethodInfo CastToStringMethod => typeof(DatabaseExtensionFunctions).GetMethod("CastToString");
        public MethodInfo ContainsCaseInsensitiveMethod => typeof(DatabaseExtensionFunctions).GetMethod("ContainsCaseInsensitive");
        public MethodInfo EndsWithCaseInsensitiveMethod => typeof(DatabaseExtensionFunctions).GetMethod("EndsWithCaseInsensitive");
        public MethodInfo EqualsCaseInsensitiveMethod => typeof(DatabaseExtensionFunctions).GetMethod("EqualsCaseInsensitive");
        public MethodInfo GuidIsGreaterThanMethod => typeof(DatabaseExtensionFunctions).GetMethod("GuidIsGreaterThan");
        public MethodInfo GuidIsGreaterThanOrEqualMethod => typeof(DatabaseExtensionFunctions).GetMethod("GuidIsGreaterThanOrEqual");
        public MethodInfo GuidIsLessThanMethod => typeof(DatabaseExtensionFunctions).GetMethod("GuidIsLessThan");
        public MethodInfo GuidIsLessThanOrEqualMethod => typeof(DatabaseExtensionFunctions).GetMethod("GuidIsLessThanOrEqual");
        public MethodInfo IsGreaterThanMethod => typeof(DatabaseExtensionFunctions).GetMethod("IsGreaterThan");
        public MethodInfo IsGreaterThanOrEqualMethod => typeof(DatabaseExtensionFunctions).GetMethod("IsGreaterThanOrEqual");
        public MethodInfo IsLessThanMethod => typeof(DatabaseExtensionFunctions).GetMethod("IsLessThan");
        public MethodInfo IsLessThanOrEqualMethod => typeof(DatabaseExtensionFunctions).GetMethod("IsLessThanOrEqual");
        public MethodInfo NotEqualsCaseInsensitiveMethod => typeof(DatabaseExtensionFunctions).GetMethod("NotEqualsCaseInsensitive");
        public MethodInfo StartsWithCaseInsensitiveMethod => typeof(DatabaseExtensionFunctions).GetMethod("StartsWithCaseInsensitive");

        public Expression OptimizeContains(Expression expressionToOptimize) => EFExpression.OptimizeContains(expressionToOptimize);
        public Expression<Func<T, bool>> OptimizeContains<T>(Expression<Func<T, bool>> expressionToOptimize) => EFExpression.OptimizeContains(expressionToOptimize);
        public IQueryable<T> WhereContains<T>(IQueryable<T> query, List<Guid> ids, Expression<Func<T, Guid>> memberSelector) => EFExpression.WhereContains(query, ids, memberSelector);
    }
}
