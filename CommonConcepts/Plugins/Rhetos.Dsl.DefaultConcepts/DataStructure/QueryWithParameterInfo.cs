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
using System.ComponentModel.Composition;
using Rhetos.Utilities;
using Rhetos.Compiler;
using System.Globalization;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Query")]
    public class QueryWithParameterInfo : IValidatedConcept
    {
        [ConceptKey]
        public DataStructureInfo DataStructure { get; set; }

        /// <summary>FullName of the parameter type.</summary>
        [ConceptKey]
        public string ParameterType { get; set; }

        public string QueryImplementation { get; set; }

        public void CheckSemantics(IDslModel existingConcepts)
        {
            if (!ParameterType.Contains('.'))
                throw new DslSyntaxException(this, "ParameterType must be full type name, including Module name for a data structure, or C# namespace for other parameter types.");
        }
    }
}
