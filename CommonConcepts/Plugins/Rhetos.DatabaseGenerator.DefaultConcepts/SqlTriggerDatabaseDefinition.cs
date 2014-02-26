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
using Rhetos.Utilities;
using Rhetos.Extensibility;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Dsl;
using System.Globalization;
using Rhetos.Compiler;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    [Export(typeof(IConceptDatabaseDefinition))]
    [ExportMetadata(MefProvider.Implements, typeof(SqlTriggerInfo))]
    public class SqlTriggerDatabaseDefinition : IConceptDatabaseDefinition
    {
        public string CreateDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (SqlTriggerInfo) conceptInfo;
            var orm = (IOrmDataStructure) info.Structure;

            return Sql.Format("SqlTriggerDatabaseDefinition_Create",
                SqlUtility.Identifier(orm.GetOrmSchema()),
                TriggerName(info),
                SqlUtility.Identifier(orm.GetOrmDatabaseObject()),
                info.Events,
                info.TriggerSource);
        }

        private static string TriggerName(SqlTriggerInfo info)
        {
            return SqlUtility.Identifier(Sql.Format("SqlTriggerDatabaseDefinition_TriggerName", info.Structure.Name, info.Name));
        }

        public string RemoveDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (SqlTriggerInfo) conceptInfo;
            var orm = (IOrmDataStructure) info.Structure;

            return Sql.Format("SqlTriggerDatabaseDefinition_Remove",
                SqlUtility.Identifier(orm.GetOrmSchema()),
                TriggerName(info));
        }
    }
}
