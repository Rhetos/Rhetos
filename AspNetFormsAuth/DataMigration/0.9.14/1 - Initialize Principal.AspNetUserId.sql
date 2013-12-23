/*DATAMIGRATION C68D1E92-C433-45F9-A2B5-1867C121841C*/ -- Change this code only if the script needs to be executed again.

--EXEC Rhetos.HelpDataMigration 'Common', 'Principal'
EXEC Rhetos.DataMigrationUse 'Common', 'Principal', 'ID', 'uniqueidentifier';
EXEC Rhetos.DataMigrationUse 'Common', 'Principal', 'Name', 'nvarchar(256)';
EXEC Rhetos.DataMigrationUse 'Common', 'Principal', 'AspNetUserId', 'integer';
GO

DECLARE @lastId INTEGER;
SELECT @lastId = ISNULL(MAX(AspNetUserId), 0) FROM _Common.Principal;

SELECT ID, GeneratedId = @lastId + ROW_NUMBER() OVER (ORDER BY Name)
INTO #PrincipalNewId
FROM _Common.Principal WHERE AspNetUserId IS NULL;

UPDATE cp
SET AspNetUserId = GeneratedId
FROM _Common.Principal cp
INNER JOIN #PrincipalNewId pni ON pni.ID = cp.ID;

DROP TABLE #PrincipalNewId;

EXEC Rhetos.DataMigrationApplyMultiple 'Common', 'Principal', 'ID, Name, AspNetUserId';
