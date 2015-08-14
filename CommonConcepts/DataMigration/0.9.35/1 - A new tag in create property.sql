/*DATAMIGRATION E2B91BB7-FED3-45DB-AFB1-61CEF615C2C3*/

UPDATE
	Rhetos.AppliedConcept
SET
	CreateQuery = CreateQuery + CHAR(13) + CHAR(10) + '/*PropertyInfo AfterCreate ' + SUBSTRING(ConceptInfoKey, 14, 256) + '*/'
WHERE
	ConceptInfoKey LIKE 'PropertyInfo %'
	AND CreateQuery LIKE '%ALTER TABLE%DataMigrationApply%'
	AND CreateQuery NOT LIKE '%/*PropertyInfo AfterCreate ' + SUBSTRING(ConceptInfoKey, 14, 256) + '*/%'
