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

using Rhetos.Utilities;
using Rhetos.Dom.DefaultConcepts.Persistence;

namespace Rhetos.MsSqlEf6.CommonConcepts
{
    /// <summary>
    /// This connection string is used for global EF6 initialization (for the provider manifest token in <see cref="MetadataWorkspaceFileProvider"/>).
    /// By default, it returns the standard Rhetos connection string, but in some cases it needs to be customized.
    /// <para>
    /// For example, in a multitenant application, at runtime, the connection string may resolved within the user's web request for the specified tenant (scoped), and there might be no global (singleton)
    /// connection string available. In that case, the registration of this class needs to be overridden to allow EF6 initialization to temporarily connect to some given reference database
    /// that has the same technology and version as the tenant's databases. It can be any of the tenant's databases, or some common master database.
    /// </para>
    /// </summary>
    public class Ef6InitializationConnectionString
    {
        public string ConnectionString { get; }

        public Ef6InitializationConnectionString(ConnectionString connectionString)
            : this(connectionString.ToString())
        {
        }

        public Ef6InitializationConnectionString(string connectionString)
        {
            ConnectionString = connectionString;
        }
    }
}
