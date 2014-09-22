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
using System.Reflection;

namespace Rhetos.Processing
{
    public class ProcessingEngine : IProcessingEngine
    {
        private readonly IPluginsContainer<ICommandImplementation> _commandRepository;
        private readonly IPluginsContainer<ICommandObserver> _commandObservers;
        private readonly ILogger _logger;
        private readonly ILogger _performanceLogger;
        private readonly IPersistenceTransaction _persistenceTransaction;
        private readonly IAuthorizationManager _authorizationManager;
        private readonly XmlUtility _xmlUtility;
        private readonly IUserInfo _userInfo;

        private static string _clientExceptionUserMessage = "Client request error";

        public ProcessingEngine(
            IPluginsContainer<ICommandImplementation> commandRepository,
            IPluginsContainer<ICommandObserver> commandObservers,
            ILogProvider logProvider,
            IPersistenceTransaction persistenceTransaction,
            IAuthorizationManager authorizationManager,
            XmlUtility xmlUtility,
            IUserInfo userInfo)
        {
            _commandRepository = commandRepository;
            _commandObservers = commandObservers;
            _logger = logProvider.GetLogger("ProcessingEngine");
            _performanceLogger = logProvider.GetLogger("Performance");
            _persistenceTransaction = persistenceTransaction;
            _authorizationManager = authorizationManager;
            _xmlUtility = xmlUtility;
            _userInfo = userInfo;
        }

        string StringifyCommandNames(IList<ICommandInfo> commands)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var commandInfo in commands)
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(commandInfo.GetType().Name);
            }
            return sb.ToString();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public ProcessingResult Execute(IList<ICommandInfo> commands)
        {
            _logger.Info(() => string.Format("Process Request, User: {0}, Commands({1}): {2}", _userInfo.UserName, commands.Count, StringifyCommandNames(commands)));

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

            try
            {
                foreach (var commandInfo in commands)
                {
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

                    var commandObserversForThisCommand = _commandObservers.GetImplementations(commandInfo.GetType());
                    var stopwatch = Stopwatch.StartNew();

                    foreach (var commandObeserver in commandObserversForThisCommand)
                    {
                        commandObeserver.BeforeExecute(commandInfo);
                        _performanceLogger.Write(stopwatch, () => "ProcessingEngine: CommandObeserver.BeforeExecute " + commandObeserver.GetType().FullName);
                    }

                    var commandResult = commandImplementation.Execute(commandInfo);

                    _performanceLogger.Write(stopwatch, "ProcessingEngine: Command executed.");
                    _logger.Trace("Execution result message: {0}", commandResult.Message);

                    if (commandResult.Success)
                        foreach (var commandObeserver in commandObserversForThisCommand)
                        {
                            commandObeserver.AfterExecute(commandInfo, commandResult);
                            _performanceLogger.Write(stopwatch, () => "ProcessingEngine: CommandObeserver.AfterExecute " + commandObeserver.GetType().FullName);
                        }

                    commandResults.Add(commandResult);

                    if (!commandResult.Success)
                    {
                        _persistenceTransaction.DiscardChanges();

                        var systemMessage = String.Format(CultureInfo.InvariantCulture, "Command failed. {0} {1} {2}", commandInfo.GetType().Name, commandInfo, commandImplementation.GetType().Name);
                        return LogResultsReturnError(commandResults, systemMessage + " " + commandResult.Message, systemMessage, commandResult.Message);
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

                if (ex is TargetInvocationException && ex.InnerException is RhetosException)
                {
                    _logger.Error(() => "Unwrapping exception: " + ex.ToString());
                    ex = ex.InnerException;
                }

                string userMessage = null;
                string systemMessage = null;
                
                if (ex is UserException) 
                {
                    userMessage = ex.Message;
                    systemMessage = (ex as UserException).SystemMessage;
                }
                else if (ex is ClientException)
                {
                    // some interfaces (REST) assume that internal error (FrameworkException) occured if userMessage is not set
                    userMessage = _clientExceptionUserMessage;
                    systemMessage = ex.Message;
                }
                else
                {
                    var permissionError = CheckSqlPermissionError(ex);
                    if (permissionError != null)
                        systemMessage = permissionError;
                    else
                        userMessage = TryParseSqlException(ex);
                }

                if (userMessage == null && systemMessage == null)
                    systemMessage = ex.GetType().Name + ". For details see RhetosServer.log.";

                return LogResultsReturnError(
                    commandResults,
                    "Command execution error: " + ex,
                    systemMessage,
                    userMessage);
            }
        }

        // TODO: Make this check DB Provider independant. This implementation works only on MS SQL
        private static string CheckSqlPermissionError(Exception exception)
        {
            var sqlException = ExtractSqlException(exception);
            if (sqlException == null) return null;
            if (sqlException.Number == 229 || sqlException.Number == 230)
                if (sqlException.Message.Contains("permission was denied"))
                    return "Rhetos server lacks sufficient database permissions for this operation! It is suggested that Rhetos Server process has db_owner role for the database.";

            return null;
        }

        private static string TryParseSqlException(Exception exception)
        {
            var sqlException = ExtractSqlException(exception);
            if (sqlException == null)
                return null;

            if (sqlException.State == 101) // Rhetos convention for an error raised in SQL that is intended as a message to the end user.
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

        private ProcessingResult LogResultsReturnError(List<CommandResult> commandResults, string logError, string systemMessage, string userMessage)
        {
            _logger.Error(logError);
            _logger.Trace(_xmlUtility.SerializeArrayToXml(commandResults.ToArray()));
            return new ProcessingResult
                {
                    Success = false,
                    SystemMessage = systemMessage,
                    UserMessage = userMessage
                };
        }
    }
}
