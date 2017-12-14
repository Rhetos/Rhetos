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
    [ConceptKeyword("AfterSave")]
    public class SaveMethodAfterSaveInfo : IConceptInfo
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
        ///     inserted (array of new items),
        ///     updated (array of new items).
        /// If LoadOldItems concept is used, there are also available:
        ///     updatedOld (array of old items),
        ///     deletedOld (array of old items).
        /// </summary>
        public string CsCodeSnippet { get; set; }
    }
}
