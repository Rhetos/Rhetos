﻿/*
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
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// A queryable data structure, implemented with a C# code (the lambda expression), for extending entities with simple computed data.
    /// The lambda expression returns records as a query: (IQueryable&lt;BaseEntity&gt; source, repository) => IQueryable&lt;Extension&gt;.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("QueryableExtension")]
    public class QueryableExtensionInfo : DataStructureInfo, IMacroConcept
    {
        public DataStructureInfo Base { get; set; }

        public string Expression { get; set; }

        public IEnumerable<IConceptInfo> CreateNewConcepts()
        {
            // TODO (?) Copy all filters from base concept (it makes more sense for ComputedExtension because of efficient implementation, not really necessary for QueryableExtension)
            return new[] { new DataStructureExtendsInfo { Base = Base, Extension = this } };
        }
    }
}
