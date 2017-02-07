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
using System.Text;
using System.ComponentModel.Composition;
using Rhetos.Dom.DefaultConcepts;

namespace Rhetos.Processing.DefaultCommands
{
    [Obsolete("Use ReadCommand")]
    [Export(typeof(ICommandInfo))]
    public class QueryDataSourceCommandInfo : ICommandInfo
    {
        public string DataSource { get; set; }

        /// <summary>
        ///  Optional.
        /// </summary>
        public object Filter { get; set; }
        
        /// <summary>
        ///  Optional.
        /// </summary>
        public FilterCriteria[] GenericFilter { get; set; }

        /// <summary>
        /// Optional. Set to 0 if no paging is used.
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// Optional. Set to 0 if no paging is used.
        /// </summary>
        public int RecordsPerPage { get; set; }

        /// <summary>
        ///  Optional.
        /// </summary>
        public string OrderByProperty { get; set; }

        public bool OrderDescending { get; set; }

        public ReadCommandInfo ToReadCommandInfo()
        {
            var filters = new List<FilterCriteria>();
            if (this.Filter != null)
                filters.Add(new FilterCriteria { Filter = this.Filter.GetType().AssemblyQualifiedName, Value = this.Filter });
            if (this.GenericFilter != null)
                filters.AddRange(this.GenericFilter);

            var order = new List<OrderByProperty>();
            if (!string.IsNullOrEmpty(this.OrderByProperty))
                order.Add(new OrderByProperty { Property = this.OrderByProperty, Descending = this.OrderDescending });

            return new ReadCommandInfo
            {
                DataSource = this.DataSource,
                Filters = filters.ToArray(),
                ReadRecords = true,
                ReadTotalCount = true,
                Skip = this.PageNumber > 0 ? this.RecordsPerPage * (this.PageNumber - 1) : 0,
                Top = this.RecordsPerPage,
                OrderByProperties = order.ToArray()
            };
        }

        public override string ToString()
        {
            return GetType().Name + " " + DataSource;
        }
    }
}
