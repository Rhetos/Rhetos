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
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// Initialize or change the data, before saving it to the database, if previous data state needs to be considered. See related LoadOldItems concept.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("OldDataLoaded")]
    public class OldDataLoadedInfo : IConceptInfo
    {
        [ConceptKey]
        public SaveMethodInfo SaveMethod { get; set; }

        /// <summary>
        /// Unique name of this business rule.
        /// </summary>
        [ConceptKey]
        public string RuleName { get; set; }

        /// <summary>
        /// Available variables in this context:
        ///     _executionContext,
        ///     checkUserPermissions (whether the Save command is called directly by client through a web API)
        ///     insertedNew (array of new items for insert),
        ///     updatedNew (array of new items for update),
        ///     deletedIds (array of items to be deleted).
        /// </summary>
        public string CsCodeSnippet { get; set; }
    }
}
