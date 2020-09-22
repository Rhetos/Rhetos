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
    /// <summary>
    /// See <see cref="IDelayedLogger"/> for usage instructions.
    /// </summary>
    public class DelayedLogger : IDelayedLogger
    {
        private readonly TimeSpan _delay;
        private readonly ILogProvider _logProvider;
        private readonly string _eventName;

        public DelayedLogger(TimeSpan delay, ILogProvider logProvider, string eventName)
        {
            _delay = delay;
            _logProvider = logProvider; // No need to resolve the ILogger early, because the delayed log message is rarely written.
            _eventName = eventName;
        }

        public IDisposable TimoutWarning(Func<string> logMessage)
        {
            return new DelayedAction(_delay, () =>
            {
                var startTime = DateTime.Now - _delay; // The current time needs to be resolved immediately, instead of when log entry is processed.
                _logProvider.GetLogger(_eventName).Warning(() => $"Long operation started at {startTime:T}. " + logMessage());
            });
        }
    }
}
