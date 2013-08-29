﻿/*
    Copyright (C) 2013 Omega software d.o.o.

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
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Text;
using Rhetos.Utilities;
using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    [Export(typeof(IConceptDatabaseDefinition))]
    [ExportMetadata(MefProvider.Implements, typeof(SqlIndexMultipleInfo))]
    [ConceptImplementationVersion(2, 0)]
    public class SqlIndexMultipleDatabaseDefinition : IConceptDatabaseDefinition
    {
        /// <summary>
        /// Options inserted between CREATE and INDEX
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


        private static string ConstraintName(SqlIndexMultipleInfo info)
        {
            var cleanColumnNames = info.PropertyNames.Split(' ').Select(name => name.Trim()).ToArray();
            var joinedColumnNames = string.Join("_", cleanColumnNames.Select(CsUtility.TextToIdentifier));
            var basicConstraintName = Sql.Format("SqlIndexMultipleDatabaseDefinition_ConstraintName", info.Entity.Name, joinedColumnNames);
            return SqlUtility.Identifier(basicConstraintName);
        }

        public static bool IsSupported(SqlIndexMultipleInfo info)
        {
            return info.Entity is EntityInfo;
        }

        public string CreateDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (SqlIndexMultipleInfo)conceptInfo;

            if (IsSupported(info))
                return Sql.Format("SqlIndexMultipleDatabaseDefinition_Create",
                    ConstraintName(info),
                    SqlUtility.Identifier(info.Entity.Module.Name),
                    SqlUtility.Identifier(info.Entity.Name),
                    ColumnsTag.Evaluate(info),
                    Options1Tag.Evaluate(info),
                    Options2Tag.Evaluate(info));
            return null;
        }

        public string RemoveDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (SqlIndexMultipleInfo)conceptInfo;

            if (IsSupported(info))
                return Sql.Format("SqlIndexMultipleDatabaseDefinition_Remove",
                    SqlUtility.Identifier(info.Entity.Module.Name),
                    SqlUtility.Identifier(info.Entity.Name),
                    ConstraintName(info));

            return null;
        }
    }
}
