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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos;
using Rhetos.Utilities;
using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CommonConcepts.Test
{
    public static class ConcurrencyUtility
    {
        private static int _checkedForParallelismThreadCount = 0;

        public static void CheckForParallelism(IUnitOfWorkScope scope, int requiredNumberOfThreads)
        {
            if (_checkedForParallelismThreadCount >= requiredNumberOfThreads)
                return;

            string connectionString = scope.Resolve<ConnectionString>();

            ExecuteSqlCommand(connectionString, "WAITFOR DELAY '00:00:00.000'"); // Possible cold start.

            var sw = Stopwatch.StartNew();
            Parallel.For(0, requiredNumberOfThreads,
                x => { ExecuteSqlCommand(connectionString, "WAITFOR DELAY '00:00:00.100'"); });
            sw.Stop();

            Console.WriteLine($"CheckForParallelism: {sw.ElapsedMilliseconds} ms.");

            if (sw.ElapsedMilliseconds < 90)
                Assert.Fail($"Delay is unexpectedly short: {sw.ElapsedMilliseconds}");

            if (sw.Elapsed.TotalMilliseconds > 190)
                Assert.Inconclusive($"This test requires {requiredNumberOfThreads} parallel SQL queries. {requiredNumberOfThreads} parallel delays for 100 ms are executed in {sw.ElapsedMilliseconds} ms.");

            _checkedForParallelismThreadCount = requiredNumberOfThreads;
        }

        private static void ExecuteSqlCommand(string connectionString, string sql)
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            using var command = new SqlCommand(sql, connection);
            command.ExecuteNonQuery();
            connection.Close();
        }
    }
}
