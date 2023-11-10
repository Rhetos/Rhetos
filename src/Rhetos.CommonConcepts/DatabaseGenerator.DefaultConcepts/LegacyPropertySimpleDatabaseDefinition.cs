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
using System.ComponentModel.Composition;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    [Export(typeof(IConceptDatabaseGenerator))]
    public class LegacyPropertySimpleDatabaseDefinition : IConceptDatabaseGenerator<LegacyPropertySimpleInfo>
    {
        public void GenerateCode(LegacyPropertySimpleInfo info, ISqlCodeBuilder sql)
        {
            sql.CodeBuilder.InsertCode(sql.Resources.Format("LegacyPropertySimpleDatabaseDefinition_ExtendViewSelect", sql.Utility.Identifier(info.Property.Name), sql.Utility.Identifier(info.Column)),
                LegacyEntityWithAutoCreatedViewDatabaseDefinition.ViewSelectPartTag, info.Dependency_LegacyEntityWithAutoCreatedView);

            sql.CodeBuilder.InsertCode(sql.Resources.Format("LegacyPropertySimpleDatabaseDefinition_ExtendTriggerInsert", sql.Utility.Identifier(info.Column)),
                LegacyEntityWithAutoCreatedViewDatabaseDefinition.TriggerInsertPartTag, info.Dependency_LegacyEntityWithAutoCreatedView);

            sql.CodeBuilder.InsertCode(sql.Resources.Format("LegacyPropertySimpleDatabaseDefinition_ExtendTriggerSelectForInsert", sql.Utility.Identifier(info.Column), sql.Utility.Identifier(info.Property.Name)),
                LegacyEntityWithAutoCreatedViewDatabaseDefinition.TriggerSelectForInsertPartTag, info.Dependency_LegacyEntityWithAutoCreatedView);

            sql.CodeBuilder.InsertCode(sql.Resources.Format("LegacyPropertySimpleDatabaseDefinition_ExtendTriggerSelectForUpdate", sql.Utility.Identifier(info.Column), sql.Utility.Identifier(info.Property.Name)),
                LegacyEntityWithAutoCreatedViewDatabaseDefinition.TriggerSelectForUpdatePartTag, info.Dependency_LegacyEntityWithAutoCreatedView);
        }
    }
}
