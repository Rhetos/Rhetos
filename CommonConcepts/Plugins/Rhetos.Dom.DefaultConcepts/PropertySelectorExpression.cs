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
using System.Text;

namespace Rhetos.Dom.DefaultConcepts
{
    /// <summary>
    /// A helper class for lambda expression handling.
    /// </summary>
    public class PropertySelectorExpression<TEntityInterface, TProperties>
    {
        readonly Expression<Func<TEntityInterface, TProperties>> _propertiesSelector;

        readonly Lazy<bool> _simpleConstruct; // True if propertiesSelector is "item=>item.SomeProperty", false if propertiesSelector is "item => new { item.Prop1, item.Prop2 }"
        readonly Lazy<Func<TEntityInterface, TProperties>> _propertiesSelectorFunc;
        readonly Lazy<IEnumerable<Expression>> _propertiesExpression;
        readonly Lazy<IEnumerable<PropertyInfo>> _propertiesInfo;

        public PropertySelectorExpression(Expression<Func<TEntityInterface, TProperties>> propertiesSelector)
        {
            _propertiesSelector = propertiesSelector;

            _simpleConstruct = new Lazy<bool>(InitializeSimpleConstruct);
            _propertiesSelectorFunc = new Lazy<Func<TEntityInterface, TProperties>>(() => _propertiesSelector.Compile());
            _propertiesExpression = new Lazy<IEnumerable<Expression>>(InitializeMemberExpressions);
            _propertiesInfo = new Lazy<IEnumerable<PropertyInfo>>(InitializePropertyInfos);
        }

        public string ToString(TEntityInterface item)
        {
            var value = _propertiesSelectorFunc.Value.Invoke(item);
            if (value == null)
                return "<null>";
            if (value is string)
                return @"""" + value.ToString() + @"""";
            return
                value.ToString();
        }

        public Expression<Func<TEntityInterface, bool>> BuildComparisonPredicate(TEntityInterface item)
        {
            Expression allEquals = null;

            var propertiesValue = GetPropertiesValue(item);

            if (propertiesValue.Count() != _propertiesExpression.Value.Count())
                throw new Rhetos.FrameworkException(string.Format(
                    "Internal error: propertiesValue and _propertiesExpression length mismatch ({0} != {1}).",
                    propertiesValue.Count(), _propertiesExpression.Value.Count()));

            var members = propertiesValue.Zip(_propertiesExpression.Value, (value, expression) => new { value, expression });
            foreach (var member in members)
            {
                var compareToValueExpression = Expression.Constant(member.value, member.expression.Type);
                var equals = Expression.Equal(member.expression, compareToValueExpression);
                allEquals = allEquals == null ? equals : Expression.AndAlso(allEquals, equals);
            }

            return Expression.Lambda<Func<TEntityInterface, bool>>(allEquals, _propertiesSelector.Parameters.Single());
        }

        public void Assign(TEntityInterface destination, TEntityInterface source)
        {
            var propertiesValue = GetPropertiesValue(source);

            if (propertiesValue.Count() != _propertiesExpression.Value.Count())
                throw new FrameworkException(string.Format(
                    "Internal error: propertiesValue and _propertiesExpression length mismatch ({0} != {1}).",
                    propertiesValue.Count(), _propertiesExpression.Value.Count()));

            var members = propertiesValue.Zip(_propertiesExpression.Value, (value, expression) => new { value, expression });
            foreach (var member in members)
            {
                var memberExpression = member.expression as MemberExpression;
                if (memberExpression == null)
                    throw new FrameworkException("Assign function supports only simple property selector. (" + member.expression.ToString() + " is not a MemberExpression)");

                var propertyInfo = memberExpression.Member as PropertyInfo;
                if (memberExpression == null)
                    throw new FrameworkException("Assign function supports only simple property selector. (" + memberExpression.Member.ToString() + " is not a PropertyInfo)");

                if (!(memberExpression.Expression is ParameterExpression))
                    throw new FrameworkException("Assign function supports only simple property selector. (" + memberExpression.Expression.ToString() + " is not a ParameterExpression)");

                propertyInfo.SetValue(destination, member.value, null);
            }
        }

        //========================================================

        private bool InitializeSimpleConstruct()
        {
            if (_propertiesSelector.Body is MemberExpression)
                return true;
            if (_propertiesSelector.Body is NewExpression)
                return false;
            throw new ApplicationException("The given propertiesSelector must be a MemberExpression or a NewExpression (for multiple members).");
        }

        private IEnumerable<Expression> InitializeMemberExpressions()
        {
            if (_simpleConstruct.Value)
                return new[] { (MemberExpression)_propertiesSelector.Body };
            else
                return ((NewExpression)_propertiesSelector.Body).Arguments.ToList();
        }

        private IEnumerable<PropertyInfo> InitializePropertyInfos()
        {
            if (_simpleConstruct.Value)
                return null;
            else
                return typeof(TProperties).GetProperties();
        }

        private IEnumerable<object> GetPropertiesValue(TEntityInterface item)
        {
            object instanceSelectedProperties = _propertiesSelectorFunc.Value.Invoke(item);

            if (_simpleConstruct.Value)
                return new object[] { instanceSelectedProperties };
            else
                return _propertiesInfo.Value.Select(pi => pi.GetValue(instanceSelectedProperties, null));
        }
    }
}
