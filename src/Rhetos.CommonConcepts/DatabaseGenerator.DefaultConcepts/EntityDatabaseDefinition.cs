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

using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Utilities;
using System.ComponentModel.Composition;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    [Export(typeof(IConceptDatabaseGenerator))]
    public class EntityDatabaseDefinition : IConceptDatabaseGenerator<EntityInfo>
    {
        public void GenerateCode(EntityInfo info, ISqlCodeBuilder sql)
        {
            string createQuery = sql.Resources.Format("EntityDatabaseDefinition_Create",
                sql.Utility.Identifier(info.Module.Name),
                sql.Utility.Identifier(info.Name),
                PrimaryKeyConstraintName(info, sql.Utility, sql.Resources),
                DefaultConstraintName(info, sql.Utility, sql.Resources));

            sql.CreateDatabaseStructure(createQuery);

            string removeQuery = sql.Resources.Format("EntityDatabaseDefinition_Remove",
                sql.Utility.Identifier(info.Module.Name),
                sql.Utility.Identifier(info.Name),
                PrimaryKeyConstraintName(info, sql.Utility, sql.Resources),
                DefaultConstraintName(info, sql.Utility, sql.Resources));

            sql.RemoveDatabaseStructure(removeQuery);
        }

        public static string PrimaryKeyConstraintName(EntityInfo info, ISqlUtility sqlUtility, ISqlResources sqlResources)
        {
            return sqlUtility.Identifier(sqlResources.Format("EntityDatabaseDefinition_PrimaryKeyConstraintName",
                info.Module.Name,
                info.Name));
        }

        public static string DefaultConstraintName(EntityInfo info, ISqlUtility sqlUtility, ISqlResources sqlResources)
        {
            return sqlUtility.Identifier(sqlResources.Format("EntityDatabaseDefinition_DefaultConstraintName",
                info.Module.Name,
                info.Name));
        }
    }
}
