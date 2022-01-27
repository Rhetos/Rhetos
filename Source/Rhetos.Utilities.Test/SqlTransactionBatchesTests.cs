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
using Rhetos.TestCommon;
using Rhetos.Utilities.Test.Helpers;
using System.Linq;

namespace Rhetos.Utilities.Test
{
    [TestClass()]
    public class SqlTransactionBatchesTests
    {
        [TestMethod()]
        public void JoinScriptsTest()
        {
            // input scripts, max count, max size, expected output scripts
            // "//" is a shortcut for "\r\n"
            var tests = new ListOfTuples<string[], int, int, string[]>
            {
                {  System.Array.Empty<string>(), 10, 1000, System.Array.Empty<string>() },
                {  System.Array.Empty<string>(), 0, 0, System.Array.Empty<string>() },

                {  new[] { "a", "b", "c" }, 10, 1000, new[] { "a//b//c" } },

                {  new[] { "12345", "12345", "12345" }, 3, 1000, new[] { "12345//12345//12345" } },
                {  new[] { "12345", "12345", "12345" }, 2, 1000, new[] { "12345//12345", "12345" } },
                {  new[] { "12345", "12345", "12345" }, 1, 1000, new[] { "12345", "12345", "12345" } },
                {  new[] { "12345", "12345", "12345" }, 0, 1000, new[] { "12345", "12345", "12345" } },

                {  new[] { "12345", "12345", "12345" }, 3, 19, new[] { "12345//12345//12345" } },
                {  new[] { "12345", "12345", "12345" }, 3, 18, new[] { "12345//12345", "12345" } },
                {  new[] { "12345", "12345", "12345" }, 3, 12, new[] { "12345//12345", "12345" } },
                {  new[] { "12345", "12345", "12345" }, 3, 11, new[] { "12345", "12345", "12345" } },
                {  new[] { "12345", "12345", "12345" }, 3, 5, new[] { "12345", "12345", "12345" } },
                {  new[] { "12345", "12345", "12345" }, 3, 1, new[] { "12345", "12345", "12345" } },
                {  new[] { "12345", "12345", "12345" }, 3, 0, new[] { "12345", "12345", "12345" } },
            };

            foreach (var test in tests)
            {
                var options = new SqlTransactionBatchesOptions
                {
                    MaxJoinedScriptCount = test.Item2,
                    MaxJoinedScriptSize = test.Item3,
                };
                var batches = new SqlTransactionBatches(
                    options, new FakeUnitOfWorkFactory(), new PersistenceTransactionOptions(),
                    new TestUserInfo(), new ConsoleLogProvider(),
                    new DelayedLogProvider(new LoggingOptions { DelayedLogTimout = 0 }, new ConsoleLogProvider()));

                var joinedScripts = batches.JoinScripts(test.Item1);

                Assert.AreEqual(
                    TestUtility.Dump(test.Item4),
                    TestUtility.Dump(joinedScripts.Select(s => s.Replace("\r\n", "//"))),
                    $"Test: '{TestUtility.Dump(test.Item1)}' - {test.Item2}, {test.Item3} => {joinedScripts.Count}, {joinedScripts.Sum(s => s.Length)}");
            }
        }
    }
}