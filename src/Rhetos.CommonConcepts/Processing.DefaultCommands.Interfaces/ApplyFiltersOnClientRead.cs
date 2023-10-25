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

using Rhetos.Processing.DefaultCommands;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rhetos.Dom.DefaultConcepts
{
    public class ApplyFilterWhere
    {
        public string FilterName;

        /// <summary>
        /// (Optional) Selection of command where the filter will be applied.
        /// If not set (null), it is equivalent to "command => true".
        /// </summary>
        public Func<ReadCommandInfo, bool> Where;
    }

    /// <summary>
    /// For a given data structure key, dictionary contains list of filters that will be
    /// automatically applied when executing ReadCommand.
    /// </summary>
    public class ApplyFiltersOnClientRead : MultiDictionary<string, ApplyFilterWhere>
    {
        public void Add(string dataStructure, string filterName)
        {
            Add(dataStructure, new ApplyFilterWhere { FilterName = filterName, Where = null });
        }

        public void Add(string dataStructure, string filterName, Func<ReadCommandInfo, bool> where)
        {
            Add(dataStructure, new ApplyFilterWhere { FilterName = filterName, Where = where });
        }
    }
}
