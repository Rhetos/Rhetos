/*DATAMIGRATION C91DE223-880F-40E8-9D14-75883886B6EC*/ -- Change the script's code only if it needs to be executed again.

SELECT
    ID,
    SchemaName = SUBSTRING(CreateQuery, 15, 256)
INTO
    #RhetosSchemas
FROM
    Rhetos.AppliedConcept
WHERE
    CreateQuery like 'CREATE SCHEMA %'
    AND ConceptInfoKey like 'ModuleInfo %';

DECLARE @sql nvarchar(max) = '';

SELECT
    @sql = @sql + 'ALTER AUTHORIZATION ON SCHEMA::' + name + ' TO dbo;
'
FROM
    sys.schemas
WHERE
    (
        name IN (select SchemaName from #RhetosSchemas)
        OR name IN (select '_' + SchemaName from #RhetosSchemas)
    )
    AND principal_id <> USER_ID('dbo');

EXEC sp_executesql @sql;

UPDATE
    ac
SET
    CreateQuery = ac.CreateQuery + ' AUTHORIZATION dbo'
FROM
    Rhetos.AppliedConcept ac
    INNER JOIN #RhetosSchemas rs on rs.ID = ac.ID
WHERE
    ac.CreateQuery NOT LIKE '% AUTHORIZATION dbo';

DROP TABLE #RhetosSchemas;
