﻿/*
    Copyright (C) 2013 Omega software d.o.o.

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

namespace Rhetos.Dom.DefaultConcepts
{
    public static class GenericFilterWithPagingUtility // TODO: Better unit tests
    {
        public static Expression<Func<T, bool>> ToExpression<T>(IEnumerable<FilterCriteria> filterCriterias)
        {
            if (filterCriterias == null || filterCriterias.Count() == 0)
                return (t => true);

            Expression resultCondition = null;

            // Create a member expression pointing to given type
            ParameterExpression parameter = Expression.Parameter(typeof(T), "p");

            foreach (var criteria in filterCriterias)
            {
                if (string.IsNullOrEmpty(criteria.Property))
                    continue;

                MemberExpression memberAccess = null;
                foreach (var property in criteria.Property.Split('.'))
                    memberAccess = Expression.Property(memberAccess ?? (Expression)parameter, property);

                // Change the type of the parameter 'value'. it is necessary for comparisons (specially for booleans)
                Type basicType = (memberAccess.Type.IsGenericType && memberAccess.Type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    ? memberAccess.Type.GetGenericArguments().Single() : memberAccess.Type;

                ConstantExpression constant;
                if (new[] { "equal", "notequal", "greater", "greaterequal", "less", "lessequal" }.Contains(criteria.Operation.ToLower()))
                {
                    // Constant value should be of same type as the member it is compared to.

                    object convertedValue;
                    if (criteria.Value == null || basicType.IsAssignableFrom(criteria.Value.GetType()))
                        convertedValue = criteria.Value;
                    else if (basicType == typeof(Guid) && criteria.Value is string) // Guid object's type was not automatically recognized when deserializing from JSON.
                        convertedValue = Guid.Parse(criteria.Value.ToString());
                    else if (basicType == typeof(DateTime) && criteria.Value is string) // DateTime object's type was not automatically recognized when deserializing from JSON.
                    {
                        string dateString = "\"" + ((string)criteria.Value).Replace("/", "\\/") + "\"";
                        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(dateString)))
                        {
                            var serializer = new DataContractJsonSerializer(typeof(DateTime));
                            try
                            {
                                convertedValue = (DateTime)serializer.ReadObject(stream);
                            }
                            catch (SerializationException ex)
                            {
                                throw new UserException("Invalid JSON format of " + basicType.Name + " propery '" + criteria.Property + "'. " + ex.Message, ex);
                            }
                        }
                    }
                    else if ((basicType == typeof(decimal) || basicType == typeof(int)) && criteria.Value is string)
                        throw new FrameworkException("Invalid JSON format of " + basicType.Name + " propery '" + criteria.Property + "'. Numeric value should not be passed as a string in JSON serialized object.");
                    else
                        convertedValue = Convert.ChangeType(criteria.Value, basicType);

                    constant = Expression.Constant(convertedValue, memberAccess.Type);
                }
                else if (new[] { "startswith", "contains" }.Contains(criteria.Operation.ToLower()))
                {
                    // Constant value should be string.
                    constant = Expression.Constant(criteria.Value.ToString(), typeof(string));
                }
                else if (new[] { "datein" }.Contains(criteria.Operation.ToLower()))
                {
                    constant = null;
                }
                else
                    throw new FrameworkException("Unsupported generic filter operation '" + criteria.Operation + "'.");

                Expression expression;
                switch (criteria.Operation.ToLower())
                {
                    case "equal":
                        expression = Expression.Equal(memberAccess, constant);
                        break;
                    case "notequal":
                        expression = Expression.NotEqual(memberAccess, constant);
                        break;
                    case "greater":
                        if (basicType == typeof(string))
                            expression = Expression.Not(Expression.Call(typeof(DatabaseExtensionFunctions).GetMethod("IsLessThenOrEqual"), memberAccess, constant));
                        else
                            expression = Expression.GreaterThan(memberAccess, constant);
                        break;
                    case "greaterequal":
                        if (basicType == typeof(string))
                            expression = Expression.Not(Expression.Call(typeof(DatabaseExtensionFunctions).GetMethod("IsLessThen"), memberAccess, constant));
                        else
                            expression = Expression.GreaterThanOrEqual(memberAccess, constant);
                        break;
                    case "less":
                        if (basicType == typeof(string))
                            expression = Expression.Call(typeof(DatabaseExtensionFunctions).GetMethod("IsLessThen"), memberAccess, constant);
                        else
                            expression = Expression.LessThan(memberAccess, constant);
                        break;
                    case "lessequal":
                        if (basicType == typeof(string))
                            expression = Expression.Call(typeof(DatabaseExtensionFunctions).GetMethod("IsLessThenOrEqual"), memberAccess, constant);
                        else
                            expression = Expression.LessThanOrEqual(memberAccess, constant);
                        break;
                    case "startswith":
                        {
                            Expression stringMember;
                            if (basicType == typeof(string))
                                stringMember = memberAccess;
                            else
                            {
                                var castMethod = typeof(DatabaseExtensionFunctions).GetMethod("CastToString", new[] { memberAccess.Type });
                                if (castMethod == null)
                                    throw new FrameworkException("Generic filter operation '" + criteria.Operation + "' is not supported on property type '" + basicType.Name + "'. There is no overload of 'DatabaseExtensionFunctions.CastToString' function for the type.");
                                stringMember = Expression.Call(castMethod, memberAccess);
                            }
                            expression = Expression.Call(stringMember, typeof(String).GetMethod("StartsWith", new[] { typeof(string) }), constant);
                            break;
                        }
                    case "contains":
                        {
                            Expression stringMember;
                            if (basicType == typeof(string))
                                stringMember = memberAccess;
                            else
                            {
                                var castMethod = typeof(DatabaseExtensionFunctions).GetMethod("CastToString", new[] { memberAccess.Type });
                                if (castMethod == null)
                                    throw new FrameworkException("Generic filter operation '" + criteria.Operation + "' is not supported on property type '" + basicType.Name + "'. There is no overload of 'DatabaseExtensionFunctions.CastToString' function for the type.");
                                stringMember = Expression.Call(castMethod, memberAccess);
                            }
                            expression = Expression.Call(stringMember, typeof(String).GetMethod("Contains", new[] { typeof(string) }), constant);
                            break;
                        }
                    case "datein":
                        {
                            if (basicType != typeof(DateTime))
                                throw new FrameworkException("Generic filter operation '" + criteria.Operation
                                    + "' is not supported for property type '" + basicType.Name + "'. Expected property type 'DateTime'.");

                            var match = DateRangeRegex.Match(criteria.Value.ToString());
                            if (!match.Success)
                                throw new FrameworkException("Generic filter operation '" + criteria.Operation + "' expects format 'yyyy-mm-dd', 'yyyy-mm' or 'yyyy'. Value '" + criteria.Value + "' has invalid format.");

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
                            break;
                        }
                    default:
                        throw new FrameworkException("Unsupported generic filter operation '" + criteria.Operation + "' while generating expression.");
                }

                resultCondition = resultCondition != null ? Expression.AndAlso(resultCondition, expression) : expression;
            }

            return Expression.Lambda<Func<T, bool>>(resultCondition, parameter);
        }

        private static readonly Regex DateRangeRegex = new Regex(@"^(?<y>\d{4})(-(?<m>\d{1,2}))?(-(?<d>\d{1,2}))?$");

        public static IQueryable<T> Filter<T>(IQueryable<T> query, IEnumerable<FilterCriteria> filterCriterias)
        {
            return query.Where(ToExpression<T>(filterCriterias));
        }

        public static IQueryable<T> SortAndPaginate<T>(IQueryable<T> query, QueryDataSourceCommandInfo parameters, ref int totalRecords)
        {
            if (string.IsNullOrEmpty(parameters.OrderByProperty) && parameters.PageNumber > 0 && parameters.RecordsPerPage > 0)
                throw new ArgumentException("OrderByProperty must be set when paging is used in QueryDataSourceCommand.");

            if (!string.IsNullOrEmpty(parameters.OrderByProperty))
                query = Sort(query, parameters.OrderByProperty, !parameters.OrderDescending);

            if (parameters.PageNumber > 0 && parameters.RecordsPerPage > 0)
            {
                totalRecords = query.Count();
                query = query.Skip((parameters.PageNumber - 1) * parameters.RecordsPerPage).Take(parameters.RecordsPerPage);
            }

            return query;
        }

        private static IQueryable<T> Sort<T>(IQueryable<T> source, string orderByProperty, bool ascending = true)
        {
            if (string.IsNullOrEmpty(orderByProperty))
                return source;

            ParameterExpression parameter = Expression.Parameter(typeof(T), "posting");
            Expression property = Expression.Property(parameter, orderByProperty);
            LambdaExpression propertySelector = Expression.Lambda(property, new[] { parameter });

            MethodInfo orderMethod = ascending ? OrderByAscendingMethod : OrderByDescendingMethod;
            MethodInfo genericOrderMethod = orderMethod.MakeGenericMethod(new[] { typeof(T), property.Type });

            return (IQueryable<T>)genericOrderMethod.Invoke(null, new[] { (object)source, propertySelector });
        }

        private static readonly MethodInfo OrderByAscendingMethod =
            typeof(Queryable).GetMethods()
                .Where(method => method.Name == "OrderBy")
                .Where(method => method.GetParameters().Length == 2)
                .Single();

        private static readonly MethodInfo OrderByDescendingMethod =
            typeof(Queryable).GetMethods()
                .Where(method => method.Name == "OrderByDescending")
                .Where(method => method.GetParameters().Length == 2)
                .Single();
    }
}
