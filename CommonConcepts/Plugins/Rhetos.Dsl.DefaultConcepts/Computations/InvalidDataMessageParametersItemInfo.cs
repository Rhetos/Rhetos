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

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// Use this concept to separate message parameters from the error message, for easier translation to another language.
    /// Example: InvalidData with error message 'Maximum value of property {0} is {1}. Current value ({2}) is {3} characters long.'
    /// may contain MessageParameters 'item => new object[] { item.ID, P0 = "Age", P1 = 200, P2 = item.Age, P3 = item.Age.Length }'.
    /// By separating the parameters from the error message, only one error message needs to be translated
    /// for many different max-value constraints.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("MessageParametersItem")]
    public class InvalidDataMessageParametersItemInfo : InvalidDataMessageInfo
    {
        /// <summary>
        /// Lambda expression: item => new { ID = item.ID, P0 = item..., P1 = item..., ... }
        /// </summary>
        public string MessageParameters { get; set; }
    }
}
