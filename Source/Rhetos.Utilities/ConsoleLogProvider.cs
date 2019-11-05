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
using Rhetos.Logging;

namespace Rhetos.Utilities
{
    public delegate void LogMonitor(EventType eventType, string eventName, Func<string> message);

    public class ConsoleLogProvider : ILogProvider
    {
        public ConsoleLogProvider()
        {
        }

        public ConsoleLogProvider(LogMonitor logMonitor)
        {
            _logMonitor = logMonitor;
        }

        public ILogger GetLogger(string eventName)
        {
            return new ConsoleLogger(eventName, _logMonitor);
        }

        private readonly LogMonitor _logMonitor;
    }

    public class ConsoleLogger : ILogger
    {
        private readonly LogMonitor _logMonitor;

        public ConsoleLogger(string eventName = null, LogMonitor logMonitor = null)
        {
            Name = eventName;
            _logMonitor = logMonitor;
        }

        public void Write(EventType eventType, Func<string> logMessage)
        {
            string message = null;

            if (eventType >= MinLevel)
            {
                try
                {
                    message = logMessage();
                    Write(eventType, Name, message.Limit(10000, true));
                }
                catch (Exception ex)
                {
                    Write(EventType.Error, GetType().Name, string.Format(
                        "Error while getting the log message ({0}: {1}). {2}",
                        eventType, Name, ex.ToString()));
                }
            }

            _logMonitor?.Invoke(eventType, Name, message != null ? () => message : logMessage); // Ensures only one evaluation of the logMessage function.
        }

        private static void Write(EventType eventType, string eventName, string message)
        {
            Console.WriteLine(
                "[" + eventType + "] "
                + (eventName != null ? (eventName + ": ") : "")
                + message);
        }

        public static EventType MinLevel { get; set; } = 0;

        public string Name { get; }
    }
}
