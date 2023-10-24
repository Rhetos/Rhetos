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

using System.Data.Common;

namespace Rhetos.Utilities
{
    /// <summary>
    /// Runtime options, to be used when connecting to a database.
    /// </summary>
    [Options("Rhetos:Database")]
    public class DatabaseOptions
    {
        /// <summary>
        /// The time to wait (in seconds) for each SQL command to execute, <see cref="DbCommand.CommandTimeout"/>.
        /// After the timeout, the command will be terminated and an error returned.
        /// </summary>
        public int SqlCommandTimeout { get; set; } = 30;

        /// <summary>
        /// If the ApplicationName is not set in the provided connection string, it will be automatically set by Rhetos to the current host application name.
        /// </summary>
        public bool SetApplicationName { get; set; } = true;
    }
}
