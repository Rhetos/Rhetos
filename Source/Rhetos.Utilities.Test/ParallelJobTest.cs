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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Rhetos.TestCommon;

namespace Rhetos.Utilities.Test
{
    [TestClass]
    public class ParallelJobTest
    {
        [TestMethod]
        public void ThrowOnDuplicateId()
        {
            var job = new ParallelJob(new ConsoleLogProvider())
                .AddTask("a", () => { });

            TestUtility.ShouldFail<InvalidOperationException>(() => job.AddTask("a", () => { }), "has already been added");
        }

        [TestMethod]
        public void SimpleDependency()
        {
            var result = new ConcurrentQueue<string>();

            var job = new ParallelJob(new ConsoleLogProvider())
                .AddTask("a", () =>
                {
                    Task.Delay(50).Wait();
                    result.Enqueue("a");
                })
                .AddTask("b", () => result.Enqueue("b"), new [] {"a"})
                .AddTask("c", () => result.Enqueue("c"));

            job.RunAllTasks();
            Assert.AreEqual("cab", string.Concat(result));
        }

        [TestMethod]
        public void ConfigureConcurrency()
        {
            var result = new ConcurrentQueue<string>();
            var job = new ParallelJob(new ConsoleLogProvider())
                .AddTask("a", () =>
                {
                    Task.Delay(50).Wait();
                    result.Enqueue("a");
                })
                .AddTask("b", () => result.Enqueue("b"))
                .AddTask("c", () => result.Enqueue("c"), new[] { "a" });

            job.RunAllTasks(0);
            Assert.AreEqual("bac", string.Concat(result));

            result = new ConcurrentQueue<string>();
            job.RunAllTasks(1);
            Assert.AreEqual("abc", string.Concat(result));
        }


        [TestMethod]
        public void StopOnException()
        {
            var result = new ConcurrentQueue<string>();

            var job = new ParallelJob(new ConsoleLogProvider())
                .AddTask("a", () =>
                {
                    Task.Delay(50).Wait();
                    result.Enqueue("a");
                })
                .AddTask("b", () =>
                {
                    result.Enqueue("b");
                    throw new InvalidOperationException("Test exception");
                })
                .AddTask("c", () => result.Enqueue("c"), new[] {"a"});


            _ = TestUtility.ShouldFail<InvalidOperationException>(() => job.RunAllTasks(), "Test exception");

            Assert.AreEqual("ab", string.Concat(result.OrderBy(a => a)));
        }

        [TestMethod]
        public void CorrectlyCancels()
        {
            using var lockAStart = new SemaphoreSlim(0, 1);
            using var lockAFinished = new SemaphoreSlim(0, 1);
            using var lockBFinished = new SemaphoreSlim(0, 1);

            var result = new ConcurrentQueue<string>();
            var job = new ParallelJob(new ConsoleLogProvider())
                .AddTask("a", () =>
                {
                    lockAStart.Wait();
                    result.Enqueue("a");
                    lockAFinished.Release();
                })
                .AddTask("b", () =>
                {
                    result.Enqueue("b");
                    lockBFinished.Release();
                })
                .AddTask("c", () => result.Enqueue("c"), new[] { "a" });

            using var cancellationTokenSource = new CancellationTokenSource();
            var task = Task.Run(() => job.RunAllTasks(0, cancellationTokenSource.Token));

            lockBFinished.Wait(); // Wait for "b" to finish.
            cancellationTokenSource.Cancel();
            var e = TestUtility.ShouldFail<AggregateException>(() => task.Wait());
            Assert.IsTrue(e.InnerException is OperationCanceledException);

            // Only "b" should be completed. "a" is waiting for lock, "c" i waiting for "a".
            Assert.AreEqual("b", TestUtility.Dump(result));
            lockAStart.Release(); // Allow "a" to run now.

            // "a" is expected to complete also, since it has been started prior to cancellation.
            lockAFinished.Wait(); // Wait for "a" to finish.
            Thread.Sleep(50); // Wait a little longer, but "c" should still not start, because it had not started prior to cancellation.
            Assert.AreEqual("b, a", TestUtility.Dump(result));
        }

        [TestMethod]
        public void ComplexDependencies()
        {
            var result = new ConcurrentQueue<string>();

            var job = new ParallelJob(new ConsoleLogProvider())
                .AddTask("a", () => result.Enqueue("a"), new[] {"c", "d", "e"})
                .AddTask("b", () => result.Enqueue("b"), new[] {"c", "d", "e"})
                .AddTask("c", () => result.Enqueue("c"), new[] {"d", "e"})
                .AddTask("d", () => result.Enqueue("d"), new[] {"e"})
                .AddTask("e", () => result.Enqueue("e"));

            job.RunAllTasks();
            var sequence = string.Concat(result);
            Assert.IsTrue(sequence == "edcab" || sequence == "edcba");
        }

        [TestMethod]
        public void UnresolvableDependencies()
        {
            var result = new ConcurrentQueue<string>();

            var job = new ParallelJob(new ConsoleLogProvider())
                .AddTask("a", () => result.Enqueue("a"), new[] { "c", "d", "e" })
                .AddTask("b", () => result.Enqueue("b"), new[] { "c", "d", "e" })
                .AddTask("c", () => result.Enqueue("c"), new[] { "d", "e" })
                .AddTask("d", () => result.Enqueue("d"), new[] { "a" })
                .AddTask("e", () => result.Enqueue("e"));

            TestUtility.ShouldFail<InvalidOperationException>(() => job.RunAllTasks(), "Unable to resolve required task dependencies");
            Assert.AreEqual("e", string.Concat(result));
        }

        [TestMethod]
        public void UnresolvableDependenciesLongCycle()
        {
            var result = new ConcurrentQueue<string>();

            var job = new ParallelJob(new ConsoleLogProvider())
                .AddTask("a", () => result.Enqueue("a"), new[] { "b" })
                .AddTask("b", () => result.Enqueue("b"), new[] { "c" })
                .AddTask("c", () => result.Enqueue("c"), new[] { "d" })
                .AddTask("d", () => result.Enqueue("d"), new[] { "a" });

            TestUtility.ShouldFail<InvalidOperationException>(() => job.RunAllTasks(),
                "Unable to resolve required task dependencies",
                "'a'", "'b'", "'c'", "'d'");
            Assert.AreEqual("", string.Concat(result));
        }
    }
}
