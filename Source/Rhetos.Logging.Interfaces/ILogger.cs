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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Globalization;

namespace Rhetos.Logging
{
    public enum EventType
    {
        /// <summary>
        /// Very detailed logs, which may include high-volume information such as protocol payloads. This log level is typically only enabled during development.
        /// </summary>
        Trace,
        /// <summary>
        /// Information messages and warnings, which are normally enabled in production environment.
        /// </summary>
        Info,
        /// <summary>
        /// Error messages, which are normally sent to administrator in production environment.
        /// </summary>
        Error
    };

    public interface ILogger
    {
        void Write(EventType eventType, Func<string> logMessage);
        /// <summary>
        /// Returns the logger name that was provided as argument for <see cref="ILogProvider.GetLogger(string)"/>.
        /// </summary>
        string Name { get; }
    }

    public static class LoggerHelper
    {
        public static void Write(this ILogger logger, EventType eventType, string eventData, params object[] eventDataParams)
        {
            if (eventDataParams.Length == 0)
                logger.Write(eventType, () => eventData);
            else
                logger.Write(eventType, () => string.Format(CultureInfo.InvariantCulture, eventData, eventDataParams));
        }

        public static TimeSpan SlowEvent { get; set; } = TimeSpan.FromSeconds(10);

        private static void PerformanceWrite(this ILogger performanceLogger, Stopwatch stopwatch, Func<string> fullMessage)
        {
            if (stopwatch.Elapsed >= SlowEvent)
                performanceLogger.Info(fullMessage);
            else
                performanceLogger.Trace(fullMessage);
            stopwatch.Restart();
        }

        /// <summary>
        /// Logs 'Trace' or 'Info' level, depending on the event duration.
        /// Restarts the stopwatch.
        /// </summary>
        public static void Write(this ILogger performanceLogger, Stopwatch stopwatch, Func<string> message)
        {
            PerformanceWrite(performanceLogger, stopwatch, () => stopwatch.Elapsed + " " + message());
        }

        /// <summary>
        /// Logs 'Trace' or 'Info' level, depending on the event duration.
        /// Restarts the stopwatch.
        /// </summary>
        public static void Write(this ILogger performanceLogger, Stopwatch stopwatch, string message)
        {
            PerformanceWrite(performanceLogger, stopwatch, () => stopwatch.Elapsed + " " + message);
        }

        public static void Error(this ILogger log, string eventData, params object[] eventDataParams)
        {
            log.Write(EventType.Error, eventData, eventDataParams);
        }
        public static void Error(this ILogger log, Func<string> logMessage)
        {
            log.Write(EventType.Error, logMessage);
        }
        public static void Info(this ILogger log, string eventData, params object[] eventDataParams)
        {
            log.Write(EventType.Info, eventData, eventDataParams);
        }
        public static void Info(this ILogger log, Func<string> logMessage)
        {
            log.Write(EventType.Info, logMessage);
        }
        public static void Trace(this ILogger log, string eventData, params object[] eventDataParams)
        {
            log.Write(EventType.Trace, eventData, eventDataParams);
        }
        public static void Trace(this ILogger log, Func<string> logMessage)
        {
            log.Write(EventType.Trace, logMessage);
        }
    }
}