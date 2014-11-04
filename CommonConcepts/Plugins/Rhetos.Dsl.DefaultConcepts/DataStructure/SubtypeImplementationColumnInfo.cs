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
using Rhetos.Dsl;
using System.ComponentModel.Composition;
using Rhetos.Utilities;
using Rhetos.Compiler;
using Rhetos.Dom.DefaultConcepts;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// This concept is separated from IsSubtypeOfInfo, because there is no need to create a new computed column
    /// for each Supertype; only Subtype and ImplementationName are unique.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    public class SubtypeImplementationColumnInfo : IMacroConcept
    {
        [ConceptKey]
        public DataStructureInfo Subtype { get; set; }

        [ConceptKey]
        public string ImplementationName { get; set; }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            return new[] { GetSqlObject() };
        }

        public string GetComputedColumnName()
        {
            return "Subtype" + ImplementationName + "ID";
        }

        public SqlObjectInfo GetSqlObject()
        {
            return new SqlObjectInfo
            {
                Module = Subtype.Module,
                Name = Subtype.Name + "_" + GetComputedColumnName(),
                CreateSql = CreateComputedColumnSnippet(),
                RemoveSql = RemoveComputedColumnSnippet(),
            };
        }

        private string CreateComputedColumnSnippet()
        {
            return string.Format(
@"ALTER TABLE {0}.{1} ADD {2}
	AS CONVERT(UNIQUEIDENTIFIER, CONVERT(BINARY(4), CONVERT(INT, CONVERT(BINARY(4), ID)) ^ 881400495) + SUBSTRING(CONVERT(BINARY(16), ID), 5, 12))
	PERSISTED NOT NULL;
CREATE UNIQUE INDEX IX_{1}_{2} ON {0}.{1}({2});",
                Subtype.Module.Name,
                Subtype.Name,
                GetComputedColumnName());
        }

        private string RemoveComputedColumnSnippet()
        {
            return string.Format(
@"DROP INDEX {0}.{1}.IX_{1}_{2};
ALTER TABLE {0}.{1} DROP COLUMN {2};",
                Subtype.Module.Name,
                Subtype.Name,
                GetComputedColumnName());
        }
    }
}
