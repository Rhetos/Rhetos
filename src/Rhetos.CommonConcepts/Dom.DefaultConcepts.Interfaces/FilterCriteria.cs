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
using System.Text;

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
        /// Property name for generic property filter.
        /// </summary>
        /// <remarks>
        /// <see cref="Property"/> and <see cref="Filter"/> members should not be both set.
        /// </remarks>
        public string Property { get; set; }

        /// <summary>
        /// Name of a specific filter type.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property should contain the filter name as specified in the DSL model, or a type name as provided by <see cref="Type.ToString()"/>.
        /// Namespace is optional in some cases if the filter type is implemented in the same module, or in a default namespace,
        /// but using the complete type name is recommended.
        /// </para>
        /// <para>
        /// <see cref="Property"/> and <see cref="Filter"/> members should not be both set.
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
        /// Filter parameter value.
        /// Optional when Filter is set.
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Create a generic property filter.
        /// </summary>
        public FilterCriteria(string property, string operation, object value)
        {
            ArgumentNullException.ThrowIfNull(property);
            ArgumentNullException.ThrowIfNull(operation);
            Property = property;
            Operation = operation;
            Value = value;
        }

        /// <summary>
        /// Create a predefined filter, specified by the filter type, without parameters.
        /// </summary>
        public FilterCriteria(Type filterType)
        {
            ArgumentNullException.ThrowIfNull(filterType);
            Filter = filterType.ToString();
        }

        /// <summary>
        /// Create a predefined filter, specified by the filter parameter value.
        /// </summary>
        /// <remarks>
        /// For more strict behavior, specify the exact filter type with constructor <see cref="FilterCriteria(Type, object)"/>.
        /// </remarks>
        public FilterCriteria(object filterValue)
        {
            ArgumentNullException.ThrowIfNull(filterValue);
            Value = filterValue;
        }

        /// <summary>
        /// Create a predefined filter, specified by the filter type, with a parameter value.
        /// </summary>
        /// <remarks>
        /// If using FilterCriteria to execute methods other then DSL filters, and the parameter is not provided by an external API,
        /// specify the filter by parameter only, with constructor <see cref="FilterCriteria(object)"/>.
        /// </remarks>
        public FilterCriteria(Type filterType, object filterValue)
        {
            ArgumentNullException.ThrowIfNull(filterType);
            if (filterValue != null && !filterType.IsInstanceOfType(filterValue))
                throw new ArgumentException($"Provided {nameof(filterValue)} is not an instance of the {nameof(filterType)} '{filterType}'.");
            Filter = filterType.ToString();
            Value = filterValue;
        }

        public FilterCriteria()
        {
        }

        public string Summary()
        {
            var result = new StringBuilder();

            if (!string.IsNullOrEmpty(Property))
                result.Append(Property);
            else if (!string.IsNullOrEmpty(Filter))
                result.Append(Filter);
            else if (Value != null)
                result.Append(Value.GetType().ToString());
            else
                result.Append("<null>");

            if (!string.IsNullOrEmpty(Operation))
                result.Append(' ').Append(Operation);

            if (Value != null)
                result.Append(' ').Append(ValueDescription(Value));

            return result.ToString();
        }

        public override string ToString() => Summary();

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