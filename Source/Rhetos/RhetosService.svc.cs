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
using System.Diagnostics;
using System.Linq;
using Rhetos.Dom;
using Rhetos.Factory;
using Rhetos.Processing;
using Rhetos.Utilities;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Security;

namespace Rhetos
{
    public class RhetosService : IServerApplication
    {
        private readonly IProcessingEngine _processingEngine;
        private readonly IEnumerable<ICommandInfo> _commands;
        private IDictionary<string, ICommandInfo> _commandsByName;
        private readonly ILogger _logger;
        private readonly ILogger _commandsLogger;
        private readonly ILogger _commandResultsLogger;
        private readonly ILogger _performanceLogger;
        private readonly IAuthorizationManager _authorizationManager;
        private readonly Func<WcfUserInfo> _wcfUserInfoFactory;
        private readonly IDomainObjectModel _domainObjectModel;

        public RhetosService(
            IProcessingEngine processingEngine,
            IEnumerable<ICommandInfo> commands,
            ILogProvider logProvider,
            IAuthorizationManager authorizationManager,
            Func<WcfUserInfo> wcfUserInfoFactory,
            IDomainObjectModel domainObjectModel)
        {
            _processingEngine = processingEngine;
            _commands = commands;
            _logger = logProvider.GetLogger("IServerApplication.RhetosService.Execute");
            _commandsLogger = logProvider.GetLogger("IServerApplication Commands");
            _commandResultsLogger = logProvider.GetLogger("IServerApplication CommandResults");
            _performanceLogger = logProvider.GetLogger("Performance");
            _authorizationManager = authorizationManager;
            _wcfUserInfoFactory = wcfUserInfoFactory;
            _domainObjectModel = domainObjectModel;
        }

        public ServerProcessingResult Execute(ServerCommandInfo[] commands)
        {
            var stopwatch = Stopwatch.StartNew();

            var serverCallID = Guid.NewGuid();
            _commandsLogger.Trace(() => XmlUtility.SerializeLogElementToXml(commands, serverCallID));

            var result = ExecuteInner(commands);

            _commandResultsLogger.Trace(() => XmlUtility.SerializeLogElementToXml(result, serverCallID));
            _performanceLogger.Write(stopwatch, "RhetosService: Executed " + string.Join(",", commands.Select(c => c.CommandName)) + ".");

            return result;
        }

        private ServerProcessingResult ExecuteInner(ServerCommandInfo[] commands)
        {
            var stopwatch = Stopwatch.StartNew();

            if (_commandsByName == null)
                PrepareCommandByName();

            if (commands == null || commands.Length == 0)
                return new ServerProcessingResult { SystemMessage = "Commands missing", Success = false };

            if (XmlUtility.Dom == null)
                lock (XmlUtility.DomLock)
                    if (XmlUtility.Dom == null)
                        XmlUtility.Dom = _domainObjectModel.ObjectModel;

            _performanceLogger.Write(stopwatch, "RhetosService.ExecuteInner: Server initialization done.");

            var processingCommandsOrError = Deserialize(commands);
            if (processingCommandsOrError.IsError)
                return new ServerProcessingResult
                    {
                        Success = false,
                        SystemMessage = processingCommandsOrError.Error
                    };
            var processingCommands = processingCommandsOrError.Value;

            _performanceLogger.Write(stopwatch, "RhetosService.ExecuteInner: Commands deserialized.");
            
            var authorizationMessage = _authorizationManager.Authorize(processingCommands);

            if (!String.IsNullOrEmpty(authorizationMessage))
                return new ServerProcessingResult
                    {
                        Success = false,
                        SystemMessage = authorizationMessage,
                        UserMessage = authorizationMessage
                    };

            _performanceLogger.Write(stopwatch, "RhetosService.ExecuteInner: Commands authorized.");

            var result = _processingEngine.Execute(processingCommands, _wcfUserInfoFactory());

            _performanceLogger.Write(stopwatch, "RhetosService.ExecuteInner: Commands executed.");

            var convertedResult = ConvertResult(result);

            _performanceLogger.Write(stopwatch, "RhetosService.ExecuteInner: Result converted.");

            return convertedResult;
        }

        private void PrepareCommandByName()
        {
            var commandNames = _commands
                .SelectMany(command => new[] { command.GetType().Name, command.GetType().FullName, command.GetType().AssemblyQualifiedName }
                    .Select(name => new { command, name }));

            var invalidGroup = commandNames.GroupBy(cn => cn.name).Where(g => g.Count() > 1).FirstOrDefault();
            if (invalidGroup != null)
                throw new FrameworkException(string.Format(
                    "Two commands {0} and {1} have the same name: \"{2}\".",
                    invalidGroup.ToArray()[0].command.GetType().AssemblyQualifiedName,
                    invalidGroup.ToArray()[1].command.GetType().AssemblyQualifiedName,
                    invalidGroup.Key));

            _commandsByName = commandNames.ToDictionary(cn => cn.name, cn => cn.command);
        }

        private ValueOrError<List<ICommandInfo>> Deserialize(IEnumerable<ServerCommandInfo> commands)
        {
            if (commands.Any(c => c == null))
                return ValueOrError.CreateError("Null command sent.");

            var commandsWithType = commands.Select(c => 
                {
                    Type commandType = null;
                    ICommandInfo command;
                    if (_commandsByName.TryGetValue(c.CommandName, out command))
                        commandType = command.GetType();

                    return new { Command = c, Type = commandType };
                }).ToArray();

            var unknownCommandNames = commandsWithType.Where(c => c.Type == null).Select(c => c.Command.CommandName).ToArray();
            if (unknownCommandNames.Length > 0)
                return ValueOrError.CreateError("Unknown command type: " + string.Join(", ", unknownCommandNames) + ".");

            var dataNotSetCommandNames = commands.Where(c => c.Data == null).Select(c => c.CommandName).ToArray();
            if (dataNotSetCommandNames.Length > 0)
                return ValueOrError.CreateError("Command data not set: " + string.Join(", ", dataNotSetCommandNames) + ".");

            var processingCommands = new List<ICommandInfo>();
            foreach (var cmd in commandsWithType)
            {
                try
                {
                    var deserializedData = XmlUtility.DeserializeFromXml(cmd.Type, cmd.Command.Data);
                    if (deserializedData == null)
                        return ValueOrError.CreateError("Deserialization of " + cmd.Command.CommandName + " resulted in null value.");

                    var commandInfo = deserializedData as ICommandInfo;
                    if (commandInfo == null)
                        return ValueOrError.CreateError("Cannot cast " + cmd.Command.CommandName + " to ICommandInfo.");

                    processingCommands.Add(commandInfo);
                }
                catch (Exception ex)
                {
                    return ValueOrError.CreateError("Exception while deserializing " + cmd.Command.CommandName + "." + Environment.NewLine + ex);
                }
            }

            return processingCommands;
        }

        private static ServerProcessingResult ConvertResult(ProcessingResult result)
        {
            return new ServerProcessingResult
            {
                Success = result.Success,
                UserMessage = result.UserMessage,
                SystemMessage = result.SystemMessage,
                ServerCommandResults = result.CommandResults == null ? null :
                  (from c in result.CommandResults
                   select new ServerCommandResult
                   {
                       Message = c.Message,
                       Data = c.Data != null && c.Data.Value != null ? XmlUtility.SerializeToXml(c.Data.Value) : null
                   }).ToArray()
            };
        }
    }
}
