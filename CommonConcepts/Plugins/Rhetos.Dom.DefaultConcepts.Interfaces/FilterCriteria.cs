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
        /// Either "Property" or "Filter" member should be set.
        /// </summary>
        public string Property { get; set; }

        /// <summary>
        /// Predefined filter name (filter type). May be a data structure name form DSL script (Common.Principal, e.g.).
        /// Either "Property" or "Filter" member should be set.
        /// </summary>
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
        /// Create a predefined filter.
        /// </summary>
        public FilterCriteria(Type filterType)
        {
            Filter = filterType.FullName;
        }

        /// <summary>
        /// Create a predefined filter.
        /// </summary>
        public FilterCriteria(object filterValue)
        {
            Filter = filterValue.GetType().FullName;
            Value = filterValue;
        }

        public FilterCriteria()
        {
        }

        public override string ToString()
        {
            string valueShortInfo = Value != null ? "..." : null;

            var guidList = Value as IList<Guid>;
            if (guidList != null && guidList.Count == 1)
                valueShortInfo = guidList[0].ToString();

            return (Filter ?? (Property + " " + Operation))
                + (valueShortInfo != null ? " " + valueShortInfo : "");
        }
    }
}