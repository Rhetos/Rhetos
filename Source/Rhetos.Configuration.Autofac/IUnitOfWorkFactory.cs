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
    /// Helper for manual scope control.
    /// It is intended for components that need to manually execute a separate unit of work, unrelated to the current operation's unit of work
    /// (for example, unrelated to the current web request).
    /// </summary>
    public interface IUnitOfWorkFactory
    {
        /// <summary>
        /// Creates a thread-safe lifetime scope for dependency injection container,
        /// in order to isolate a unit of work in a separate database transaction.
        /// To commit changes to database, call <see cref="UnitOfWorkScope.CommitAndClose"/> at the end of the 'using' block.
        /// Note that created lifetime scope is <see cref="IDisposable"/>.
        /// Transaction will be committed or rolled back when scope is disposed.
        /// </summary>
        /// <param name="registerScopeComponentsAction">
        /// Register custom components that may override system and plugins services.
        /// This is commonly used by utilities and tests that need to override host application's components or register additional plugins.
        /// <para>
        /// Note that the transaction-scope component registration might not affect singleton components.
        /// Customize the behavior of singleton components with <see cref="IRhetosHostBuilder"/> methods,
        /// before building the <see cref="RhetosHost"/> instance with <see cref="IRhetosHostBuilder.Build"/>.
        /// </para>
        /// </param>
        UnitOfWorkScope CreateScope(Action<ContainerBuilder> registerScopeComponentsAction = null);
    }
}