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
using System.Diagnostics.Contracts;

namespace Rhetos.Dsl
{
    /// <summary>
    /// DSL model represents a list of features of the generated application.
    /// It includes concepts directly written in DSL scripts, and additionally generated concepts (by macro concepts, for example).
    /// </summary>
    /// <remarks>
    /// DSL model is similar to an abstract syntax tree, but it is represented as a directed acyclic graph with multiple root nodes.
    /// </remarks>
    public interface IDslModel
    {
        /// <summary>
        /// The concepts are already sorted by their dependencies.
        /// </summary>
        IEnumerable<IConceptInfo> Concepts { get; }

        /// <summary>
        /// See ConceptInfoHelper.GetKey function description for expected format of conceptKey.
        /// Returns null is there is no concept with the given key.
        /// </summary>
        IConceptInfo FindByKey(string conceptKey);

        T GetIndex<T>() where T : IDslModelIndex;
    }
}
