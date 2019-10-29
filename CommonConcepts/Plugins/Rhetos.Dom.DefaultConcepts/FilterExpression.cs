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
    /// Helper class for joining multiple filter expressions to a single filter expression (Expression&lt;Func&lt;T, bool&gt;&gt;).
    /// The class is used for joining row permission rules to a single row permission filter.
    /// Use of expression that includes or excludes all items ("item => true") will be optimized when generating the final expression.
    /// </summary>
    public class FilterExpression<T>
    {
        private static readonly Expression<Func<T, bool>> _selectAll = item => true;
        private static readonly Expression<Func<T, bool>> _selectNone = item => false;

        private Expression<Func<T, bool>> _allowExpression = _selectNone;
        private Expression<Func<T, bool>> _denyExpression = _selectAll;

        public void Include(Expression<Func<T, bool>> newExpression)
        {
            RecognizeConstantExpression(ref newExpression);
            _allowExpression = Or(_allowExpression, newExpression);
        }

        public void Exclude(Expression<Func<T, bool>> newExpression)
        {
            RecognizeConstantExpression(ref newExpression);
            _denyExpression = And(_denyExpression, Not(newExpression));
        }

        public Expression<Func<T, bool>> GetFilter()
        {
            var finalExpression = And(_allowExpression, _denyExpression);

            return finalExpression;
        }

        public static IQueryable<T> OptimizedWhere(IQueryable<T> source, Expression<Func<T, bool>> expression)
        {
            RecognizeConstantExpression(ref expression);
            if (expression == _selectAll) return source;
            else if (expression == _selectNone) return (new T[] { }).AsQueryable();
            else return source.Where(expression);
        }

        #region Optimized boolean expression functions

        private static void RecognizeConstantExpression(ref Expression<Func<T, bool>> a)
        {
            var constant = a.Body as ConstantExpression;
            if (constant != null && constant.Type == typeof(bool))
                a = (bool)constant.Value ? _selectAll : _selectNone;
        }

        public static Expression<Func<T, bool>> Not(Expression<Func<T, bool>> a)
        {
            if (a == _selectAll)
                return _selectNone;
            else if (a == _selectNone)
                return _selectAll;
            else
                return Expression.Lambda<Func<T, bool>>(Expression.Not(a.Body), a.Parameters);
        }

        public static Expression<Func<T, bool>> Or(Expression<Func<T, bool>> a, Expression<Func<T, bool>> b)
        {
            if (a == _selectAll || b == _selectAll)
                return _selectAll;
            else if (a == _selectNone)
                return b;
            else if (b == _selectNone)
                return a;
            else
            {
                MatchExpressionParameter(a, ref b);
                return Expression.Lambda<Func<T, bool>>(Expression.OrElse(a.Body, b.Body), a.Parameters);
            }
        }

        public static Expression<Func<T, bool>> And(Expression<Func<T, bool>> a, Expression<Func<T, bool>> b)
        {
            if (a == _selectNone || b == _selectNone)
                return _selectNone;
            else if (a == _selectAll)
                return b;
            else if (b == _selectAll)
                return a;
            else
            {
                MatchExpressionParameter(a, ref b);
                return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(a.Body, b.Body), a.Parameters);
            }
        }

        #endregion

        /// <summary>
        /// Modifies expression b to have the same parameter instance as expression a.
        /// </summary>
        private static void MatchExpressionParameter(Expression<Func<T, bool>> a, ref Expression<Func<T, bool>> b)
        {
            var parameterReplacer = new ParameterReplacer(b.Parameters.Single(), a.Parameters.Single());
            b = (Expression<Func<T, bool>>)parameterReplacer.Visit(b);
        }
    }
}
