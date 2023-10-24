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

namespace Rhetos.Utilities
{
    public class DelayedLogProvider : IDelayedLogProvider
    {
        private readonly TimeSpan _delay;
        private readonly ILogProvider _logProvider;

        public DelayedLogProvider(LoggingOptions loggingOptions, ILogProvider logProvider)
        {
            _delay = TimeSpan.FromSeconds(loggingOptions.DelayedLogTimout);
            _logProvider = loggingOptions.DelayedLogTimout > 0 ? logProvider : null;
        }

        public IDelayedLogger GetLogger(string eventName)
        {
            if (_logProvider != null)
                return new DelayedLogger(_delay, _logProvider, eventName);
            else
                return new NullDelayedLogger();
        }
    }
}
