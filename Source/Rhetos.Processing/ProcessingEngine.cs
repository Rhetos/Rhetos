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
using Rhetos.Security;

namespace Rhetos.Processing
{
    public class ProcessingEngine : IProcessingEngine
    {
        private readonly IPluginsContainer<ICommandImplementation> _commandRepository;
        private readonly ILogger _logger;
        private readonly ILogger _performanceLogger;
        private readonly IPersistenceTransaction _persistenceTransaction;
        private readonly IAuthorizationManager _authorizationManager;

        public ProcessingEngine(
            IPluginsContainer<ICommandImplementation> commandRepository,
            ILogProvider logProvider,
            IPersistenceTransaction persistenceTransaction,
            IAuthorizationManager authorizationManager)
        {
            _commandRepository = commandRepository;
            _logger = logProvider.GetLogger("ProcessingEngine");
            _performanceLogger = logProvider.GetLogger("Performance");
            _persistenceTransaction = persistenceTransaction;
            _authorizationManager = authorizationManager;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public ProcessingResult Execute(IEnumerable<ICommandInfo> commands)
        {
            var authorizationMessage = _authorizationManager.Authorize(commands);
            _persistenceTransaction.NHibernateSession.Clear(); // NHibernate cached data from AuthorizationManager may cause problems later with serializing arrays that mix cached proxies with POCO instance.

            if (!String.IsNullOrEmpty(authorizationMessage))
                return new ProcessingResult
                {
                    UserMessage = authorizationMessage,
                    SystemMessage = authorizationMessage,
                    Success = false
                };

            var commandResults = new List<CommandResult>();
            int commandCount = 0;

            try
            {
                foreach (var commandInfo in commands)
                {
                    commandCount++;

                    _logger.Trace("Executing command {0}: {1}.", commandInfo.GetType().Name, commandInfo);

                    var implementations = _commandRepository.GetImplementations(commandInfo.GetType());

                    if (implementations.Count() == 0)
                        throw new FrameworkException(string.Format(CultureInfo.InvariantCulture,
                            "Cannot execute command \"{0}\". There are no command implementations loaded that implement the command.", commandInfo));

                    if (implementations.Count() > 1)
                        throw new FrameworkException(string.Format(CultureInfo.InvariantCulture, 
                            "Cannot execute command \"{0}\". It has more than one implementation registered: {1}.", commandInfo, String.Join(", ", implementations.Select(i => i.GetType().Name))));

                    var commandImplementation = implementations.Single();
                    _logger.Trace("Executing implementation {0}.", commandImplementation.GetType().Name);

                    var swCommand = Stopwatch.StartNew();

                    var commandResult = commandImplementation.Execute(commandInfo);

                    swCommand.Stop();
                    _logger.Trace("Execution result message: {0}", commandResult.Message);
                    _performanceLogger.Write(swCommand, "ProcessingEngine: Command executed.");

                    commandResults.Add(commandResult);

                    if (!commandResult.Success)
                    {
                        _persistenceTransaction.DiscardChanges();

                        var systemMessage = String.Format(CultureInfo.InvariantCulture, "Command failed. {0} {1} {2}", commandInfo.GetType().Name, commandInfo, commandImplementation.GetType().Name);
                        return LogResultsReturnError(commandResults, systemMessage + " " + commandResult.Message, commandCount, systemMessage, commandResult.Message);
                    }
                }

                return new ProcessingResult
                {
                    CommandResults = commandResults.ToArray(),
                    Success = true,
                    SystemMessage = null
                };
            }
            catch (Exception ex)
            {
                _persistenceTransaction.DiscardChanges();

                if (commandCount == 0)
                {
                    _logger.Error("Processing engine exception. {0}", ex);
                    return new ProcessingResult
                    {
                        SystemMessage = "Server exception." + Environment.NewLine + ex,
                        Success = false
                    };
                }

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
                    "Command execution error: " + ex,
                    commandCount,
                    systemMessage,
                    userMessage);
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
                return "It is not allowed to enter a duplicate record."; // TODO: Internationalization.

            if (sqlException.Message.StartsWith("The DELETE statement conflicted with the REFERENCE constraint"))
                return "It is not allowed to delete a record that is referenced by other records."; // TODO: Internationalization.

            if (sqlException.Message.StartsWith("The DELETE statement conflicted with the SAME TABLE REFERENCE constraint"))
                return "It is not allowed to delete a record that is referenced by other records."; // TODO: Internationalization.

            if (sqlException.Message.StartsWith("The INSERT statement conflicted with the FOREIGN KEY constraint"))
                return "It is not allowed to enter the record. An entered value references nonexistent record."; // TODO: Internationalization.

            if (sqlException.Message.StartsWith("The UPDATE statement conflicted with the FOREIGN KEY constraint"))
                return "It is not allowed to edit the record. An entered value references nonexistent record."; // TODO: Internationalization.

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
            _logger.Error(logError);
            _logger.Trace(XmlUtility.SerializeArrayToXml(commandResults.ToArray()));
            return new ProcessingResult
                {
                    Success = false,
                    SystemMessage = systemMessage,
                    UserMessage = userMessage
                };
        }
    }
}
