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
using NLog;

namespace Rhetos.Logging
{
    public class NLogger : ILogger
    {
        private readonly Logger Logger;
        private readonly bool _msBuildErrorFormat;

        /// <summary>
        /// This is a subjective estimate of a maximal comfortable-to-read message length in Visual Studio Error List window.
        /// Longer messages will be trimmed, with the full message text displayed in Build Output.
        /// </summary>
        private static readonly int _msBuildMessageLimit = 500;

        public NLogger(string eventName, bool msBuildErrorFormat)
        {
            Logger = LogManager.GetLogger(eventName);
            _msBuildErrorFormat = msBuildErrorFormat;
        }

        public void Write(EventType eventType, Func<string> logMessage)
        {
            LogLevel logLevel;
            switch (eventType)
            {
                case EventType.Trace:
                    logLevel = LogLevel.Trace;
                    break;
                case EventType.Info:
                    logLevel = LogLevel.Info;
                    break;
                case EventType.Warning:
                    logLevel = LogLevel.Warn;
                    break;
                default:
                    logLevel = LogLevel.Error;
                    break;
            }

            if (!_msBuildErrorFormat)
                Logger.Log(logLevel, () => logMessage());
            else if (eventType >= EventType.Warning) // HACK: Specific logger implementation for Rhetos CLI when executed by MSBuild integration. It should be refactored to a separate ILogProvider implementation.
            {
                string msg = logMessage();
                string singleLine = msg.Replace("\r\n", " ").Replace("\n", " ");
                if (singleLine.Length <= _msBuildMessageLimit)
                    Logger.Log(logLevel, singleLine);
                else
                {
                    Logger.Log(logLevel, string.Concat(singleLine.AsSpan(0, _msBuildMessageLimit), " ... (see build output for more info)"));
                    Logger.Info(RemoveAccidentalMsBuildErrorFormat(msg));
                }
            }
            else
                Logger.Log(logLevel, () => RemoveAccidentalMsBuildErrorFormat(logMessage()));
                
        }

        private string RemoveAccidentalMsBuildErrorFormat(string msg)
        {
            // HACK: SqlException can contain a text pattern, which is misinterpreted by MSBuild as a canonical error format.
            return msg.Replace("Error Number:", "ErrorNumber:", StringComparison.Ordinal);
        }

        public string Name => Logger.Name;
    }
}
