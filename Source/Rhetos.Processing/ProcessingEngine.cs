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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhetos.Utilities;
using Rhetos.Persistence;
using System.Diagnostics;
using System.Globalization;
using Rhetos.Extensibility;
using Rhetos.Logging;
using System.Data.SqlClient;
using Autofac.Features.OwnedInstances;

namespace Rhetos.Processing
{
    public class ProcessingEngine : IProcessingEngine
    {
        private readonly IPluginsContainer<ICommandImplementation> CommandRepository;
        private readonly ILogger Logger;
        private readonly ILogger PerformanceLogger;
        private readonly Lazy<IPersistenceTransaction> _persistenceTransactionLazy;

        public ProcessingEngine(
            IPluginsContainer<ICommandImplementation> commandRepository,
            ILogProvider logProvider,
            Lazy<IPersistenceTransaction> persistenceTransactionLazy)
        {
            CommandRepository = commandRepository;
            Logger = logProvider.GetLogger("ProcessingEngine");
            PerformanceLogger = logProvider.GetLogger("Performance");
            _persistenceTransactionLazy = persistenceTransactionLazy;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public ProcessingResult Execute(IEnumerable<ICommandInfo> commands)
        {
            using (var persistenceTransaction = _persistenceTransactionLazy.Value)
            {
                var commandResults = new List<CommandResult>();
                int commandCount = 0;

                try
                {
                    persistenceTransaction.Initialize();

                    foreach (var commandInfo in commands)
                    {
                        commandCount++;

                        Logger.Trace("Executing command {0}: {1}.", commandInfo.GetType().Name, commandInfo);

                        var implementations = CommandRepository.GetImplementations(commandInfo.GetType());

                        if (implementations.Count() == 0)
                            throw new FrameworkException(string.Format(CultureInfo.InvariantCulture,
                                "Cannot execute command \"{0}\". There are no command implementations loaded that implement the command.", commandInfo));

                        if (implementations.Count() > 1)
                            throw new FrameworkException(string.Format(CultureInfo.InvariantCulture, 
                                "Cannot execute command \"{0}\". It has more than one implementation registered: {1}.", commandInfo, String.Join(", ", implementations.Select(i => i.GetType().Name))));

                        var commandImplementation = implementations.Single();
                        Logger.Trace("Executing implementation {0}.", commandImplementation.GetType().Name);

                        var swCommand = Stopwatch.StartNew();

                        var commandResult = commandImplementation.Execute(commandInfo);

                        swCommand.Stop();
                        Logger.Trace("Execution result message: {0}", commandResult.Message);
                        PerformanceLogger.Write(swCommand, "ProcessingEngine: Command executed.");

                        commandResults.Add(commandResult);

                        if (!commandResult.Success)
                        {
                            persistenceTransaction.DiscardChanges();

                            var systemMessage = String.Format(CultureInfo.InvariantCulture, "Command failed. {0} {1} {2}", commandInfo.GetType().Name, commandInfo, commandImplementation.GetType().Name);
                            return LogResultsReturnError(commandResults, systemMessage + " " + commandResult.Message, commandCount, systemMessage, commandResult.Message);
                        }
                    }

                    persistenceTransaction.ApplyChanges();

                    return new ProcessingResult
                    {
                        CommandResults = commandResults.ToArray(),
                        Success = true,
                        SystemMessage = null
                    };
                }
                catch (Exception ex)
                {
                    if (commandCount == 0)
                    {
                        Logger.Error("Processing engine exception. {0}", ex);
                        return new ProcessingResult
                        {
                            SystemMessage = "Server exception." + Environment.NewLine + ex,
                            Success = false
                        };
                    }

                    persistenceTransaction.DiscardChanges();
                    string userMessage = null;
                    string systemMessage = null;
                    if (ex is UserException) {
                        userMessage = ex.Message;
                        systemMessage = (ex as UserException).SystemMessage;
                    }
                    if (userMessage == null)
                        userMessage = TryParseSqlException(ex);

                    if (userMessage == null && systemMessage == null)
                        systemMessage = ex.GetType().Name + ". For details see RhetosServer.log.";

                    return LogResultsReturnError(
                        commandResults,
                        "Exception in command execution or ApplyChanges. " + ex,
                        commandCount,
                        systemMessage,
                        userMessage);
                }
            }
        }

        private static string TryParseSqlException(Exception exception)
        {
            var sqlException = ExtractSqlException(exception);
            if (sqlException == null)
                return null;

            if (sqlException.State == 101) // Our convention for an error raised in SQL that is intended as a message to the end user.
                return sqlException.Message;

            if (sqlException.Message.StartsWith("Cannot insert duplicate key"))
                return "It not allowed to enter a duplicate record."; // TODO: Internationalization.

            if (sqlException.Message.StartsWith("The DELETE statement conflicted with the REFERENCE constraint"))
                return "It not allowed to delete a record that is referenced by other records."; // TODO: Internationalization.

            if (sqlException.Message.StartsWith("The DELETE statement conflicted with the SAME TABLE REFERENCE constraint"))
                return "It not allowed to delete a record that is referenced by other records."; // TODO: Internationalization.

            if (sqlException.Message.StartsWith("The INSERT statement conflicted with the FOREIGN KEY constraint"))
                return "It not allowed to enter the record. An entered value references nonexistent record."; // TODO: Internationalization.

            if (sqlException.Message.StartsWith("The UPDATE statement conflicted with the FOREIGN KEY constraint"))
                return "It not allowed to edit the record. An entered value references nonexistent record."; // TODO: Internationalization.

            return null;
        }

        private static SqlException ExtractSqlException(Exception exception)
        {
            if (exception is SqlException)
                return (SqlException)exception;
            if (exception.InnerException != null)
                return ExtractSqlException(exception.InnerException);
            return null;
        }

        private ProcessingResult LogResultsReturnError(List<CommandResult> commandResults, string logError, int commandCount, string systemMessage, string userMessage)
        {
            Logger.Error(logError);
            Logger.Trace(XmlUtility.SerializeArrayToXml(commandResults.ToArray()));
            return new ProcessingResult
                {
                    Success = false,
                    SystemMessage = systemMessage,
                    UserMessage = userMessage
                };
        }
    }
}
