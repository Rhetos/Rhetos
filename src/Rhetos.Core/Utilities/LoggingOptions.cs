﻿/*
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

namespace Rhetos.Utilities
{
    [Options("Rhetos:Logging")]
    public class LoggingOptions
    {
        /// <summary>
        /// Timeout in seconds for warning on long operations, typically used on deployment.
        /// If an operation takes longer than the timeout value, a warning is written to log in a separate thread, while the operation continues.
        /// If the value is 0, the logging is turned off.
        /// For usage see <see cref="IDelayedLogger"/>.
        /// </summary>
        public double DelayedLogTimout { get; set; } = 0;

        /// <summary>
        /// Adjust error output format for Rhetos CLI MSBuild integration.
        /// </summary>
        public bool MsBuildErrorFormat { get; set; }
    }
}
