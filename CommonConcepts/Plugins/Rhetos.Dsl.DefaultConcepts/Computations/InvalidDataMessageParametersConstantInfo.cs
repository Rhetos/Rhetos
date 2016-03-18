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
    /// Optimized version of "MessageParameters" concept; no need to query database to retrieve error message parameters.
    /// Example: InvalidData with error message 'Maximum value of property {0} is {1}.'
    /// may contain MessageParametersConstant '"Age", 200'.
    /// By separating the parameters from the error message, only one error message needs to be translated
    /// for many different max-value constraints.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("MessageParametersConstant")]
    public class InvalidDataMessageParametersConstantInfo : InvalidDataMessageInfo
    {
        /// <summary>
        /// Comma-separated list for C# object[] initializer.
        /// For example: MessageParameters ' "Age", 200 ';
        /// will generate C# code 'new object[] { "Age", 200 }'
        /// The resulting array will be used for MessageParameters member of InvalidDataMessage class.
        /// </summary>
        public string MessageParameters { get; set; }
    }
}
