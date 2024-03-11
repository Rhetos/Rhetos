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

using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using Rhetos.Utilities;
using System.ComponentModel.Composition;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
#pragma warning disable CS0618 // Type or member is obsolete
    [Export(typeof(IConceptDatabaseDefinition))]
    [ExportMetadata(MefProvider.Implements, typeof(SqlTriggerInfo))]
    public class SqlTriggerDatabaseDefinition : IConceptDatabaseDefinition
#pragma warning restore CS0618 // Type or member is obsolete
    {
        private readonly ConceptMetadata _conceptMetadata;

        protected ISqlResources Sql { get; private set; }

        protected ISqlUtility SqlUtility { get; private set; }

        public SqlTriggerDatabaseDefinition(ISqlResources sqlResources, ISqlUtility sqlUtility, ConceptMetadata conceptMetadata)
        {
            Sql = sqlResources;
            SqlUtility = sqlUtility;
            _conceptMetadata = conceptMetadata;
        }

        public string CreateDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (SqlTriggerInfo) conceptInfo;

            return Sql.Format("SqlTriggerDatabaseDefinition_Create",
                SqlUtility.Identifier(_conceptMetadata.GetOrmSchema(info.Structure)),
                TriggerName(info),
                SqlUtility.Identifier(_conceptMetadata.GetOrmDatabaseObject(info.Structure)),
                info.Events,
                info.TriggerSource);
        }

        private string TriggerName(SqlTriggerInfo info)
        {
            return SqlUtility.Identifier(Sql.Format("SqlTriggerDatabaseDefinition_TriggerName", info.Structure.Name, info.Name));
        }

        public string RemoveDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (SqlTriggerInfo) conceptInfo;

            return Sql.Format("SqlTriggerDatabaseDefinition_Remove",
                SqlUtility.Identifier(_conceptMetadata.GetOrmSchema(info.Structure)),
                TriggerName(info));
        }
    }
}
