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
    /// <summary>
    /// All members except DataSource are optional.
    /// </summary>
    [Export(typeof(ICommandInfo))]
    public class ReadCommandInfo : ICommandInfo
    {
        /// <summary>
        /// Name of the entity or other readable data structure.
        /// </summary>
        public string DataSource { get; set; }

        /// <summary>
        /// If not set, the command will not return ReadCommandResult.Records.
        /// </summary>
        public bool ReadRecords { get; set; }

        /// <summary>
        /// If not set, the command will not return ReadCommandResult.TotalCount.
        /// </summary>
        public bool ReadTotalCount { get; set; }

        public FilterCriteria[] Filters { get; set; }

        /// <summary>
        /// Limit the number of records in the result. Set to 0 (default) if not used.
        /// </summary>
        public int Top { get; set; }

        /// <summary>
        /// Skip the number of records in the result. Set to 0 (default) if not used.
        /// </summary>
        public int Skip { get; set; }

        public OrderByProperty[] OrderByProperties { get; set; }

        public override string ToString()
        {
            // This function is intended for a short preview, not a complete data serialization.
            return GetType().Name
                + " " + DataSource
                + (ReadRecords ? " records" : "")
                + (ReadTotalCount ? " count" : "")
                + (OrderByProperties != null && OrderByProperties.Length > 0 ? ", order by " + string.Join(" ", OrderByProperties.Select(o => o != null ? o.ToString() : "")) : "")
                + (Skip != 0 ? ", skip " + Skip : "")
                + (Top != 0 ? ", top " + Top : "")
                + (Filters != null && Filters.Length > 0 ? ", filters: " + string.Join(", ", Filters.Select(f => f != null ? f.ToString() : "")) : "");
        }
    }

    public class OrderByProperty
    {
        public string Property { get; set; }
        public bool Descending { get; set; }

        public override string ToString()
        {
            return (Descending ? "-" : "") + Property;
        }
    }
}
