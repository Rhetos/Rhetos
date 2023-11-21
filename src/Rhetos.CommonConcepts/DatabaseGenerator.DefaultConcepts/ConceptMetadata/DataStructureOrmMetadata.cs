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
using Rhetos.Utilities;
using System.ComponentModel.Composition;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    [Export(typeof(IConceptMetadataExtension))]
    public class EntityOrmMetadata : DataStructureOrmMetadataBase<EntityInfo>
    {
        public override string GetOrmDatabaseObject(EntityInfo dataStructure) => dataStructure.Name;

        public override string GetOrmSchema(EntityInfo dataStructure) => dataStructure.Module.Name;
    }

    [Export(typeof(IConceptMetadataExtension))]
    public class SqlQueryableOrmMetadata : DataStructureOrmMetadataBase<SqlQueryableInfo>
    {
        public override string GetOrmDatabaseObject(SqlQueryableInfo dataStructure) => dataStructure.Name;

        public override string GetOrmSchema(SqlQueryableInfo dataStructure) => dataStructure.Module.Name;
    }

    [Export(typeof(IConceptMetadataExtension))]
    public class PolymorphicOrmMetadata : DataStructureOrmMetadataBase<PolymorphicInfo>
    {
        public override string GetOrmDatabaseObject(PolymorphicInfo dataStructure) => dataStructure.Name;

        /// <summary>
        /// ORM will be mapped to <see cref="PolymorphicUnionViewInfo"/> from the database.
        /// </summary>
        public override string GetOrmSchema(PolymorphicInfo dataStructure) => dataStructure.Module.Name;
    }

    [Export(typeof(IConceptMetadataExtension))]
    public class LegacyEntityOrmMetadata : DataStructureOrmMetadataBase<LegacyEntityInfo>
    {
        private readonly ISqlUtility _sqlUtility;

        public LegacyEntityOrmMetadata(ISqlUtility sqlUtility)
        {
            _sqlUtility = sqlUtility;
        }

        public override string GetOrmDatabaseObject(LegacyEntityInfo dataStructure) => _sqlUtility.GetShortName(dataStructure.View);

        public override string GetOrmSchema(LegacyEntityInfo dataStructure) => _sqlUtility.GetSchemaName(dataStructure.View);
    }


    [Export(typeof(IConceptMetadataExtension))]
    public class LegacyEntityWithAutoCreatedViewOrmMetadata : DataStructureOrmMetadataBase<LegacyEntityWithAutoCreatedViewInfo>
    {
        public override string GetOrmDatabaseObject(LegacyEntityWithAutoCreatedViewInfo dataStructure) => dataStructure.Name;

        public override string GetOrmSchema(LegacyEntityWithAutoCreatedViewInfo dataStructure) => dataStructure.Module.Name;
    }
}
