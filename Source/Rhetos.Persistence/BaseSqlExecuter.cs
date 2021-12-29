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
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rhetos.Persistence
{
    /// <summary>
    /// Contains some default implementations for <see cref="Rhetos.Utilities.ISqlExecuter" />.
    /// Consumer should use it as a mixin class, without having to reimplement the default methods.
    /// Also, it provides some cross-cutting concern utility methods, which are database provider agnostic.
    /// </summary>
    public class BaseSqlExecuter
    {
        protected readonly IPersistenceTransaction _persistenceTransaction;
        protected readonly IUserInfo _userInfo;
        protected readonly ILogger _logger;
        protected readonly ILogger _performanceLogger;

        public BaseSqlExecuter(ILogProvider logProvider, 
            IUserInfo userInfo,
            IPersistenceTransaction persistenceTransaction)
        {
            _logger = logProvider.GetLogger(GetType().Name);
            _performanceLogger = logProvider.GetLogger("Performance." + GetType().Name);
            _userInfo = userInfo;
            _persistenceTransaction = persistenceTransaction;
        }

        public int ExecuteSqlRaw(string query, object[] parameters)
        {
            using var command = CreateCommand(query, parameters);
            return command.ExecuteNonQuery();
        }

        public async Task<int> ExecuteSqlRawAsync(string query, object[] parameters, CancellationToken cancellationToken)
        {
            using var command = CreateCommand(query, parameters);
            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        public void ExecuteReaderRaw(string query, object[] parameters, Action<DbDataReader> read)
        {
            using var command = CreateCommand(query, parameters);
            using var reader = command.ExecuteReader();
            while (reader.Read())
                read(reader);
        }

        public async Task ExecuteReaderRawAsync(string query, object[] parameters, Action<DbDataReader> read, CancellationToken cancellationToken)
        {
            using var command = CreateCommand(query, parameters);
            using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                read(reader);
        }

        private DbCommand CreateCommand(string sql, object[] parameters)
        {
            var command = _persistenceTransaction.Connection.CreateCommand();
            command.Transaction = _persistenceTransaction.Transaction;
            PrepareCommandTextAndParameters(command, sql, parameters);
            return command;
        }

        private const string parameterPrefix = "@__p";

        private void PrepareCommandTextAndParameters(DbCommand command, string sql, object[] parameters)
        {
            var substitutions = new List<string>();
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                if (parameter is DbParameter dbParameter)
                {
                    if (dbParameter.ParameterName.StartsWith(parameterPrefix))
                        throw new ArgumentException($"¨SQL parameter name should not start with '{parameterPrefix}'.");
                    substitutions.Add(dbParameter.ParameterName);
                    command.Parameters.Add(dbParameter);
                }
                else
                {
                    substitutions.Add(parameterPrefix + i);
                    var createdDbParameter = command.CreateParameter();
                    createdDbParameter.ParameterName = parameterPrefix + i.ToString(CultureInfo.InvariantCulture);
                    createdDbParameter.Value = parameter ?? DBNull.Value;
                    command.Parameters.Add(createdDbParameter);
                }
            }

            command.CommandText = string.Format(sql, substitutions.ToArray());
        }

        protected void LogPerformanceIssue(Stopwatch sw, string sql)
        {
            if (sw.Elapsed >= LoggerHelper.SlowEvent) // Avoid flooding the performance trace log.
                _performanceLogger.Write(sw, () => sql.Limit(50000, true));
            else
                sw.Restart(); // _performanceLogger.Write would restart the stopwatch.
        }

        protected void ExecuteReader(DbCommand sqlCommand, Action<DbDataReader> action)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                var dataReader = sqlCommand.ExecuteReader();
                while (!dataReader.IsClosed && dataReader.Read())
                    action(dataReader);
                dataReader.Close();
            }
            finally
            {
                LogPerformanceIssue(sw, sqlCommand.CommandText);
            }
        }

        protected static string ReportSqlScripts(IEnumerable<string> commands, int maxLength)
        {
            var report = new StringBuilder();
            report.Append($"Executing {commands.Count()} scripts:");

            foreach (var sql in commands)
            {
                report.Append("\r\n");
                report.Append(sql.Limit(maxLength, true));
                if (report.Length > maxLength)
                {
                    report.Append("\r\n...");
                    break;
                }
            }

            return report.ToString();
        }
    }
}
