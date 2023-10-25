/*DATAMIGRATION 323D7F48-433F-49E9-983B-09A4B7317077*/ -- Change this code only if the script needs to be executed again.

--EXEC Rhetos.HelpDataMigration 'Common', 'Claim'
EXEC Rhetos.DataMigrationUse 'Common', 'Claim', 'ID', 'uniqueidentifier';
EXEC Rhetos.DataMigrationUse 'Common', 'Claim', 'Active', 'bit';
GO

UPDATE _Common.Claim SET Active = 1 WHERE Active IS NULL;

EXEC Rhetos.DataMigrationApplyMultiple 'Common', 'Claim', 'ID, Active';
