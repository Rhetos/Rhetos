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
using Rhetos.Utilities;
using Rhetos.Compiler;
using Rhetos.Dom.DefaultConcepts;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    ///  Instead of using property implementations (Implements keyword), a specific SQL query may be provided 
    ///  to implement the mapping between the subtype and the polymorphic entity.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("SqlImplementation")]
    public class SpecificSubtypeSqlViewInfo : SqlViewInfo, IAlternativeInitializationConcept
    {
        public IsSubtypeOfInfo IsSubtypeOf { get; set; }

        /// <summary>Existing property ViewSource is replaced with SqlQuery to force that the property IsSubtypeOf is first when parsed,
        /// so that this concept can be embedded within IsSubtypeOfInfo in a DSL script.</summary>
        public string SqlQuery { get; set; }

        public IEnumerable<string> DeclareNonparsableProperties()
        {
            return new[] { "Module", "Name", "ViewSource" };
        }

        void IAlternativeInitializationConcept.InitializeNonparsableProperties(out IEnumerable<IConceptInfo> createdConcepts)
        {
            var prototype = IsSubtypeOf.GetImplementationViewPrototype();

            Module = prototype.Module;
            Name = prototype.Name;
            ViewSource = SqlQuery;

            createdConcepts = null;
        }
    }
}
