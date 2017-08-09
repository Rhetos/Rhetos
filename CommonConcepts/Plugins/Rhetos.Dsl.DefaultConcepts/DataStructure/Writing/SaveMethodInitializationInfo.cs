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
using Rhetos.Dsl;
using System.ComponentModel.Composition;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Initialization")]
    public class SaveMethodInitializationInfo : IConceptInfo
    {
        [ConceptKey]
        public SaveMethodInfo SaveMethod { get; set; }

        /// <summary>
        /// Name of this business rule, unique among this entity's initializations.
        /// </summary>
        [ConceptKey]
        public string RuleName { get; set; }

        /// <summary>
        /// Available variables in this context:
        ///     _executionContext,
        ///     checkUserPermissions (whether the Save command is called directly by client through a web API)
        ///     insertedNew (array of new items),
        ///     updatedNew (array of new items - update),
        ///     deletedIds (array of items to be deleted).
        /// In current state old items are not loaded. These are bare data provided to save method.
        /// See <see cref="WritableOrmDataStructureCodeGenerator.InitializationTag">WritableOrmDataStructureCodeGenerator.InitializationTag</see> for more info.
        /// </summary>
        public string CsCodeSnippet { get; set; }
    }
}
