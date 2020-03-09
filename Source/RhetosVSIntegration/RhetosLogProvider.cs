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
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Rhetos.Logging;
using Rhetos.Utilities;

namespace RhetosVSIntegration
{
    public class RhetosLogProvider : Rhetos.Logging.ILogProvider
    {
        private readonly TaskLoggingHelper _logger;

        public RhetosLogProvider(TaskLoggingHelper logger)
        {
            _logger = logger;
        }

        public Rhetos.Logging.ILogger GetLogger(string eventName)
        {
            return new RhetosLogger(_logger, eventName);
        }
    }

    public class RhetosLogger : Rhetos.Logging.ILogger
    {
        private static Dictionary<EventType, MessageImportance> _levelMapping = new Dictionary<EventType, MessageImportance>
        {
            { EventType.Trace, MessageImportance.Low },
            { EventType.Info, MessageImportance.Normal },
            { EventType.Error, MessageImportance.High }
        };

        private readonly TaskLoggingHelper _logger;

        public RhetosLogger(TaskLoggingHelper logger, string eventName = null)
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
                Write(EventType.Error, GetType().Name, string.Format(
                    "Error while getting the log message ({0}: {1}). {2}",
                    eventType, Name, ex.ToString()));
            }
        }

        private void Write(EventType eventType, string eventName, string message)
        {
            var fullMessage = "[" + eventType + "] "
                + (eventName != null ? (eventName + ": ") : "")
                + message;

            if (eventType == EventType.Error)
                _logger.LogError(fullMessage);
            else
                _logger.LogMessage(_levelMapping[eventType], fullMessage);
        }

        public string Name { get; }
    }
}
