/*DATAMIGRATION C91DE223-880F-40E8-9D14-75883886B6EC*/ -- Change the script's code only if it needs to be executed again.

SELECT
	ID,
    OldCreateQuery = CreateQuery,
    SchemaName = SUBSTRING(CreateQuery, 15, 256),
    NewCreateQuery = CreateQuery + ' AUTHORIZATION dbo'
INTO
    #RhetosSchemas
FROM
    Rhetos.AppliedConcept
WHERE
    CreateQuery like 'CREATE SCHEMA %'
    AND CreateQuery NOT LIKE '% AUTHORIZATION dbo'
    AND ConceptInfoKey like 'ModuleInfo %';

SELECT
    AlterQuery = 'ALTER AUTHORIZATION ON SCHEMA::' + name + ' TO dbo'
INTO
    #AlterSchemas
FROM
    sys.schemas
WHERE
    (
        name IN (select SchemaName from #RhetosSchemas)
        OR name IN (select '_' + SchemaName from #RhetosSchemas)
    )
    AND USER_NAME(principal_id) <> 'dbo'


DECLARE @alterSchemaQuery nvarchar(MAX);
DECLARE createQueryCursor CURSOR FOR     
SELECT AlterQuery
FROM #AlterSchemas;
  
OPEN createQueryCursor;
  
FETCH NEXT FROM createQueryCursor
INTO @alterSchemaQuery;
  
WHILE @@FETCH_STATUS = 0    
BEGIN    
    EXEC sp_executesql @alterSchemaQuery;
  
	FETCH NEXT FROM createQueryCursor
	INTO @alterSchemaQuery;
   
END
CLOSE createQueryCursor;
DEALLOCATE createQueryCursor;

UPDATE ac
SET CreateQuery = rs.NewCreateQuery
FROM
	Rhetos.AppliedConcept ac
	INNER JOIN #RhetosSchemas rs on rs.ID = ac.ID

DROP TABLE #RhetosSchemas;
DROP TABLE #AlterSchemas;