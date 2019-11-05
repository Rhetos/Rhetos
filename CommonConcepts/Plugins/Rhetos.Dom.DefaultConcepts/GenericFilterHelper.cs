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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using Rhetos.Processing.DefaultCommands;
using Rhetos.Utilities;
using System.Collections;

namespace Rhetos.Dom.DefaultConcepts
{
    public class GenericFilterHelper
    {
        readonly IDomainObjectModel _domainObjectModel;

        public GenericFilterHelper(IDomainObjectModel domainObjectModel)
        {
            _domainObjectModel = domainObjectModel;
        }

        //================================================================
        #region Property filters

        public Expression<Func<T, bool>> ToExpression<T>(IEnumerable<PropertyFilter> propertyFilters)
        {
            return (Expression<Func<T, bool>>)ToExpression(propertyFilters, typeof(T));
        }

        public LambdaExpression ToExpression(IEnumerable<PropertyFilter> propertyFilters, Type parameterType)
        {
            ParameterExpression parameter = Expression.Parameter(parameterType, "p");

            if (propertyFilters == null || !propertyFilters.Any())
                return Expression.Lambda(Expression.Constant(true), parameter);

            Expression resultCondition = null;

            foreach (var filter in propertyFilters)
            {
                if (string.IsNullOrEmpty(filter.Property))
                    continue;

                Expression memberAccess = null;
                foreach (var property in filter.Property.Split('.'))
                {
                    var parentExpression = memberAccess ?? (Expression)parameter;
                    if (parentExpression.Type.GetProperty(property) == null)
                        throw new ClientException("Invalid generic filter parameter: Type '" + parentExpression.Type.FullName + "' does not have property '" + property + "'.");
                    memberAccess = Expression.Property(parentExpression, property);
                }

                // Change the type of the parameter 'value'. It is necessary for comparisons.

                bool propertyIsNullableValueType = memberAccess.Type.IsGenericType && memberAccess.Type.GetGenericTypeDefinition() == typeof(Nullable<>);
                Type propertyBasicType = propertyIsNullableValueType ? memberAccess.Type.GetGenericArguments().Single() : memberAccess.Type;

                ConstantExpression constant;
                // Operations 'equal' and 'notequal' are supported for backward compatibility.
                if (new[] { "equals", "equal", "notequals", "notequal", "greater", "greaterequal", "less", "lessequal" }.Contains(filter.Operation, StringComparer.OrdinalIgnoreCase))
                {
                    // Constant value should be of same type as the member it is compared to.
                    object convertedValue;
                    if (filter.Value == null || propertyBasicType.IsAssignableFrom(filter.Value.GetType()))
                        convertedValue = filter.Value;

                    // Guid object's type was not automatically recognized when deserializing from JSON:
                    else if (propertyBasicType == typeof(Guid) && filter.Value is string)
                        convertedValue = Guid.Parse(filter.Value.ToString());

                    // DateTime object's type was not automatically recognized when deserializing from JSON:
                    else if (propertyBasicType == typeof(DateTime) && filter.Value is string)
                        convertedValue = ParseJsonDateTime((string)filter.Value, filter.Property, propertyBasicType);

                    else if ((propertyBasicType == typeof(decimal) || propertyBasicType == typeof(int)) && filter.Value is string)
                        throw new FrameworkException($"Invalid JSON format of {propertyBasicType.Name} property '{filter.Property}'. Numeric value should not be passed as a string in JSON serialized object.");
                    else
                        convertedValue = Convert.ChangeType(filter.Value, propertyBasicType);

                    if (convertedValue == null && memberAccess.Type.IsValueType && !propertyIsNullableValueType)
                    {
                        Type nullableMemberType = typeof(Nullable<>).MakeGenericType(memberAccess.Type);
                        memberAccess = Expression.Convert(memberAccess, nullableMemberType);
                    }

                    constant = Expression.Constant(convertedValue, memberAccess.Type);
                }
                else if (new[] { "startswith", "endswith", "contains", "notcontains" }.Contains(filter.Operation, StringComparer.OrdinalIgnoreCase))
                {
                    // Constant value should be string.
                    constant = Expression.Constant(filter.Value.ToString(), typeof(string));
                }
                else if (new[] { "datein", "datenotin" }.Contains(filter.Operation, StringComparer.OrdinalIgnoreCase))
                {
                    constant = null;
                }
                else if (new[] { "in", "notin" }.Contains(filter.Operation, StringComparer.OrdinalIgnoreCase))
                {
                    if (filter.Value == null)
                        throw new ClientException($"Invalid generic filter parameter for operation '{filter.Operation}' on {propertyBasicType.Name} property '{filter.Property}'."
                            + $" The provided value is null, instead of an Array.");

                    // The list element should be of same type as the member it is compared to.
                    var parameterMismatchInfo = new Lazy<string>(() =>
                        $"Invalid generic filter parameter for operation '{filter.Operation}' on {propertyBasicType.Name} property '{filter.Property}'."
                            + $" The provided value type is '{filter.Value.GetType()}', instead of an Array of {propertyBasicType.Name}.");

                    if (!(filter.Value is IEnumerable))
                        throw new ClientException(parameterMismatchInfo.Value);

                    var list = (IEnumerable)filter.Value;

                    // Guid object's type was not automatically recognized when deserializing from JSON:
                    if (propertyBasicType == typeof(Guid) && list is IEnumerable<string>)
                        list = ((IEnumerable<string>)list).Select(s => !string.IsNullOrEmpty(s) ? (Guid?)Guid.Parse(s) : null).ToList();

                    // DateTime object's type was not automatically recognized when deserializing from JSON:
                    if (propertyBasicType == typeof(DateTime) && list is IEnumerable<string>)
                        list = ((IEnumerable<string>)list).Select(s => ParseJsonDateTime(s, filter.Property, propertyBasicType)).ToList();

                    // Adjust the list element type to exactly match the property type:
                    if (GetElementType(list) != memberAccess.Type)
                    {
                        if (list is IList && ((IList)list).Count == 0)
                            list = EmptyList(memberAccess.Type);
                        else if (propertyBasicType == typeof(Guid))
                            AdjustListTypeNullable<Guid>(ref list, propertyIsNullableValueType);
                        else if (propertyBasicType == typeof(DateTime))
                            AdjustListTypeNullable<DateTime>(ref list, propertyIsNullableValueType);
                        else if (propertyBasicType == typeof(int))
                            AdjustListTypeNullable<int>(ref list, propertyIsNullableValueType);
                        else if (propertyBasicType == typeof(decimal))
                            AdjustListTypeNullable<decimal>(ref list, propertyIsNullableValueType);

                        if (!(GetElementType(list).IsAssignableFrom(memberAccess.Type)))
                            throw new ClientException(parameterMismatchInfo.Value);
                    }

                    constant = Expression.Constant(list, list.GetType());
                }
                else
                    throw new ClientException($"Unsupported generic filter operation '{filter.Operation}' on a property.");

                Expression expression;
                switch (filter.Operation.ToLower())
                {
                    case "equals":
                    case "equal":
                        if (propertyBasicType == typeof(Guid) && constant.Value is Guid constantIdEquals)
                        {
                            // Using a different expression instead of the constant, to force Entity Framework to
                            // use query parameter instead of hardcoding the constant value (literal) into the generated query.
                            // Query with parameter will allow cache reuse for both EF LINQ compiler and database SQL compiler.
                            Expression<Func<object>> idLambda = () => constantIdEquals;
                            expression = Expression.Equal(memberAccess, Expression.Convert(idLambda.Body, memberAccess.Type));
                        }
                        else if (propertyBasicType == typeof(string) && constant.Value != null)
                            expression = Expression.Call(typeof(DatabaseExtensionFunctions).GetMethod("EqualsCaseInsensitive"), memberAccess, constant);
                        else
                            expression = Expression.Equal(memberAccess, constant);
                        break;
                    case "notequals":
                    case "notequal":
                        if (propertyBasicType == typeof(Guid) && constant.Value is Guid constantIdNotEquals)
                        {
                            // Using a different expression instead of the constant, to force Entity Framework to
                            // use query parameter instead of hardcoding the constant value (literal) into the generated query.
                            // Query with parameter will allow cache reuse for both EF LINQ compiler and database SQL compiler.
                            Expression<Func<object>> idLambda = () => constantIdNotEquals;
                            expression = Expression.Equal(memberAccess, Expression.Convert(idLambda.Body, memberAccess.Type));
                        }
                        else if (propertyBasicType == typeof(string) && constant.Value != null)
                            expression = Expression.Call(typeof(DatabaseExtensionFunctions).GetMethod("NotEqualsCaseInsensitive"), memberAccess, constant);
                        else
                            expression = Expression.NotEqual(memberAccess, constant);
                        break;
                    case "greater":
                        if (propertyBasicType == typeof(string))
                            expression = Expression.Call(typeof(DatabaseExtensionFunctions).GetMethod("IsGreaterThen"), memberAccess, constant);
                        else if (propertyBasicType == typeof(Guid))
                            expression = Expression.Call(typeof(DatabaseExtensionFunctions).GetMethod("GuidIsGreaterThan"), memberAccess, constant);
                        else expression = Expression.GreaterThan(memberAccess, constant);
                        break;
                    case "greaterequal":
                        if (propertyBasicType == typeof(string))
                            expression = Expression.Call(typeof(DatabaseExtensionFunctions).GetMethod("IsGreaterThenOrEqual"), memberAccess, constant);
                        else if (propertyBasicType == typeof(Guid))
                            expression = Expression.Call(typeof(DatabaseExtensionFunctions).GetMethod("GuidIsGreaterThanOrEqual"), memberAccess, constant);
                        else expression = Expression.GreaterThanOrEqual(memberAccess, constant);
                        break;
                    case "less":
                        if (propertyBasicType == typeof(string))
                            expression = Expression.Call(typeof(DatabaseExtensionFunctions).GetMethod("IsLessThen"), memberAccess, constant);
                        else if (propertyBasicType == typeof(Guid))
                            expression = Expression.Call(typeof(DatabaseExtensionFunctions).GetMethod("GuidIsLessThan"), memberAccess, constant);
                        else expression = Expression.LessThan(memberAccess, constant);
                        break;
                    case "lessequal":
                        if (propertyBasicType == typeof(string))
                            expression = Expression.Call(typeof(DatabaseExtensionFunctions).GetMethod("IsLessThenOrEqual"), memberAccess, constant);
                        else if (propertyBasicType == typeof(Guid))
                            expression = Expression.Call(typeof(DatabaseExtensionFunctions).GetMethod("GuidIsLessThanOrEqual"), memberAccess, constant);
                        else expression = Expression.LessThanOrEqual(memberAccess, constant);
                        break;
                    case "startswith":
                    case "endswith":
                        {
                            Expression stringMember;
                            if (propertyBasicType == typeof(string))
                                stringMember = memberAccess;
                            else
                            {
                                var castMethod = typeof(DatabaseExtensionFunctions).GetMethod("CastToString", new[] { memberAccess.Type });
                                if (castMethod == null)
                                    throw new FrameworkException("Generic filter operation '" + filter.Operation + "' is not supported on property type '" + propertyBasicType.Name + "'. There is no overload of 'DatabaseExtensionFunctions.CastToString' function for the type.");
                                stringMember = Expression.Call(castMethod, memberAccess);
                            }
                            string dbMethodName = filter.Operation.Equals("startswith", StringComparison.OrdinalIgnoreCase) ? "StartsWithCaseInsensitive" : "EndsWithCaseInsensitive";
                            expression = Expression.Call(typeof(DatabaseExtensionFunctions).GetMethod(dbMethodName), stringMember, constant);
                            break;
                        }
                    case "contains":
                    case "notcontains":
                        {
                            Expression stringMember;
                            if (propertyBasicType == typeof(string))
                                stringMember = memberAccess;
                            else
                            {
                                var castMethod = typeof(DatabaseExtensionFunctions).GetMethod("CastToString", new[] { memberAccess.Type });
                                if (castMethod == null)
                                    throw new FrameworkException("Generic filter operation '" + filter.Operation + "' is not supported on property type '" + propertyBasicType.Name + "'. There is no overload of 'DatabaseExtensionFunctions.CastToString' function for the type.");
                                stringMember = Expression.Call(castMethod, memberAccess);
                            }
                            expression = Expression.Call(typeof(DatabaseExtensionFunctions).GetMethod("ContainsCaseInsensitive"), stringMember, constant);

                            if (filter.Operation.Equals("notcontains", StringComparison.OrdinalIgnoreCase))
                                expression = Expression.Not(expression);
                            break;
                        }
                    case "datein":
                    case "datenotin":
                        {
                            if (propertyBasicType != typeof(DateTime))
                                throw new FrameworkException("Generic filter operation '" + filter.Operation
                                    + "' is not supported for property type '" + propertyBasicType.Name + "'. Expected property type 'DateTime'.");

                            var match = DateRangeRegex.Match(filter.Value.ToString());
                            if (!match.Success)
                                throw new FrameworkException("Generic filter operation '" + filter.Operation + "' expects format 'yyyy-mm-dd', 'yyyy-mm' or 'yyyy'. Value '" + filter.Value + "' has invalid format.");

                            DateTime? date1, date2;
                            int year = int.Parse(match.Groups["y"].Value);
                            if (string.IsNullOrEmpty(match.Groups["m"].Value))
                            {
                                date1 = new DateTime(year, 1, 1);
                                date2 = date1.Value.AddYears(1);
                            }
                            else
                            {
                                int month = int.Parse(match.Groups["m"].Value);
                                if (string.IsNullOrEmpty(match.Groups["d"].Value))
                                {
                                    date1 = new DateTime(year, month, 1);
                                    date2 = date1.Value.AddMonths(1);
                                }
                                else
                                {
                                    int day = int.Parse(match.Groups["d"].Value);
                                    date1 = new DateTime(year, month, day);
                                    date2 = date1.Value.AddDays(1);
                                }
                            }

                            expression = Expression.AndAlso(
                                Expression.GreaterThanOrEqual(memberAccess, Expression.Constant(date1, typeof(DateTime?))),
                                Expression.LessThan(memberAccess, Expression.Constant(date2, typeof(DateTime?))));

                            if (filter.Operation.Equals("datenotin", StringComparison.OrdinalIgnoreCase))
                                expression = Expression.Not(expression);
                            break;
                        }
                    case "in":
                    case "notin":
                        {
                            Type collectionElement = GetElementType(constant.Type);
                            Expression convertedMemberAccess = memberAccess.Type != collectionElement
                                ? Expression.Convert(memberAccess, collectionElement)
                                : memberAccess;

                            Type collectionBasicType = typeof(IQueryable).IsAssignableFrom(constant.Type)
                                ? typeof(Queryable) : typeof(Enumerable);
                            var containsMethod = collectionBasicType.GetMethods()
                                .Single(m => m.Name == "Contains" && m.GetParameters().Count() == 2)
                                .MakeGenericMethod(collectionElement);

                            expression = EFExpression.OptimizeContains(Expression.Call(containsMethod, constant, convertedMemberAccess));

                            if (filter.Operation.Equals("notin", StringComparison.OrdinalIgnoreCase))
                                expression = Expression.Not(expression);
                            break;
                        }
                    default:
                        throw new FrameworkException("Unsupported generic filter operation '" + filter.Operation + "' on property (while generating expression).");
                }

                resultCondition = resultCondition != null ? Expression.AndAlso(resultCondition, expression) : expression;
            }

            return Expression.Lambda(resultCondition, parameter);
        }

        private static Type GetElementType(IEnumerable enumerable)
        {
            return GetElementType(enumerable.GetType());
        }

        private static Type GetElementType(Type enumerableType)
        {
            return enumerableType.GetInterface("IEnumerable`1").GenericTypeArguments.Single();
        }

        private static IList EmptyList(Type elementType)
        {
            return (IList)Activator.CreateInstance(
                typeof(List<>).MakeGenericType(new[] { elementType }),
                new object[] { 0 });
        }

        private static DateTime ParseJsonDateTime(string text, string infoPropertyName, Type infoPropertyType)
        {
            string dateString = "\"" + text.Replace("/", "\\/") + "\"";
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(dateString)))
            {
                var serializer = new DataContractJsonSerializer(typeof(DateTime));
                try
                {
                    return (DateTime)serializer.ReadObject(stream);
                }
                catch (SerializationException ex)
                {
                    throw new ClientException($"Invalid JSON format of {infoPropertyType.Name} property '{infoPropertyName}'. {ex.Message}", ex);
                }
            }
        }

        private static readonly Regex DateRangeRegex = new Regex(@"^(?<y>\d{4})(-(?<m>\d{1,2}))?(-(?<d>\d{1,2}))?$");

        private void AdjustListTypeNullable<TPropertyBasicType>(ref IEnumerable list, bool propertyIsNullableValueType)
            where TPropertyBasicType : struct
        {
            if (list is IQueryable<TPropertyBasicType> queryable)
            {
                if (propertyIsNullableValueType)
                    list = queryable.Cast<TPropertyBasicType?>();
            }
            else if (list is IEnumerable<TPropertyBasicType> enumerable)
            {
                if (propertyIsNullableValueType)
                    list = enumerable.Cast<TPropertyBasicType?>().ToList();
            }
            else if (list is IList<TPropertyBasicType?> listOfNullable) // This scenario is optional, but simplifies the resulting filter expression.
            {
                if (!propertyIsNullableValueType)
                    list = listOfNullable.Where(g => g.HasValue).Select(g => g.Value).ToList();
            }
        }

        #endregion
        //================================================================
        #region Sorting and paging

        public static IQueryable<T> SortAndPaginate<T>(IQueryable<T> query, ReadCommandInfo commandInfo)
        {
            bool pagingIsUsed = commandInfo.Top > 0 || commandInfo.Skip > 0;

            if (pagingIsUsed && (commandInfo.OrderByProperties == null || commandInfo.OrderByProperties.Length == 0))
                throw new ClientException("Invalid ReadCommand argument: Sort order must be set if paging is used (Top or Skip).");

            if (commandInfo.OrderByProperties != null)
                foreach (var order in commandInfo.OrderByProperties)
                    query = Sort(query, order.Property,
                        ascending: !order.Descending,
                        firstProperty: order == commandInfo.OrderByProperties.First());

            Type itemType = query.GetType().GetInterface("IQueryable`1").GetGenericArguments().Single();

            if (commandInfo.Skip > 0)
            {
                // "query.Skip(commandInfo.Skip)" would convert the result IQueryable<T> and not use the actual queryable generic type.
                var skipMethod = typeof(Queryable).GetMethod("Skip").MakeGenericMethod(itemType);
                query = (IQueryable<T>)skipMethod.InvokeEx(null, query, commandInfo.Skip);
            }

            if (commandInfo.Top > 0)
            {
                // "query.Take(commandInfo.Top)" would convert the result IQueryable<T> and not use the actual queryable generic type.
                var takeMethod = typeof(Queryable).GetMethod("Take").MakeGenericMethod(itemType);
                query = (IQueryable<T>)takeMethod.InvokeEx(null, query, commandInfo.Top);
            }

            return query;
        }

        public static IQueryable<T> Sort<T>(IQueryable<T> source, string orderByProperty, bool ascending = true, bool firstProperty = false)
        {
            if (string.IsNullOrEmpty(orderByProperty))
                return source;

            Type itemType = source.GetType().GetInterface("IQueryable`1").GetGenericArguments().Single();

            ParameterExpression parameter = Expression.Parameter(itemType, "posting");
            Expression property = Expression.Property(parameter, orderByProperty);
            LambdaExpression propertySelector = Expression.Lambda(property, new[] { parameter });

            MethodInfo orderMethod = firstProperty
                    ? (ascending ? OrderByAscendingMethod : OrderByDescendingMethod)
                    : (ascending ? ThenByAscendingMethod : ThenByDescendingMethod);
            MethodInfo genericOrderMethod = orderMethod.MakeGenericMethod(new[] { itemType, property.Type });

            return (IQueryable<T>)genericOrderMethod.InvokeEx(null, source, propertySelector);
        }

        private static readonly MethodInfo OrderByAscendingMethod =
            typeof(Queryable).GetMethods()
                .Where(method => method.Name == "OrderBy")
                .Single(method => method.GetParameters().Length == 2);

        private static readonly MethodInfo OrderByDescendingMethod =
            typeof(Queryable).GetMethods()
                .Where(method => method.Name == "OrderByDescending")
                .Single(method => method.GetParameters().Length == 2);

        private static readonly MethodInfo ThenByAscendingMethod =
            typeof(Queryable).GetMethods()
                .Where(method => method.Name == "ThenBy")
                .Single(method => method.GetParameters().Length == 2);

        private static readonly MethodInfo ThenByDescendingMethod =
            typeof(Queryable).GetMethods()
                .Where(method => method.Name == "ThenByDescending")
                .Single(method => method.GetParameters().Length == 2);

        #endregion
        //================================================================
        #region Generic filters (FilterCriteria)

        public const string FilterOperationMatches = "Matches";
        public const string FilterOperationNotMatches = "NotMatches";

        private void ValidateAndPrepare(FilterCriteria filter)
        {
            if (!string.IsNullOrEmpty(filter.Property) && !string.IsNullOrEmpty(filter.Filter))
                throw new ClientException("Invalid generic filter criteria: both property filter and predefined filter are set. (Property = '" + filter.Property + "', Filter = '" + filter.Filter + "')");

            if (string.IsNullOrEmpty(filter.Property) && string.IsNullOrEmpty(filter.Filter))
                throw new ClientException("Invalid generic filter criteria: both property filter and predefined filter are null.");

            if (!string.IsNullOrEmpty(filter.Property))
            {
                // Property filter:

                if (string.IsNullOrEmpty(filter.Operation))
                    throw new ClientException("Invalid generic filter criteria: Operation is not set. (Property = '" + filter.Property + "')");
            }

            if (!string.IsNullOrEmpty(filter.Filter))
            {
                // Specific filter:

                if (string.IsNullOrEmpty(filter.Operation))
                    filter.Operation = FilterOperationMatches;

                if (!string.Equals(filter.Operation, FilterOperationMatches, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(filter.Operation, FilterOperationNotMatches, StringComparison.OrdinalIgnoreCase))
                    throw new ClientException(string.Format(
                        "Invalid generic filter criteria: Supported predefined filter operations are '{0}' and '{1}'. (Filter = '{2}', Operation = '{3}')",
                        FilterOperationMatches, FilterOperationNotMatches, filter.Filter, filter.Operation));
            }
        }

        private Type GetSpecificFilterType(string filterName)
        {
            if (string.IsNullOrEmpty(filterName))
                throw new ArgumentNullException("filterName");

            Type filterType = _domainObjectModel.GetType(filterName) ?? Type.GetType(filterName);

            if (filterType == null)
                throw new ClientException(string.Format("Unknown filter type '{0}'.", filterName));

            return filterType;
        }

        public class FilterObject
        {
            public Type FilterType;
            public object Parameter;
        }

        public IList<FilterObject> ToFilterObjects(IEnumerable<FilterCriteria> genericFilter)
        {
            CsUtility.Materialize(ref genericFilter);

            foreach (var filter in genericFilter)
                ValidateAndPrepare(filter);

            var filterObjects = new List<FilterObject>(genericFilter.Count());

            bool handledPropertyFilter = false;

            foreach (var filter in genericFilter)
            {
                if (IsPropertyFilter(filter))
                {
                    if (!handledPropertyFilter)
                    {
                        handledPropertyFilter = true;
                        filterObjects.Add(CombinePropertyFilters(genericFilter));
                    }
                }
                else
                {
                    var filterObject = new FilterObject
                    {
                        FilterType = GetSpecificFilterType(filter.Filter),
                        Parameter = filter.Value
                    };

                    if (string.Equals(filter.Operation, FilterOperationNotMatches, StringComparison.OrdinalIgnoreCase))
                    {
                        if (ReflectionHelper<IEntity>.GetPredicateExpressionParameter(filterObject.FilterType) != null)
                            filterObject.Parameter = Not((LambdaExpression)filterObject.Parameter);
                        else
                            throw new FrameworkException(FilterOperationNotMatches + " operation is only allowed on a filter expression: Expression<Func<T, bool>>.");
                    }

                    filterObjects.Add(filterObject);
                }
            }

            return filterObjects;
        }

        private LambdaExpression Not(LambdaExpression expression)
        {
            return Expression.Lambda(Expression.Not(expression.Body), expression.Parameters);
        }

        private bool IsPropertyFilter(FilterCriteria filter)
        {
            return !string.IsNullOrEmpty(filter.Property);
        }

        private FilterObject CombinePropertyFilters(IEnumerable<FilterCriteria> genericFilter)
        {
            var propertyFilters = genericFilter.Where(IsPropertyFilter)
                .Select(fc => new PropertyFilter { Property = fc.Property, Operation = fc.Operation, Value = fc.Value })
                .ToList();

            return new FilterObject
            {
                Parameter = propertyFilters,
                FilterType = propertyFilters.GetType()
            };
        }

        /// <summary>
        /// Compares only filters without parameters.
        /// </summary>
        public static bool EqualsSimpleFilter(FilterCriteria filter, string filterName)
        {
            return filter.Filter == filterName
                && filter.Value == null
                && (string.IsNullOrEmpty(filter.Operation)
                    || string.Equals(filter.Operation, GenericFilterHelper.FilterOperationMatches, StringComparison.OrdinalIgnoreCase));
        }

        #endregion
    }
}
