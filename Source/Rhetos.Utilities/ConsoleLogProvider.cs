/*
    Copyright (C) 2013 Omega software d.o.o.

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
    public class ConsoleLogProvider : ILogProvider
    {
        public ILogger GetLogger(string eventName)
        {
            return new ConsoleLogger(eventName);
        }
    }

    public class ConsoleLogger : ILogger
    {
        private readonly string _eventName;
        private readonly ILogger _decoratedLogger;

        public ConsoleLogger(string eventName = null, ILogger decoratedLogger = null)
        {
            _eventName = eventName;
            _decoratedLogger = decoratedLogger;
        }

        public void Write(EventType eventType, Func<string> logMessage)
        {
            Console.WriteLine(
                "[" + eventType + "] "
                + (_eventName != null ? (_eventName + ": ") : "" )
                + logMessage());

            if (_decoratedLogger != null)
                _decoratedLogger.Write(eventType, logMessage);
        }
    }
}
