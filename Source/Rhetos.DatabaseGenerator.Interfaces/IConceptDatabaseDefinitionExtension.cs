/*
    Copyright (C) 2013 Omega software d.o.o.

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
using Rhetos.Compiler;
using Rhetos.Dsl;

namespace Rhetos.DatabaseGenerator
{
    public interface IConceptDatabaseDefinitionExtension : IConceptDatabaseDefinition
    {
        /// <summary>
        /// Used to modify "create queries" that are produced by other concept implementations.
        /// </summary>
        /// <param name="conceptInfo">Concept instance for which database structure needs to be created.</param>
        /// <param name="codeBuilder">Use tags to insert new code in extension points 
        /// that are provided by other concept implementations.</param>
        /// <param name="createdDependencies">Provide new dependencies if inserted code have created additional 
        /// dependencies that are not obvious from ConceptInfo dependencies or from DepenedsOn metadata.
        /// In the Tuple, Item2 depends on Item1. If no additional dependencies are created, set the output value to null.
        /// In createdDependencies you can create and use a new instance of concept info to reference existing concept info with same concept info key.</param>
        void ExtendDatabaseStructure(IConceptInfo conceptInfo, ICodeBuilder codeBuilder, out IEnumerable<Tuple<IConceptInfo, IConceptInfo>> createdDependencies);
    }
}
