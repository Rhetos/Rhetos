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

namespace Rhetos.Persistence
{
    /// <summary>
    /// Represents an atomic "unit of work" pattern where all operations are executed in a single database transaction.
    /// All operations in a single unit of work are either committed together, or all rolled back.
    /// </summary>
    /// <remarks>
    /// See also <see cref="IPersistenceTransaction"/> interface for low-level control of the database transaction.
    /// </remarks>
    public interface IUnitOfWork
    {
        /// <summary>
        /// Commits and closes the database transaction for the current unit of work (lifetime scope).
        /// It is a good practice to put the call as the last statement in the using block.
        /// </summary>
        /// <remarks>
        /// After calling this method, any later database operation in the current scope might result with an error.
        /// The transaction will be rolled back, instead of committed, if <see cref="IPersistenceTransaction.DiscardOnDispose"/> method was called earlier.
        /// </remarks>
        void CommitAndClose();

        /// <summary>
        /// Discards and closes the database transaction for the current unit of work (lifetime scope).
        /// </summary>
        /// <remarks>
        /// After calling this method, any later database operation in the current scope might result with an error.
        /// </remarks>
        void RollbackAndClose();
    }
}