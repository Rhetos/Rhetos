/*
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
using System.Linq;
using System.Linq.Expressions;

namespace Rhetos.Dom.DefaultConcepts
{
    /// <summary>
    /// IFilterRepository implementation of this filter is expected to have performance and memory EFFICIENT filtering.
    /// For example, if a repository loads data from a relational database, the filtering condition should be propagated
    /// to SQL so that only data that is already filtered will be loaded from database to application server.
    /// 
    /// It is possible for IFilterRepository implementation to fail with NotSupportedException if the given filter is
    /// too complex to be efficiently computed. In such situations, a specific filter should be implemented and used.
    /// </summary>
    public class FilterCriteria
    {
        public string Property { get; set; }
        public string Operation { get; set; }
        public object Value { get; set; }
    }
}