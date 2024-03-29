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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Persistence;
using Rhetos.Processing;
using Rhetos.Processing.DefaultCommands;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CommonConcepts.Test
{
    [TestClass]
    public class UnitOfWorkScopeTest
    {
        [TestMethod]
        public void ExplicitCommitOnDispose()
        {
            var id1 = Guid.NewGuid();

            using (var scope = TestScope.Create())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                context.Repository.TestEntity.BaseEntity.Insert(new TestEntity.BaseEntity { ID = id1, Name = TestNamePrefix + Guid.NewGuid() });
                scope.Resolve<IUnitOfWork>().CommitAndClose();
            }

            using (var scope = TestScope.Create())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                Assert.IsTrue(context.Repository.TestEntity.BaseEntity.Query(new[] { id1 }).Any());
            }
        }

        [TestMethod]
        public void ExplicitCommitAndClose()
        {
            var id1 = Guid.NewGuid();

            using (var scope = TestScope.Create())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                context.Repository.TestEntity.BaseEntity.Insert(new TestEntity.BaseEntity { ID = id1, Name = TestNamePrefix + Guid.NewGuid() });
                scope.CommitAndClose();
            }

            using (var scope = TestScope.Create())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                Assert.IsTrue(context.Repository.TestEntity.BaseEntity.Query(new[] { id1 }).Any());
            }
        }

        [TestMethod]
        public void InterfaceCommitAndClose()
        {
            using (var scope = TestScope.Create())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                context.Repository.TestEntity.BaseEntity.Insert(new TestEntity.BaseEntity { Name = TestNamePrefix + Guid.NewGuid() });

                var unitOfWork = scope.Resolve<IUnitOfWork>();
                unitOfWork.CommitAndClose();

                TestUtility.ShouldFail<FrameworkException>(
                    () => context.Repository.TestEntity.BaseEntity.Insert(new TestEntity.BaseEntity { Name = TestNamePrefix + Guid.NewGuid() }),
                    "Trying to use the Connection property of a disposed persistence transaction.");
            }
        }

        [TestMethod]
        public void RollbackByDefault()
        {
            var id1 = Guid.NewGuid();

            TestUtility.ShouldFail<FrameworkException>(() =>
                {
                    using (var scope = TestScope.Create())
                    {
                        var context = scope.Resolve<Common.ExecutionContext>();
                        context.Repository.TestEntity.BaseEntity.Insert(new TestEntity.BaseEntity { ID = id1, Name = TestNamePrefix + Guid.NewGuid() });
                        throw new FrameworkException(nameof(RollbackByDefault)); // The exception that is not handled within transaction scope.
#pragma warning disable CS0162 // Unreachable code detected
                        scope.Resolve<IUnitOfWork>().CommitAndClose();
#pragma warning restore CS0162 // Unreachable code detected
                    }
                },
                nameof(RollbackByDefault));

            using (var scope = TestScope.Create())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                Assert.IsFalse(context.Repository.TestEntity.BaseEntity.Query(new[] { id1 }).Any());
            }
        }

        /// <summary>
        /// This is not an intended usage of IUnitOfWorkScope because CommitAndClose should be called at the end of the using block.
        /// Any database operation in the unit-of-work scope after CommitAndClose will throw an exception.
        /// </summary>
        [TestMethod]
        public void EarlyCommitAndClose_Repository()
        {
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();

            TestUtility.ShouldFail<FrameworkException>(() =>
                {
                    using (var scope = TestScope.Create())
                    {
                        var context = scope.Resolve<Common.ExecutionContext>();
                        context.Repository.TestEntity.BaseEntity.Insert(new TestEntity.BaseEntity { ID = id1 });
                        scope.CommitAndClose(); // CommitAndClose is incorrectly placed at this position.
                        context.Repository.TestEntity.BaseEntity.Insert(new TestEntity.BaseEntity { ID = id2 });
                    }
                },
                "disposed persistence transaction");

            using (var scope = TestScope.Create())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                // Only operations before CommitAndClose are committed.
                var ids = new[] { id1, id2 };
                Assert.AreEqual(
                    id1.ToString(),
                    TestUtility.DumpSorted(context.Repository.TestEntity.BaseEntity.Load(ids), item => item.ID));
            }
        }

        /// <summary>
        /// This is not an intended usage of IUnitOfWorkScope because CommitAndClose should be called at the end of the using block.
        /// Any database operation in the unit-of-work scope after CommitAndClose will throw an exception.
        /// </summary>
        [TestMethod]
        public void EarlyCommitAndClose_SqlExecuter()
        {
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();

            TestUtility.ShouldFail<FrameworkException>(() =>
            {
                using (var scope = TestScope.Create())
                {
                    var sqlExecuter = scope.Resolve<ISqlExecuter>();
                    sqlExecuter.ExecuteSqlInterpolated($"INSERT INTO TestEntity.BaseEntity (ID) SELECT {id1}");
                    scope.CommitAndClose(); // CommitAndClose is incorrectly placed at this position.
                    sqlExecuter.ExecuteSqlInterpolated($"INSERT INTO TestEntity.BaseEntity (ID) SELECT {id2}");
                }
            },
                "disposed persistence transaction");

            using (var scope = TestScope.Create())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                // Only operations before CommitAndClose are committed.
                var ids = new[] { id1, id2 };
                Assert.AreEqual(
                    id1.ToString(),
                    TestUtility.DumpSorted(context.Repository.TestEntity.BaseEntity.Load(ids), item => item.ID));
            }
        }

        /// <summary>
        /// This is not an intended usage of IUnitOfWorkScope because CommitAndClose should be called at the end of the using block.
        /// Any database operation in the unit-of-work scope after CommitAndClose will throw an exception.
        /// </summary>
        [TestMethod]
        public void EarlyCommitAndClose_ProcessingEngine()
        {
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();

            TestUtility.ShouldFail<FrameworkException>(() =>
            {
                using (var scope = TestScope.Create(b => b.ConfigureIgnoreClaims()))
                {
                    var processingEngine = scope.Resolve<IProcessingEngine>();
                    var logCommand = new ExecuteActionCommandInfo { Action = new Common.AddToLog { Action = "EarlyCommitAndClose", ItemId = id1 } };
                    processingEngine.Execute(logCommand);
                    scope.CommitAndClose(); // CommitAndClose is incorrectly placed at this position.
                    ((Common.AddToLog)logCommand.Action).ItemId = id2;
                    processingEngine.Execute(logCommand);
                }
            },
                "disposed persistence transaction");

            using (var scope = TestScope.Create())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                // Only operations before CommitAndClose are committed.
                var ids = new[] { id1, id2 };
                var items = context.Repository.Common.Log.Query(l => l.Action == "EarlyCommitAndClose" && l.TableName == null);
                Assert.AreEqual(
                    id1.ToString(),
                    TestUtility.DumpSorted(items.Where(log => ids.Contains(log.ItemId.Value)), log => log.ItemId));

                context.Repository.Common.Log.Delete(items);
                scope.CommitAndClose();
            }
        }

        [TestMethod]
        public void CommitAndCloseWithDiscard()
        {
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();

            TestUtility.ShouldFail<FrameworkException>(() =>
            {
                using (var scope = TestScope.Create())
                {
                    var context = scope.Resolve<Common.ExecutionContext>();
                    context.Repository.TestEntity.BaseEntity.Insert(new TestEntity.BaseEntity { ID = id1, Name = TestNamePrefix + Guid.NewGuid() });
                    scope.Resolve<IPersistenceTransaction>().DiscardOnDispose();
                    scope.CommitAndClose();
                    context.Repository.TestEntity.BaseEntity.Insert(new TestEntity.BaseEntity { ID = id2, Name = TestNamePrefix + Guid.NewGuid() });
                }
            },
                "disposed persistence transaction"); // CommitAndClose should close the transaction, even if it was discarded.

            using (var scope = TestScope.Create())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                // CommitAndClose should rollback the transaction.
                var ids = new[] { id1, id2 };
                Assert.AreEqual(
                    "",
                    TestUtility.DumpSorted(context.Repository.TestEntity.BaseEntity.Load(ids), item => item.ID));
            }
        }

        [TestMethod]
        public void DiscardInvalidatesCommit()
        {
            var ids = Enumerable.Range(0, 4).Select(x => Guid.NewGuid()).ToArray();

            using (var scope = TestScope.Create())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                context.Repository.TestEntity.BaseEntity.Insert(new TestEntity.BaseEntity { ID = ids[0], Name = TestNamePrefix + Guid.NewGuid() });
                scope.Resolve<IPersistenceTransaction>().DiscardOnDispose();
                scope.Resolve<IUnitOfWork>().CommitAndClose();
            }

            using (var scope = TestScope.Create())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                context.Repository.TestEntity.BaseEntity.Insert(new TestEntity.BaseEntity { ID = ids[1], Name = TestNamePrefix + Guid.NewGuid() });
                scope.Resolve<IPersistenceTransaction>().DiscardOnDispose();
                scope.CommitAndClose();
            }

            using (var scope = TestScope.Create())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                context.Repository.TestEntity.BaseEntity.Insert(new TestEntity.BaseEntity { ID = ids[2], Name = TestNamePrefix + Guid.NewGuid() });
                scope.Resolve<IPersistenceTransaction>().CommitAndClose(); // This commit is immediate, so discard will have no effect.
                scope.Resolve<IPersistenceTransaction>().DiscardOnDispose();
            }

            using (var scope = TestScope.Create())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                context.Repository.TestEntity.BaseEntity.Insert(new TestEntity.BaseEntity { ID = ids[3], Name = TestNamePrefix + Guid.NewGuid() });
                scope.CommitAndClose(); // This commit is immediate, so discard will have no effect.
                scope.Resolve<IPersistenceTransaction>().DiscardOnDispose();
            }

            using (var scope = TestScope.Create())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                Assert.AreEqual(
                    TestUtility.DumpSorted(new[] { ids[2], ids[3] }),
                    TestUtility.DumpSorted(context.Repository.TestEntity.BaseEntity.Query(ids), item => item.ID));
            }
        }

        [TestMethod]
        public void IndependentTransactions()
        {
            const int threadCount = 2;

            int initialCount;
            using (var scope = TestScope.Create())
            {
                ConcurrencyUtility.CheckForParallelism(scope, threadCount);

                var context = scope.Resolve<Common.ExecutionContext>();
                initialCount = context.Repository.TestEntity.BaseEntity.Query().Count();
            }

            var id1 = Guid.NewGuid();

            Parallel.For(0, threadCount, thread =>
            {
                using (var scope = TestScope.Create())
                {
                    var context = scope.Resolve<Common.ExecutionContext>();

                    Assert.AreEqual(initialCount, context.Repository.TestEntity.BaseEntity.Query().Count());
                    Thread.Sleep(100);
                    context.Repository.TestEntity.BaseEntity.Insert(new TestEntity.BaseEntity { ID = id1, Name = TestNamePrefix + Guid.NewGuid() }); // Each thread uses the same ID to make sure only one thread can run this code at same time.
                    Assert.AreEqual(initialCount + 1, context.Repository.TestEntity.BaseEntity.Query().Count());
                }
            });
        }

        private const string TestNamePrefix = "RhetosUnitOfWorkScopeTest_";

        [ClassCleanup]
        public static void ClassCleanup()
        {
            using (var scope = TestScope.Create())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                var testItems = context.Repository.TestEntity.BaseEntity.Load(item => item.Name.StartsWith(TestNamePrefix));
                context.Repository.TestEntity.BaseEntity.Delete(testItems);
                scope.CommitAndClose();
            }
        }
    }
}
