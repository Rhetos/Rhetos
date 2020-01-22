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

using NuGet.Common;
using Rhetos.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rhetos
{
    internal class NuGetLogger : NuGet.Common.ILogger
    {
        private readonly Logging.ILogger _rhetosLogger;

        public NuGetLogger(ILogProvider logProvider)
        {
            _rhetosLogger = logProvider.GetLogger("NuGet");
        }

        private static Dictionary<LogLevel, EventType> _levelMapping = new Dictionary<LogLevel, EventType>
        {
            { LogLevel.Debug, EventType.Trace },
            { LogLevel.Verbose, EventType.Trace },
            { LogLevel.Information, EventType.Info },
            { LogLevel.Minimal, EventType.Info },
            { LogLevel.Warning, EventType.Info },
            { LogLevel.Error, EventType.Error },
        };

        public void Log(LogLevel level, string data) => _rhetosLogger.Write(_levelMapping[level], () => data);

        public void Log(ILogMessage message) => _rhetosLogger.Write(_levelMapping[message.Level], () => message.ToString());

        public Task LogAsync(LogLevel level, string data) => new Task(() => Log(level, data));

        public Task LogAsync(ILogMessage message) => new Task(() => Log(message));

        public void LogDebug(string data) => Log(LogLevel.Debug, data);

        public void LogError(string data) => Log(LogLevel.Error, data);

        public void LogInformation(string data) => Log(LogLevel.Information, data);

        public void LogInformationSummary(string data) => Log(LogLevel.Information, data);

        public void LogMinimal(string data) => Log(LogLevel.Minimal, data);

        public void LogVerbose(string data) => Log(LogLevel.Verbose, data);

        public void LogWarning(string data) => Log(LogLevel.Warning, data);
    }
}