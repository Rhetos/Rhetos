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

using CommonConcepts.Test.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Utilities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CommonConcepts.Test
{
    [TestClass]
    public class RhetosUnitOfWorkScopeTest
    {
        [TestMethod]
        public void ExplicitCommit()
        {
            var id1 = Guid.NewGuid();

            using (var container = RhetosProcessHelper.CreateScope())
            {
                var context = container.Resolve<Common.ExecutionContext>();
                context.Repository.TestEntity.BaseEntity.Insert(new TestEntity.BaseEntity { ID = id1 });
                container.CommitChanges();
            }

            using (var container = RhetosProcessHelper.CreateScope())
            {
                var context = container.Resolve<Common.ExecutionContext>();
                Assert.IsTrue(context.Repository.TestEntity.BaseEntity.Query(new[] { id1 }).Any());
            }
        }

        [TestMethod]
        public void RollbackByDefault()
        {
            var id1 = Guid.NewGuid();

            try
            {
                using (var container = RhetosProcessHelper.CreateScope())
                {
                    var context = container.Resolve<Common.ExecutionContext>();
                    context.Repository.TestEntity.BaseEntity.Insert(new TestEntity.BaseEntity { ID = id1 });
                    throw new FrameworkException(nameof(RollbackByDefault)); // The exception that is not handled within transaction scope.
#pragma warning disable CS0162 // Unreachable code detected
                    container.CommitChanges();
#pragma warning restore CS0162 // Unreachable code detected
                }
            }
            catch (FrameworkException ex)
            {
                Console.WriteLine($"{ex.GetType().Name}: {ex.Message}");
            }

            using (var container = RhetosProcessHelper.CreateScope())
            {
                var context = container.Resolve<Common.ExecutionContext>();
                Assert.IsFalse(context.Repository.TestEntity.BaseEntity.Query(new[] { id1 }).Any());
            }
        }

        /// <summary>
        /// This is not an intended usage of UnitOfWorkScope because an unhandled exception will
        /// incorrectly commit the transaction, but currently the framework allows it.
        /// </summary>
        [TestMethod]
        public void EarlyCommit()
        {
            var id1 = Guid.NewGuid();

            try
            {
                using (var container = RhetosProcessHelper.CreateScope())
                {
                    container.CommitChanges(); // CommitChanges is incorrectly places at this position.
                    var context = container.Resolve<Common.ExecutionContext>();
                    context.Repository.TestEntity.BaseEntity.Insert(new TestEntity.BaseEntity { ID = id1 });
                    throw new FrameworkException(nameof(EarlyCommit)); // The exception is not handled within transaction scope to discard the transaction.
                }
            }
            catch (FrameworkException ex)
            {
                Console.WriteLine($"{ex.GetType().Name}: {ex.Message}");
            }

            using (var container = RhetosProcessHelper.CreateScope())
            {
                var context = container.Resolve<Common.ExecutionContext>();
                Assert.IsTrue(context.Repository.TestEntity.BaseEntity.Query(new[] { id1 }).Any()); // The transaction is committed because of incorrect implementation pattern above.
            }
        }

        [TestMethod]
        public void IndependentTransactions()
        {
            const int threadCount = 2;

            int initialCount;
            using (var container = RhetosProcessHelper.CreateScope())
            {
                RhetosProcessHelper.CheckForParallelism(container.Resolve<ISqlExecuter>(), threadCount);

                var context = container.Resolve<Common.ExecutionContext>();
                initialCount = context.Repository.TestEntity.BaseEntity.Query().Count();
            }

            var id1 = Guid.NewGuid();

            Parallel.For(0, threadCount, thread =>
            {
                using (var container = RhetosProcessHelper.CreateScope())
                {
                    var context = container.Resolve<Common.ExecutionContext>();

                    Assert.AreEqual(initialCount, context.Repository.TestEntity.BaseEntity.Query().Count());
                    Thread.Sleep(100);
                    context.Repository.TestEntity.BaseEntity.Insert(new TestEntity.BaseEntity { ID = id1 }); // Each thread uses the same ID to make sure only one thread can run this code at same time.
                    Assert.AreEqual(initialCount + 1, context.Repository.TestEntity.BaseEntity.Query().Count());
                }
            });
        }
    }
}
