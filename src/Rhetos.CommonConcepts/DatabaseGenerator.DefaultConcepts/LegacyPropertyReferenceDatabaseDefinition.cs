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
using System;
using System.ComponentModel.Composition;
using System.Linq;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    [Export(typeof(IConceptDatabaseGenerator))]
    public class LegacyPropertyReferenceDatabaseDefinition : IConceptDatabaseGenerator<LegacyPropertyReferenceInfo>
    {
        private static int _uniqueNum = 1;

        public void GenerateCode(LegacyPropertyReferenceInfo info, ISqlCodeBuilder sql)
        {
            var sourceColumns = info.Columns.Split(',').Select(s => s.Trim()).Select(s => sql.Utility.Identifier(s)).ToArray();
            var refColumns = info.ReferencedColumns.Split(',').Select(s => s.Trim()).Select(s => sql.Utility.Identifier(s)).ToArray();
            if (sourceColumns.Length != refColumns.Length)
                throw new DslSyntaxException("Count of references columns does not match count of source columns in " + info.GetUserDescription() + ". "
                    + "There are " + sourceColumns.Length + " source columns and " + refColumns.Length + " referenced columns.");

            string refAlias = sql.Utility.Identifier("ref" + _uniqueNum++);

            // Add column to view:

            sql.CodeBuilder.InsertCode(sql.Resources.Format("LegacyPropertyReferenceDatabaseDefinition_ExtendViewSelect", sql.Utility.Identifier(info.Property.Name), refAlias),
                LegacyEntityWithAutoCreatedViewDatabaseDefinition.ViewSelectPartTag, info.Dependency_LegacyEntityWithAutoCreatedView);

            var allColumnsEqual = string.Join(" AND ", sourceColumns.Zip(refColumns,
                (sCol, rCol) => sql.Resources.Format("LegacyPropertyReferenceDatabaseDefinition_ExtendFromJoin", refAlias, rCol, sCol)));
            sql.CodeBuilder.InsertCode(sql.Resources.Format("LegacyPropertyReferenceDatabaseDefinition_ExtendViewFrom", sql.Utility.GetFullName(info.ReferencedTable), refAlias, allColumnsEqual),
                LegacyEntityWithAutoCreatedViewDatabaseDefinition.ViewFromPartTag, info.Dependency_LegacyEntityWithAutoCreatedView);

            // Add columns to instead-of trigger:

            foreach (var fkColumn in sourceColumns.Zip(refColumns, Tuple.Create))
            {
                sql.CodeBuilder.InsertCode(sql.Resources.Format("LegacyPropertyReferenceDatabaseDefinition_ExtendTriggerInsert", fkColumn.Item1),
                    LegacyEntityWithAutoCreatedViewDatabaseDefinition.TriggerInsertPartTag, info.Dependency_LegacyEntityWithAutoCreatedView);

                sql.CodeBuilder.InsertCode(sql.Resources.Format("LegacyPropertyReferenceDatabaseDefinition_ExtendTriggerSelectForInsert",
                        fkColumn.Item1,
                        refAlias,
                        fkColumn.Item2,
                        sql.Utility.GetFullName(info.ReferencedTable),
                        sql.Utility.Identifier(info.Property.Name)),
                    LegacyEntityWithAutoCreatedViewDatabaseDefinition.TriggerSelectForInsertPartTag, info.Dependency_LegacyEntityWithAutoCreatedView);

                sql.CodeBuilder.InsertCode(sql.Resources.Format("LegacyPropertyReferenceDatabaseDefinition_ExtendTriggerSelectForUpdate",
                        fkColumn.Item1,
                        refAlias,
                        fkColumn.Item2,
                        sql.Utility.GetFullName(info.ReferencedTable),
                        sql.Utility.Identifier(info.Property.Name)),
                    LegacyEntityWithAutoCreatedViewDatabaseDefinition.TriggerSelectForUpdatePartTag, info.Dependency_LegacyEntityWithAutoCreatedView);
            }

            sql.CodeBuilder.InsertCode(
                sql.Resources.Format("LegacyPropertyReferenceDatabaseDefinition_ExtendTriggerFrom", sql.Utility.GetFullName(info.ReferencedTable), refAlias, sql.Utility.Identifier(info.Property.Name)),
                LegacyEntityWithAutoCreatedViewDatabaseDefinition.TriggerFromPartTag, info.Dependency_LegacyEntityWithAutoCreatedView);
        }
    }
}
