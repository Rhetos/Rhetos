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
    /// This base class for different implementations of error messages is used to
    /// ensure only one implementation will be used on a single InvalidData concept.
    /// This concept will suppress default validation result (message, parameters and metadata) to allow custom implementation.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    public class InvalidDataMessageInfo : IConceptInfo
    {
        [ConceptKey]
        public InvalidDataInfo InvalidData { get; set; }
    }
}
