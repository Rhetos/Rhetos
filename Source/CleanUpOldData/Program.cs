/*
    Copyright (C) 2013 Omega software d.o.o.

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
using System.Text;
using System.Threading;
using Rhetos.Utilities;
using Rhetos.Deployment;
using System.Collections.Generic;
using Rhetos;

namespace CleanupOldData
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                string connectionString = SqlUtility.ConnectionString;
                Console.WriteLine("SQL connection string: " + SqlUtility.MaskPassword(connectionString));
                var sqlExecuter = GetSqlExecuterImplementation(connectionString);

                var databaseCleaner = new DatabaseCleaner(new ConsoleLogProvider(), sqlExecuter);
                databaseCleaner.CleanupOldData();
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                DeploymentUtility.WriteError(ex.Message);
                Console.WriteLine("Details:");
                Console.WriteLine(ex);
                Thread.Sleep(3000);
                return 1;
            }
        }

        private static ISqlExecuter GetSqlExecuterImplementation(string connectionString)
        {
            var sqlExecuterImplementations = new Dictionary<string, Lazy<ISqlExecuter>>()
            {
                { "MsSql", new Lazy<ISqlExecuter>(() => new MsSqlExecuter(connectionString, new ConsoleLogProvider(), new NullUserInfo())) },
                { "Oracle", new Lazy<ISqlExecuter>(() => new OracleSqlExecuter(connectionString, new ConsoleLogProvider(), new NullUserInfo())) }
            };

            Lazy<ISqlExecuter> sqlExecuter;
            if (!sqlExecuterImplementations.TryGetValue(SqlUtility.DatabaseLanguage, out sqlExecuter))
                throw new FrameworkException("Unsupported database language '" + SqlUtility.DatabaseLanguage
                    + "'. Supported languages are: " + string.Join(", ", sqlExecuterImplementations.Keys) + ".");

            return sqlExecuter.Value;
        }
    }
}
