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

using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;

namespace Rhetos.DatabaseGenerator
{
    public class SqlCodeBuilder : ISqlCodeBuilder
    {
        /// <summary>
        /// Use <see cref="CodeBuilder"/> to modify "create queries" that are produced by other concept implementations.
        /// Use tags to insert new code in extension points that are provided by other concept implementations.
        /// </summary>
        /// <remarks>
        /// When inserting new code, use <see cref="AddDependencies"/>
        /// to provide new dependencies if the inserted code have created additional 
        /// dependencies that are not obvious from the ConceptInfo dependencies or from the DepenedsOn metadata.
        /// </remarks>
        public ICodeBuilder CodeBuilder { get; }

        public ISqlUtility Utility { get; }

        public ISqlResources Resources { get; }

        private readonly Action<string> _onCreateDatabaseStructure;
        private readonly Action<string> _onRemoveDatabaseStructure;
        private readonly Action<IEnumerable<Tuple<IConceptInfo, IConceptInfo>>> _onAddDependencies;

        public SqlCodeBuilder(
            ICodeBuilder codeBuilder,
            ISqlUtility utility,
            ISqlResources resources,
            Action<string> onCreateDatabaseStructure,
            Action<string> onRemoveDatabaseStructure,
            Action<IEnumerable<Tuple<IConceptInfo, IConceptInfo>>> onAddDependencies)
        {
            CodeBuilder = codeBuilder;
            Utility = utility;
            Resources = resources;
            _onCreateDatabaseStructure = onCreateDatabaseStructure;
            _onRemoveDatabaseStructure = onRemoveDatabaseStructure;
            _onAddDependencies = onAddDependencies;
        }

        public void CreateDatabaseStructure(string sqlScript) => _onCreateDatabaseStructure(sqlScript);

        public void RemoveDatabaseStructure(string sqlScript) => _onRemoveDatabaseStructure(sqlScript);

        /// <summary>
        /// Add new dependencies between database objects, that are result of inserting a new code to the existing SQL scripts
        /// with <see cref="CodeBuilder"/>, if the inserted code have created additional dependencies that are not obvious
        /// from the ConceptInfo dependencies or from the DepenedsOn metadata.
        /// In the Tuple, Item2 depends on Item1.
        /// In the provided parameters, you can create and use a new instance of concept info to reference existing concept info with same concept info key.
        /// </summary>
        public void AddDependencies(IEnumerable<Tuple<IConceptInfo, IConceptInfo>> createdDependencies) => _onAddDependencies(createdDependencies);
    }
}
