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

namespace Rhetos.Utilities
{
    /// <summary>
    /// Database connection string for Rhetos components.
    /// </summary>
    public class ConnectionString
    {
        /// <summary>
        /// Default connection string name in application's configuration.
        /// </summary>
        public static readonly string RhetosConnectionStringName = "RhetosConnectionString";

        /// <summary>
        /// Default configuration settings key for the connection string.
        /// </summary>
        /// <remarks>
        /// Note that the connection string could be registered to the application's context in different ways, circumventing the configuration settings.
        /// In application code, do not resolve the connection string directly from the application's configuration using this key,
        /// instead use the <see cref="ConnectionString"/> class from dependency injection.
        /// </remarks>
        public static readonly string ConnectionStringConfigurationKey = "ConnectionStrings:" + RhetosConnectionStringName;

        private readonly string _value;

        public ConnectionString(IConfiguration configuration, ISqlUtility sqlUtility)
        {
            var connectionString = configuration.GetValue<string>(ConnectionStringConfigurationKey);

            var dbOptions = configuration.GetOptions<DatabaseOptions>();
            if (dbOptions.SetApplicationName)
                connectionString = sqlUtility.TrySetApplicationName(connectionString);

            _value = connectionString;
        }

        public ConnectionString(string connectionString)
        {
            _value = connectionString;
        }

        public override string ToString()
        {
            return _value;
        }

        public static implicit operator string(ConnectionString connectionString)
        {
            return connectionString._value;
        }

        public static implicit operator ConnectionString(string connectionString)
        {
            return new ConnectionString(connectionString);
        }
    }
}
