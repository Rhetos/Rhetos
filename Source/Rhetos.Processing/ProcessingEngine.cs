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

using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Persistence;
using Rhetos.Security;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Rhetos.Processing
{
    public class ProcessingEngine : IProcessingEngine
    {
        private readonly IPluginsContainer<ICommandImplementation> _commandRepository;
        private readonly IPluginsContainer<ICommandObserver> _commandObservers;
        private readonly ILogger _logger;
        private readonly ILogger _performanceLogger;
        private readonly ILogger _requestLogger;
        private readonly ILogger _commandsLogger;
        private readonly ILogger _commandsResultLogger;
        private readonly ILogger _commandsClientErrorLogger;
        private readonly ILogger _commandsServerErrorLogger;
        private readonly IPersistenceTransaction _persistenceTransaction;
        private readonly IAuthorizationManager _authorizationManager;
        private readonly XmlUtility _xmlUtility;
        private readonly IUserInfo _userInfo;
        private readonly ISqlUtility _sqlUtility;
        private readonly ILocalizer _localizer;

        private static string _clientExceptionUserMessage = "Operation could not be completed because the request sent to the server was not valid or not properly formatted.";

        public ProcessingEngine(
            IPluginsContainer<ICommandImplementation> commandRepository,
            IPluginsContainer<ICommandObserver> commandObservers,
            ILogProvider logProvider,
            IPersistenceTransaction persistenceTransaction,
            IAuthorizationManager authorizationManager,
            XmlUtility xmlUtility,
            IUserInfo userInfo,
            ISqlUtility sqlUtility,
            ILocalizer localizer)
        {
            _commandRepository = commandRepository;
            _commandObservers = commandObservers;
            _logger = logProvider.GetLogger("ProcessingEngine");
            _performanceLogger = logProvider.GetLogger("Performance");
            _requestLogger = logProvider.GetLogger("ProcessingEngine Request");
            _commandsLogger = logProvider.GetLogger("ProcessingEngine Commands");
            _commandsResultLogger = logProvider.GetLogger("ProcessingEngine CommandsResult");
            _commandsClientErrorLogger = logProvider.GetLogger("ProcessingEngine CommandsWithClientError");
            _commandsServerErrorLogger = logProvider.GetLogger("ProcessingEngine CommandsWithServerError");
            _persistenceTransaction = persistenceTransaction;
            _authorizationManager = authorizationManager;
            _xmlUtility = xmlUtility;
            _userInfo = userInfo;
            _sqlUtility = sqlUtility;
            _localizer = localizer;
        }

        public ProcessingResult Execute(IList<ICommandInfo> commands)
        {
            _requestLogger.Trace(() => $"User: {ReportUserNameOrAnonymous(_userInfo)}, Commands({commands.Count}): {string.Join(", ", commands.Select(c => c.ToString()))}.");
            var executionId = Guid.NewGuid();
            _commandsLogger.Trace(() => _xmlUtility.SerializeToXml(new ExecutionCommandsLogEntry { ExecutionId = executionId, UserInfo = _userInfo.Report(), Commands = commands }));

            var result = ExecuteInner(commands, executionId);
            _commandsResultLogger.Trace(() => _xmlUtility.SerializeToXml(new ExecutionResultLogEntry { ExecutionId = executionId, Result = result }));

            // On error, the CommandResults will contain partial results of the commands executed before the failed one, and should be cleared.
            if (!result.Success)
                result.CommandResults = null;

            return result;
        }

        public ProcessingResult ExecuteInner(IList<ICommandInfo> commands, Guid executionId)
        {
            var authorizationMessage = _authorizationManager.Authorize(commands);

            if (!string.IsNullOrEmpty(authorizationMessage))
                return new ProcessingResult
                {
                    UserMessage = authorizationMessage,
                    SystemMessage = authorizationMessage,
                    Success = false
                };

            var commandResults = new List<CommandResult>();
            var stopwatch = Stopwatch.StartNew();

            foreach (var commandInfo in commands)
            {
                try
                {
                    _logger.Trace("Executing command {0}.", commandInfo);

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
                    stopwatch.Restart();

                    foreach (var commandObeserver in commandObserversForThisCommand)
                    {
                        commandObeserver.BeforeExecute(commandInfo);
                        _performanceLogger.Write(stopwatch, () => "ProcessingEngine: CommandObeserver.BeforeExecute " + commandObeserver.GetType().FullName);
                    }

                    CommandResult commandResult;
                    try
                    {
                        commandResult = commandImplementation.Execute(commandInfo);
                    }
                    finally
                    {
                        _performanceLogger.Write(stopwatch, () => "ProcessingEngine: Command executed (" + commandImplementation + ": " + commandInfo + ").");
                    }
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

                        var systemMessage = "Command failed: " + commandImplementation + ", " + commandInfo + ".";
                        return LogAndReturnError(commandResults, systemMessage + " " + commandResult.Message, systemMessage, commandResult.Message, null, commands, executionId);
                    }
                }
                catch (Exception ex)
                {
                    _persistenceTransaction.DiscardChanges();

                    if (ex is TargetInvocationException && ex.InnerException is RhetosException)
                    {
                        _logger.Trace(() => "Unwrapping exception: " + ex.ToString());
                        ex = ex.InnerException;
                    }

                    string userMessage = null;
                    string systemMessage = null;

                    ex = _sqlUtility.InterpretSqlException(ex) ?? ex;

                    if (ex is UserException userException)
                    {
                        userMessage = _localizer[userException.Message, userException.MessageParameters]; // TODO: Remove this code after cleaning the double layer of exceptions in the server response call stack.
                        systemMessage = userException.SystemMessage;
                    }
                    else if (ex is ClientException)
                    {
                        userMessage = _clientExceptionUserMessage;
                        systemMessage = ex.Message;
                    }
                    else
                    {
                        userMessage = null;
                        systemMessage = FrameworkException.GetInternalServerErrorMessage(_localizer, ex);
                    }

                    return LogAndReturnError(commandResults, "Command failed: " + commandInfo + ".", systemMessage, userMessage, ex, commands, executionId);
                }
            }

            return new ProcessingResult
            {
                CommandResults = commandResults.ToArray(),
                Success = true,
                SystemMessage = null
            };
        }

        private ProcessingResult LogAndReturnError(List<CommandResult> commandResults, string logError, string systemMessage, string userMessage, Exception logException, IList<ICommandInfo> commands, Guid executionId)
        {
            var errorSeverity = logException == null ? EventType.Error
                : logException is UserException ? EventType.Trace
                : logException is ClientException ? EventType.Info
                : EventType.Error;
                 
            _logger.Write(errorSeverity, () => logError + (string.IsNullOrEmpty(logError) ? "" : " ") + logException);

            var result = new ProcessingResult
            {
                CommandResults = commandResults.ToArray(),
                Success = false,
                SystemMessage = systemMessage,
                UserMessage = userMessage
            };

            var commandsErrorLogger = (errorSeverity == EventType.Error) ? _commandsServerErrorLogger : _commandsClientErrorLogger;
            commandsErrorLogger.Trace(() => _xmlUtility.SerializeToXml(new ExecutionCommandsLogEntry { ExecutionId = executionId, UserInfo = _userInfo.Report(), Commands = commands }));
            commandsErrorLogger.Trace(() => _xmlUtility.SerializeToXml(new ExecutionResultLogEntry { ExecutionId = executionId, Result = result }));

            return result;
        }

        private static string ReportUserNameOrAnonymous(IUserInfo userInfo) => userInfo.IsUserRecognized ? userInfo.UserName : "<anonymous>";
    }
}
