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
using System;
using System.Runtime.InteropServices;

namespace Rhetos.Host.Net.Logging
{
    // Leverage .NET Core logging infrastructure to create default cross-platform logger which logs to three different targets:
    // Console, Debug and EventLog(Windows only)
    public sealed class RhetosBuilderDefaultLogProvider : Rhetos.Logging.ILogProvider, IDisposable
    {
        private readonly Lazy<ILoggerFactory> loggerFactory = new Lazy<ILoggerFactory>(CreateLoggerFactory);

        public Rhetos.Logging.ILogger GetLogger(string eventName)
        {
            return new RhetosNetCoreLogger(loggerFactory.Value.CreateLogger(eventName), eventName);
        }

        private static ILoggerFactory CreateLoggerFactory()
        {
            return LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    builder.AddEventLog();
            });
        }

        public void Dispose()
        {
            if (loggerFactory.IsValueCreated)
                loggerFactory.Value.Dispose();
        }
    }
}
