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

using Rhetos.Logging;
using Rhetos.Processing.DefaultCommands;
using Rhetos.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace Rhetos.Dom.DefaultConcepts
{
    public class GenericFilterHelper
    {
        private readonly IDataStructureReadParameters _dataStructureReadParameters;
        private readonly IOrmUtility _ormUtility;
        private readonly ILogger _logger;

        public GenericFilterHelper(
            IDataStructureReadParameters dataStructureReadParameters,
            ILogProvider logProvider,
            IOrmUtility ormUtility)
        {
            _dataStructureReadParameters = dataStructureReadParameters;
            _ormUtility = ormUtility;
            _logger = logProvider.GetLogger(GetType().Name);
        }

        //================================================================
        #region Property filters

        public Expression<Func<T, bool>> ToExpression<T>(IEnumerable<PropertyFilter> propertyFilters)
        {
            return (Expression<Func<T, bool>>)ToExpression(propertyFilters, typeof(T));
        }

        public LambdaExpression ToExpression(IEnumerable<PropertyFilter> propertyFilters, Type parameterType)
        {
            var propertyFiltersCollection = CsUtility.Materialized(propertyFilters);

            ParameterExpression parameter = Expression.Parameter(parameterType, "p");

            if (propertyFiltersCollection == null || propertyFiltersCollection.Count == 0)
                return Expression.Lambda(Expression.Constant(true), parameter);

            Expression resultCondition = null;

            foreach (var filter in propertyFiltersCollection)
            {
                if (string.IsNullOrEmpty(filter.Property))
                    continue;

                Expression memberAccess = null;
                foreach (var property in filter.Property.Split('.'))
                {
                    var parentExpression = memberAccess ?? (Expression)parameter;
                    if (parentExpression.Type.GetProperty(property) == null)
                        throw new ClientException($"Invalid generic filter parameter: Type '{parentExpression.Type}' does not have property '{property}'.");
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
                    if (filter.Value == null || propertyBasicType.IsInstanceOfType(filter.Value))
                        convertedValue = filter.Value;

                    // Guid object's type was not automatically recognized when deserializing from JSON:
                    else if (propertyBasicType == typeof(Guid) && filter.Value is string) // TODO: Remove this as a breaking change in next major release.
                        convertedValue = Guid.Parse(filter.Value.ToString());

                    // DateTime object's type was not automatically recognized when deserializing from JSON:
                    else if (propertyBasicType == typeof(DateTime) && filter.Value is string) // TODO: Remove this as a breaking change in next major release.
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
                    if (propertyBasicType == typeof(Guid) && list is IEnumerable<string>) // TODO: Remove this as a breaking change in next major release.
                        list = ((IEnumerable<string>)list).Select(s => !string.IsNullOrEmpty(s) ? (Guid?)Guid.Parse(s) : null).ToList();

                    // DateTime object's type was not automatically recognized when deserializing from JSON:
                    if (propertyBasicType == typeof(DateTime) && list is IEnumerable<string>) // TODO: Remove this as a breaking change in next major release.
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

                static Expression ConvertConstantToParameterGuid(Guid value)
                {
                    Expression<Func<Guid>> idLambda = () => value;
                    return idLambda.Body;
                }

                static Expression ConvertConstantToParameterGuidNullable(Guid value)
                {
                    Expression<Func<Guid?>> idLambda = () => value;
                    return idLambda.Body;
                }

                Expression expression;
                switch (filter.Operation.ToLowerInvariant())
                {
                    case "equals":
                    case "equal":
                        if (propertyBasicType == typeof(Guid) && constant.Value is Guid constantIdEquals)
                        {
                            // Using a different expression instead of the constant, to force Entity Framework to
                            // use query parameter instead of hardcoding the constant value (literal) into the generated query.
                            // Query with parameter will allow cache reuse for both EF LINQ compiler and database SQL compiler.
                            Expression guidParameter = memberAccess.Type == typeof(Guid?)
                                ? ConvertConstantToParameterGuidNullable(constantIdEquals)
                                : ConvertConstantToParameterGuid(constantIdEquals);
                            expression = Expression.Equal(memberAccess, guidParameter);
                        }
                        else if (propertyBasicType == typeof(string) && constant.Value != null)
                            expression = Expression.Call(_ormUtility.EqualsCaseInsensitiveMethod, memberAccess, constant);
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
                            Expression guidParameter = memberAccess.Type == typeof(Guid?)
                                ? ConvertConstantToParameterGuidNullable(constantIdNotEquals)
                                : ConvertConstantToParameterGuid(constantIdNotEquals);
                            expression = Expression.NotEqual(memberAccess, guidParameter);
                        }
                        else if (propertyBasicType == typeof(string) && constant.Value != null)
                            expression = Expression.Call(_ormUtility.NotEqualsCaseInsensitiveMethod, memberAccess, constant);
                        else
                            expression = Expression.NotEqual(memberAccess, constant);
                        break;
                    case "greater":
                        if (propertyBasicType == typeof(string))
                            expression = Expression.Call(_ormUtility.IsGreaterThanMethod, memberAccess, constant);
                        else if (propertyBasicType == typeof(Guid))
                            expression = Expression.Call(_ormUtility.GuidIsGreaterThanMethod, memberAccess, constant);
                        else expression = Expression.GreaterThan(memberAccess, constant);
                        break;
                    case "greaterequal":
                        if (propertyBasicType == typeof(string))
                            expression = Expression.Call(_ormUtility.IsGreaterThanOrEqualMethod, memberAccess, constant);
                        else if (propertyBasicType == typeof(Guid))
                            expression = Expression.Call(_ormUtility.GuidIsGreaterThanOrEqualMethod, memberAccess, constant);
                        else expression = Expression.GreaterThanOrEqual(memberAccess, constant);
                        break;
                    case "less":
                        if (propertyBasicType == typeof(string))
                            expression = Expression.Call(_ormUtility.IsLessThanMethod, memberAccess, constant);
                        else if (propertyBasicType == typeof(Guid))
                            expression = Expression.Call(_ormUtility.GuidIsLessThanMethod, memberAccess, constant);
                        else expression = Expression.LessThan(memberAccess, constant);
                        break;
                    case "lessequal":
                        if (propertyBasicType == typeof(string))
                            expression = Expression.Call(_ormUtility.IsLessThanOrEqualMethod, memberAccess, constant);
                        else if (propertyBasicType == typeof(Guid))
                            expression = Expression.Call(_ormUtility.GuidIsLessThanOrEqualMethod, memberAccess, constant);
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
                                if (propertyBasicType != typeof(int))
                                    throw new FrameworkException("Generic filter operation '" + filter.Operation + "' is not supported on property type '" + propertyBasicType.Name + "'.");
                                stringMember = Expression.Call(_ormUtility.CastToStringMethod, memberAccess);
                            }
                            var dbMethod = filter.Operation.Equals("startswith", StringComparison.OrdinalIgnoreCase)
                                ? _ormUtility.StartsWithCaseInsensitiveMethod
                                : _ormUtility.EndsWithCaseInsensitiveMethod;
                            expression = Expression.Call(dbMethod, stringMember, constant);
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
                                if (propertyBasicType != typeof(int))
                                    throw new FrameworkException("Generic filter operation '" + filter.Operation + "' is not supported on property type '" + propertyBasicType.Name + "'.");
                                stringMember = Expression.Call(_ormUtility.CastToStringMethod, memberAccess);
                            }
                            expression = Expression.Call(_ormUtility.ContainsCaseInsensitiveMethod, stringMember, constant);

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
                                .Single(m => m.Name == "Contains" && m.GetParameters().Length == 2)
                                .MakeGenericMethod(collectionElement);

                            expression = _ormUtility.OptimizeContains(Expression.Call(containsMethod, _ormUtility.CreateContainsItemsExpression(constant.Value), convertedMemberAccess));

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
                    throw new ClientException($"Invalid JSON format of {infoPropertyType.Name} property '{infoPropertyName}'. See server log for more information.", ex);
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
                    list = queryable.Select(element => (TPropertyBasicType?)element);
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

            if (commandInfo.Skip > 0 || commandInfo.Top > 0)
            {
                // Using MakeGenericMethod with 'itemType' instead of calling with 'T' directly, because Skip<T> or Take<T>
                // would convert the result IQueryable<T> and not use the actual queryable generic type 'itemType'.
                var skipAndTakeMethod = typeof(GenericFilterHelper).GetMethod("SkipAndTake").MakeGenericMethod(itemType);
                query = (IQueryable<T>)skipAndTakeMethod.InvokeEx(null, query, commandInfo.Skip, commandInfo.Top);
            }
            return query;
        }

        public static IQueryable<TItem> SkipAndTake<TItem>(IQueryable<TItem> query, int skip, int take)
        {
            if (skip > 0)
                query = query.Skip(skip);
            if (take > 0)
                query = query.Take(take);
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
                throw new ClientException($"Invalid generic filter criteria: both property filter and predefined filter are set. (Property = '{filter.Property}', Filter = '{filter.Filter}')");

            if (string.IsNullOrEmpty(filter.Property) && string.IsNullOrEmpty(filter.Filter) && filter.Value == null)
                throw new ClientException("Invalid generic filter criteria: Property, Filter and Value are null.");

            if (!string.IsNullOrEmpty(filter.Property))
            {
                // Property filter:

                if (string.IsNullOrEmpty(filter.Operation))
                    throw new ClientException("Invalid generic filter criteria: Operation is not set. (Property = '" + filter.Property + "')");
            }
            else
            {
                // Specific filter:

                if (!string.IsNullOrEmpty(filter.Operation)
                    && !string.Equals(filter.Operation, FilterOperationMatches, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(filter.Operation, FilterOperationNotMatches, StringComparison.OrdinalIgnoreCase))
                    throw new ClientException($"Invalid generic filter criteria:" +
                        $" Supported predefined filter operations are '{FilterOperationMatches}' and '{FilterOperationNotMatches}'." +
                        $" (Filter = '{filter.Filter ?? filter.Value.GetType().ToString()}', Operation = '{filter.Operation}')");
            }
        }

        public Type GetFilterType(string dataStructureFullName, string filterName, object filterInstance = null)
        {
            if (string.IsNullOrEmpty(filterName))
                throw new ArgumentNullException(nameof(filterName));

            var supportedTypes = _dataStructureReadParameters.GetReadParameters(dataStructureFullName, true);

            // Exact type name provided:
            {
                List<Type> matchingTypes = supportedTypes
                    .Where(filter => filter.Name.Equals(filterName, StringComparison.Ordinal))
                    .Select(filter => filter.Type)
                    .Distinct().ToList();

                if (matchingTypes.Count > 1)
                    throw new ClientException($"Filter type '{filterName}' on '{dataStructureFullName}' is ambiguous" +
                        $" ({matchingTypes.First()}, {matchingTypes.Last()}). Please specify exact filter name.");

                if (matchingTypes.Count == 1)
                    return matchingTypes.Single();
            }

            // Runtime type from provided instance, if assignable to any supported filter type:
            if (filterInstance != null)
            {
                var baseTypes = supportedTypes.Select(filter => filter.Type)
                    .Distinct()
                    .Where(type => type.IsInstanceOfType(filterInstance))
                    .ToList();

                if (baseTypes.Count > 1)
                    throw new ClientException($"Filter type '{filterName}' with instance type '{filterInstance.GetType()}' on '{dataStructureFullName}' is ambiguous" +
                        $" ({baseTypes.First()}, {baseTypes.Last()}). Please specify exact filter name.");

                if (baseTypes.Count == 1)
                    return baseTypes.Single();
            }

            _logger.Warning(() =>
                $"Filter type '{filterName}' is not available."
                + $" Supported parameter types on '{dataStructureFullName}' are: {SupportedTypesReport(dataStructureFullName)}.");
            throw new ClientException(
                $"Filter type '{filterName}' is not available."
                + $" See server log for information on supported types. ({_logger.Name}, {DateTime.Now:s})");
        }

        private string SupportedTypesReport(string dataStructureFullName)
        {
            return string.Join(", ", _dataStructureReadParameters
                .GetReadParameters(dataStructureFullName, true)
                .OrderBy(p => p.Name)
                .Select(p => $"'{p.Name}'"));
        }

        public class FilterObject
        {
            public Type FilterType { get; set; }
            public object Parameter { get; set; }
        }

        public IList<FilterObject> ToFilterObjects(string dataStructureFullName, IEnumerable<FilterCriteria> genericFilter)
        {
            var genericFilterCollection = CsUtility.Materialized(genericFilter);

            foreach (var filter in genericFilterCollection)
                ValidateAndPrepare(filter);

            var filterObjects = new List<FilterObject>(genericFilterCollection.Count);

            bool handledPropertyFilter = false;

            foreach (var filter in genericFilterCollection)
            {
                if (IsPropertyFilter(filter))
                {
                    if (!handledPropertyFilter)
                    {
                        handledPropertyFilter = true;
                        filterObjects.Add(CombinePropertyFilters(genericFilterCollection));
                    }
                }
                else
                {
                    var filterObject = new FilterObject
                    {
                        FilterType = !string.IsNullOrEmpty(filter.Filter)
                            ? GetFilterType(dataStructureFullName, filter.Filter, filter.Value)
                            : filter.Value.GetType(),
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
        /// Note: This method will not detected same filter types if they differ in namespace usage, since <see cref="FilterCriteria.Filter"/>
        /// should have full namespace declared as provided by <see cref="Type.ToString()"/>.
        /// </summary>
        public static bool EqualsSimpleFilter(FilterCriteria filter, string filterName)
        {
            return filter.Value == null
                && (string.IsNullOrEmpty(filter.Operation) || string.Equals(filter.Operation, FilterOperationMatches, StringComparison.OrdinalIgnoreCase))
                && IsSameType(filter.Filter, filterName);
        }

        private static bool IsSameType(string t1, string t2)
        {
            return !string.IsNullOrEmpty(t1) && !string.IsNullOrEmpty(t2) &&
                (t1 == t2 || IsShortenedType(t1, t2) || IsShortenedType(t2, t1));
        }

        private static bool IsShortenedType(string shorter, string longer)
        {
            return longer.StartsWith(shorter) && longer.Length > shorter.Length && longer[shorter.Length] == ',';
        }

        #endregion
    }
}
