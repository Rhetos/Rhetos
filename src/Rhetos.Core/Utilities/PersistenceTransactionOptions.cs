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

namespace Rhetos.Utilities
{
    [Options("Rhetos:PersistenceTransaction")]
    public class PersistenceTransactionOptions
    {
        /// <summary>
        /// <para>
        /// If true (default), a database transaction is automatically created for each unit-of-work scope (a web request, for example).
        /// It is shared between components within that scope, such as ISqlExecuter and EntityFrameworkContext (DbContext).
        /// </para>
        /// <para>
        /// If false, no database transaction will be automatically created for the unit-of-work scope.
        /// ISqlExecuter will execute database command without transaction.
        /// </para>
        /// </summary>
        /// <remarks>
        /// This option is intended for configuring transaction usage in an new scope created by <see cref="IUnitOfWorkFactory"/>.
        /// Since the <see cref="PersistenceTransactionOptions"/> is singleton, make sure not to modify the global
        /// configuration unintentionally.
        /// </remarks>
        public bool UseDatabaseTransaction { get; set; } = true;
    }
}
