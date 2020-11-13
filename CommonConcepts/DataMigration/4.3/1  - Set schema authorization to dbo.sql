/*DATAMIGRATION 191DE223-880F-40E8-9D14-75883886B6E1*/ -- Change the script's code only if it needs to be executed again.

SELECT
    SchemaName = SUBSTRING(ConceptInfoKey, 12, 256)
INTO
    #RhetosSchemas
FROM
    Rhetos.AppliedConcept
WHERE
    CreateQuery LIKE 'CREATE SCHEMA %'
    AND ConceptInfoKey LIKE 'ModuleInfo %';

DECLARE @sql nvarchar(max) = '';

SELECT
    @sql = @sql + 'ALTER AUTHORIZATION ON SCHEMA::[' + name + '] TO dbo;
'
FROM
    sys.schemas
WHERE
    (
        name IN (SELECT SchemaName FROM #RhetosSchemas)
        OR name IN (SELECT '_' + SchemaName FROM #RhetosSchemas)
    )
    AND principal_id <> USER_ID('dbo');

PRINT @sql;
EXEC sp_executesql @sql;

DROP TABLE #RhetosSchemas;
