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

using Rhetos.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Rhetos.Dom.DefaultConcepts
{
    /// <summary>
    /// FilterCriteria is a generic filter's element that defines a filter by property value or a predefined filter call.
    /// 
    /// If a data structure's repository implements a Query, Load or Filter function with IEnumerable&lt;FilterCriteria&gt; argument,
    /// it will be used when reading data by GenericRepository or a server command.
    /// </summary>
    public class FilterCriteria
    {
        /// <summary>
        /// Property name.
        /// </summary>
        /// <remarks>
        /// Either "Property" or "Filter" member should be set.
        /// </remarks>
        public string Property { get; set; }

        /// <summary>
        /// Predefined filter name (filter type).
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property should contain the filter name as specified in the DSL model, or a type name as provided by <see cref="Type.ToString()"/> (recommended).
        /// Namespace is optional in some cases if the filter type is implemented in the same module, or in a default namespace,
        /// but using the complete type name is recommended.
        /// </para>
        /// <para>
        /// Either <see cref="Property"/> or <see cref="Filter"/> member should be set in <see cref="FilterCriteria"/>.
        /// </para>
        /// See <see cref="IDataStructureReadParameters"/> and <see cref="CommonConceptsRuntimeOptions.DynamicTypeResolution"/>
        /// for more info on supported types implementation and usage.
        /// </remarks>
        public string Filter { get; set; }

        /// <summary>
        /// Optional when Filter is set.
        /// </summary>
        public string Operation { get; set; }

        /// <summary>
        /// Filter parameter.
        /// Optional when Filter is set.
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Create a property filter.
        /// </summary>
        public FilterCriteria(string property, string operation, object value)
        {
            Property = property;
            Operation = operation;
            Value = value;
        }

        /// <summary>
        /// Create a predefined filter, specified by filter type, without parameters.
        /// </summary>
        public FilterCriteria(Type filterType)
        {
            Filter = filterType.ToString();
        }

        /// <summary>
        /// Create a predefined filter, specified by an instance of filter parameters.
        /// </summary>
        public FilterCriteria(object filterValue)
        {
            Filter = filterValue.GetType().ToString();
            Value = filterValue;
        }

        public static FilterCriteria FilterValue<T>(T filterValue)
        {
            return new FilterCriteria
            {
                Filter = typeof(T).ToString(),
                Value = filterValue,
            };
        }

        public FilterCriteria()
        {
        }

        public string Summary()
        {
            var valueDescription = ValueDescription(Value);

            return (FilterDescription(Filter) ?? (Property + " " + Operation))
                + (valueDescription != null ? " " + valueDescription : "");
        }

        public override string ToString() => Summary();

        public string FilterDescription(string filter)
        {
            if (filter == null)
                return null;
            Type filterType = null;
            try
            {
                filterType = Type.GetType(filter, throwOnError: false);
            }
            catch
            {
                // Trying to get the filter type just to improve the reported description.
                // Any errors resolving the type can be ignored.
                // Note: Even when throwOnError is false in Type.GetType, some exceptions are thrown.
            }
            if (filterType != null)
                return filterType.ToString();
            return filter;
        }

        public string ValueDescription(object value)
        {
            if (value == null)
                return null;

            string report;

            if (value is IList list)
            {
                report = list.Count + " items";
                if (list.Count >= 1)
                    report += ": " + SingleValueDescription(list[0]);
                if (list.Count >= 2)
                    report += " ...";
            }
            else
                report = SingleValueDescription(value);

            return "\"" + report.Limit(100, "...") + "\"";
        }

        public string SingleValueDescription(object value)
        {
            if (value == null)
                return "null";
            if (value.GetType().IsValueType || value is string)
                return value.ToString();
            if (value is IEntity entity)
                return "ID " + entity.ID.ToString();
            return "...";
        }
    }
}