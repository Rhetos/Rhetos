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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos;
using Rhetos.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CommonConcepts.Test.Helpers
{
    /// <summary>
    /// Helper class for unit tests.
    /// </summary>
    public static class RhetosProcessHelper
    {
        /// <summary>
        /// Creates a thread-safe lifetime scope DI container to isolate unit of work in a separate database transaction.
        /// To commit changes to database, call <see cref="UnitOfWorkScope.CommitAndClose"/> at the end of the 'using' block.
        /// </summary>
        public static UnitOfWorkScope CreateScope(Action<ContainerBuilder> registerCustomComponents = null)
        {
            return ProcessContainer.CreateScope(registerCustomComponents);
        }

        /// <summary>
        /// Shared DI container to be reused between tests, to reduce initialization time for each test.
        /// Each test should create a child container with <see cref="CreateScope"/> to start a 'using' block.
        /// </summary>
        public static ProcessContainer ProcessContainer = new ProcessContainer(FindRhetosApplicationFolder());

        /// <summary>
        /// Unit tests can be executed at different disk locations depending on whether they are run at the solution or project level, from Visual Studio or another utility.
        /// Therefore, instead of providing a simple relative path, this method searches for the main application location.
        /// </summary>
        private static string FindRhetosApplicationFolder()
        {
            var folder = new DirectoryInfo(Environment.CurrentDirectory);

            if (IsValidRhetosServerDirectory(folder.FullName))
                return folder.FullName;

            // Unit testing subfolder.
            if (folder.Name == "Out")
                folder = folder.Parent.Parent.Parent;

            // Unit testing at project level, not at solution level. It depends on the way the testing has been started.
            if (folder.Name == "Debug")
                folder = folder.Parent.Parent.Parent.Parent.Parent; // Climbing up CommonConcepts\CommonConceptsTest\CommonConcepts.Test\bin\Debug.

            if (folder.GetDirectories().Any(subDir => subDir.Name == "Source"))
                folder = new DirectoryInfo(Path.Combine(folder.FullName, @".\Source\Rhetos\"));

            // For unit tests, project's source folder name is ".\Source\Rhetos".
            if (folder.Name == "Rhetos" && IsValidRhetosServerDirectory(folder.FullName))
                return folder.FullName;

            throw new FrameworkException($"Cannot locate a valid Rhetos server's folder from '{Environment.CurrentDirectory}'. Unexpected folder '{folder.FullName}'.");
        }

        private static bool IsValidRhetosServerDirectory(string path)
        {
            // Heuristics for recognizing Source\Rhetos project folder.
            return File.Exists(Path.Combine(path, @"Web.config"))
                && File.Exists(Path.Combine(path, @"bin\Rhetos.Utilities.dll"));
        }

        private static int _checkedForParallelismThreadCount = 0;

        public static void CheckForParallelism(ISqlExecuter sqlExecuter, int requiredNumberOfThreads)
        {
            if (_checkedForParallelismThreadCount >= requiredNumberOfThreads)
                return;

            sqlExecuter.ExecuteSql("WAITFOR DELAY '00:00:00.000'"); // Possible cold start.

            var sw = Stopwatch.StartNew();
            var queries = new[] { "WAITFOR DELAY '00:00:00.100'" };
            Parallel.For(0, requiredNumberOfThreads, x => { sqlExecuter.ExecuteSql(queries, false); });
            sw.Stop();

            Console.WriteLine($"CheckForParallelism: {sw.ElapsedMilliseconds} ms.");

            if (sw.ElapsedMilliseconds < 50)
                Assert.Fail($"Delay is unexpectedly short: {sw.ElapsedMilliseconds}");

            if (sw.Elapsed.TotalMilliseconds > 190)
                Assert.Inconclusive($"This test requires {requiredNumberOfThreads} parallel SQL queries. {requiredNumberOfThreads} parallel delays for 100 ms are executed in {sw.ElapsedMilliseconds} ms.");

            _checkedForParallelismThreadCount = requiredNumberOfThreads;
        }
    }
}
