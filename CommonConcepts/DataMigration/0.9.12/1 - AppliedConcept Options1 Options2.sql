/*DATAMIGRATION 533F534B-388C-4CE1-B8AE-A308542471F5*/

-- This script updates all properties' metadata to the new version, in order to avoid dropping and creating all the related columns.

UPDATE
	Rhetos.AppliedConcept
SET
	CreateQuery = STUFF(CreateQuery, CHARINDEX(
		';' + CHAR(13) + CHAR(10) + 'EXEC Rhetos.DataMigrationApply',
		CreateQuery), 0, ' /*' + REPLACE(ConceptInfoKey, ' ', ' Options1 ') + '*/ /*' + REPLACE(ConceptInfoKey, ' ', ' Options2 ') + '*/')
WHERE
	ConceptInfoKey LIKE 'PropertyInfo %'
	AND CreateQuery LIKE 'ALTER TABLE % ADD %;' + CHAR(13) + CHAR(10) + 'EXEC Rhetos.DataMigrationApply %;'
	AND CreateQuery NOT LIKE '%PropertyInfo Options1%';
