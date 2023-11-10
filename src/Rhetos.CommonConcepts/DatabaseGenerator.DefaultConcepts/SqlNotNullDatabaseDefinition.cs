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
using System.ComponentModel.Composition;
using System.Linq;
using Rhetos.Utilities;
using Rhetos.Extensibility;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Dsl;
using System.Globalization;
using Rhetos.Compiler;
using System.Text;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    [Export(typeof(IConceptDatabaseDefinition))]
    [ExportMetadata(MefProvider.Implements, typeof(SqlNotNullInfo))]
    public class SqlNotNullDatabaseDefinition : IConceptDatabaseDefinitionExtension
    {
        private readonly ConceptMetadata _conceptMetadata;

        protected ISqlResources Sql { get; private set; }

        protected ISqlUtility SqlUtility { get; private set; }

        public SqlNotNullDatabaseDefinition(ConceptMetadata conceptMetadata, ISqlResources sqlResources, ISqlUtility sqlUtility)
        {
            _conceptMetadata = conceptMetadata;
            this.Sql = sqlResources;
            this.SqlUtility = sqlUtility;
        }

        public string CreateDatabaseStructure(IConceptInfo conceptInfo)
        {
            return null;
        }

        public string RemoveDatabaseStructure(IConceptInfo conceptInfo)
        {
            return null;
        }

        public void ExtendDatabaseStructure(IConceptInfo conceptInfo, ICodeBuilder codeBuilder, out System.Collections.Generic.IEnumerable<Tuple<IConceptInfo, IConceptInfo>> createdDependencies)
        {
            var info = (SqlNotNullInfo)conceptInfo;
            var sql = new StringBuilder();

            var columnName = _conceptMetadata.GetColumnName(info.Property);
            var columnType = _conceptMetadata.GetColumnType(info.Property);

            if (columnType != null)
            {
                sql.AppendLine(Sql.Format("SqlNotNull_Create",
                    SqlUtility.Identifier(info.Property.DataStructure.Module.Name),
                    SqlUtility.Identifier(info.Property.DataStructure.Name),
                    columnName,
                    columnType,
                    info.InitialValueSqlExpression,
                    Rhetos.Utilities.SqlUtility.ScriptSplitterTag).Trim());
            }

            var sqlSnippet = sql.ToString().Trim() + "\r\n";
            if (!string.IsNullOrWhiteSpace(sqlSnippet))
                codeBuilder.InsertCode(sqlSnippet, PropertyDatabaseDefinition.AfterCreateTag, info.Property);

            createdDependencies = null;
        }
    }
}
