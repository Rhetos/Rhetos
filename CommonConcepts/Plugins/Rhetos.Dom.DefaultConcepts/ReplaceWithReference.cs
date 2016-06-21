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
        private readonly ParameterExpression _parameter;
        private readonly IEnumerable<Tuple<string, string>> _copiedProperties;

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
            IEnumerable<Tuple<string, string>> copiedProperties = null)
        {
            _expression = expression;
            _referenceProperty = typeof(TTo).GetProperty(referenceName);
            if (_referenceProperty == null)
                throw new FrameworkException("Cannot replace references in the expression. The type '"
                    + typeof(TTo).Name + "' does not contain property '" + referenceName + "' that references '" + typeof(TFrom).Name + "'.");
            _parameter = Expression.Parameter(typeof(TTo), parameterName);
            _copiedProperties = copiedProperties ?? Enumerable.Empty<Tuple<string, string>>();
        }

        public Expression<Func<TTo, bool>> NewExpression
        {
            get
            {
                var newBody = Visit(_expression.Body);
                return Expression.Lambda<Func<TTo, bool>>(newBody, _parameter);
            }
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            return _parameter;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if ((node.Expression != null && node.Expression.Type == typeof(TFrom))
                || (node.Member.DeclaringType == typeof(TFrom)))
            {
                var newExpression = Visit(node.Expression);

                string copiedProperty = _copiedProperties
                    .Where(cp => cp.Item2 == node.Member.Name)
                    .Select(cp => cp.Item1)
                    .OrderBy(name => name)
                    .FirstOrDefault();

                if (copiedProperty != null)
                {
                    var memberInfo = typeof(TTo).GetProperty(copiedProperty);
                    if (memberInfo == null)
                        throw new FrameworkException("Cannot replace references in the expression. The type '"
                            + typeof(TTo).Name + "' does not contain property '" + copiedProperty + "' that is copied from '" + typeof(TFrom).Name + "'.");
                    return Expression.MakeMemberAccess(newExpression, memberInfo);
                }
                else
                {
                    var referenceExp = Expression.MakeMemberAccess(newExpression, _referenceProperty);
                    var memberInfo = typeof(TFrom).GetProperty(node.Member.Name);
                    return Expression.MakeMemberAccess(referenceExp, memberInfo);
                }
            }
            else
            {
                return base.VisitMember(node);
            }
        }
    }
}
