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
--EXEC Rhetos.HelpDataMigration 'Common', 'Claim'
EXEC Rhetos.DataMigrationUse 'Common', 'Claim', 'ID', 'uniqueidentifier';
GO

IF EXISTS (SELECT TOP 1 1 FROM _Common.Permission WHERE RoleID IS NULL AND PrincipalID IS NOT NULL)
BEGIN
	CREATE TABLE #principalToRole (PrincipalID UNIQUEIDENTIFIER, RoleID UNIQUEIDENTIFIER);
	
	INSERT INTO
		#principalToRole (PrincipalID)
	SELECT
		pri.ID
	FROM
		_Common.Principal pri
		INNER JOIN (SELECT DISTINCT PrincipalID FROM _Common.Permission) per ON per.PrincipalID = pri.ID;

	INSERT INTO
		_Common.Role (ID, Name)
	SELECT
		NEWID(), pri.Name + ' role'
	FROM
		#principalToRole ptr
		INNER JOIN _Common.Principal pri ON pri.ID = ptr.PrincipalID
		LEFT JOIN _Common.Role r ON r.Name = pri.Name OR r.Name = pri.Name + ' role'
	WHERE
		r.ID IS NULL;

	UPDATE
		ptr
	SET
		RoleID = r.ID
	FROM
		#principalToRole ptr
		INNER JOIN _Common.Principal pri ON pri.ID = ptr.PrincipalID
		LEFT JOIN _Common.Role r ON r.Name = pri.Name OR r.Name = pri.Name + ' role';

	INSERT INTO
		_Common.PrincipalHasRole (ID, PrincipalID, RoleID)
	SELECT
		NEWID(), PrincipalID, RoleID
	FROM
		#principalToRole

	UPDATE
		per
	SET
		RoleID = ptr.RoleID
	FROM
		_Common.Permission per
		INNER JOIN #principalToRole ptr ON ptr.PrincipalID = per.PrincipalID;
END

DELETE p FROM _Common.Permission p LEFT JOIN _Common.Claim c ON c.ID = p.ClaimID WHERE c.ID IS NULL;

EXEC Rhetos.DataMigrationApplyMultiple 'Common', 'Role', 'ID, Name';
EXEC Rhetos.DataMigrationApplyMultiple 'Common', 'PrincipalHasRole', 'ID, PrincipalID, RoleID';
EXEC Rhetos.DataMigrationApplyMultiple 'Common', 'Permission', 'ID, RoleID';
