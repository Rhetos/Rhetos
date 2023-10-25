/*DATAMIGRATION 366824BC-4E2F-4025-97CE-6C892A00C5DA*/ -- Change the script's code only if it needs to be executed again.

DECLARE @sql nvarchar(max) = '';

SELECT
    @sql = @sql +
        'ALTER TABLE ' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ' DROP CONSTRAINT ' + QUOTENAME(pkIndex.name) + ';' + CHAR(13) + CHAR(10) +
        'ALTER TABLE ' + QUOTENAME(s.name) + '.' + QUOTENAME(t.name) + ' ADD CONSTRAINT ' + QUOTENAME(pkIndex.name) + ' PRIMARY KEY CLUSTERED (ID);' + CHAR(13) + CHAR(10)
FROM
    sys.tables t
    INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
    LEFT JOIN sys.indexes clusteredIndex
        ON clusteredIndex.object_id = t.object_id
        AND clusteredIndex.type_desc = 'CLUSTERED'
    INNER JOIN sys.indexes pkIndex
        ON pkIndex.object_id = t.object_id
        AND pkIndex.is_primary_key = 1
    INNER JOIN sys.index_columns pkIndexColumn
        ON pkIndexColumn.object_id = pkIndex.object_id
        AND pkIndexColumn.index_id = pkIndex.index_id
    INNER JOIN sys.columns pkColumn
        ON pkColumn.object_id = pkIndexColumn.object_id
        AND pkColumn.column_id = pkIndexColumn.column_id
WHERE
    s.name LIKE '[_]%'
    AND t.type_desc = 'USER_TABLE'
    AND clusteredIndex.index_id IS NULL
GROUP BY
    s.name,
    t.name,
    pkIndex.name
HAVING
    MIN(pkColumn.name) = 'ID'
    AND MAX(pkColumn.name) = 'ID'
ORDER BY
    s.name,
    t.name,
    pkIndex.name

PRINT @sql;
EXEC sp_executesql @sql;
