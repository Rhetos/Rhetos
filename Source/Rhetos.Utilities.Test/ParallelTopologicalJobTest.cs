using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rhetos.Utilities.Test
{
    [TestClass]
    public class ParallelTopologicalJobTest
    {
        [TestMethod]
        public void TestTrivial()
        {
            var job = new ParallelTopologicalJob(new ConsoleLogProvider())
                .AddTask("1", () =>
                {
                    Task.Delay(100).Wait();
                    Console.WriteLine("o1");
                }, Enumerable.Empty<string>())
                .AddTask("2", () => Console.WriteLine("o2"), Enumerable.Empty<string>())
                .AddTask("3", () => Console.WriteLine("o3"), new[] {"1"})
                .AddTask("4", () => Console.WriteLine("o4"), new[] {"4"})
                .AddTask("5", () => Console.WriteLine("o5"), new[] {"4"});


            job.RunAllTasks();
        }

        [TestMethod]
        public void TestTrivial2()
        {
            var job = new ParallelTopologicalJob(new ConsoleLogProvider())
                .AddTask("1", () =>
                {
                    Task.Delay(100).Wait();
                    Console.WriteLine("o1");
                }, Enumerable.Empty<string>())
                .AddTask("2", () =>
                {
                    Console.WriteLine("o2");
                    throw new InvalidOperationException("ble");
                }, Enumerable.Empty<string>())
                .AddTask("3", () => Console.WriteLine("o3"), new[] {"1"});


            job.RunAllTasks();
        }

        [TestMethod]
        public void TestTrivial3()
        {
            var job = new ParallelTopologicalJob(new ConsoleLogProvider())
                .AddTask("1", () =>
                {
                    Task.Delay(100).Wait();
                    Console.WriteLine("o1");
                }, Enumerable.Empty<string>())
                .AddTask("2", () =>
                {
                    Console.WriteLine("o2");
                    throw new InvalidOperationException("ble");
                }, Enumerable.Empty<string>())
                .AddTask("3", () => Console.WriteLine("o3"), new[] { "1" });


            var cancellationTokenSource = new CancellationTokenSource();
            var task = Task.Run(() => job.RunAllTasks(0, cancellationTokenSource.Token));
            Task.Delay(50).Wait();
            cancellationTokenSource.Cancel();
            task.Wait();
            Console.WriteLine(task.Exception);
        }

        [TestMethod]
        public void TestTrivial4()
        {
            var job = new ParallelTopologicalJob(new ConsoleLogProvider())
                .AddTask("1", () =>
                {
                    Task.Delay(100).Wait();
                    Console.WriteLine("o1");
                }, Enumerable.Empty<string>())
                .AddTask("2", () =>
                {
                    Console.WriteLine("o2");
                }, Enumerable.Empty<string>())
                .AddTask("3", () => Console.WriteLine("o3"), new[] { "1" });

            job.RunAllTasks(2);
        }

    }

}
