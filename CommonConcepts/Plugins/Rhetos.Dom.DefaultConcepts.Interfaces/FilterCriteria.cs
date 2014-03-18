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

namespace Rhetos.Dom.DefaultConcepts
{
    /// <summary>
    /// FilterCriteria is a generic filter's element that defines a filter by property value or a specific filter call.
    /// 
    /// If a data structure's repository implements a Query, Load or Filter function with IEnumerable&lt;FilterCriteria&gt; argument,
    /// it will be used when reading data using QueryDataSourceCommand.
    /// </summary>
    public class FilterCriteria
    {
        public string Property { get; set; }
        public string Filter { get; set; }
        public string Operation { get; set; }
        public object Value { get; set; }
    }
}