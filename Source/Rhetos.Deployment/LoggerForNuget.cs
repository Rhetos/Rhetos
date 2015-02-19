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

using Rhetos.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rhetos.Deployment
{
    public class LoggerForNuget : NuGet.ILogger
    {
        ILogger _logger;

        public LoggerForNuget(ILogProvider logProvider)
        {
            _logger = logProvider.GetLogger("NuGet");
        }

        Dictionary<NuGet.MessageLevel, EventType> logLevels = new Dictionary<NuGet.MessageLevel, EventType>
        {
            { NuGet.MessageLevel.Debug, EventType.Trace },
            { NuGet.MessageLevel.Info, EventType.Trace },
            { NuGet.MessageLevel.Warning, EventType.Info },
            { NuGet.MessageLevel.Error, EventType.Error },
        };

        public void Log(NuGet.MessageLevel nugetLevel, string message, params object[] args)
        {
            EventType logLevel;
            if (!logLevels.TryGetValue(nugetLevel, out logLevel))
                logLevel = EventType.Error;

            _logger.Write(logLevel, message, args);
        }

        public NuGet.FileConflictResolution ResolveFileConflict(string message)
        {
            _logger.Error(message);
            return NuGet.FileConflictResolution.OverwriteAll;
        }
    }
}
