/*DATAMIGRATION 910326F5-B1E4-4E83-8006-3D2FEA0CFF27*/

-- This script modifies metadata for the generated indexes in database in order to optimize dbupdate to Rhetos v5.5.0:
-- In new Rhetos version, SqlIndexMultipleInfo generates additional tags (SQL comments) in SQL script that creates the index.
-- This scripts add these comments the the existing metadata for SqlIndexMultipleInfo, to avoid Rhetos detecting the changes and refreshing the indexes.

IF EXISTS (SELECT TOP 1 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'Rhetos' AND TABLE_NAME = 'AppliedConcept' AND COLUMN_NAME = 'ConceptInfoKey')
BEGIN

	EXEC('
		UPDATE Rhetos.AppliedConcept
		SET CreateQuery =
				SUBSTRING(CreateQuery, 1, CHARINDEX('')'', CreateQuery) + 1)
				+ ''/*'' + STUFF(ConceptInfoKey, 22, 0, ''Include '') + ''*/ /*'' + STUFF(ConceptInfoKey, 22, 0, ''Where '') + ''*/ ''
				+ SUBSTRING(CreateQuery, CHARINDEX('')'', CreateQuery) + 2, 8000)
		FROM
			Rhetos.AppliedConcept
		WHERE
			ConceptInfoKey LIKE ''SqlIndexMultipleInfo %''
			AND CHARINDEX('')'', CreateQuery) > 0
			AND CreateQuery NOT LIKE ''%) /*SqlIndexMultipleInfo Include%''
			');
END
