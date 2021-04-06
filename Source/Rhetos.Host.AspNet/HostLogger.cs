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

using Microsoft.Extensions.Logging;
using Rhetos.Logging;
using System;

namespace Rhetos.Host.AspNet
{
    public class HostLogProvider : ILogProvider
    {
        private readonly ILoggerProvider _loggerProvider;

        public HostLogProvider(ILoggerProvider loggerProvider)
        {
            _loggerProvider = loggerProvider;
        }
        
        public Logging.ILogger GetLogger(string eventName)
        {
            return new HostLogger(eventName, _loggerProvider.CreateLogger(eventName));
        }
    }

    public class HostLogger : Logging.ILogger
    {
        private readonly Microsoft.Extensions.Logging.ILogger _logger;

        public string Name { get; private set; }

        public HostLogger(string name, Microsoft.Extensions.Logging.ILogger logger)
        {
            Name = name;
            _logger = logger;
        }

        public void Write(EventType eventType, Func<string> logMessage)
        {
            _logger.Log(MapEventTypeToLogevel(eventType), 0, logMessage, null, (state, exception) => state());
        }

        public LogLevel MapEventTypeToLogevel(EventType eventType)
        {
            switch (eventType)
            {
                case EventType.Error:
                    return LogLevel.Error;
                case EventType.Info:
                    return LogLevel.Information;
                case EventType.Warning:
                    return LogLevel.Warning;
                default:
                    return LogLevel.Trace;
            }
        }
    }
}
