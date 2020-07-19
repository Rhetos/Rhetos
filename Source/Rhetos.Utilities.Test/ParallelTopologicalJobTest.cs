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
    public class ParallelTopologicalJobTest
    {
        [TestMethod]
        public void ThrowOnDuplicateId()
        {
            var job = new ParallelTopologicalJob(new ConsoleLogProvider())
                .AddTask("a", () => { });

            TestUtility.ShouldFail<InvalidOperationException>(() => job.AddTask("a", () => { }), "has already been added");
        }

        [TestMethod]
        public void SimpleDependency()
        {
            var result = new ConcurrentQueue<string>();

            var job = new ParallelTopologicalJob(new ConsoleLogProvider())
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
            var job = new ParallelTopologicalJob(new ConsoleLogProvider())
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

            var job = new ParallelTopologicalJob(new ConsoleLogProvider())
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


            var e = TestUtility.ShouldFail<AggregateException>(() => job.RunAllTasks());
            TestUtility.AssertContains(e.InnerException.Message, "Test exception");

            Assert.AreEqual("ab", string.Concat(result.OrderBy(a => a)));
        }

        [TestMethod]
        public void CorrectlyCancels()
        {
            var result = new ConcurrentQueue<string>();
            var job = new ParallelTopologicalJob(new ConsoleLogProvider())
                .AddTask("a", () =>
                {
                    Task.Delay(100).Wait();
                    result.Enqueue("a");
                })
                .AddTask("b", () => result.Enqueue("b"))
                .AddTask("c", () => result.Enqueue("c"), new[] { "a" });


            var cancellationTokenSource = new CancellationTokenSource();
            var task = Task.Run(() => job.RunAllTasks(0, cancellationTokenSource.Token));
            Task.Delay(50).Wait();
            cancellationTokenSource.Cancel();
            var e = TestUtility.ShouldFail<AggregateException>(() => task.Wait());
            Assert.IsTrue(e.InnerException is OperationCanceledException);

            // only b completes immediately after cancellation
            Assert.AreEqual("b", string.Concat(result));

            Task.Delay(100).Wait();
            // a should complete also, since it has been started prior to cancellation
            Assert.AreEqual("ba", string.Concat(result));
        }

        [TestMethod]
        public void ComplexDependencies()
        {
            var result = new ConcurrentQueue<string>();

            var job = new ParallelTopologicalJob(new ConsoleLogProvider())
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

            var job = new ParallelTopologicalJob(new ConsoleLogProvider())
                .AddTask("a", () => result.Enqueue("a"), new[] { "c", "d", "e" })
                .AddTask("b", () => result.Enqueue("b"), new[] { "c", "d", "e" })
                .AddTask("c", () => result.Enqueue("c"), new[] { "d", "e" })
                .AddTask("d", () => result.Enqueue("d"), new[] { "a" })
                .AddTask("e", () => result.Enqueue("e"));

            TestUtility.ShouldFail<InvalidOperationException>(() => job.RunAllTasks(), "Unable to resolve required task dependencies");
            Assert.AreEqual("e", string.Concat(result));
        }
    }
}
