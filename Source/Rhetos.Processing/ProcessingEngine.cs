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

        public ProcessingEngine(
            IPluginsContainer<ICommandImplementation> commandRepository,
            IPluginsContainer<ICommandObserver> commandObservers,
            ILogProvider logProvider,
            IPersistenceTransaction persistenceTransaction,
            IAuthorizationManager authorizationManager,
            XmlUtility xmlUtility,
            IUserInfo userInfo,
            ISqlUtility sqlUtility)
        {
            _commandRepository = commandRepository;
            _commandObservers = commandObservers;
            _logger = logProvider.GetLogger("ProcessingEngine");
            _performanceLogger = logProvider.GetLogger("Performance." + GetType().Name);
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
        }

        public ProcessingResult Execute(IList<ICommandInfo> commands)
        {
            var executionId = Guid.NewGuid();

            _requestLogger.Trace(() => $"User: {ReportUserNameOrAnonymous(_userInfo)}, Commands({commands.Count}): {string.Join(", ", commands.Select(c => c.Summary()))}.");

            _commandsLogger.Trace(() => _xmlUtility.SerializeToXml(new ExecutionCommandsLogEntry { ExecutionId = executionId, UserInfo = _userInfo.Report(), Commands = commands }));

            ProcessingResult result = null;
            try
            {
                result = ExecuteInner(commands, executionId);
            }
            catch (Exception e)
            {
                _commandsResultLogger.Trace(() => _xmlUtility.SerializeToXml(new ExecutionResultLogEntry { ExecutionId = executionId, Error = e.ToString() }));
                ExceptionsUtility.Rethrow(e);
            }
            _commandsResultLogger.Trace(() => SafeSerialize(new ExecutionResultLogEntry { ExecutionId = executionId, Result = result }));
            return result;
        }

        private string SafeSerialize(ExecutionResultLogEntry logEntry)
        {
            try
            {
                return _xmlUtility.SerializeToXml(logEntry);
            }
            catch (Exception e)
            {
                return $"Cannot serialize command result '{logEntry.Result.GetType()}' for detailed logging, ExecutionId {logEntry.ExecutionId}. {e}";
            }
        }

        public ProcessingResult ExecuteInner(IList<ICommandInfo> commands, Guid executionId)
        {
            var authorizationErrorMessage = _authorizationManager.Authorize(commands);
            if (!string.IsNullOrEmpty(authorizationErrorMessage))
                throw new UserException(authorizationErrorMessage, authorizationErrorMessage); // Setting both messages for backward compatibility.

            var commandResults = new List<object>();

            foreach (var commandInfo in commands)
            {
                try
                {
                    _logger.Trace("Executing command {0}.", commandInfo.Summary());

                    var implementations = _commandRepository.GetImplementations(commandInfo.GetType());

                    if (!implementations.Any())
                        throw new FrameworkException(string.Format(CultureInfo.InvariantCulture,
                            "Cannot execute command \"{0}\". There are no command implementations loaded that implement the command.", commandInfo.Summary()));

                    if (implementations.Count() > 1)
                        throw new FrameworkException(string.Format(CultureInfo.InvariantCulture, 
                            "Cannot execute command \"{0}\". It has more than one implementation registered: {1}.", commandInfo.Summary(), string.Join(", ", implementations.Select(i => i.GetType().Name))));

                    var commandImplementation = implementations.Single();
                    _logger.Trace("Executing implementation {0}.", commandImplementation.GetType().Name);

                    var commandObserversForThisCommand = _commandObservers.GetImplementations(commandInfo.GetType());
                    var stopwatch = Stopwatch.StartNew();

                    foreach (var commandObeserver in commandObserversForThisCommand)
                    {
                        commandObeserver.BeforeExecute(commandInfo);
                        _performanceLogger.Write(stopwatch, () => "CommandObeserver.BeforeExecute " + commandObeserver.GetType().FullName);
                    }

                    object commandResult;
                    try
                    {
                        commandResult = commandImplementation.Execute(commandInfo);
                    }
                    finally
                    {
                        _performanceLogger.Write(stopwatch, () => "Command executed (" + commandImplementation + ": " + commandInfo.Summary() + ").");
                    }

                    foreach (var commandObeserver in commandObserversForThisCommand)
                    {
                        commandObeserver.AfterExecute(commandInfo, commandResult);
                        _performanceLogger.Write(stopwatch, () => "CommandObeserver.AfterExecute " + commandObeserver.GetType().FullName);
                    }

                    commandResults.Add(commandResult);
                }
                catch (Exception ex)
                {
                    _persistenceTransaction.DiscardOnDispose(); // This is not needed since Rhetos v5 because the transaction should be disposed by default. Review if needed for backward compatibility.

                    if (ex is TargetInvocationException && ex.InnerException is RhetosException)
                    {
                        _logger.Trace(() => "Unwrapping exception: " + ex.ToString());
                        ex = ex.InnerException;
                    }

                    ex = _sqlUtility.InterpretSqlException(ex) ?? ex;

                    _logger.Trace(() => $"Command failed: {commandInfo.Summary()}. {ex}");

                    var commandsErrorLogger = (ex is UserException || ex is ClientException)
                        ? _commandsClientErrorLogger
                        : _commandsServerErrorLogger;
                    commandsErrorLogger.Trace(() => _xmlUtility.SerializeToXml(new ExecutionCommandsLogEntry { ExecutionId = executionId, UserInfo = _userInfo.Report(), Commands = commands }));
                    commandsErrorLogger.Trace(() => _xmlUtility.SerializeToXml(new ExecutionResultLogEntry { ExecutionId = executionId, Error = ex.ToString() }));

                    if (ex is not UserException) // Skipped as a performance optimization, since UserException is a standard app behavior. Use other ProcessingEngine loggers instead to debug a UserException.
                        ExceptionsUtility.SetCommandSummary(ex, commandInfo.Summary());

                    ExceptionsUtility.Rethrow(ex);
                }
            }

            return new ProcessingResult { CommandResults = commandResults };
        }

        private static string ReportUserNameOrAnonymous(IUserInfo userInfo)
            => userInfo.IsUserRecognized ? userInfo.UserName : "<anonymous>";
    }
}
