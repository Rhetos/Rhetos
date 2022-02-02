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

using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Rhetos.DatabaseGenerator.Test
{
    public class MockSqlExecuterReport : List<(List<string> Scripts, bool UseTransaction)>
    {
    }

    public class MockSqlExecuter : ISqlExecuter
    {
        private readonly PersistenceTransactionOptions _persistenceTransactionOptions;
        private readonly MockSqlExecuterReport _mockSqlExecuterReport;

        public MockSqlExecuter(PersistenceTransactionOptions persistenceTransactionOptions, MockSqlExecuterReport mockSqlExecuterReport)
        {
            _persistenceTransactionOptions = persistenceTransactionOptions;
            _mockSqlExecuterReport = mockSqlExecuterReport;
        }

        public int GetTransactionCount() => _persistenceTransactionOptions.UseDatabaseTransaction ? 1 : 0;

        public void ExecuteReader(string command, Action<DbDataReader> action)
        {
            throw new NotImplementedException();
        }

        public void ExecuteReaderRaw(string query, object[] parameters, Action<DbDataReader> read)
        {
            throw new NotImplementedException();
        }

        public Task ExecuteReaderRawAsync(string query, object[] parameters, Action<DbDataReader> read, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public void ExecuteSql(IEnumerable<string> commands)
        {
            ExecuteSql(commands, null, null);
        }

        public void ExecuteSql(IEnumerable<string> commands,
            Action<int> beforeExecute, Action<int> afterExecute)
        {
            _mockSqlExecuterReport.Add((commands.ToList(), _persistenceTransactionOptions.UseDatabaseTransaction));
        }

        public int ExecuteSqlRaw(string query, object[] parameters)
        {
            throw new NotImplementedException();
        }

        public Task<int> ExecuteSqlRawAsync(string query, object[] parameters, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
