﻿/*
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

using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Rhetos.Deployment.Test
{
#pragma warning disable CA1001 // Types that own disposable fields should be disposable. Suppressed to simplify tests, as DataTable fields used here do no need disposing, see https://stackoverflow.com/questions/913228/should-i-dispose-dataset-and-datatable.
    public class MockSqlExecuter : ISqlExecuter
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
    {
        readonly DataTable Columns;
        readonly DataTable Tables;
        readonly DataTable Schemas;

        public MockSqlExecuter(string columns, string tables, string schemas)
        {
            Columns = new DataTable();
            Columns.Columns.Add();
            Columns.Columns.Add();
            Columns.Columns.Add();
            foreach (var c in columns.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)))
                Columns.Rows.Add(c.Split('.'));

            Tables = new DataTable();
            Tables.Columns.Add();
            Tables.Columns.Add();
            foreach (var t in tables.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)))
                Tables.Rows.Add(t.Split('.'));

            Schemas = new DataTable();
            Schemas.Columns.Add();
            foreach (var s in schemas.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)))
            {
                if (!s.StartsWith("_"))
                    throw new ArgumentException("Invalid test data: Migration schema name must start with '_'.");
                Schemas.Rows.Add(s);
            }
        }

        public void ExecuteReader(string command, Action<System.Data.Common.DbDataReader> action)
        {
            var options = new Dictionary<string, DataTable>
                {
                    { "COLUMNS", Columns },
                    { "TABLES", Tables },
                    { "SCHEMATA", Schemas }
                };

            var dataTable = options.First(o => command.Contains(o.Key)).Value;

            using (var reader = new DataTableReader(dataTable))
            {
                while (reader.Read())
                    action(reader);
            }
        }

        readonly Regex DropColumn = new Regex(@"^ALTER TABLE \[(\w+)\].\[(\w+)\] DROP COLUMN \[(\w+)\]$");
        readonly Regex DropTable = new Regex(@"^DROP TABLE \[(\w+)\].\[(\w+)\]$");
        readonly Regex DropSchema = new Regex(@"^DROP SCHEMA \[(\w+)\]$");

        public List<string> DroppedColumns = new List<string>();
        public List<string> DroppedTables = new List<string>();
        public List<string> DroppedSchemas = new List<string>();

        public void ExecuteSql(IEnumerable<string> commands)
        {
            ExecuteSql(commands, null, null);
        }

        public void ExecuteSql(IEnumerable<string> commands, Action<int> beforeExecute, Action<int> afterExecute)
        {
            foreach (var command in commands)
            {
                Console.WriteLine("[SQL] " + command);

                var match = DropColumn.Match(command);
                if (match.Success)
                {
                    DroppedColumns.Add(match.Groups[1] + "." + match.Groups[2] + "." + match.Groups[3]);
                    continue;
                }

                match = DropTable.Match(command);
                if (match.Success)
                {
                    DroppedTables.Add(match.Groups[1] + "." + match.Groups[2]);
                    continue;
                }

                match = DropSchema.Match(command);
                if (match.Success)
                {
                    DroppedSchemas.Add(match.Groups[1].ToString());
                    continue;
                }

                throw new ArgumentException("Unexpected SQL command in MockSqlExecuter.");
            }
        }

        public void ExecuteReaderRaw(string query, object[] parameters, Action<DbDataReader> read)
        {
            throw new NotImplementedException();
        }

        public Task ExecuteReaderRawAsync(string query, object[] parameters, Action<DbDataReader> read, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public int ExecuteSqlRaw(string query, object[] parameters)
        {
            throw new NotImplementedException();
        }

        public Task<int> ExecuteSqlRawAsync(string query, object[] parameters, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public int GetTransactionCount() => 1;
    }
}