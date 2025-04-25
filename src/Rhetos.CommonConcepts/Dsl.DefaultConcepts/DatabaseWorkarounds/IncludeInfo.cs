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

using System.ComponentModel.Composition;

namespace Rhetos.Dsl.DefaultConcepts.DatabaseWorkarounds
{
    /// <summary>
    /// Adds the INCLUDE columns (nonkey) to the index.
    /// The <see cref="Columns"/> parameter is a list of properties separated by space.
    /// It may also contain other database columns that are not created as Rhetos properties, such as ID.
    /// </summary>
    /// <remarks>
    /// If including a column that is not recognized as a Rhetos property (for example a column created by custom SqlObject),
    /// then add a SqlDependsOnSqlObject on the SQL index (typically on SqlIndexMultiple) with a reference
    /// to the SqlObject that created that column.
    /// </remarks>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Include")]
    public class IncludeInfo : IConceptInfo
    {
        [ConceptKey]
        public SqlIndexMultipleInfo SqlIndex { get; set; }

        [ConceptKey]
        public string Columns { get; set; }
    }
}
