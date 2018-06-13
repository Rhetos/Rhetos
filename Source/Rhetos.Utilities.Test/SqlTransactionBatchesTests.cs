using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Utilities.Tests
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
                {  new string[] { }, 10, 1000, new string[] { } },
                {  new string[] { }, 0, 0, new string[] { } },

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
                var config = new MockConfiguration
                {
                    { SqlTransactionBatches.MaxJoinedScriptCountConfigKey, test.Item2 },
                    { SqlTransactionBatches.MaxJoinedScriptSizeConfigKey, test.Item3 },
                };
                var batches = new SqlTransactionBatches(null, config, new ConsoleLogProvider());
                var joinedScripts = batches.JoinScripts(test.Item1);

                Assert.AreEqual(
                    TestUtility.Dump(test.Item4),
                    TestUtility.Dump(joinedScripts.Select(s => s.Replace("\r\n", "//"))),
                    $"Test: '{TestUtility.Dump(test.Item1)}' - {test.Item2}, {test.Item3} => {joinedScripts.Count()}, {joinedScripts.Sum(s => s.Length)}");
            }
        }
    }
}