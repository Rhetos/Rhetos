--=================================================
-- ANY CHANGES IN THIS SCRIPT MUST ALLOW:
-- 1. UPGRADE FROM ANY PREVIOUS VERSION OF RHETOS DATABASE (IT IS SAFEST TO ONLY *APPEND* CHANGES AT THE END OF THIS SCRIPT).
-- 2. DOWNGRADE FROM CURRENT VERSION TO THE PREVIOUS MAJOR VERSION.
--=================================================

CREATE SCHEMA IF NOT EXISTS Rhetos AUTHORIZATION pg_database_owner;

CREATE TABLE IF NOT EXISTS Rhetos.AppliedConcept
(
    ID uuid NOT NULL CONSTRAINT PK_AppliedConcept PRIMARY KEY,
    InfoType text NOT NULL,
    ImplementationType text NOT NULL,
    LastModified timestamp(3) NOT NULL CONSTRAINT DF_AppliedConcept_LastModified DEFAULT LOCALTIMESTAMP(3),
    AppliedBy varchar(256) NULL CONSTRAINT DF_AppliedConcept_AppliedBy DEFAULT current_user,
    Client varchar(256) NULL CONSTRAINT DF_AppliedConcept_Client DEFAULT CONCAT_WS(':', inet_client_addr(), inet_client_port()),
    Server varchar(256) NULL CONSTRAINT DF_AppliedConcept_Server DEFAULT inet_server_addr(),
    ModificationOrder serial NOT NULL,
    CreateQuery text NOT NULL,
    RemoveQuery text NOT NULL,
    ConceptInfoKey text NOT NULL
);

CREATE TABLE IF NOT EXISTS Rhetos.DataMigrationScript
(
    ID uuid NOT NULL CONSTRAINT PK_DataMigrationScript PRIMARY KEY CONSTRAINT DF_DataMigrationScript_ID DEFAULT gen_random_uuid(),
    Tag varchar(256) NOT NULL,
    Path varchar(256) NOT NULL,
    Content text NOT NULL,
    DateExecuted timestamp(3) NOT NULL CONSTRAINT DF_DataMigrationScript_LastModified DEFAULT LOCALTIMESTAMP(3),
    ExecutedBy varchar(256) NULL CONSTRAINT DF_DataMigrationScript_AppliedBy DEFAULT current_user,
    Client varchar(256) NULL CONSTRAINT DF_DataMigrationScript_Client DEFAULT CONCAT_WS(':', inet_client_addr(), inet_client_port()),
    Server varchar(256) NULL CONSTRAINT DF_DataMigrationScript_Server DEFAULT inet_server_addr(),
    OrderExecuted serial NOT NULL,
    Active boolean NOT NULL,
    Down text
);

CREATE UNIQUE INDEX IF NOT EXISTS IX_DataMigrationScript_Tag ON Rhetos.DataMigrationScript(Tag);

CREATE OR REPLACE FUNCTION Rhetos.GetColumnType
    (SchemaName varchar(256), TableName varchar(256), ColumnName varchar(256))
RETURNS varchar(256)
LANGUAGE sql
AS $$
SELECT
    CASE
    WHEN udt_name = 'numeric' AND numeric_precision IS NOT NULL AND numeric_scale IS NOT NULL THEN
        udt_name || '(' || numeric_precision || ', ' || numeric_scale || ')'
    WHEN udt_name = 'timestamp' AND datetime_precision IS NOT NULL THEN
        udt_name || '(' || datetime_precision || ')'
    WHEN udt_name = 'varchar' AND character_maximum_length IS NOT NULL THEN
        udt_name || '(' || character_maximum_length || ')'
    ELSE
        udt_name
    END
FROM
    information_schema.columns
WHERE
    table_schema = lower(SchemaName) AND table_name = lower(TableName) AND column_name = lower(ColumnName)
$$;

CREATE OR REPLACE PROCEDURE Rhetos.DataMigrationApply
    (SchemaName varchar(256), TableName varchar(256), ColumnName varchar(256))
LANGUAGE plpgsql
AS $$
DECLARE
    MigrationSchemaName varchar := '_' || lower(SchemaName);
    ColumnType varchar;
    ExistingMigrationColumnType varchar;
BEGIN
    SchemaName := lower(SchemaName);
    TableName := lower(TableName);
    ColumnName := lower(ColumnName);

    -- Get column types
    ColumnType := Rhetos.GetColumnType(SchemaName, TableName, ColumnName);
    ExistingMigrationColumnType := Rhetos.GetColumnType(MigrationSchemaName, TableName, ColumnName);

    -- Copy data if the columns exist
    IF ColumnType IS NULL THEN
        RAISE NOTICE 'Column %.%.% does not exist. It will be safely ignored.', SchemaName, TableName, ColumnName;
    ELSIF ExistingMigrationColumnType IS NULL THEN
        RAISE NOTICE 'Data-migration column %.%.% does not exist. It will be safely ignored.', MigrationSchemaName, TableName, ColumnName;
    ELSIF ColumnName = 'id' THEN
        EXECUTE format('
            DELETE FROM %I.%I
            WHERE ID NOT IN (SELECT ID FROM %I.%I)', SchemaName, TableName, MigrationSchemaName, TableName);
        
        EXECUTE format('
            INSERT INTO %I.%I (ID)
            SELECT ID
            FROM %I.%I
            WHERE ID NOT IN (SELECT ID FROM %I.%I)', SchemaName, TableName, MigrationSchemaName, TableName, SchemaName, TableName);
    ELSE
        -- Non-ID column logic
        IF ColumnType <> ExistingMigrationColumnType THEN
            RAISE NOTICE 'Automatically changing data-migration column type from % to % for column %.%.%', ExistingMigrationColumnType, ColumnType, SchemaName, TableName, ColumnName;
            EXECUTE format('ALTER TABLE %I.%I ALTER COLUMN %I TYPE %s USING %I::%s', MigrationSchemaName, TableName, ColumnName, ColumnType, ColumnName, ColumnType);
        END IF;

        EXECUTE format('
            UPDATE
                %I.%I original
            SET
                %I = migration.%I
            FROM
                %I.%I migration
            WHERE
                migration.ID = original.ID
                AND original.%I IS DISTINCT FROM migration.%I', SchemaName, TableName, ColumnName, ColumnName, MigrationSchemaName, TableName, ColumnName, ColumnName);
    END IF;
END;
$$;

CREATE OR REPLACE FUNCTION Rhetos.HelpDataMigration(SchemaName varchar, TableName varchar)
RETURNS text
LANGUAGE plpgsql
AS $$
DECLARE
    columnName TEXT;
    columnType TEXT;
    column_list TEXT := '';
    result TEXT;
BEGIN
    IF LEFT(SchemaName, 1) = '_' THEN
        RAISE EXCEPTION 'Use a regular table, not a data-migration table %.%', SchemaName, TableName;
    END IF;

    result := concat('/*DATAMIGRATION ', gen_random_uuid(), ' */');
    result := concat(result, chr(10));
    result := concat(result, chr(10), '-- The following lines are generated by: SELECT Rhetos.HelpDataMigration(''', SchemaName, ''', ''', TableName, ''');');
    
    FOR columnName, columnType IN
        SELECT COLUMN_NAME, Rhetos.GetColumnType(TABLE_SCHEMA, TABLE_NAME, COLUMN_NAME)
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = lower(SchemaName) AND TABLE_NAME = lower(TableName)
        ORDER BY CASE WHEN COLUMN_NAME = 'id' THEN -1 ELSE ORDINAL_POSITION END
    LOOP
        IF column_list <> '' THEN
            column_list := column_list || ', ';
        END IF;
        column_list := column_list || columnName;

        result := concat(result, chr(10), 'CALL Rhetos.DataMigrationUse(''', SchemaName, ''', ''', TableName, ''', ''', columnName, ''', ''', columnType, ''');');
    END LOOP;
    
    result := concat(result, chr(10));
    result := concat(result, chr(10), '-- ... write the data migration queries here (don''t forget to use the underscore ''_'' in schema name) ...');
    result := concat(result, chr(10));
    result := concat(result, chr(10), 'CALL Rhetos.DataMigrationApplyMultiple(''', SchemaName, ''', ''', TableName, ''', ''', column_list, ''');');

    RETURN result;
END;
$$;

CREATE OR REPLACE PROCEDURE Rhetos.DataMigrationApplyMultiple
    (SchemaName varchar(256), TableName varchar(256), ColumnNames text)
LANGUAGE plpgsql
AS $$
DECLARE
    MigrationSchemaName varchar := '_' || lower(SchemaName);
    sqlDelete text;
    sqlUpdate text;
    sqlInsert text;
    columnInfo RECORD;
BEGIN

    SchemaName := lower(SchemaName);
    TableName := lower(TableName);
    ColumnNames := lower(ColumnNames);

    -- Rhetos.DataMigrationApplyMultiple will not automatically change the migration column type (unlike Rhetos.DataMigrationApply).
    -- That is good enough for use in data migration scripts, because the developer can alter the column if needed,
    -- but cannot be used in DatabaseGenerator plugins for automatic column backup/restore (Rhetos.DataMigrationApply).
    
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = SchemaName AND table_name = TableName AND table_type = 'BASE TABLE') THEN
        RAISE NOTICE 'Nothing to migrate. Table "%"."%" does not exist. It is expected to be created later during this upgrade.', SchemaName, TableName;
    ELSIF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = MigrationSchemaName AND table_name = TableName) THEN
        RAISE NOTICE 'Nothing to migrate. Data-migration table "%"."%" is not prepared. Execute "Rhetos.DataMigrationUse" to prepare the migration table.', MigrationSchemaName, TableName;
    ELSE
        -- Parse column names:

        CREATE TEMPORARY TABLE IF NOT EXISTS columns (columnName varchar(256), missingInSchema varchar(256));
        TRUNCATE TABLE columns;

        INSERT INTO columns
        SELECT
            name,
            CASE
                WHEN name NOT IN (SELECT column_name FROM information_schema.columns WHERE table_schema = SchemaName AND table_name = TableName)
                    THEN SchemaName
                WHEN name NOT IN (SELECT column_name FROM information_schema.columns WHERE table_schema = MigrationSchemaName AND table_name = TableName)
                    THEN MigrationSchemaName
            END
        FROM
            unnest(regexp_split_to_array(regexp_replace(ColumnNames, '\s+', '', 'g'), ',')) name;

        IF NOT EXISTS (SELECT 1 FROM columns WHERE columnName = 'id') THEN
            RAISE EXCEPTION 'The "ID" column must be included in ColumnNames.';
        END IF;

        -- Remove columns that are not prepared for migration:

        FOR columnInfo IN SELECT columnName, missingInSchema FROM columns WHERE missingInSchema IS NOT NULL LOOP
            RAISE NOTICE 'Column %.%.% does not exist. It will be safely ignored.', columnInfo.missingInSchema, TableName, columnInfo.columnName;
        END LOOP;

        DELETE FROM columns
            WHERE missingInSchema IS NOT NULL
                OR columnName = 'id';

        -- Migrate data:

        sqlDelete := format('DELETE FROM %I.%I WHERE ID NOT IN (SELECT ID FROM %I.%I)', SchemaName, TableName, MigrationSchemaName, TableName);
        --TODO: REMOVE DEBUG INFO.
        RAISE NOTICE '%', sqlDelete;
        EXECUTE sqlDelete;

        IF EXISTS (SELECT 1 FROM columns) THEN
            sqlUpdate := format('UPDATE %I.%I original SET ', SchemaName, TableName);
            FOR columnInfo IN SELECT columnName FROM columns LOOP
                sqlUpdate := sqlUpdate || format('%I = migration.%I, ', columnInfo.columnName, columnInfo.columnName);
            END LOOP;
            sqlUpdate := rtrim(sqlUpdate, ', ');
            sqlUpdate := sqlUpdate || format(' FROM %I.%I migration WHERE original.ID = migration.ID AND (', MigrationSchemaName, TableName);
            FOR columnInfo IN SELECT columnName FROM columns LOOP
                sqlUpdate := sqlUpdate || format('original.%1$I IS DISTINCT FROM migration.%1$I OR ',
                    columnInfo.columnName);
            END LOOP;
            sqlUpdate := rtrim(sqlUpdate, ' OR ') || ')';
            --TODO: REMOVE DEBUG INFO.
            RAISE NOTICE '%', sqlUpdate;
            EXECUTE sqlUpdate;
        END IF;

        sqlInsert := format('INSERT INTO %I.%I (ID', SchemaName, TableName);
        FOR columnInfo IN SELECT columnName FROM columns LOOP
            sqlInsert := sqlInsert || ', ' || quote_ident(columnInfo.columnName);
        END LOOP;
        sqlInsert := sqlInsert || ') SELECT ID';
        FOR columnInfo IN SELECT columnName FROM columns LOOP
            sqlInsert := sqlInsert || ', ' || quote_ident(columnInfo.columnName);
        END LOOP;
        sqlInsert := sqlInsert || format(' FROM %I.%I WHERE ID NOT IN (SELECT ID FROM %I.%I)', MigrationSchemaName, TableName, SchemaName, TableName);
        --TODO: REMOVE DEBUG INFO.
        RAISE NOTICE '%', sqlInsert;
        EXECUTE sqlInsert;

        DROP TABLE columns;
        
    END IF;

END;
$$;

CREATE TABLE IF NOT EXISTS Rhetos.AppliedConceptDependsOn
(
    ID uuid NOT NULL CONSTRAINT PK_AppliedConceptDependsOn PRIMARY KEY CONSTRAINT DF_AppliedConceptDependsOn_ID DEFAULT gen_random_uuid(),
    DependentID uuid NOT NULL,
    DependsOnID uuid NOT NULL,
    CONSTRAINT UQ_AppliedConceptDependsOn_DependentID_DependsOnID UNIQUE (DependentID, DependsOnID),
    CONSTRAINT FK_AppliedConceptDependsOn_Dependent FOREIGN KEY (DependentID) REFERENCES Rhetos.AppliedConcept (ID) ON DELETE CASCADE,
    CONSTRAINT FK_AppliedConceptDependsOn_DependsOn FOREIGN KEY (DependsOnID) REFERENCES Rhetos.AppliedConcept (ID)
);

/*
DataMigrationFreshRows table is used for internal optimization of DataMigrationUse procedure for 'ID' column.
If DataMigrationFreshRows contains the table's name, this means that the corresponding migration table has updated data:
same rows and same values in common columns as the main table.
In that case, calling Rhetos.DataMigrationUse stored procedure for the 'ID' column will simply reuse the existing data
in the migration table without reviewing and updating data in the migration table.
Since each data-migration script is expected to keep the migration table in sync (with DataMigrationUse and DataMigrationApply),
the record in DataMigrationFreshRows can stay valid during deployment process.
DataMigrationFreshRows is cleaned by Rhetos CLI before and after deployment, to make sure that stale backup data in migration table
is not unintentionally used, because any use of the application (even the application initialization phase of deployment)
can modify the application data, making the migration tables outdated.
*/
CREATE TABLE IF NOT EXISTS Rhetos.DataMigrationFreshRows
(
    ID uuid NOT NULL
        CONSTRAINT PK_DataMigrationFreshRows PRIMARY KEY
        CONSTRAINT DF_DataMigrationFreshRows_ID DEFAULT gen_random_uuid(),
    OriginalSchemaName varchar(256) NOT NULL,
    TableName varchar(256) NOT NULL,
    CONSTRAINT UQ_DataMigrationFreshRows_Table UNIQUE (OriginalSchemaName, TableName)
);

CREATE OR REPLACE PROCEDURE Rhetos.DataMigrationInitializeRows(OriginalSchemaName varchar, TableName varchar)
LANGUAGE plpgsql
AS $$
DECLARE
    MigrationSchemaName varchar := '_' || lower(OriginalSchemaName);
    commonColumns text;
    sql text;
    commonColumnsAssignment text;
BEGIN

    OriginalSchemaName := lower(OriginalSchemaName);
    TableName := lower(TableName);

    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = OriginalSchemaName AND TABLE_NAME = TableName AND COLUMN_NAME = 'id')
       AND EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = MigrationSchemaName AND TABLE_NAME = TableName AND COLUMN_NAME = 'id')
       AND NOT EXISTS(SELECT * FROM Rhetos.DataMigrationFreshRows f WHERE f.OriginalSchemaName = $1 AND f.TableName = $2)
    THEN
        
        SELECT
            string_agg(quote_ident(orig.COLUMN_NAME), ', ' ORDER BY orig.ORDINAL_POSITION)
        INTO
            commonColumns
        FROM
            INFORMATION_SCHEMA.COLUMNS orig
            INNER JOIN INFORMATION_SCHEMA.COLUMNS mig
                ON mig.TABLE_SCHEMA = '_' || orig.TABLE_SCHEMA
                AND mig.TABLE_NAME = orig.TABLE_NAME
                AND mig.COLUMN_NAME = orig.COLUMN_NAME
        WHERE
            orig.TABLE_SCHEMA = OriginalSchemaName
            AND orig.TABLE_NAME = TableName;

        sql := format('DELETE FROM %2$I.%3$I
WHERE ID NOT IN (SELECT ID FROM %1$I.%3$I);

INSERT INTO %2$I.%3$I (%4$s)
SELECT %4$s FROM %1$I.%3$I
WHERE %1$I.%3$I.ID NOT IN (SELECT ID FROM %2$I.%3$I);

INSERT INTO Rhetos.DataMigrationFreshRows (OriginalSchemaName, TableName) VALUES (''%1$I'', ''%3$I'');
',
            OriginalSchemaName, MigrationSchemaName, TableName, commonColumns);

        IF commonColumns <> 'id'
        THEN
            SELECT
                string_agg(quote_ident(orig.COLUMN_NAME) || ' = orig.' || quote_ident(orig.COLUMN_NAME),
                    ', ' ORDER BY orig.ORDINAL_POSITION)
            INTO
                commonColumnsAssignment
            FROM
                INFORMATION_SCHEMA.COLUMNS orig
                INNER JOIN INFORMATION_SCHEMA.COLUMNS mig
                    ON mig.TABLE_SCHEMA = '_' || orig.TABLE_SCHEMA
                    AND mig.TABLE_NAME = orig.TABLE_NAME
                    AND mig.COLUMN_NAME = orig.COLUMN_NAME
            WHERE
                orig.TABLE_SCHEMA = OriginalSchemaName
                AND orig.TABLE_NAME = TableName
                and orig.COLUMN_NAME <> 'id';

            sql := sql || format('
UPDATE %2$I.%3$I mig
SET %4$s
FROM %1$I.%3$I orig
WHERE orig.ID = mig.ID;
',
                OriginalSchemaName, MigrationSchemaName, TableName, commonColumnsAssignment);
        END IF;

        RAISE NOTICE '%', sql;
        EXECUTE sql;

    ELSE
        IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = OriginalSchemaName AND TABLE_NAME = TableName AND COLUMN_NAME = 'id') THEN
            RAISE NOTICE 'DataMigrationInitializeRows skipped - Original table ID does not exist.';
        END IF;
        IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = MigrationSchemaName AND TABLE_NAME = TableName AND COLUMN_NAME = 'id') THEN
            RAISE NOTICE 'DataMigrationInitializeRows skipped - Migration table ID does not exist.';
        END IF;
        IF EXISTS(SELECT * FROM Rhetos.DataMigrationFreshRows f WHERE f.OriginalSchemaName = $1 AND f.TableName = $2) THEN
            RAISE NOTICE 'DataMigrationInitializeRows skipped - Migration data is already marked as fresh.';
        END IF;
    END IF;
END;
$$;

CREATE OR REPLACE PROCEDURE Rhetos.DataMigrationUse
    (SchemaName varchar(256), TableName varchar(256), ColumnName varchar(256), ColumnType varchar(256))
LANGUAGE plpgsql
AS $$
    -- Data-migration SQL scripts must provide a valid ColumnType argument (use Rhetos.HelpDataMigration for help).
    -- ColumnType may be null (auto-detect) only when the procedure is called by server application during automatic column backup/restore process.
DECLARE
    MigrationSchemaName VARCHAR(256) := '_' || lower(SchemaName);
    ExistingMigrationColumnType VARCHAR(256);
    OriginalType VARCHAR(256);
    PKName TEXT;
    CreateType VARCHAR(256);
BEGIN

    SchemaName := lower(SchemaName);
    TableName := lower(TableName);
    ColumnName := lower(ColumnName);

    IF NOT (ColumnType ~ '^[\w,() ]+$') THEN
        RAISE EXCEPTION 'Invalid character in ColumnType "%"', ColumnType;
    END IF;

    ExistingMigrationColumnType := Rhetos.GetColumnType(MigrationSchemaName, TableName, ColumnName);
    OriginalType := Rhetos.GetColumnType(SchemaName, TableName, ColumnName);

    IF ColumnName = 'id' THEN
        IF ExistingMigrationColumnType IS NULL THEN
        
            IF ColumnType <> 'uuid' THEN
                RAISE EXCEPTION 'Column "ID" must have ColumnType "uuid".';
            END IF;

            RAISE NOTICE 'DEBUG: Creating table';
        
            EXECUTE 'CREATE SCHEMA IF NOT EXISTS ' || quote_ident(MigrationSchemaName) || ' AUTHORIZATION pg_database_owner';

            PKName := left('PK_' || TableName, 63);
            EXECUTE 'CREATE TABLE ' || quote_ident(MigrationSchemaName) || '.' || quote_ident(TableName) || ' (ID uuid NOT NULL CONSTRAINT ' || quote_ident(PKName) || ' PRIMARY KEY)';
            
            DELETE FROM Rhetos.DataMigrationFreshRows f WHERE f.OriginalSchemaName = $1 AND f.TableName = $2;

        ELSE
            RAISE NOTICE 'DataMigrationUse - %.%.% already exists', MigrationSchemaName, TableName, ColumnName;
        END IF;
       
        RAISE NOTICE 'DEBUG: CALL Rhetos.DataMigrationInitializeRows';
        CALL Rhetos.DataMigrationInitializeRows(SchemaName, TableName);
    
        IF ExistingMigrationColumnType IS NULL THEN
            RAISE NOTICE 'DEBUG: CLUSTER';
            EXECUTE 'CLUSTER ' || quote_ident(MigrationSchemaName) || '.' || quote_ident(TableName) || ' USING ' || quote_ident(PKName);
        END IF;
       
    ELSE
    
        IF ColumnType IS NULL AND OriginalType IS NULL THEN
            RAISE EXCEPTION 'The ColumnType parameter must be provided when calling DataMigrationUse. There is no column %.%.%.', SchemaName, TableName, ColumnName;
        END IF;

        RAISE NOTICE 'DEBUG: CALL Rhetos.DataMigrationUse ID before %', ColumnName;
        CALL Rhetos.DataMigrationUse(SchemaName, TableName, 'ID', 'uuid');

        IF ExistingMigrationColumnType IS NULL THEN
            CreateType := COALESCE(OriginalType, ColumnType);
            RAISE NOTICE 'DEBUG: Create column %.%.% %', MigrationSchemaName, TableName, ColumnName, CreateType;
            EXECUTE 'ALTER TABLE ' || quote_ident(MigrationSchemaName) || '.' || quote_ident(TableName) || ' ADD COLUMN ' || quote_ident(ColumnName) || ' ' || CreateType;

            ExistingMigrationColumnType := CreateType;
        
            IF OriginalType IS NOT NULL THEN
                RAISE NOTICE 'DEBUG: UPDATE column _% % from original table', ColumnName, CreateType;
                EXECUTE 'UPDATE ' || quote_ident(MigrationSchemaName) || '.' || quote_ident(TableName) || ' migration
                    SET ' || quote_ident(ColumnName) || ' = original.' || quote_ident(ColumnName) || '
                    FROM ' || quote_ident(SchemaName) || '.' || quote_ident(TableName) || ' original
                    WHERE original.ID = migration.ID';
            ELSE
                RAISE NOTICE 'DEBUG: There is no original column %.%.% to copy data to migration table.', SchemaName, TableName, ColumnName;
            END IF;
        ELSE
            RAISE NOTICE 'DEBUG: Migration column %.%.% already exists, no need to create or copy data.', MigrationSchemaName, TableName, ColumnName;
        END IF;

        IF to_regtype(ColumnType) <> to_regtype(ExistingMigrationColumnType) THEN
            RAISE NOTICE 'Automatically changing data-migration column type from % to % for column %.%.%', ExistingMigrationColumnType, ColumnType, MigrationSchemaName, TableName, ColumnName;
            EXECUTE 'ALTER TABLE ' || quote_ident(MigrationSchemaName) || '.' || quote_ident(TableName) || ' ALTER COLUMN ' || quote_ident(ColumnName) || ' TYPE ' || ColumnType || ' USING ' || quote_ident(ColumnName) || '::' || ColumnType;
        END IF;
       
    END IF;
END;
$$;
