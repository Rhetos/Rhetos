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

using Rhetos.Utilities;
using System;
using System.Data.Common;

namespace Rhetos.Persistence
{
    /// <summary>
    /// Represents a common transaction that will be used for all operations during a single lifetime scope (for example, in a single web request),
    /// similar to the "unit of work" pattern.
    /// </summary>
    /// <remarks>
    /// Some components (for example components that are executed during database upgrade) do not use <see cref="IPersistenceTransaction"/>,
    /// because they need more specific control over database transactions.
    /// For those use cases see usage of <see cref="DbUpdateOptions.ShortTransactions"/> or <see cref="SqlUtility.NoTransactionTag"/>.
    /// <para>
    /// See also <see cref="IUnitOfWork"/> interface for high-level control of the transaction.
    /// </para>
    /// </remarks>
    public interface IPersistenceTransaction : IDisposable
    {
        /// <summary>
        /// Marks the transaction as valid, to be committed at the end of the lifetime scope (on Dispose).
        /// If <see cref="CommitOnDispose"/> is not called, the transaction will be rolled back by default.
        /// If <see cref="DiscardOnDispose"/> is also called, it will override any earlier or later call to <see cref="CommitOnDispose"/>.
        /// </summary>
        void CommitOnDispose();

        /// <summary>
        /// Marks the transaction as invalid. The transaction remains operational, but it will be <b>rolled back on Dispose</b>.
        /// </summary>
        void DiscardOnDispose();

#pragma warning disable CA1713 // Events should not have 'Before' or 'After' prefix

        /// <summary>
        /// Invoked before the transaction is committed.
        /// </summary>
        /// <remarks>
        /// Use for cleanup code, such as deleting temporary data that may be used until the transaction is closed.
        /// This event will not be invoked if the transaction rollback was executed (see <see cref="CommitOnDispose"/> and <see cref="DiscardOnDispose"/>).
        /// </remarks>
        event Action BeforeClose;

        /// <summary>
        /// Invoked after the transaction is committed.
        /// </summary>
        /// <remarks>
        /// Use for optional features that do not affect validity of the operation executed in this transaction.
        /// If the <see cref="AfterClose"/> event <b>fails</b>, any data modifications from the current transaction will
        /// remain in the database.
        /// This event will not be invoked if the current transaction is rolled back (see <see cref="DiscardOnDispose()"/>).
        /// </remarks>
        event Action AfterClose;

#pragma warning restore CA1713 // Events should not have 'Before' or 'After' prefix

        /// <summary>
        /// When reading this property the database connection will be automatically opened and a transaction started.
        /// Do not close or modify this connection directly.
        /// A single server request will be executed in one transaction. If the server request fails, the transaction will be rolled back.
        /// If you need to execute an SQL query outside of the server request's transaction, create a new database connection using Rhetos.Utilities.SqlUtility.ConnectionString.
        /// </summary>
        DbConnection Connection { get; }

        /// <summary>
        /// Returns null if the <see cref="Connection"/> is not used yet.
        /// Do not close or modify this transaction directly.
        /// See the <see cref="Connection"/> property for more details.
        /// </summary>
        DbTransaction Transaction { get; }
    }
}
