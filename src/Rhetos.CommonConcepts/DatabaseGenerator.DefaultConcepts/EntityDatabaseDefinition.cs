﻿/*
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
    [Export(typeof(IConceptDatabaseDefinition))]
    [ExportMetadata(MefProvider.Implements, typeof(EntityInfo))]
    public class EntityDatabaseDefinition : IConceptDatabaseDefinition
    {
        private readonly ISqlResources _sqlResources;
        private readonly ISqlUtility _sqlUtility;

        public EntityDatabaseDefinition(ISqlResources sqlResources, ISqlUtility sqlUtility)
        {
            _sqlResources = sqlResources;
            _sqlUtility = sqlUtility;
        }

        private string PrimaryKeyConstraintName(EntityInfo info)
        {
            return _sqlUtility.Identifier(_sqlResources.Format("EntityDatabaseDefinition_PrimaryKeyConstraintName",
                info.Module.Name,
                info.Name));
        }

        private string DefaultConstraintName(EntityInfo info)
        {
            return _sqlUtility.Identifier(_sqlResources.Format("EntityDatabaseDefinition_DefaultConstraintName",
                info.Module.Name,
                info.Name));
        }

        public string CreateDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (EntityInfo)conceptInfo;

            return _sqlResources.Format("EntityDatabaseDefinition_Create",
                _sqlUtility.Identifier(info.Module.Name),
                _sqlUtility.Identifier(info.Name),
                PrimaryKeyConstraintName(info),
                DefaultConstraintName(info));
        }

        public string RemoveDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (EntityInfo)conceptInfo;

            return _sqlResources.Format("EntityDatabaseDefinition_Remove",
                _sqlUtility.Identifier(info.Module.Name),
                _sqlUtility.Identifier(info.Name),
                PrimaryKeyConstraintName(info),
                DefaultConstraintName(info));
        }
    }
}
