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

using System.ComponentModel.Composition;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// Aditional options when creating an SQL INDEX appended at the end of generated code for creating an index.
    /// Use options such as WITH and ON.
    /// Don't use INCLUDE or WHERE as a part of these options, as they have their own Concepts.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Options")]
    public class SqlIndexOptionsInfo : IConceptInfo
    {
        [ConceptKey]
        public SqlIndexMultipleInfo SqlIndex { get; set; }

        [ConceptKey]
        public string SqlOptions { get; set; }
    }
}
