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
using Rhetos;
using Rhetos.Utilities;

namespace TestAction.Repositories
{
    public partial class OutOfTransaction_Repository
    {
        public static string TestName => "OutOfTransactionTest";

        /// <summary>
        /// Inserts log record with value '2' with ISqlExecuter without transaction.
        /// </summary>
        public void InsertRecordOutOfTransaction(OutOfTransaction parameter)
        {
            // Insert a database log record in the transaction from the current unit of work scope:
            _domRepository.Common.AddToLog.Execute(new Common.AddToLog
            {
                Action = TestName,
                ItemId = parameter.ItemId,
                Description = "1"
            });

            // Create a new unit of work scope *without* transaction:
            var options = new PersistenceTransactionOptions { UseDatabaseTransaction = false };
            using (var scope = _unitOfWorkFactory.CreateScope(builder => builder.RegisterInstance(options)))
            {
                var scopeRepository = scope.Resolve<Common.DomRepository>();

                // Insert a database log record in the new scope, without transaction (it is immediately committed)
                scopeRepository.Common.AddToLog.Execute(new Common.AddToLog
                {
                    Action = TestName,
                    ItemId = parameter.ItemId,
                    Description = "2"
                });

                // Demo: Exception will cause cancellation of both current and manually created scopes,
                // removing value "1" from the database,
                // but the value "2" should remain in database because the second scope did not have transaction.
                throw new FrameworkException(TestName);

                // Commit the unit of work transaction is not needed since transaction is not used.
            }
        }
    }

    public partial class SeparateTransaction_Repository
    {
        public static string TestName => "SeparateTransactionTest";

        public void InsertRecordInSeparateTransaction(SeparateTransaction parameter)
        {
            // Insert a database log record in the transaction from the current unit of work scope:
            _domRepository.Common.AddToLog.Execute(new Common.AddToLog
            {
                Action = TestName,
                ItemId = parameter.ItemId,
                Description = "1"
            });

            // Create a new unit of work scope with a new connection and transaction:
            using (var scope = _unitOfWorkFactory.CreateScope())
            {
                var scopeRepository = scope.Resolve<Common.DomRepository>();

                // Insert a database log record in the new scope transaction with a new database connection:
                scopeRepository.Common.AddToLog.Execute(new Common.AddToLog
                {
                    Action = TestName,
                    ItemId = parameter.ItemId,
                    Description = "2"
                });

                // Testing: Exception would cause cancellation of both current and manually created scopes,
                // rolling back both transactions, so that neither record "1" nor "2" remain in the database.
                if (parameter.ThrowExceptionInInnerScope == true)
                    throw new FrameworkException(TestName);

                // Commit the unit of work transaction, otherwise it will be rolled back by default.
                scope.CommitAndClose();
            }

            // Demo: Exception will cause the current transaction to rollback,
            // removing value "1" from the database,
            // but the value "2" should remain in database since the second scope has already been committed.
            throw new FrameworkException(TestName);
        }
    }
}
