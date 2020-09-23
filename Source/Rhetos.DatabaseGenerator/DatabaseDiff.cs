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

using System.Collections.Generic;

namespace Rhetos.DatabaseGenerator
{
    /// <summary>
    /// Result of comparison between new database model to the existing database.
    /// </summary>
    public class DatabaseDiff
    {
        public List<ConceptApplication> OldApplications { get; set; }

        public List<ConceptApplication> NewApplications { get; set; }

        public List<ConceptApplication> ToBeRemoved { get; set; }

        public List<ConceptApplication> ToBeInserted { get; set; }

        /// <summary>
        /// For debugging only.
        /// List of database objects that need to be dropped and created again,
        /// because their create SQL queries have changed.
        /// </summary>
        public List<(ConceptApplication Old, ConceptApplication New)> ChangedQueries { get; set; }

        /// <summary>
        /// For debugging only.
        /// List of database objects that need to be dropped and created again,
        /// because some of their dependent objects have been changed.
        /// DependencyStatus describes the change.
        /// </summary>
        public List<(ConceptApplication RefreshedConcept, ConceptApplication Dependency, RefreshDependencyStatus DependencyStatus)> Refreshes { get; set; }
    }

    public enum RefreshDependencyStatus { New, Changed, Refreshed, Removed }
}