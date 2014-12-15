/*DATAMIGRATION 2ABDA524-0285-4128-9FF4-8ABB2A1C0821*/ -- Change this code only if the script needs to be executed again.

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

IF EXISTS (SELECT TOP 1 1 FROM _Common.Permission WHERE RoleID IS NOT NULL AND PrincipalID IS NULL)
BEGIN

	SELECT
		RoleID = rol.ID, PrincipalID = NEWID(), PrincipalName = rol.Name
	INTO
		#roleToPrincipal
	FROM
		_Common.Role rol;
	
	UPDATE
		#roleToPrincipal
	SET
		PrincipalName = SUBSTRING(PrincipalName, 1, LEN(PrincipalName) - 5)
	WHERE
		RIGHT(PrincipalName, 5) = ' role'
		AND SUBSTRING(PrincipalName, 1, LEN(PrincipalName) - 5)
			NOT IN (SELECT PrincipalName FROM #roleToPrincipal)

	UPDATE
		rtp
	SET
		PrincipalID = pri.ID
	FROM	
		#roleToPrincipal rtp
		INNER JOIN _Common.Principal pri ON pri.Name = rtp.PrincipalName;

	INSERT INTO
		_Common.Principal (ID, Name)
	SELECT
		rtp.PrincipalID, rtp.PrincipalName
	FROM
		#roleToPrincipal rtp
		LEFT JOIN _Common.Principal pri ON pri.Name = rtp.PrincipalName
	WHERE
		pri.ID IS NULL;

	UPDATE
		per
	SET
		PrincipalID = rtp.PrincipalID
	FROM
		_Common.Permission per
		INNER JOIN #roleToPrincipal rtp ON rtp.RoleID = per.RoleID;

	DROP TABLE #roleToPrincipal;
END

DELETE p FROM _Common.Permission p LEFT JOIN _Common.Claim c ON c.ID = p.ClaimID WHERE c.ID IS NULL;

EXEC Rhetos.DataMigrationApplyMultiple 'Common', 'Principal', 'ID, Name';
EXEC Rhetos.DataMigrationApplyMultiple 'Common', 'Permission', 'ID, PrincipalID';
