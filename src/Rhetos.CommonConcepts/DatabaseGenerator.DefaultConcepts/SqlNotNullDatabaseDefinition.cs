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
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using Rhetos.Utilities;
using System.ComponentModel.Composition;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    [Export(typeof(IConceptDatabaseGenerator))]
    public class SqlNotNullDatabaseDefinition : IConceptDatabaseGenerator<SqlNotNullInfo>
    {
        private readonly ConceptMetadata _conceptMetadata;

        public SqlNotNullDatabaseDefinition(ConceptMetadata conceptMetadata)
        {
            _conceptMetadata = conceptMetadata;
        }

        public void GenerateCode(SqlNotNullInfo info, ISqlCodeBuilder sql)
        {
            var columnName = _conceptMetadata.GetColumnName(info.Property);
            var columnType = _conceptMetadata.GetColumnType(info.Property);

            if (columnType != null)
            {
                string sqlSnippet = sql.Resources.Format("SqlNotNull_Create",
                    sql.Utility.Identifier(info.Property.DataStructure.Module.Name),
                    sql.Utility.Identifier(info.Property.DataStructure.Name),
                    columnName,
                    columnType,
                    info.InitialValueSqlExpression,
                    SqlUtility.ScriptSplitterTag)
                    .Trim() + "\r\n";

                sql.CodeBuilder.InsertCode(sqlSnippet, PropertyDatabaseDefinition.AfterCreateTag, info.Property);
            }
        }
    }
}
