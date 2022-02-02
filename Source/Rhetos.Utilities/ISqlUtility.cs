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

using Rhetos.Persistence;
using System;
using System.Data.Common;

namespace Rhetos.Utilities
{
    /// <remarks>
    /// The main difference between <see cref="ISqlUtility"/> and <see cref="ISqlExecuter"/>
    /// is that <see cref="ISqlUtility"/> implementations do now require <see cref="IPersistenceTransaction"/>
    /// or an active database connection.
    /// <see cref="ISqlUtility"/> can be used at build-time, or to initialize a new database connection.
    /// </remarks>
    public interface ISqlUtility
    {
        /// <summary>
        /// Created database connection and initializes it with used information and settings.
        /// </summary>
        DbConnection CreateConnection(string connectionString, IUserInfo _userInfo);

        /// <summary>
        /// Checks the exception for database errors and attempts to transform it to a RhetosException.
        /// It the function returns null, the original exception should be used.
        /// </summary>
        RhetosException InterpretSqlException(Exception exception);

        /// <summary>
        /// Simplifies ORM exception by detecting the SQL exception that caused it.
        /// </summary>
        Exception ExtractSqlException(Exception exception);
    }
}
