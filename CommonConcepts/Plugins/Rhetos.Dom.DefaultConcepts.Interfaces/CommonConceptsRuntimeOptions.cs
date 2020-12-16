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

namespace Rhetos.Dom.DefaultConcepts
{
    /// <summary>
    /// Run-time configuration.
    /// </summary>
    [Options("CommonConcepts")]
    public class CommonConceptsRuntimeOptions
    {
        /// <summary>
        /// Number of records inserted updated or deleted in a single SQL command.
        /// When saving a list of records with count larger then this limit, the list will be automatically split to batches.
        /// </summary>
        public int SaveSqlCommandBatchSize { get; set; } = 20;
    }
}