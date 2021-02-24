using System;
using Microsoft.Extensions.Logging;
using Rhetos.Logging;
using ILogger = Rhetos.Logging.ILogger;

namespace Rhetos.Extensions.NetCore.Logging
{
    public class RhetosNetCoreLogger : ILogger
    {
        public string Name { get; }

        private readonly Microsoft.Extensions.Logging.ILogger netCoreLogger;
        public RhetosNetCoreLogger(Microsoft.Extensions.Logging.ILogger netCoreLogger, string name)
        {
            this.netCoreLogger = netCoreLogger;
            this.Name = name;
        }

        public void Write(EventType eventType, Func<string> logMessage)
        {
            switch (eventType)
            {
                case EventType.Trace:
                    netCoreLogger.LogTrace(logMessage());
                    break;
                case EventType.Info:
                    netCoreLogger.LogInformation(logMessage());
                    break;
                case EventType.Warning:
                    netCoreLogger.LogWarning(logMessage());
                    break;
                case EventType.Error:
                    netCoreLogger.LogError(logMessage());
                    break;
            }
        }
    }
}
