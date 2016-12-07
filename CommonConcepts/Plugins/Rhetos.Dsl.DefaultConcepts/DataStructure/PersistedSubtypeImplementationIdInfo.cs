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
    /// Represents the persisted table column that captures the alternative ID for the subtype implementation.
    /// The alternative ID is needed when a subtype implements the same supertype multiple times, in order to disambiguate implementations without performance loss.
    /// 
    /// This concept is separated from IsSubtypeOfInfo, because there is no need to create a new computed column
    /// for each Supertype: only Subtype and ImplementationName need to be unique.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    public class PersistedSubtypeImplementationIdInfo : IMacroConcept
    {
        [ConceptKey]
        public DataStructureInfo Subtype { get; set; }

        [ConceptKey]
        public string ImplementationName { get; set; }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            var sqlObject = GetSqlObjectPrototype();
            sqlObject.CreateSql = CreateComputedColumnSnippet();
            sqlObject.RemoveSql = RemoveComputedColumnSnippet();

            var sqlDependency = new SqlDependsOnDataStructureInfo { DependsOn = Subtype, Dependent = sqlObject };

            return new IConceptInfo[] { sqlObject, sqlDependency };
        }

        public static string GetComputedColumnName(string implementationName)
        {
            return "Subtype" + implementationName + "ID";
        }

        /// <summary>The returned prototype can be used as a reference to the actual object in the IDslModel.</summary>
        public SqlObjectInfo GetSqlObjectPrototype()
        {
            return new SqlObjectInfo
            {
                Module = Subtype.Module,
                Name = Subtype.Name + "_" + GetComputedColumnName(ImplementationName),
            };
        }

        private string CreateComputedColumnSnippet()
        {
            return string.Format(
@"ALTER TABLE {0}.{1} ADD {2}
	AS CONVERT(UNIQUEIDENTIFIER, CONVERT(BINARY(4), CONVERT(INT, CONVERT(BINARY(4), ID)) ^ {3}) + SUBSTRING(CONVERT(BINARY(16), ID), 5, 12))
	PERSISTED NOT NULL;
CREATE UNIQUE INDEX IX_{1}_{2} ON {0}.{1}({2});",
                Subtype.Module.Name,
                Subtype.Name,
                GetComputedColumnName(ImplementationName),
                DomUtility.GetSubtypeImplementationHash(ImplementationName));
        }

        private string RemoveComputedColumnSnippet()
        {
            return string.Format(
@"DROP INDEX {0}.{1}.IX_{1}_{2};
ALTER TABLE {0}.{1} DROP COLUMN {2};",
                Subtype.Module.Name,
                Subtype.Name,
                GetComputedColumnName(ImplementationName));
        }
    }
}
