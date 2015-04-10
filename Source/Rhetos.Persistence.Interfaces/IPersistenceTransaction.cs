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
using NHibernate;

namespace Rhetos.Persistence
{
    /// <summary>
    /// Implementation similar to the "unit of work" pattern.
    /// </summary>
    public interface IPersistenceTransaction : IDisposable
    {
        /// <summary>
        /// DiscardChanges marks the transaction as invalid. The changes will be descarded (rollback executed) on Dispose.
        /// </summary>
        void DiscardChanges();

        /// <summary>
        /// Use for cleanup code, such as deleting temporary data that may be used until the transaction is closed.
        /// This event will not be invoked if the transaction rollback was executed (see <see cref="DiscardChanges()"/>).
        /// </summary>
        event Action BeforeClose;

        /// <summary>
        /// Drops the database connection and creates a new one to release the database locks.
        /// This method should not be used during regular server run-time because it splits the unit of work
        /// making it impossible to rollback the whole session in case of a need.
        /// </summary>
        void CommitAndReconnect();

        /// <summary>
        /// Clears in-memory cache that is used for lazy loading.
        /// </summary>
        void ClearCache();

        /// <summary>
        /// Clears the item from the in-memory cache that is used for lazy loading.
        /// </summary>
        void ClearCache(object item);
    }
}
