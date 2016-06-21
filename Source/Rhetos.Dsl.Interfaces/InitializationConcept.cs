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

namespace Rhetos.Dsl
{
    /// <summary>
    /// An instance of this concept is always present as the first concept in the DSL model.
    /// This concept can be used for code generators that generate infrastructure classes and singletons.
    /// </summary>
    public class InitializationConcept : IConceptInfo
    {
        /// <summary>
        /// Version of the currently running Rhetos server.
        /// Note that it is not compatible with System.Version because Rhetos version may contain
        /// textual pre-release information and build metadata (see Semantic Versioning 2.0.0 for example).
        /// </summary>
        [ConceptKey]
        public string RhetosVersion { get; set; }
    }
}
