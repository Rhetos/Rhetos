using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;
using Rhetos.Logging;
using ILogger = Rhetos.Logging.ILogger;

namespace Rhetos.Extensions.NetCore.Logging
{
    // Leverage .NET Core logging infrastructure to create default cross-platform logger which logs to three different targets:
    // Console, Debug and EventLog(Windows only)
    public sealed class RhetosBuilderDefaultLogProvider : ILogProvider, IDisposable
    {
        private readonly Lazy<ILoggerFactory> loggerFactory = new Lazy<ILoggerFactory>(CreateLoggerFactory);
        public ILogger GetLogger(string name)
        {
            return new RhetosNetCoreLogger(loggerFactory.Value.CreateLogger(name), name);
        }

        private static ILoggerFactory CreateLoggerFactory()
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    builder.AddEventLog();
            });
            return loggerFactory;
        }

        public void Dispose()
        {
            if (loggerFactory.IsValueCreated)
                loggerFactory.Value.Dispose();
        }
    }
}
