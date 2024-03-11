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
using System.Linq;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
#pragma warning disable CS0618 // Type or member is obsolete
    [Export(typeof(IConceptDatabaseDefinition))]
    [ExportMetadata(MefProvider.Implements, typeof(SqlIndexMultipleInfo))]
    public class SqlIndexMultipleDatabaseDefinition : IConceptDatabaseDefinition
#pragma warning restore CS0618 // Type or member is obsolete
    {
        /// <summary>
        /// Options inserted between CREATE and INDEX, before Options1Tag.
        /// </summary>
        public static readonly SqlTag<SqlIndexMultipleInfo> Options0Tag = "Options0";
        /// <summary>
        /// Options inserted between CREATE and INDEX, after Options0Tag.
        /// </summary>
        public static readonly SqlTag<SqlIndexMultipleInfo> Options1Tag = "Options1";
        /// <summary>
        /// Options inserted at the end of the CREATE INDEX query.
        /// </summary>
        public static readonly SqlTag<SqlIndexMultipleInfo> Options2Tag = "Options2";
        /// <summary>
        /// Options inserted after each column name in the CREATE INDEX query.
        /// </summary>
        public static readonly SqlTag<SqlIndexMultipleInfo> ColumnsTag = new SqlTag<SqlIndexMultipleInfo>("Columns", TagType.Appendable, "{0}", ", {0}");

        protected ISqlResources Sql { get; private set; }

        protected ISqlUtility SqlUtility { get; private set; }

        public SqlIndexMultipleDatabaseDefinition(ISqlResources sqlResources, ISqlUtility sqlUtility)
        {
            this.Sql = sqlResources;
            this.SqlUtility = sqlUtility;
        }

        public string ConstraintName(SqlIndexMultipleInfo info)
        {
            var cleanColumnNames = info.PropertyNames.Split(' ').Select(name => name.Trim()).ToArray();
            var joinedColumnNames = string.Join("_", cleanColumnNames.Select(CsUtility.TextToIdentifier));
            var basicConstraintName = Sql.Format("SqlIndexMultipleDatabaseDefinition_ConstraintName", info.DataStructure.Name, joinedColumnNames);
            return SqlUtility.Identifier(basicConstraintName);
        }

        public string CreateDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (SqlIndexMultipleInfo)conceptInfo;

            if (info.SqlImplementation())
                return Sql.Format("SqlIndexMultipleDatabaseDefinition_Create",
                    ConstraintName(info),
                    SqlUtility.Identifier(info.DataStructure.Module.Name),
                    SqlUtility.Identifier(info.DataStructure.Name),
                    ColumnsTag.Evaluate(info),
                    Options0Tag.Evaluate(info),
                    Options1Tag.Evaluate(info),
                    Options2Tag.Evaluate(info));
            return null;
        }

        public string RemoveDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (SqlIndexMultipleInfo)conceptInfo;

            if (info.SqlImplementation())
                return Sql.Format("SqlIndexMultipleDatabaseDefinition_Remove",
                    SqlUtility.Identifier(info.DataStructure.Module.Name),
                    SqlUtility.Identifier(info.DataStructure.Name),
                    ConstraintName(info));

            return null;
        }
    }
}
