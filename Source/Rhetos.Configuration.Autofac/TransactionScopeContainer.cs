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

using Autofac;
using System;

namespace Rhetos
{
    /// <summary>
    /// Dependency Injection container which scope is the same as the scope of the database transaction, for executing a single unit of work.
    /// Note that the changes in database will be rolled back by default.
    /// To commit changes to database, call <see cref="UnitOfWorkScope.CommitChanges"/> method on this instance, at the end of the 'using' block.
    /// </summary>
    [Obsolete("Use " + nameof(UnitOfWorkScope) + " instead.")]
    public class TransactionScopeContainer : UnitOfWorkScope
    {
        /// <param name="iocContainer">
        /// The Dependency Injection container used to create the transaction scope container.
        /// </param>
        /// <param name="registerCustomComponents">
        /// Register custom components that may override system and plugins services.
        /// This is commonly used by utilities and tests that need to override host application's components or register additional plugins.
        /// <para>
        /// Note that the transaction-scope component registration might not affect singleton components.
        /// Customize the behavior of singleton components in <see cref="ProcessContainer"/> constructor.
        /// </para>
        /// </param>
        [Obsolete("Use " + nameof(UnitOfWorkScope) + " instead.")]
        public TransactionScopeContainer(IContainer iocContainer, Action<ContainerBuilder> registerCustomComponents = null) : base(iocContainer, registerCustomComponents)
        {
        }
    }
}
