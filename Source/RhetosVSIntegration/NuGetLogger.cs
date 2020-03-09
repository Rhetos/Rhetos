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
using NuGet.Common;
using System.Collections.Generic;

namespace Rhetos
{
    internal class NuGetLogger : NuGet.Common.ILogger
    {
        private readonly TaskLoggingHelper _logger;

        public NuGetLogger(TaskLoggingHelper logger)
        {
            _logger = logger;
        }

        private static Dictionary<LogLevel, MessageImportance> _levelMapping = new Dictionary<LogLevel, MessageImportance>
        {
            { LogLevel.Debug, MessageImportance.Low },
            { LogLevel.Verbose, MessageImportance.Low },
            { LogLevel.Information, MessageImportance.Normal },
            { LogLevel.Minimal, MessageImportance.Normal },
            { LogLevel.Warning, MessageImportance.Normal },
            { LogLevel.Error, MessageImportance.High },
        };

        public void Log(LogLevel level, string data)
        {
            if (level == LogLevel.Error)
                _logger.LogError(data);
            else
                _logger.LogMessage(_levelMapping[level], data);
        }

        public void Log(ILogMessage message)
        {
            if (message.Level == LogLevel.Error)
                _logger.LogError(message.ToString());
            else
                _logger.LogMessage(_levelMapping[message.Level], message.ToString());
        }

    public System.Threading.Tasks.Task LogAsync(LogLevel level, string data) => new System.Threading.Tasks.Task(() => Log(level, data));

        public System.Threading.Tasks.Task LogAsync(ILogMessage message) => new System.Threading.Tasks.Task(() => Log(message));

        public void LogDebug(string data) => Log(LogLevel.Debug, data);

        public void LogError(string data) => Log(LogLevel.Error, data);

        public void LogInformation(string data) => Log(LogLevel.Information, data);

        public void LogInformationSummary(string data) => Log(LogLevel.Information, data);

        public void LogMinimal(string data) => Log(LogLevel.Minimal, data);

        public void LogVerbose(string data) => Log(LogLevel.Verbose, data);

        public void LogWarning(string data) => Log(LogLevel.Warning, data);
    }
}