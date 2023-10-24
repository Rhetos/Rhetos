/*
    Copyright (C) 2014 Omega software d.o.o.

    This file is part of Rhetos.

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as
    published by the Free Software Foundation, either version 3 of the
    License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using Rhetos.Logging;
using Rhetos.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.Deployment
{
    public class DatabaseCleaner
    {
        private readonly ILogger _logger;
        private readonly ISqlExecuter _sqlExecuter;
        private readonly ISqlTransactionBatches _sqlTransactionBatches;

        public DatabaseCleaner(ILogProvider logProvider, ISqlExecuter sqlExecuter, ISqlTransactionBatches sqlTransactionBatches)
        {
            _logger = logProvider.GetLogger(GetType().Name);
            _sqlExecuter = sqlExecuter;
            _sqlTransactionBatches = sqlTransactionBatches;
        }

        public string DeleteAllMigrationData()
        {
            if (SqlUtility.DatabaseLanguage != "MsSql")
            {
                var reportSkip = "Skipped DeleteAllMigrationData (DatabaseLanguage=" + SqlUtility.DatabaseLanguage + ").";
                _logger.Warning(reportSkip);
                return reportSkip;
            }

            var dataMigrationTables = ReadDataMigrationTablesFromDatabase();
            var deleteMigrationSchemas = ReadDataMigrationSchemasFromDatabase();
            DeleteDatabaseObjects(System.Array.Empty<ColumnInfo>(), dataMigrationTables, deleteMigrationSchemas);

            var report = $"Deleted {dataMigrationTables.Count} tables in data migration schemas.";
            _logger.Info(report);
            return report;
        }

        /// <summary>
        /// Invalidates (resets) the data-migration optimization cache to make sure that next DataMigrationUse call
        /// will update the migration table.
        /// </summary>
        /// <remarks>
        /// If the DataMigrationFreshRows contains a table name for some entity, calling Rhetos.DataMigrationUse stored procedure
        /// for 'ID' column would simply reuse the existing data in the migration table without reviewing and updating data in the migration table.
        /// </remarks>
        public string RefreshDataMigrationRows()
        {
            if (SqlUtility.DatabaseLanguage != "MsSql")
            {
                var reportSkip = "Skipped RefreshDataMigrationRows (DatabaseLanguage=" + SqlUtility.DatabaseLanguage + ").";
                _logger.Warning(reportSkip);
                return reportSkip;
            }

            var script = new SqlBatchScript { Sql = "DELETE FROM Rhetos.DataMigrationFreshRows;" };
            _sqlTransactionBatches.Execute(new[] { script });
            return null;
        }

        public void RemoveRedundantMigrationColumns()
        {
            if (SqlUtility.DatabaseLanguage != "MsSql")
            {
                _logger.Warning("Skipped RemoveRedundantMigrationColumns (DatabaseLanguage=" + SqlUtility.DatabaseLanguage + ").");
                return;
            }

            var allColumns = ReadAllColumnsFromDatabase();
            var migrationTables = ReadDataMigrationTablesFromDatabase();
            var migrationSchemas = ReadDataMigrationSchemasFromDatabase();

            // Compute columns, tables and schemas to be deleted:

            var allColumnsIndex = new HashSet<string>(allColumns.Select(c => c.Key));
            var migrationColumns = allColumns.Where(c => c.SchemaName.StartsWith("_")).ToList();
            var redundantMigrationColumns = migrationColumns.Where(dmc => allColumnsIndex.Contains(dmc.Key.Substring(1))).ToList();

            var redundantMigrationColumnsIndex = new HashSet<string>(redundantMigrationColumns.Select(c => c.Key));
            var remainingMigrationColumns = migrationColumns.Where(c => !redundantMigrationColumnsIndex.Contains(c.Key)).ToList();
            var remainingMigrationTablesIndex = new HashSet<string>(remainingMigrationColumns.Select(c => c.KeyTable).Distinct());
            var emptyMigrationTables = migrationTables.Where(t => !remainingMigrationTablesIndex.Contains(t.Key)).ToList();

            var remainingMigrationSchemasIndex = new HashSet<string>(remainingMigrationColumns.Select(c => c.SchemaName).Distinct());
            var emptyMigrationSchemas = migrationSchemas.Where(s => !remainingMigrationSchemasIndex.Contains(s)).ToList();

            var emptyMigrationTablesIndex = new HashSet<string>(emptyMigrationTables.Select(t => t.Key));
            var deleteMigrationColumns = redundantMigrationColumns.Where(c => c.ColumnName != "ID").ToList(); // If any data migration column remains in a table, the ID column must also remain in the table.
            var deleteMigrationColumnsOptimized = deleteMigrationColumns.Where(c => !emptyMigrationTablesIndex.Contains(c.KeyTable)).ToList(); // Deleting a table will automatically delete all the columns. This is also needed to remove a table that does not contain ID property.

            // Drop database objects:

            DeleteDatabaseObjects(deleteMigrationColumnsOptimized, emptyMigrationTables, emptyMigrationSchemas);

            _logger.Info(() =>
                "Deleted " + deleteMigrationColumns.Count + " columns in data migration schemas, "
                + remainingMigrationColumns.Count + " remaining.");
        }

        private void DeleteDatabaseObjects(IEnumerable<ColumnInfo> deleteColumns, List<TableInfo> deleteTables, List<string> deleteSchemas)
        {
            var sqlCommands = new List<string>();
            foreach (var c in deleteColumns)
                _logger.Trace("Deleting data-migration column " + c.SchemaName + "." + c.TableName + "." + c.ColumnName + ".");
            sqlCommands.AddRange(deleteColumns.Select(DropColumn));

            foreach (var t in deleteTables)
                _logger.Trace("Deleting data-migration table " + t.SchemaName + "." + t.TableName + ".");
            sqlCommands.AddRange(deleteTables.Select(DropTable));

            foreach (var s in deleteSchemas)
                _logger.Trace("Deleting data-migration schema " + s + ".");
            sqlCommands.AddRange(deleteSchemas.Select(DropSchema));

            var scripts = sqlCommands.Select(sql => new SqlBatchScript { Sql = sql });
            _sqlTransactionBatches.Execute(scripts);
        }

        private List<ColumnInfo> ReadAllColumnsFromDatabase()
        {
            var allColumns = new List<ColumnInfo>();

            _sqlExecuter.ExecuteReader(
                // PersistenceTransactionOptions.UseDatabaseTransaction is disable on dbupdate.
                @"SELECT
                    c.TABLE_SCHEMA, c.TABLE_NAME, c.COLUMN_NAME
                FROM
                    INFORMATION_SCHEMA.COLUMNS c
                    INNER JOIN INFORMATION_SCHEMA.TABLES t
                        ON t.TABLE_CATALOG = c.TABLE_CATALOG
                        AND t.TABLE_SCHEMA = c.TABLE_SCHEMA
                        AND t.TABLE_NAME = c.TABLE_NAME
                WHERE
                    t.TABLE_TYPE = 'BASE TABLE'",
                reader =>
                {
                    allColumns.Add(new ColumnInfo(reader.GetString(0), reader.GetString(1), reader.GetString(2)));
                });

            return allColumns;
        }

        private List<TableInfo> ReadDataMigrationTablesFromDatabase()
        {
            var tables = new List<TableInfo>();

            _sqlExecuter.ExecuteReader(
                // PersistenceTransactionOptions.UseDatabaseTransaction is disable on dbupdate.
                "SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA LIKE '[_]%';",
                reader =>
                {
                    tables.Add(new TableInfo(reader.GetString(0), reader.GetString(1)));
                });

            return tables;
        }

        private List<string> ReadDataMigrationSchemasFromDatabase()
        {
            var schemas= new List<string>();

            _sqlExecuter.ExecuteReader(
                // PersistenceTransactionOptions.UseDatabaseTransaction is disable on dbupdate.
                "SELECT SCHEMA_NAME FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME LIKE '[_]%'",
                reader =>
                {
                    schemas.Add(reader.GetString(0));
                });

            return schemas;
        }

        private string DropColumn(ColumnInfo column)
        {
            return string.Format(
                "ALTER TABLE {0}.{1} DROP COLUMN {2}",
                SqlUtility.QuoteIdentifier(column.SchemaName),
                SqlUtility.QuoteIdentifier(column.TableName),
                SqlUtility.QuoteIdentifier(column.ColumnName));
        }

        private string DropTable(TableInfo table)
        {
            return string.Format(
                "DROP TABLE {0}.{1}",
                SqlUtility.QuoteIdentifier(table.SchemaName),
                SqlUtility.QuoteIdentifier(table.TableName));
        }

        private string DropSchema(string schema)
        {
            return string.Format(
                "DROP SCHEMA {0}",
                SqlUtility.QuoteIdentifier(schema));
        }
    }

    public class ColumnInfo
    {
        public readonly string SchemaName;
        public readonly string TableName;
        public readonly string ColumnName;

        public ColumnInfo(string schemaName, string tableName, string columName)
        {
            SchemaName = schemaName;
            TableName = tableName;
            ColumnName = columName;

            Key = SchemaName + "." + TableName + "." + ColumnName;
            KeyTable = SchemaName + "." + TableName;
        }

        public readonly string Key;
        public readonly string KeyTable;
    }

    public class TableInfo
    {
        public readonly string SchemaName;
        public readonly string TableName;

        public TableInfo(string schemaName, string tableName)
        {
            SchemaName = schemaName;
            TableName = tableName;

            Key = SchemaName + "." + TableName;
        }

        public readonly string Key;
    }
}