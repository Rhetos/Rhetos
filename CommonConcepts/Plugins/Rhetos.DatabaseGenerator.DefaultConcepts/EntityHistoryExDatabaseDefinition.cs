/*
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
    [ExportMetadata(MefProvider.Implements, typeof(EntityHistoryExInfo))]
    public class EntityHistoryExDatabaseDefinition : IConceptDatabaseDefinition
    {
        public static readonly DataStructureTag SelectHistoryProperties =
            new DataStructureTag(TagType.Appendable, "/*EntityHistory SelectHistoryProperties {0}.{1}*/");

        public static readonly DataStructureTag SelectEntityProperties =
            new DataStructureTag(TagType.Appendable, "/*EntityHistory SelectEntityProperties {0}.{1}*/");

        protected string SqlCreate(EntityHistoryExInfo info)
        {
            return string.Format(
@"
CREATE VIEW {0}.{3}_ActiveUntil
AS
SELECT
	history.ID,
	ActiveUntil = COALESCE(MIN(newerVersion.ActiveSince), MIN(currentItem.ActiveSince)) 
FROM {0}.{1}_History history
	LEFT JOIN {0}.{1}_History newerVersion ON 
				newerVersion.EntityID = history.EntityID AND 
				newerVersion.ActiveSince > history.ActiveSince
	INNER JOIN {0}.{1} currentItem ON currentItem.ID = history.EntityID
GROUP BY history.ID

{5}

CREATE VIEW {0}.{4}
AS
    SELECT
        ID = entity.ID,
        ActiveUntil = CAST(NULL AS DateTime),
        EntityID = entity.ID{7}
    FROM
        {0}.{1} entity

    UNION ALL

    SELECT
        ID = history.ID,
        au.ActiveUntil,
        EntityID = history.EntityID{6}
    FROM
        {0}.{3} history
        INNER JOIN {0}.{3}_ActiveUntil au ON au.ID = history.ID

{5}

CREATE FUNCTION {0}.{2} (@ContextTime DATETIME)
RETURNS TABLE
AS
RETURN
	SELECT
        ID = history.EntityID,
        ActiveUntil,
        EntityID = history.EntityID{6}
    FROM
        {0}.{4} history
        INNER JOIN
        (
            SELECT EntityID, Max_ActiveSince = MAX(ActiveSince)
            FROM {0}.{4}
            WHERE ActiveSince <= @ContextTime
            GROUP BY EntityID
        ) last ON last.EntityID = history.EntityID AND last.Max_ActiveSince = history.ActiveSince
",
                SqlUtility.Identifier(info.Entity.Module.Name),
                SqlUtility.Identifier(info.Entity.Name),
                SqlUtility.Identifier(info.Entity.Name + "_AtTime"), //2
                SqlUtility.Identifier(info.Entity.Name + "_History"), //3
                SqlUtility.Identifier(info.Entity.Name + "_FullHistory"), //4
                SqlUtility.ScriptSplitter, //5
                SelectHistoryProperties.Evaluate(info.Entity), //6
                SelectEntityProperties.Evaluate(info.Entity)); //7
        }

        public string CreateDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (EntityHistoryExInfo)conceptInfo;
            return SqlCreate(info);
        }

        public string RemoveDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (EntityHistoryExInfo)conceptInfo;

            return string.Format(
@"DROP FUNCTION {0}.{1};
DROP VIEW {0}.{2};",
                SqlUtility.Identifier(info.Entity.Module.Name),
                SqlUtility.Identifier(info.Entity.Name + "_AtTime"),
                SqlUtility.Identifier(info.Entity.Name + "_FullHistory"));
        }
    }
}
