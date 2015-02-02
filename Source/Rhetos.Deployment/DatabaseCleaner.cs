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

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Rhetos.Utilities;
using Rhetos.Logging;

namespace Rhetos.Deployment
{
    public class DatabaseCleaner
    {
        private readonly ISqlExecuter _sqlExecuter;
        private readonly ILogger _logger;

        public DatabaseCleaner(ILogProvider logProvider, ISqlExecuter sqlExecuter)
        {
            _logger = logProvider.GetLogger("DatabaseCleaner");
            _sqlExecuter = sqlExecuter;
        }

        public string DeleteAllMigrationData()
        {
            if (SqlUtility.DatabaseLanguage != "MsSql")
            {
                var reportSkip = "Skipped DatabaseCleaner.DeleteAllMigrationData (DatabaseLanguage=" + SqlUtility.DatabaseLanguage + ").";
                _logger.Info(reportSkip);
                return reportSkip;
            }

            var dataMigrationTables = ReadDataMigrationTablesFromDatabase();
            var deleteMigrationSchemas = ReadDataMigrationSchemasFromDatabase();
            DeleteDatabaseObjects(new ColumnInfo[] { }, dataMigrationTables, deleteMigrationSchemas);

            var report = "Deleted " + dataMigrationTables.Count() + " tables in data migration schemas.";
            _logger.Info(report);
            return report;
        }

        public string RefreshDataMigrationRows()
        {
            if (SqlUtility.DatabaseLanguage != "MsSql")
            {
                var reportSkip = "Skipped DatabaseCleaner.RefreshDataMigrationRows (DatabaseLanguage=" + SqlUtility.DatabaseLanguage + ").";
                _logger.Info(reportSkip);
                return reportSkip;
            }

            _sqlExecuter.ExecuteSql(new[] { "DELETE FROM Rhetos.DataMigrationFreshRows;" });
            return null;
        }

        public string RemoveRedundantMigrationColumns()
        {
            if (SqlUtility.DatabaseLanguage != "MsSql")
            {
                var reportSkip = "Skipped DatabaseCleaner.RemoveRedundantMigrationColumns (DatabaseLanguage=" + SqlUtility.DatabaseLanguage + ").";
                _logger.Info(reportSkip);
                return reportSkip;
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

            var report = "Deleted " + deleteMigrationColumns.Count() + " columns in data migration schemas, " + remainingMigrationColumns.Count() + " remaining.";
            if (deleteMigrationColumns.Count() != 0)
                _logger.Info(report);
            else
                _logger.Trace(report);
            return report;
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

            _sqlExecuter.ExecuteSql(sqlCommands);
        }

        private List<ColumnInfo> ReadAllColumnsFromDatabase()
        {
            var allColumns = new List<ColumnInfo>();

            _sqlExecuter.ExecuteReader(
                "SELECT TABLE_SCHEMA, TABLE_NAME, COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS",
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