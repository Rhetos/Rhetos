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

using Autofac;
using Rhetos.Configuration.Autofac;
using Rhetos.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonConcepts.Test.Helpers
{
    public class RhetosTestContainerWithLogMonitor : RhetosTestContainer, ILogProvider
    {
        public List<string> Log { get; private set; }

        public RhetosTestContainerWithLogMonitor(bool commitChanges = false)
            : base(commitChanges)
        {
            Log = new List<string>();
            this._initializeSession += OverrideRegistrations;
        }

        void OverrideRegistrations(ContainerBuilder builder)
        {
            builder.RegisterInstance(this).As<ILogProvider>().ExternallyOwned();
        }

        public ILogger GetLogger(string eventName)
        {
            return new LogMonitor(eventName, Log);
        }

        private class LogMonitor : ILogger
        {
            string _eventName;
            List<string> _log;

            public LogMonitor(string eventName, List<string> log)
            {
                _eventName = eventName;
                _log = log;
            }

            public void Write(EventType eventType, Func<string> logMessage)
            {
                string logEntry = "[" + eventType + "] " + _eventName + ": " + logMessage();
                Console.WriteLine(logEntry);
                _log.Add(logEntry);
            }
        }
    }
}
