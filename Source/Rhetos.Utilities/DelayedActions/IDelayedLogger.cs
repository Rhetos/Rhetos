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

namespace Rhetos.Utilities
{
    /// <summary>
    /// Utility for writing a warning log message if some operation takes too long.
    /// The message is written asynchronously, after a timeout (configurable), before the long operation is completed.
    /// See usage instructions in <see cref="PerformanceWarning"/> method.
    /// Use <see cref="IDelayedLogProvider"/> from dependency injection, then call <see cref="IDelayedLogProvider.GetLogger"/> to get an instance of <see cref="IDelayedLogger"/>.
    /// </summary>
    public interface IDelayedLogger
    {
        /// <summary>
        /// Starts a separate thread to write the log message if the parent thread operation takes too long.
        /// The message is written asynchronously, after a timeout (configurable), before the long operation is completed.
        /// The logging is canceled if the IDelayedLogger is disposed before the timeout occurred.
        /// Usage:
        /// <code>
        /// using (delayedLogger.PerformanceWarning(() => "Some message."))
        /// {
        ///     ... Some code that might take a long time to execute.
        /// }
        /// </code>
        /// </summary>
        IDisposable PerformanceWarning(Func<string> logMessage);
    }
}