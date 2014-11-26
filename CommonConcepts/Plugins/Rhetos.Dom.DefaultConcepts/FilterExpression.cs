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
    public class FilterExpression<TEntity>
        where TEntity : class
    {
        private Expression<Func<TEntity, bool>> _filterExpression = null;

        private static readonly Expression<Func<TEntity, bool>> _selectAll = item => true;

        public void Include(Expression<Func<TEntity, bool>> newExpression)
        {
            var constant = newExpression.Body as ConstantExpression;
            if (constant != null && constant.Type == typeof(bool) && (bool)constant.Value == true)
                newExpression = _selectAll;

            if (_filterExpression == _selectAll || newExpression == _selectAll)
                _filterExpression = _selectAll;
            else if (_filterExpression == null)
                _filterExpression = newExpression;
            else
                _filterExpression = Expression.Lambda<Func<TEntity, bool>>(
                    Expression.OrElse(_filterExpression.Body, newExpression.Body),
                    _filterExpression.Parameters);
        }

        public IQueryable<TEntity> Filter(IQueryable<TEntity> query)
        {
            if (_filterExpression == null)
                return CreateEmptyQuery();
            else if (_filterExpression == _selectAll)
                return query;
            else
                return query.Where(_filterExpression);
        }

        private IQueryable<TEntity> CreateEmptyQuery()
        {
            return new TEntity[] { }.AsQueryable();
        }
    }
}
