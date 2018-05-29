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
using System.Collections.Generic;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// A low-level concept that inserts the SQL code snippet to the log reader SqlQueryable at the place of the given tag (an SQL comment).
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("LogReaderAdditionalSource")]
    public class LogReaderAdditionalSourceInfo : IConceptInfo
    {
        [ConceptKey]
        public SqlQueryableInfo SqlQueryable { get; set; }

        /// <summary>A description of the business rule or a purpose of the snippet.</summary>
        [ConceptKey]
        public string Name { get; set; }

        public string Snippet { get; set; }

        public string GetTag() => $"/*{SqlQueryable.Module.Name}.{SqlQueryable.Name} AdditionalSource*/";
    }

    [Export(typeof(IConceptMacro))]
    public class LogReaderAdditionalSourceMacro : IConceptMacro<LogReaderAdditionalSourceInfo>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(LogReaderAdditionalSourceInfo conceptInfo, IDslModel existingConcepts)
        {
            return DslUtility.CopySqlDependencies(conceptInfo, conceptInfo.SqlQueryable, existingConcepts);
        }
    }
}
