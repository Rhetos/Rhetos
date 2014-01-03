/*DATAMIGRATION E800503D-5BBC-482F-AEFF-DF7F57338C1B*/ -- Change this code only if the script needs to be executed again.

--EXEC Rhetos.HelpDataMigration 'Common', 'Permission'
EXEC Rhetos.DataMigrationUse 'Common', 'Permission', 'ID', 'uniqueidentifier';
EXEC Rhetos.DataMigrationUse 'Common', 'Permission', 'ClaimID', 'uniqueidentifier';
EXEC Rhetos.DataMigrationUse 'Common', 'Permission', 'IsAuthorized', 'bit';
EXEC Rhetos.DataMigrationUse 'Common', 'Permission', 'PrincipalID', 'uniqueidentifier';
EXEC Rhetos.DataMigrationUse 'Common', 'Permission', 'RoleID', 'uniqueidentifier';
--EXEC Rhetos.HelpDataMigration 'Common', 'Principal'
EXEC Rhetos.DataMigrationUse 'Common', 'Principal', 'ID', 'uniqueidentifier';
EXEC Rhetos.DataMigrationUse 'Common', 'Principal', 'Name', 'nvarchar(256)';
--EXEC Rhetos.HelpDataMigration 'Common', 'Role'
EXEC Rhetos.DataMigrationUse 'Common', 'Role', 'ID', 'uniqueidentifier';
EXEC Rhetos.DataMigrationUse 'Common', 'Role', 'Name', 'nvarchar(256)';
--EXEC Rhetos.HelpDataMigration 'Common', 'PrincipalHasRole'
EXEC Rhetos.DataMigrationUse 'Common', 'PrincipalHasRole', 'ID', 'uniqueidentifier';
EXEC Rhetos.DataMigrationUse 'Common', 'PrincipalHasRole', 'PrincipalID', 'uniqueidentifier';
EXEC Rhetos.DataMigrationUse 'Common', 'PrincipalHasRole', 'RoleID', 'uniqueidentifier';
GO

IF NOT EXISTS (SELECT TOP 1 1 FROM _Common.Role)
BEGIN
	INSERT INTO _Common.Role (ID, Name)
	SELECT NEWID(), pri.Name + ' role'
	FROM _Common.Principal pri;

	INSERT INTO _Common.PrincipalHasRole (ID, PrincipalID, RoleID)
	SELECT NEWID(), pri.ID, r.ID
	FROM _Common.Principal pri
	INNER JOIN _Common.Role r ON r.Name = pri.Name + ' role';

	UPDATE per
	SET RoleID = phr.RoleID
	FROM _Common.Permission per
	INNER JOIN _Common.PrincipalHasRole phr ON phr.PrincipalID = per.PrincipalID;
END

EXEC Rhetos.DataMigrationApplyMultiple 'Common', 'Role', 'ID, Name';
EXEC Rhetos.DataMigrationApplyMultiple 'Common', 'PrincipalHasRole', 'ID, PrincipalID, RoleID';
EXEC Rhetos.DataMigrationApplyMultiple 'Common', 'Permission', 'ID, RoleID';
