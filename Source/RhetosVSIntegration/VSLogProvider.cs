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

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;

namespace RhetosVSIntegration
{
    public class VSLogProvider : Rhetos.Logging.ILogProvider
    {
        private readonly TaskLoggingHelper _logger;

        public VSLogProvider(TaskLoggingHelper logger)
        {
            _logger = logger;
        }

        public Rhetos.Logging.ILogger GetLogger(string eventName)
        {
            return new VSLogger(_logger, eventName);
        }
    }

    public class VSLogger : Rhetos.Logging.ILogger
    {
        private readonly TaskLoggingHelper _logger;

        public VSLogger(TaskLoggingHelper logger, string eventName = null)
        {
            Name = eventName;
            _logger = logger;
        }

        public void Write(EventType eventType, Func<string> logMessage)
        {
            try
            {
                var message = logMessage();
                Write(eventType, Name, message.Limit(10000, true));
            }
            catch (Exception ex)
            {
                Write(EventType.Error, GetType().Name, $"Error while getting the log message ({eventType}: {Name}). {ex}");
            }
        }

        private void Write(EventType eventType, string eventName, string message)
        {
            var fullMessage = (eventName != null ? (eventName + ": ") : "") + message;

            switch (eventType)
            {
                case EventType.Trace:
                    _logger.LogMessage(MessageImportance.Low, fullMessage);
                    break;
                case EventType.Info:
                    _logger.LogMessage(MessageImportance.Normal, fullMessage);
                    break;
                case EventType.Warning:
                    _logger.LogWarning(fullMessage);
                    break;
                default:
                    _logger.LogError(fullMessage);
                    break;
            }
        }

        public string Name { get; }
    }
}
