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
        private readonly ParameterExpression _parameter;
        private readonly string _referenceName;
        private readonly Expression<Func<TTo, bool>> _newExpression;

        public Expression<Func<TTo, bool>> NewExpression { get { return _newExpression; } }

        public ReplaceWithReference(Expression<Func<TFrom, bool>> expression, string referenceName, string parameterName)
        {
            _referenceName = referenceName;
            _parameter = Expression.Parameter(typeof(TTo), parameterName);
            var body = Visit(expression.Body);
            _newExpression = Expression.Lambda<Func<TTo, bool>>(body, _parameter);
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
                var referenceInfo = typeof(TTo).GetProperty(_referenceName);
                var referenceExp = Expression.MakeMemberAccess(Visit(node.Expression), referenceInfo);

                var memberInfo = typeof(TFrom).GetProperty(node.Member.Name);
                var memberExp = Expression.MakeMemberAccess(referenceExp, memberInfo);
                return memberExp;
            }
            else
            {
                return base.VisitMember(node);
            }
        }
    }
}
