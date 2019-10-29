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

using System.ComponentModel.Composition;
using Rhetos.Compiler;
using Rhetos.Extensibility;
using Rhetos.Dsl.DefaultConcepts;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    [Export(typeof(IConceptDataMigration))]
    public class HardcodedEntityDataMigrationScript : IConceptDataMigration<HardcodedEntityInfo>
    {
        public static readonly SqlTag<HardcodedEntityInfo> InsertValuesTag = new SqlTag<HardcodedEntityInfo>("InsertValues");

        public static readonly SqlTag<HardcodedEntityInfo> DataMigrationUseTag = new SqlTag<HardcodedEntityInfo>("DataMigrationUse");

        public static readonly SqlTag<HardcodedEntityInfo> UpdateTag = new SqlTag<HardcodedEntityInfo>("Update");

        public static readonly SqlTag<HardcodedEntityInfo> DataMigrationApplyMultipleTag = new SqlTag<HardcodedEntityInfo>("DataMigrationApplyMultiple");

        public void GenerateCode(HardcodedEntityInfo concept, IDataMigrationScriptBuilder codeBuilder)
        {
            codeBuilder.AddBeforeDataMigrationScript($@"
EXEC Rhetos.DataMigrationUse '{concept.Module.Name}', '{concept.Name}', 'ID', 'uniqueidentifier';{DataMigrationUseTag.Evaluate(concept)}
GO

DECLARE @entries TABLE (ID uniqueidentifier PRIMARY KEY);
{InsertValuesTag.Evaluate(concept)}

INSERT INTO _{concept.Module.Name}.{concept.Name} (ID)
SELECT newItem.ID
FROM @entries newItem
	LEFT JOIN _{concept.Module.Name}.{concept.Name} existingItem ON existingItem.ID = newItem.ID
WHERE
	existingItem.ID IS NULL;
{UpdateTag.Evaluate(concept)}

DECLARE @ColumnNames nvarchar(max);
SET @ColumnNames = 'ID'{DataMigrationApplyMultipleTag.Evaluate(concept)};
EXEC Rhetos.DataMigrationApplyMultiple '{concept.Module.Name}', '{concept.Name}', @ColumnNames;");

            codeBuilder.AddAfterDataMigrationScript(
$@"EXEC Rhetos.DataMigrationUse '{concept.Module.Name}', '{concept.Name}', 'ID', 'uniqueidentifier';
GO

DECLARE @entries TABLE (ID uniqueidentifier PRIMARY KEY);
{InsertValuesTag.Evaluate(concept)}

DELETE FROM _{concept.Module.Name}.{concept.Name} WHERE ID NOT IN
(
    SELECT ID FROM @entries
);

EXEC Rhetos.DataMigrationApplyMultiple '{concept.Module.Name}', '{concept.Name}', 'ID';");
        }
    }
}
