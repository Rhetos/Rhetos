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
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Utilities;
using System.ComponentModel.Composition;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    [Export(typeof(IConceptDatabaseGenerator))]
    public class SqlIndexClusteredDatabaseDefinition : IConceptDatabaseGenerator<SqlIndexClusteredInfo>
    {
        public void GenerateCode(SqlIndexClusteredInfo info, ISqlCodeBuilder sql)
        {
            if (!info.SqlIndex.SqlImplementation())
                return;

            string clusteredOptionSnippet = sql.Resources.TryGet("SqlIndexClusteredDatabaseDefinition_Options1");
            if (!string.IsNullOrEmpty(clusteredOptionSnippet))
                sql.CodeBuilder.InsertCode("CLUSTERED ", SqlIndexMultipleDatabaseDefinition.Options1Tag, info.SqlIndex);

            string clusteredStatementSnippet = sql.Resources.TryGet("SqlIndexClusteredDatabaseDefinition_Create");
            if (!string.IsNullOrEmpty(clusteredStatementSnippet))
            {
                string constraintName = new SqlIndexMultipleDatabaseDefinition(sql.Resources, sql.Utility)
                    .ConstraintName(info.SqlIndex);

                sql.CreateDatabaseStructure(sql.Resources.Format("SqlIndexClusteredDatabaseDefinition_Create",
                    sql.Utility.Identifier(info.SqlIndex.DataStructure.Module.Name),
                    sql.Utility.Identifier(info.SqlIndex.DataStructure.Name),
                    constraintName));

                sql.RemoveDatabaseStructure(sql.Resources.Format("SqlIndexClusteredDatabaseDefinition_Remove",
                    sql.Utility.Identifier(info.SqlIndex.DataStructure.Module.Name),
                    sql.Utility.Identifier(info.SqlIndex.DataStructure.Name),
                    constraintName));
            }
        }
    }
}
