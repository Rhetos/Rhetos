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

        public NLogger(string eventName)
        {
            Logger = LogManager.GetLogger(eventName);
        }

        public void Write(EventType eventType, Func<string> logMessage)
        {
            LogMessageGenerator logMessageGenerator = () => logMessage();

            if (eventType == EventType.Info)
                Logger.Info(logMessageGenerator);
            else if (eventType == EventType.Error)
                Logger.Error(logMessageGenerator);
            else
                Logger.Trace(logMessageGenerator);
        }

        public string Name => Logger.Name;
    }
}
