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

using Rhetos.Dsl;
using System.Threading;

namespace Rhetos.DatabaseGenerator
{
    /// <summary>
    /// Represents a generator for a part of a database model for a single concept.
    /// Same concept can have multiple code generators for different IConceptDatabaseDefinition implementations.
    /// </summary>
    public class CodeGenerator
    {
        public IConceptInfo ConceptInfo { get; }
        public IConceptDatabaseDefinition ConceptImplementation { get; }

        public CodeGenerator(IConceptInfo conceptInfo, IConceptDatabaseDefinition conceptImplementation)
        {
            ConceptInfo = conceptInfo;
            ConceptImplementation = conceptImplementation;
        }

        /// <summary>
        /// Unique instance identifier for simpler internal optimizations.
        /// </summary>
        public int Id { get; } = Interlocked.Increment(ref idCounter);
        private static int idCounter = 0;

        /// <summary>
        /// For logging.
        /// </summary>
        public override string ToString() => ConceptInfo.GetKey() + "/" + ConceptImplementation.GetType().Name;
    }
}
