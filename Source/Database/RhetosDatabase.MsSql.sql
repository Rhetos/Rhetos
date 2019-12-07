--=================================================
-- NEVER CHANGE THIS SCRIPT, ONLY APPEND AT THE END.
--=================================================

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'Rhetos')
	EXEC ('CREATE SCHEMA Rhetos AUTHORIZATION dbo');

IF OBJECT_ID(N'[Rhetos].[DslScript]') IS NULL
CREATE TABLE Rhetos.DslScript
(
	ID uniqueidentifier NOT NULL CONSTRAINT PK_DslScript PRIMARY KEY CLUSTERED, -- Later changes to nonclustered.
	Name nvarchar(100) NOT NULL,
	Dsl nvarchar(max) NOT NULL,
	LastModified datetime NOT NULL,
	AppliedBy nvarchar(100) NOT NULL,
	Client nvarchar(100) NOT NULL,
	Server nvarchar(100) NOT NULL
);

IF OBJECT_ID(N'[Rhetos].[DatabaseGeneratorAppliedConcept]') IS NULL AND OBJECT_ID(N'[Rhetos].[AppliedConcept]') IS NULL
CREATE TABLE Rhetos.DatabaseGeneratorAppliedConcept
(
	ID uniqueidentifier NOT NULL CONSTRAINT PK_DatabaseGeneratorAppliedConcept PRIMARY KEY NONCLUSTERED, -- Later changes to nonclustered.
	InfoType nvarchar(max) NOT NULL,
	ImplementationType nvarchar(max) NOT NULL,
	SerializedInfo nvarchar(max) NOT NULL,
	LastModified datetime NOT NULL,
	AppliedBy nvarchar(200) NULL,
	Client nvarchar(200) NULL,
	Server nvarchar(200) NULL,
	ModificationOrder int IDENTITY(1,1) NOT NULL,
	CreateQuery nvarchar(max) NOT NULL,
	DependsOn nvarchar(max) NOT NULL,
	ConceptImplementationVersion nvarchar(200) NOT NULL
);

IF OBJECT_ID(N'[Rhetos].[DF_DslScript_ID]') IS NULL
ALTER TABLE Rhetos.DslScript ADD CONSTRAINT DF_DslScript_ID DEFAULT (newid()) FOR ID;

IF OBJECT_ID(N'[Rhetos].[DF_DslScript_LastModified]') IS NULL
ALTER TABLE Rhetos.DslScript ADD CONSTRAINT DF_DslScript_LastModified DEFAULT (getdate()) FOR LastModified;

IF OBJECT_ID(N'[Rhetos].[DF_DslScript_AppliedBy]') IS NULL
ALTER TABLE Rhetos.DslScript ADD CONSTRAINT DF_DslScript_AppliedBy DEFAULT (upper(isnull(suser_sname(),user_name()))) FOR AppliedBy;

IF OBJECT_ID(N'[Rhetos].[DF_DslScript_Client]') IS NULL
ALTER TABLE Rhetos.DslScript ADD CONSTRAINT DF_DslScript_Client DEFAULT (upper(host_name())) FOR Client;

IF OBJECT_ID(N'[Rhetos].[DF_DslScript_Server]') IS NULL
ALTER TABLE Rhetos.DslScript ADD CONSTRAINT DF_DslScript_Server DEFAULT (upper(@@servername)) FOR Server;

IF OBJECT_ID(N'[Rhetos].[DF_DatabaseGeneratorAppliedConcept_LastModified]') IS NULL AND OBJECT_ID(N'[Rhetos].[AppliedConcept]') IS NULL
ALTER TABLE Rhetos.DatabaseGeneratorAppliedConcept ADD CONSTRAINT DF_DatabaseGeneratorAppliedConcept_LastModified DEFAULT (getdate()) FOR LastModified;

IF OBJECT_ID(N'[Rhetos].[DF_DatabaseGeneratorAppliedConcept_AppliedBy]') IS NULL AND OBJECT_ID(N'[Rhetos].[AppliedConcept]') IS NULL
ALTER TABLE Rhetos.DatabaseGeneratorAppliedConcept ADD CONSTRAINT DF_DatabaseGeneratorAppliedConcept_AppliedBy DEFAULT (upper(isnull(suser_sname(),user_name()))) FOR AppliedBy;

IF OBJECT_ID(N'[Rhetos].[DF_DatabaseGeneratorAppliedConcept_Client]') IS NULL AND OBJECT_ID(N'[Rhetos].[AppliedConcept]') IS NULL
ALTER TABLE Rhetos.DatabaseGeneratorAppliedConcept ADD CONSTRAINT DF_DatabaseGeneratorAppliedConcept_Client DEFAULT (upper(host_name())) FOR Client;

IF OBJECT_ID(N'[Rhetos].[DF_DatabaseGeneratorAppliedConcept_Server]') IS NULL AND OBJECT_ID(N'[Rhetos].[AppliedConcept]') IS NULL
ALTER TABLE Rhetos.DatabaseGeneratorAppliedConcept ADD CONSTRAINT DF_DatabaseGeneratorAppliedConcept_Server DEFAULT (upper(@@servername)) FOR Server;

IF INDEXPROPERTY(OBJECT_ID(N'[Rhetos].[DslScript]'), 'PK_DslScript','IsClustered') = 1
BEGIN
	ALTER TABLE Rhetos.DslScript DROP CONSTRAINT PK_DslScript;
	ALTER TABLE Rhetos.DslScript ADD CONSTRAINT PK_DslScript PRIMARY KEY NONCLUSTERED (ID);
END

IF INDEXPROPERTY(OBJECT_ID(N'[Rhetos].[DatabaseGeneratorAppliedConcept]'), 'PK_DatabaseGeneratorAppliedConcept','IsClustered') = 1 AND OBJECT_ID(N'[Rhetos].[AppliedConcept]') IS NULL
BEGIN
	ALTER TABLE Rhetos.DatabaseGeneratorAppliedConcept DROP CONSTRAINT PK_DatabaseGeneratorAppliedConcept;
	ALTER TABLE Rhetos.DatabaseGeneratorAppliedConcept ADD CONSTRAINT PK_DatabaseGeneratorAppliedConcept PRIMARY KEY NONCLUSTERED (ID);
END

IF OBJECT_ID(N'[Rhetos].[DataMigrationScript]') IS NULL
CREATE TABLE Rhetos.DataMigrationScript
(
	ID uniqueidentifier NOT NULL CONSTRAINT PK_DataMigrationScript PRIMARY KEY NONCLUSTERED CONSTRAINT DF_DataMigrationScript_ID DEFAULT (newid()),
	Tag nvarchar(200) NOT NULL,
	Path nvarchar(200) NOT NULL,
	Content nvarchar(max) NOT NULL,
	DateExecuted datetime NOT NULL CONSTRAINT DF_DataMigrationScript_LastModified DEFAULT (getdate()),
	ExecutedBy nvarchar(200) NULL CONSTRAINT DF_DataMigrationScript_AppliedBy DEFAULT (upper(isnull(suser_sname(),user_name()))),
	Client nvarchar(200) NULL CONSTRAINT DF_DataMigrationScript_Client DEFAULT (upper(host_name())),
	Server nvarchar(200) NULL CONSTRAINT DF_DataMigrationScript_Server DEFAULT (upper(@@servername)),
	OrderExecuted int IDENTITY(1,1) NOT NULL
);

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[Rhetos].[DataMigrationScript]') AND name = 'IX_DataMigrationScript_Tag')
CREATE UNIQUE INDEX IX_DataMigrationScript_Tag ON Rhetos.DataMigrationScript(Tag);

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Rhetos].[DslScript]') AND name = N'Name' AND max_length = 200)
BEGIN
	ALTER TABLE Rhetos.DslScript ALTER COLUMN Name nvarchar(256) NOT NULL;
	ALTER TABLE Rhetos.DslScript ALTER COLUMN AppliedBy nvarchar(256) NOT NULL;
	ALTER TABLE Rhetos.DslScript ALTER COLUMN Client nvarchar(256) NOT NULL;
	ALTER TABLE Rhetos.DslScript ALTER COLUMN Server nvarchar(256) NOT NULL;
	
	ALTER TABLE Rhetos.DatabaseGeneratorAppliedConcept ALTER COLUMN AppliedBy nvarchar(256) NULL;
	ALTER TABLE Rhetos.DatabaseGeneratorAppliedConcept ALTER COLUMN Client nvarchar(256) NULL;
	ALTER TABLE Rhetos.DatabaseGeneratorAppliedConcept ALTER COLUMN Server nvarchar(256) NULL;
	ALTER TABLE Rhetos.DatabaseGeneratorAppliedConcept ALTER COLUMN ConceptImplementationVersion nvarchar(256) NOT NULL;

	ALTER TABLE Rhetos.DataMigrationScript ALTER COLUMN Tag nvarchar(256) NOT NULL;
	ALTER TABLE Rhetos.DataMigrationScript ALTER COLUMN Path nvarchar(256) NOT NULL;
	ALTER TABLE Rhetos.DataMigrationScript ALTER COLUMN ExecutedBy nvarchar(256) NULL;
	ALTER TABLE Rhetos.DataMigrationScript ALTER COLUMN Client nvarchar(256) NULL;
	ALTER TABLE Rhetos.DataMigrationScript ALTER COLUMN Server nvarchar(256) NULL;
END

IF OBJECT_ID(N'Rhetos.AppliedConcept') IS NULL
BEGIN
	EXEC sp_rename 'Rhetos.DatabaseGeneratorAppliedConcept', 'AppliedConcept';
	EXEC sp_rename 'Rhetos.AppliedConcept.PK_DatabaseGeneratorAppliedConcept', 'PK_AppliedConcept';
	EXEC sp_rename 'Rhetos.DF_DatabaseGeneratorAppliedConcept_LastModified', 'DF_AppliedConcept_LastModified';
	EXEC sp_rename 'Rhetos.DF_DatabaseGeneratorAppliedConcept_AppliedBy', 'DF_AppliedConcept_AppliedBy';
	EXEC sp_rename 'Rhetos.DF_DatabaseGeneratorAppliedConcept_Client', 'DF_AppliedConcept_Client';
	EXEC sp_rename 'Rhetos.DF_DatabaseGeneratorAppliedConcept_Server', 'DF_AppliedConcept_Server';
END

GO

IF OBJECT_ID('Rhetos.GetColumnType') IS NULL
EXEC ('CREATE FUNCTION Rhetos.GetColumnType
    (@SchemaName NVARCHAR(256), @TableName NVARCHAR(256), @ColumnName NVARCHAR(256))
RETURNS NVARCHAR(256)
AS
BEGIN
RETURN
    (SELECT CASE
        WHEN DATA_TYPE IN (''decimal'', ''numeric'')
            THEN DATA_TYPE + ''('' + CONVERT(nvarchar(100), NUMERIC_PRECISION) + '', '' + CONVERT(nvarchar(100), NUMERIC_PRECISION_RADIX) + '')''
        WHEN DATA_TYPE IN (''varbinary'', ''varchar'', ''binary'', ''char'', ''nvarchar'', ''nchar'')
            THEN CASE WHEN CHARACTER_MAXIMUM_LENGTH > 0
                THEN DATA_TYPE + ''('' + CONVERT(nvarchar(100), CHARACTER_MAXIMUM_LENGTH) + '')''
                ELSE DATA_TYPE + ''(MAX)''
            END
        ELSE DATA_TYPE END
    FROM
        INFORMATION_SCHEMA.COLUMNS
    WHERE
        TABLE_SCHEMA = @SchemaName AND TABLE_NAME = @TableName AND COLUMN_NAME = @ColumnName)
END')

IF OBJECT_ID('Rhetos.DataMigrationApply') IS NULL
	EXEC ('CREATE PROCEDURE Rhetos.DataMigrationApply AS SET NOCOUNT ON RAISERROR (''Procedure creation has not finished.'', 16, 62)')
GO
ALTER PROCEDURE Rhetos.DataMigrationApply
	@SchemaName NVARCHAR(256), @TableName NVARCHAR(256), @ColumnName NVARCHAR(256)
AS
	-- Standard error-handling header
	DECLARE @InitialTranCount INT
	SET @InitialTranCount = @@TRANCOUNT
	DECLARE @TranName VARCHAR(38)
	SET @TranName = NEWID()
	IF @InitialTranCount = 0 BEGIN TRANSACTION @TranName
	ELSE SAVE TRANSACTION @TranName
	DECLARE @Error INT
	SET @Error = 0

	IF CHARINDEX(']', @SchemaName) > 0 OR CHARINDEX('''', @SchemaName) > 0
	BEGIN ROLLBACK TRANSACTION @TranName RAISERROR('Invalid character in @SchemaName %s', 16, 10, @SchemaName) RETURN 50000 END

	IF CHARINDEX(']', @TableName) > 0 OR CHARINDEX('''', @TableName) > 0
	BEGIN ROLLBACK TRANSACTION @TranName RAISERROR('Invalid character in @TableName %s', 16, 10, @TableName) RETURN 50000 END

	IF CHARINDEX(']', @ColumnName) > 0 OR CHARINDEX('''', @ColumnName) > 0
	BEGIN ROLLBACK TRANSACTION @TranName RAISERROR('Invalid character in @ColumnName %s', 16, 10, @ColumnName) RETURN 50000 END

	DECLARE @MigrationSchemaName NVARCHAR(256)
	SET @MigrationSchemaName = '_' + @SchemaName
    
    DECLARE @ColumnType NVARCHAR(256)
    SET @ColumnType = Rhetos.GetColumnType(@SchemaName, @TableName, @ColumnName)
    DECLARE @ExistingMigrationColumnType NVARCHAR(256)
    SET @ExistingMigrationColumnType = Rhetos.GetColumnType(@MigrationSchemaName, @TableName, @ColumnName)

	IF @ColumnType IS NULL
        PRINT 'Column ' + @SchemaName + '.' + @TableName + '.' + @ColumnName + ' does not exist. It will be safely ignored.'
    ELSE IF @ExistingMigrationColumnType IS NULL
        PRINT 'Data-migration column ' + @MigrationSchemaName + '.' + @TableName + '.' + @ColumnName + ' does not exist. It will be safely ignored.'
	ELSE IF @ColumnName = 'ID'
	BEGIN

		EXEC ('
            DELETE FROM
                [' + @SchemaName + '].[' + @TableName + ']
            WHERE
                ID NOT IN (SELECT ID FROM [' + @MigrationSchemaName + '].[' + @TableName + '])');
		SET @Error = @@ERROR IF @Error > 0 BEGIN ROLLBACK TRANSACTION @TranName RETURN @Error END
        
        EXEC ('
            INSERT INTO
                [' + @SchemaName + '].[' + @TableName + '] (ID)
            SELECT
                ID
            FROM
                [' + @MigrationSchemaName + '].[' + @TableName + ']
            WHERE
                ID NOT IN (SELECT ID FROM [' + @SchemaName + '].[' + @TableName + '])');
		SET @Error = @@ERROR IF @Error > 0 BEGIN ROLLBACK TRANSACTION @TranName RETURN @Error END
        
	END
	ELSE
	BEGIN
    
		IF @ColumnType <> @ExistingMigrationColumnType
        BEGIN
            PRINT 'Automatically changing data-migration column type from ' + @ExistingMigrationColumnType + ' to ' + @ColumnType + ' for column ' + @SchemaName + '.' + @TableName + '.'  + @ColumnName+ '.'
            EXEC ('ALTER TABLE [' + @MigrationSchemaName + '].[' + @TableName + '] ALTER COLUMN [' + @ColumnName + '] ' + @ColumnType);
            SET @Error = @@ERROR IF @Error > 0 BEGIN ROLLBACK TRANSACTION @TranName RETURN @Error END
        END
	
        EXEC ('
            UPDATE
                original
            SET
                [' + @ColumnName + '] = migration.[' + @ColumnName + ']
            FROM
                [' + @SchemaName + '].[' + @TableName + '] original
                INNER JOIN [' + @MigrationSchemaName + '].[' + @TableName + '] migration ON migration.ID = original.ID
            WHERE
                original.[' + @ColumnName + '] <> migration.[' + @ColumnName + ']
                OR original.[' + @ColumnName + '] IS NULL AND migration.[' + @ColumnName + '] IS NOT NULL
                OR original.[' + @ColumnName + '] IS NOT NULL AND migration.[' + @ColumnName + '] IS NULL');
		SET @Error = @@ERROR IF @Error > 0 BEGIN ROLLBACK TRANSACTION @TranName RETURN @Error END
        
	END

	-- Standard error-handling footer
	IF @InitialTranCount = 0 COMMIT TRANSACTION @TranName
	RETURN 0
GO

IF OBJECT_ID('Rhetos.HelpDataMigration') IS NULL
	EXEC ('CREATE PROCEDURE Rhetos.HelpDataMigration AS SET NOCOUNT ON RAISERROR (''Procedure creation has not finished.'', 16, 62)')
GO
ALTER PROCEDURE Rhetos.HelpDataMigration
	@SchemaName NVARCHAR(256), @TableName NVARCHAR(256)
AS
    SET NOCOUNT ON
    
    IF LEFT(@SchemaName, 1) = '_'
      BEGIN RAISERROR('User a regular table, not a data-migration table %s.%s.', 16, 10, @SchemaName, @TableName) RETURN 50000 END
    
    SELECT columnName = COLUMN_NAME, columnType = Rhetos.GetColumnType(TABLE_SCHEMA, TABLE_NAME, COLUMN_NAME), sort = IDENTITY(INT)
        INTO #columns
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = @SchemaName AND TABLE_NAME = @TableName
        ORDER BY CASE WHEN COLUMN_NAME = 'ID' THEN -1 ELSE ORDINAL_POSITION END
    
    DECLARE @columnList VARCHAR(MAX)
    SET @columnList = ''
    SELECT @columnList = @columnList + CASE WHEN @columnList <> '' THEN ', ' ELSE '' END + columnName
    FROM #columns
    ORDER BY sort
    
    SELECT '/*DATAMIGRATION ' + CAST(NEWID() AS VARCHAR(40)) + '*/ -- Change the script''s code only if it needs to be executed again.'
    UNION ALL SELECT ''
    UNION ALL SELECT '-- The following lines are generated by: EXEC Rhetos.HelpDataMigration ''' + @SchemaName + ''', ''' + @TableName + ''';'
    UNION ALL
        SELECT 'EXEC Rhetos.DataMigrationUse ''' + @SchemaName + ''', ''' + @TableName + ''', ''' + columnName + ''', ''' + columnType + ''';'
        FROM #columns
    UNION ALL SELECT 'GO'
    UNION ALL SELECT ''
    UNION ALL SELECT '-- ... write the data migration queries here (don''t forget to use the underscore ''_'' in schema name) ...'
    UNION ALL SELECT ''
    UNION ALL SELECT 'EXEC Rhetos.DataMigrationApplyMultiple ''' + @SchemaName + ''', ''' + @TableName + ''', ''' + @columnList + ''';'
GO

IF OBJECT_ID('Rhetos.DataMigrationApplyMultiple') IS NULL
	EXEC ('CREATE PROCEDURE Rhetos.DataMigrationApplyMultiple AS SET NOCOUNT ON RAISERROR (''Procedure creation has not finished.'', 16, 62)')
GO
ALTER PROCEDURE Rhetos.DataMigrationApplyMultiple
    @SchemaName NVARCHAR(256), @TableName NVARCHAR(256), @ColumnNames NVARCHAR(MAX)
AS
    -- Standard error-handling header
    DECLARE @InitialTranCount INT
    SET @InitialTranCount = @@TRANCOUNT
    DECLARE @TranName VARCHAR(38)
    SET @TranName = NEWID()
    IF @InitialTranCount = 0 BEGIN TRANSACTION @TranName
    ELSE SAVE TRANSACTION @TranName
    DECLARE @Error INT
    SET @Error = 0
    
    SET NOCOUNT ON

    IF CHARINDEX(']', @SchemaName) > 0 OR CHARINDEX('''', @SchemaName) > 0
    BEGIN ROLLBACK TRANSACTION @TranName RAISERROR('Invalid character in @SchemaName %s', 16, 10, @SchemaName) RETURN 50000 END

    IF CHARINDEX(']', @TableName) > 0 OR CHARINDEX('''', @TableName) > 0
    BEGIN ROLLBACK TRANSACTION @TranName RAISERROR('Invalid character in @TableName %s', 16, 10, @TableName) RETURN 50000 END

    IF CHARINDEX(']', @ColumnNames) > 0 OR CHARINDEX('''', @ColumnNames) > 0
    BEGIN ROLLBACK TRANSACTION @TranName RAISERROR('Invalid character in @ColumnNames %s', 16, 10, @ColumnNames) RETURN 50000 END

    DECLARE @MigrationSchemaName NVARCHAR(256)
    SET @MigrationSchemaName = '_' + @SchemaName
    
    -- Rhetos.DataMigrationApplyMultiple will not automatically change the column type (unlike Rhetos.DataMigrationApply). That is good enough for use in data migration scripts, but cannot be used in DatabaseGenerator plugins.
    
    IF NOT EXISTS (SELECT TOP 1 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @SchemaName AND TABLE_NAME = @TableName AND TABLE_TYPE = 'BASE TABLE')
        PRINT 'Nothing to migrate. Table "' + @SchemaName + '.' + @TableName + '"" does not exist. It is expected to be created later during this upgrade.'
    ELSE IF NOT EXISTS (SELECT TOP 1 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = @MigrationSchemaName AND TABLE_NAME = @TableName)
        PRINT 'Nothing to migrate. Data-migration table "' + @MigrationSchemaName + '.' + @TableName + '"" is not prepared. Execute "Rhetos.DataMigrationUse" to prepare the migration table.'
    ELSE
    BEGIN

        -- Parse column names to @columns:
        
        SET @ColumnNames = REPLACE(REPLACE(REPLACE(@ColumnNames, CHAR(13), ' '), CHAR(10), ' '), CHAR(9), ' ');
        SET @ColumnNames = '<c>' + REPLACE(@ColumnNames, ',', '</c><c>') + '</c>'
        DECLARE @x XML
        SET @x = @ColumnNames
        DECLARE @columns TABLE (name NVARCHAR(256))
        INSERT INTO @columns SELECT RTRIM(LTRIM(x.col.value('.', 'nvarchar(256)'))) FROM @x.nodes('/c') AS x(col)
        IF NOT EXISTS (SELECT TOP 1 1 FROM @columns WHERE name = 'ID')
        BEGIN ROLLBACK TRANSACTION @TranName RAISERROR('Column "ID" must be listed in @ColumnNames.', 16, 10) RETURN 50000 END
        
        -- Remove columns that are not prepared for migration:
        
        DECLARE @killList TABLE (columnName NVARCHAR(256))
        
        INSERT INTO @killList SELECT name FROM @columns WHERE name NOT IN (SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @SchemaName AND TABLE_NAME = @TableName)
        IF EXISTS (SELECT TOP 1 1 FROM @killList)
        BEGIN
            SELECT 'Column ' + @SchemaName + '.' + @TableName + '.' + columnName + ' does not exist. It will be safely ignored.' FROM @killList
            DELETE FROM @columns WHERE name IN (SELECT columnName FROM @killList)
            DELETE FROM @killList
        END
        
        INSERT INTO @killList SELECT name FROM @columns WHERE name NOT IN (SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @MigrationSchemaName AND TABLE_NAME = @TableName)
        IF EXISTS (SELECT TOP 1 1 FROM @killList)
        BEGIN
            SELECT 'Data-migration column ' + @MigrationSchemaName + '.' + @TableName + '.' + columnName + ' does not exist. It will be safely ignored.' FROM @killList
            DELETE FROM @columns WHERE name IN (SELECT columnName FROM @killList)
            DELETE FROM @killList
        END
        
        -- Migrate data:
        
        DELETE FROM @columns WHERE name = 'ID'
        
        DECLARE @sqlDelete VARCHAR(MAX)
        DECLARE @sqlUpdate VARCHAR(MAX)
        DECLARE @sqlInsert VARCHAR(MAX)
        DECLARE @columns1sql VARCHAR(MAX)
        DECLARE @columns2sql VARCHAR(MAX)
        
        SET @sqlDelete = '
            DELETE FROM
                [' + @SchemaName + '].[' + @TableName + ']
            WHERE
                ID NOT IN (SELECT ID FROM [' + @MigrationSchemaName + '].[' + @TableName + '])'
        SET @Error = @@ERROR IF @Error > 0 BEGIN ROLLBACK TRANSACTION @TranName RETURN @Error END
        
        IF EXISTS (SELECT TOP 1 1 FROM @columns)
        BEGIN

            SET @columns1sql = ''
            SET @columns2sql = ''
            SELECT -- Generating 2 SQL parts at the same time to ensure same order of columns
                @columns1sql = @columns1sql + CASE WHEN @columns1sql <> '' THEN ',' ELSE '' END + '
                    [' + name + '] = migration.[' + name + ']',
                @columns2sql = @columns2sql + '
                    ' + CASE WHEN @columns2sql <> '' THEN 'OR ' ELSE '' END
                    + '(original.[' + name + '] <> migration.[' + name + '] OR original.[' + name + '] IS NULL AND migration.[' + name + '] IS NOT NULL OR original.[' + name + '] IS NOT NULL AND migration.[' + name + '] IS NULL)'
            FROM
                @columns
            SET @Error = @@ERROR IF @Error > 0 BEGIN ROLLBACK TRANSACTION @TranName RETURN @Error END
            
            SET @sqlUpdate = '
                UPDATE
                    original
                SET' + @columns1sql + '
                FROM
                    [' + @SchemaName + '].[' + @TableName + '] original
                    INNER JOIN [' + @MigrationSchemaName + '].[' + @TableName + '] migration ON migration.ID = original.ID
                WHERE' + @columns2sql
            SET @Error = @@ERROR IF @Error > 0 BEGIN ROLLBACK TRANSACTION @TranName RETURN @Error END
        
        END
        
        SET @columns1sql = ''
        SELECT
            @columns1sql = @columns1sql + ', ' + name
        FROM
            @columns
        SET @Error = @@ERROR IF @Error > 0 BEGIN ROLLBACK TRANSACTION @TranName RETURN @Error END
        
        SET @sqlInsert = '
            INSERT INTO
                [' + @SchemaName + '].[' + @TableName + '] (ID' + @columns1sql + ')
            SELECT
                ID' + @columns1sql + '
            FROM
                [' + @MigrationSchemaName + '].[' + @TableName + ']
            WHERE
                ID NOT IN (SELECT ID FROM [' + @SchemaName + '].[' + @TableName + '])'
        SET @Error = @@ERROR IF @Error > 0 BEGIN ROLLBACK TRANSACTION @TranName RETURN @Error END
        
        SET NOCOUNT OFF
        
        EXEC (@sqlDelete)
        SET @Error = @@ERROR IF @Error > 0 BEGIN ROLLBACK TRANSACTION @TranName RETURN @Error END
        
        IF @sqlUpdate IS NOT NULL EXEC (@sqlUpdate)
        SET @Error = @@ERROR IF @Error > 0 BEGIN ROLLBACK TRANSACTION @TranName RETURN @Error END
        
        EXEC (@sqlInsert)
        SET @Error = @@ERROR IF @Error > 0 BEGIN ROLLBACK TRANSACTION @TranName RETURN @Error END
        
    END

    -- Standard error-handling footer
    IF @InitialTranCount = 0 COMMIT TRANSACTION @TranName
    RETURN 0
GO

IF OBJECT_ID(N'[Rhetos].[AppliedConceptDependsOn]') IS NULL
BEGIN
	CREATE TABLE Rhetos.AppliedConceptDependsOn
	(
		ID uniqueidentifier NOT NULL CONSTRAINT PK_AppliedConceptDependsOn PRIMARY KEY NONCLUSTERED CONSTRAINT DF_AppliedConceptDependsOn_ID DEFAULT (newid()),
		DependentID uniqueidentifier NOT NULL CONSTRAINT FK_AppliedConceptDependsOn_Dependent FOREIGN KEY REFERENCES Rhetos.AppliedConcept (ID) ON DELETE CASCADE,
		DependsOnID uniqueidentifier NOT NULL CONSTRAINT FK_AppliedConceptDependsOn_DependsOn FOREIGN KEY REFERENCES Rhetos.AppliedConcept (ID),
		CONSTRAINT UQ_AppliedConceptDependsOn_DependentID_DependsOnID UNIQUE (DependentID, DependsOnID)
	);
END

IF EXISTS (SELECT TOP 1 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'Rhetos' AND TABLE_NAME = 'AppliedConcept' AND COLUMN_NAME = 'DependsOn')
BEGIN
	EXEC('INSERT INTO Rhetos.AppliedConceptDependsOn
		(DependentID, DependsOnID)
	SELECT
		DependentID = acXml.ID,
		DependsOnID = acSplit.dependsOnValue
	FROM
		(
			SELECT
				ID,
				xmlList = CONVERT(XML, ''<X>'' + REPLACE(DependsOn,'' '',''</X><X>'') + ''</X>'')
			FROM
				Rhetos.AppliedConcept
			WHERE
				DependsOn <> ''''
		) acXml
		CROSS APPLY
		(
			SELECT fdata.dependsOnElement.value(''.'',''uniqueidentifier'') as dependsOnValue
			FROM acXml.xmlList.nodes(''X'') as fdata(dependsOnElement)
		) acSplit
	WHERE
		acXml.ID IN (SELECT ID FROM Rhetos.AppliedConcept)
		AND acSplit.dependsOnValue IN (SELECT ID FROM Rhetos.AppliedConcept)');
	
	ALTER TABLE Rhetos.AppliedConcept DROP COLUMN DependsOn;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Rhetos].[AppliedConcept]') AND name = N'RemoveQuery')
BEGIN
	ALTER TABLE Rhetos.AppliedConcept
		ADD	RemoveQuery nvarchar(max) NOT NULL
		CONSTRAINT DF_AppliedConcept_RemoveQuery DEFAULT ('/*UNKNOWN*/');

	ALTER TABLE Rhetos.AppliedConcept
		DROP CONSTRAINT DF_AppliedConcept_RemoveQuery;
END

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Rhetos].[AppliedConcept]') AND name = N'ConceptInfoKey')
BEGIN
	ALTER TABLE Rhetos.AppliedConcept
		ADD	ConceptInfoKey nvarchar(max) NOT NULL
		CONSTRAINT DF_AppliedConcept_ConceptInfoKey DEFAULT ('/*UNKNOWN*/');

	ALTER TABLE Rhetos.AppliedConcept
		DROP CONSTRAINT DF_AppliedConcept_ConceptInfoKey;
END

IF OBJECT_ID('Rhetos.DataMigrationFreshRows') IS NULL
CREATE TABLE Rhetos.DataMigrationFreshRows
(
	ID UNIQUEIDENTIFIER NOT NULL
		CONSTRAINT PK_DataMigrationFreshRows PRIMARY KEY NONCLUSTERED
		CONSTRAINT DF_DataMigrationFreshRows_ID DEFAULT (NEWID()),
	OriginalSchemaName NVARCHAR(256) NOT NULL,
	TableName NVARCHAR(256) NOT NULL,
	CONSTRAINT UQ_DataMigrationFreshRows_Table UNIQUE (OriginalSchemaName, TableName)
);

IF OBJECT_ID('Rhetos.DataMigrationInitializeRows') IS NULL
	EXEC ('CREATE PROCEDURE Rhetos.DataMigrationInitializeRows AS SET NOCOUNT ON RAISERROR (''Procedure creation has not finished.'', 16, 62)')
GO
ALTER PROCEDURE Rhetos.DataMigrationInitializeRows (@OriginalSchemaName NVARCHAR(256), @TableName NVARCHAR(256))
AS
	-- Standard error-handling header
	DECLARE @InitialTranCount INT
	SET @InitialTranCount = @@TRANCOUNT
	DECLARE @TranName VARCHAR(38)
	SET @TranName = NEWID()
	IF @InitialTranCount = 0 BEGIN TRANSACTION @TranName
	ELSE SAVE TRANSACTION @TranName
	DECLARE @Error INT
	SET @Error = 0

	IF CHARINDEX(']', @OriginalSchemaName) > 0 OR CHARINDEX('''', @OriginalSchemaName) > 0
	BEGIN ROLLBACK TRANSACTION @TranName RAISERROR('Invalid character in @@OriginalSchemaName %s', 16, 10, @OriginalSchemaName) RETURN 50000 END

	IF CHARINDEX(']', @TableName) > 0 OR CHARINDEX('''', @TableName) > 0
	BEGIN ROLLBACK TRANSACTION @TranName RAISERROR('Invalid character in @TableName %s', 16, 10, @TableName) RETURN 50000 END

	IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @OriginalSchemaName AND TABLE_NAME = @TableName AND COLUMN_NAME = 'ID')
		AND EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = '_' + @OriginalSchemaName AND TABLE_NAME = @TableName AND COLUMN_NAME = 'ID')
		AND NOT EXISTS(SELECT * FROM Rhetos.DataMigrationFreshRows WHERE OriginalSchemaName = @OriginalSchemaName AND TableName = @TableName)
	BEGIN
		DECLARE @commonColumns NVARCHAR(max);
		SET @commonColumns = 'ID';

		SELECT
			@commonColumns = @commonColumns + N', ' + QUOTENAME(mig.COLUMN_NAME)
		FROM
			INFORMATION_SCHEMA.COLUMNS orig
			INNER JOIN INFORMATION_SCHEMA.COLUMNS mig
				ON mig.TABLE_SCHEMA = '_' + orig.TABLE_SCHEMA
				AND mig.TABLE_NAME = orig.TABLE_NAME
				AND mig.COLUMN_NAME = orig.COLUMN_NAME
		WHERE
			mig.COLUMN_NAME <> 'ID'
			AND orig.TABLE_SCHEMA = @OriginalSchemaName
			AND orig.TABLE_NAME = @TableName
		ORDER BY
			mig.TABLE_SCHEMA, mig.TABLE_NAME, mig.ORDINAL_POSITION;

		DECLARE @sql NVARCHAR(max);

		SET @sql = N'DELETE FROM ' + QUOTENAME(N'_' + @OriginalSchemaName) + N'.' + QUOTENAME(@TableName) + N'
WHERE ID NOT IN (SELECT ID FROM ' + QUOTENAME(@OriginalSchemaName) + N'.' + QUOTENAME(@TableName) + N');

INSERT INTO ' + QUOTENAME(N'_' + @OriginalSchemaName) + N'.' + QUOTENAME(@TableName) + N' (' + @commonColumns + N')
SELECT ' + @commonColumns + N' FROM ' + QUOTENAME(@OriginalSchemaName) + N'.' + QUOTENAME(@TableName) + N'
WHERE ' + QUOTENAME(@OriginalSchemaName) + N'.' + QUOTENAME(@TableName) + N'.ID NOT IN (SELECT ID FROM ' + QUOTENAME(N'_' + @OriginalSchemaName) + N'.' + QUOTENAME(@TableName) + N');

INSERT INTO Rhetos.DataMigrationFreshRows (OriginalSchemaName, TableName) VALUES (''' + @OriginalSchemaName + ''', ''' + @TableName + ''')';

		IF @commonColumns <> 'ID'
		BEGIN
			DECLARE @commonColumnsAssignment NVARCHAR(max);
			SET @commonColumnsAssignment = NULL;

			SELECT
				@commonColumnsAssignment = ISNULL(@commonColumnsAssignment + N', ', '')
					+ QUOTENAME(mig.COLUMN_NAME) + N' = orig.' + QUOTENAME(mig.COLUMN_NAME)
			FROM
				INFORMATION_SCHEMA.COLUMNS orig
				INNER JOIN INFORMATION_SCHEMA.COLUMNS mig
					ON mig.TABLE_SCHEMA = '_' + orig.TABLE_SCHEMA
					AND mig.TABLE_NAME = orig.TABLE_NAME
					AND mig.COLUMN_NAME = orig.COLUMN_NAME
			WHERE
				mig.COLUMN_NAME <> 'ID'
				AND orig.TABLE_SCHEMA = @OriginalSchemaName
				AND orig.TABLE_NAME = @TableName
			ORDER BY
				mig.TABLE_SCHEMA, mig.TABLE_NAME, mig.ORDINAL_POSITION;

			SET @sql = @sql + N'

UPDATE mig
SET ' + @commonColumnsAssignment + N'
FROM ' + QUOTENAME(@OriginalSchemaName) + N'.' + QUOTENAME(@TableName) + N' orig
	INNER JOIN ' + QUOTENAME(N'_' + @OriginalSchemaName) + N'.' + QUOTENAME(@TableName) + N' mig ON orig.ID = mig.ID';

		END
	
		PRINT @sql;
		EXEC (@sql);
		SET @Error = @@ERROR IF @Error > 0 BEGIN ROLLBACK TRANSACTION @TranName RETURN @Error END
	END

	-- Standard error-handling footer
	IF @InitialTranCount = 0 COMMIT TRANSACTION @TranName
	RETURN 0
GO

IF OBJECT_ID('Rhetos.DataMigrationUse') IS NULL
	EXEC ('CREATE PROCEDURE Rhetos.DataMigrationUse AS SET NOCOUNT ON RAISERROR (''Procedure creation has not finished.'', 16, 62)')
GO
ALTER PROCEDURE Rhetos.DataMigrationUse
	@SchemaName NVARCHAR(256), @TableName NVARCHAR(256), @ColumnName NVARCHAR(256), @ColumnType NVARCHAR(256)
AS
	-- Data-migration SQL scripts must provide a valid @ColumnType argument (use Rhetos.HelpDataMigration for help).
	-- @ColumnType may be null (auto-detect) only when the procedure is called by server application during automatic column backup/restore process.
    
	-- Standard error-handling header
	DECLARE @InitialTranCount INT
	SET @InitialTranCount = @@TRANCOUNT
	DECLARE @TranName VARCHAR(38)
	SET @TranName = NEWID()
	IF @InitialTranCount = 0 BEGIN TRANSACTION @TranName
	ELSE SAVE TRANSACTION @TranName
	DECLARE @Error INT
	SET @Error = 0

	IF CHARINDEX(']', @SchemaName) > 0 OR CHARINDEX('''', @SchemaName) > 0
	BEGIN ROLLBACK TRANSACTION @TranName RAISERROR('Invalid character in @SchemaName %s', 16, 10, @SchemaName) RETURN 50000 END

	IF CHARINDEX(']', @TableName) > 0 OR CHARINDEX('''', @TableName) > 0
	BEGIN ROLLBACK TRANSACTION @TranName RAISERROR('Invalid character in @TableName %s', 16, 10, @TableName) RETURN 50000 END

	IF CHARINDEX(']', @ColumnName) > 0 OR CHARINDEX('''', @ColumnName) > 0
	BEGIN ROLLBACK TRANSACTION @TranName RAISERROR('Invalid character in @ColumnName %s', 16, 10, @ColumnName) RETURN 50000 END
    
	DECLARE @MigrationSchemaName NVARCHAR(256)
	SET @MigrationSchemaName = '_' + @SchemaName
    
    DECLARE @ExistingMigrationColumnType NVARCHAR(256)
    SET @ExistingMigrationColumnType = Rhetos.GetColumnType(@MigrationSchemaName, @TableName, @ColumnName)
    
    DECLARE @OriginalType NVARCHAR(256)
    SET @OriginalType = Rhetos.GetColumnType(@SchemaName, @TableName, @ColumnName)
    
    IF @ColumnName = 'ID'
    BEGIN

		IF @ExistingMigrationColumnType IS NULL
		BEGIN
        
			IF @ColumnType <> 'UNIQUEIDENTIFIER'
			BEGIN ROLLBACK TRANSACTION @TranName RAISERROR('Column "ID" must have ColumnType "UNIQUEIDENTIFIER".', 16, 10) RETURN 50000 END

			IF NOT EXISTS(SELECT * FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = @MigrationSchemaName)
				EXEC ('CREATE SCHEMA [' + @MigrationSchemaName + ']');
			SET @Error = @@ERROR IF @Error > 0 BEGIN ROLLBACK TRANSACTION @TranName RETURN @Error END

			DECLARE @PKName SYSNAME = LEFT('PK_' + @TableName, 128);
            EXEC ('CREATE TABLE [' + @MigrationSchemaName + '].[' + @TableName + '] (ID UNIQUEIDENTIFIER NOT NULL CONSTRAINT [' + @PKName + '] PRIMARY KEY NONCLUSTERED)');
			SET @Error = @@ERROR IF @Error > 0 BEGIN ROLLBACK TRANSACTION @TranName RETURN @Error END

			DELETE FROM Rhetos.DataMigrationFreshRows WHERE OriginalSchemaName = @SchemaName AND TableName = @TableName;
			SET @Error = @@ERROR IF @Error > 0 BEGIN ROLLBACK TRANSACTION @TranName RETURN @Error END
		END

		EXEC @Error = Rhetos.DataMigrationInitializeRows @SchemaName, @TableName;
		SET @Error = ISNULL(NULLIF(@Error, 0), @@ERROR) IF @Error > 0 BEGIN ROLLBACK TRANSACTION @TranName RAISERROR('Error executing DataMigrationInitializeRows.',16,10) RETURN @Error END

    END
	ELSE IF @ColumnName <> 'ID'
	BEGIN
    
        IF @ColumnType IS NULL AND @OriginalType IS NULL
            BEGIN ROLLBACK TRANSACTION @TranName RAISERROR('Column type must be explicitly defined when executing DataMigrationUse. There is no column %s.%s.%s.', 16, 10, @SchemaName, @TableName, @ColumnName) RETURN 50000 END

        EXEC @Error = Rhetos.DataMigrationUse @SchemaName, @TableName, 'ID', 'UNIQUEIDENTIFIER';
        SET @Error = ISNULL(NULLIF(@Error, 0), @@ERROR) IF @Error > 0 BEGIN ROLLBACK TRANSACTION @TranName RAISERROR('Error executing DataMigrationUse ID.',16,10) RETURN @Error END
        
        IF @ExistingMigrationColumnType IS NULL
        BEGIN
            DECLARE @CreateType NVARCHAR(256)
            SET @CreateType = COALESCE(@OriginalType, @ColumnType)

            EXEC ('ALTER TABLE [' + @MigrationSchemaName + '].[' + @TableName + '] ADD [' + @ColumnName + '] ' + @CreateType);
            SET @Error = @@ERROR IF @Error > 0 BEGIN ROLLBACK TRANSACTION @TranName RETURN @Error END
            
            SET @ExistingMigrationColumnType = @CreateType
        
            IF @OriginalType IS NOT NULL
            BEGIN
                EXEC ('
                    UPDATE
                        migration
                    SET
                        [' + @ColumnName + '] = original.[' + @ColumnName + ']
                    FROM
                        [' + @MigrationSchemaName + '].[' + @TableName + '] migration
                        LEFT JOIN [' + @SchemaName + '].[' + @TableName + '] original ON original.ID = migration.ID');
                SET @Error = @@ERROR IF @Error > 0 BEGIN ROLLBACK TRANSACTION @TranName RETURN @Error END
            END
        END
    
        IF @ColumnType <> @ExistingMigrationColumnType
        BEGIN
            PRINT 'Automatically changing data-migration column type from ' + @ExistingMigrationColumnType + ' to ' + @ColumnType + ' for column ' + @SchemaName + '.' + @TableName + '.'  + @ColumnName+ '.'
            EXEC ('ALTER TABLE [' + @MigrationSchemaName + '].[' + @TableName + '] ALTER COLUMN [' + @ColumnName + '] ' + @ColumnType);
            SET @Error = @@ERROR IF @Error > 0 BEGIN ROLLBACK TRANSACTION @TranName RETURN @Error END
        END
        
	END

	-- Standard error-handling footer
	IF @InitialTranCount = 0 COMMIT TRANSACTION @TranName
	RETURN 0
GO

IF OBJECT_ID(N'[Rhetos].[MacroEvaluatorOrder]') IS NOT NULL
BEGIN
	DROP TABLE Rhetos.MacroEvaluatorOrder;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Rhetos].[DataMigrationScript]') AND name = N'Active')
BEGIN
    ALTER TABLE Rhetos.DataMigrationScript
        ADD Active bit NOT NULL
        CONSTRAINT DF_DataMigrationScript_Active DEFAULT (1);

    ALTER TABLE Rhetos.DataMigrationScript
        DROP CONSTRAINT DF_DataMigrationScript_Active;
END

-- Keeping the old columns for now, to avoid errors in existing applications' data-migration scripts.
UPDATE
	Rhetos.AppliedConcept
SET
	SerializedInfo = NULL,
	ConceptImplementationVersion = NULL
WHERE
	SerializedInfo IS NOT NULL
	OR ConceptImplementationVersion IS NOT NULL;
