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
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Rhetos.Dom.DefaultConcepts
{
    /// <summary>
    /// This class traverses expression tree and 
    /// a) changes expression type from TFrom to TTo
    /// b) replaces all reference to parameter in original expression to 'parameter.referenceName'
    /// Will work ONLY for properties!
    /// </summary>
    public class ReplaceWithReference<TFrom, TTo> : ExpressionVisitor
    {
        private readonly Expression<Func<TFrom, bool>> _expression;
        private readonly System.Reflection.PropertyInfo _referenceProperty;
        private readonly ParameterExpression _newParameter;
        private readonly ParameterExpression _oldParameter;
        private readonly IEnumerable<Tuple<string, string>> _copiedProperties;
        private readonly string _extensionSelfReference;

        /// <param name="copiedProperties">
        /// Tuple: inherited property, base property.
        /// Property on the inherited data structure has same value as on base data structure.
        /// Inherited row permissions will be optimized to use the property directly
        /// instead of referencing the base data structure.
        /// </param>
        public ReplaceWithReference(
            Expression<Func<TFrom, bool>> expression,
            string referenceName,
            string parameterName,
            IEnumerable<Tuple<string, string>> copiedProperties = null,
            string extensionSelfReference = null)
        {
            _expression = expression;
            _oldParameter = expression.Parameters.Single();
            _referenceProperty = typeof(TTo).GetProperty(referenceName);
            if (_referenceProperty == null)
                throw new FrameworkException("Cannot replace references in the expression. The type '"
                    + typeof(TTo).Name + "' does not contain property '" + referenceName + "' that references '" + typeof(TFrom).Name + "'.");
            _newParameter = Expression.Parameter(typeof(TTo), parameterName);
            _copiedProperties = copiedProperties ?? Enumerable.Empty<Tuple<string, string>>();
            _extensionSelfReference = extensionSelfReference;
        }

        public Expression<Func<TTo, bool>> NewExpression
        {
            get
            {
                var newBody = Visit(_expression.Body);
                return Expression.Lambda<Func<TTo, bool>>(newBody, _newParameter);
            }
        }

        protected override Expression VisitParameter(ParameterExpression original)
        {
            if (original == _oldParameter)
                return _newParameter;
            else
                return original;
        }

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            Expression obj = this.Visit(m.Object);
            IEnumerable<Expression> args = this.VisitExpressionList(m.Arguments);
            if (obj != m.Object || args != m.Arguments)
                return Expression.Call(obj, m.Method, args);
            else
                return m;
        }

        protected virtual ReadOnlyCollection<Expression> VisitExpressionList(ReadOnlyCollection<Expression> original)
        {
            List<Expression> list = null;
            for (int i = 0, n = original.Count; i < n; i++)
            {
                Expression p;
                if (original[i] == _oldParameter)
                    p = Expression.MakeMemberAccess(_newParameter, _referenceProperty);
                else
                    p = this.Visit(original[i]);

                if (p != original[i] && list == null)
                    list = original.Take(i).ToList();

                if (list != null)
                    list.Add(p);
            }

            return list?.AsReadOnly() ?? original;
        }

        protected override Expression VisitMember(MemberExpression original)
        {
            if (original.Expression == _oldParameter)
            {
                if (original.Member.Name == _extensionSelfReference)
                    return _newParameter;

                var visitedExpression = Visit(original.Expression); // Should be equal to _newParameter.

                string copiedProperty = _copiedProperties
                    .Where(cp => cp.Item2 == original.Member.Name)
                    .Select(cp => cp.Item1)
                    .OrderBy(name => name)
                    .FirstOrDefault();

                if (copiedProperty != null)
                {
                    var memberInfo = typeof(TTo).GetProperty(copiedProperty);
                    if (memberInfo == null)
                        throw new FrameworkException("Cannot replace references in the expression. The type '"
                            + typeof(TTo).Name + "' does not contain property '" + copiedProperty + "' that is copied from '" + typeof(TFrom).Name + "'.");
                    return Expression.MakeMemberAccess(visitedExpression, memberInfo);
                }
                else
                {
                    var referenceExp = Expression.MakeMemberAccess(visitedExpression, _referenceProperty);
                    return Expression.MakeMemberAccess(referenceExp, original.Member);
                }
            }
            else
            {
                return base.VisitMember(original);
            }
        }
    }
}
