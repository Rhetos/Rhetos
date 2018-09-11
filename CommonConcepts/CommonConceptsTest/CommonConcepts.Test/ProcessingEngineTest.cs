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

using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.TestCommon;
using Rhetos.Configuration.Autofac;
using Rhetos.Utilities;
using Rhetos.Processing;
using Rhetos.Processing.DefaultCommands;
using CommonConcepts.Test.Helpers;

namespace CommonConcepts.Test
{
    [TestClass]
    public class ProcessingEngineTest
    {
        [TestMethod]
        public void LogCommandDescription()
        {
            using (var container = new RhetosTestContainer())
            {
                var log = new List<string>();
                container.AddLogMonitor(log);
                container.AddIgnoreClaims();

                var processingEngine = container.Resolve<IProcessingEngine>();
                var readPrincipals = new ReadCommandInfo
                {
                    DataSource = "Common.Principal",
                    ReadTotalCount = true,
                    Filters = new[] { new FilterCriteria { Filter = "System.Guid[]", Value = new[] { new Guid("546df18b-5df8-4ffa-9b08-8da909efe067") } } }
                };
                var processingEngineResult = processingEngine.Execute(new[] { readPrincipals });
                Assert.IsTrue(processingEngineResult.Success);

                var excected = new ListOfTuples<string, IEnumerable<string>>()
                {
                    { "request info", new[] {
                        "ProcessingEngine Request",
                        "ReadCommandInfo Common.Principal count, filters: System.Guid[] \"1 items: 546df18b-5df8-4ffa-9b08-8da909efe067\"" } },
                    { "command xml", new[] {
                        "ProcessingEngine Commands", "Common.Principal</DataSource>", "true</ReadTotalCount>" } },
                };

                foreach (var test in excected)
                    Assert.IsTrue(log.Any(line => test.Item2.All(pattern => line.Contains(pattern))), "Missing a log entry for test '" + test.Item1 + "'.");

                var notExpected = new[] { "error info", "CommandsWithClientError", "CommandsWithClientError", "CommandsWithServerError", "CommandsWithServerError" };

                foreach (var test in notExpected)
                    Assert.IsFalse(log.Any(line => line.Contains(test)), "Unexpected log entry for test '" + test + "'.");
            }
        }

        [TestMethod]
        public void LogCommandClientErrorDescription()
        {
            using (var container = new RhetosTestContainer())
            {
                var log = new List<string>();
                container.AddLogMonitor(log);
                container.AddIgnoreClaims();

                var processingEngine = container.Resolve<IProcessingEngine>();
                var saveDuplicates = new SaveEntityCommandInfo
                {
                    Entity = "TestUnique.E",
                    DataToInsert = new[]
                    {
                        new TestUnique.E { I = 123, S = "abc" },
                        new TestUnique.E { I = 123, S = "abc" },
                    }
                };
                var processingEngineResult = processingEngine.Execute(new[] { saveDuplicates });
                Assert.IsFalse(processingEngineResult.Success);
                TestUtility.AssertContains(processingEngineResult.UserMessage, "duplicate");

                var excected = new ListOfTuples<string, IEnumerable<string>>()
                {
                    { "request info", new[] { "ProcessingEngine Request", "SaveEntityCommandInfo TestUnique.E, insert 2" } },
                    { "command xml", new[] { "ProcessingEngine Commands", "<DataToInsert", ">abc</S>" } },
                    { "error info", new[] { "Command failed: SaveEntityCommandInfo TestUnique.E, insert 2. Rhetos.UserException", "duplicate", "IX_E_S_I_R", "stack trace" } },
                    { "CommandsWithClientError xml", new[] { "ProcessingEngine CommandsWithClientError", "<DataToInsert", ">abc</S>" } },
                    { "CommandsWithClientError result", new[] { "ProcessingEngine CommandsWithClientError", "<UserMessage>", "It is not allowed" } },
                };

                foreach (var test in excected)
                    Assert.IsTrue(log.Any(line => test.Item2.All(pattern => line.Contains(pattern))), "Missing a log entry for test '" + test.Item1 + "'.");

                var notExpected = new[] { "CommandsWithServerError" };

                foreach (var test in notExpected)
                    Assert.IsFalse(log.Any(line => line.Contains(test)), "Unexpected log entry for test '" + test + "'.");
            }
        }

        [TestMethod]
        public void LogCommandServerErrorDescription()
        {
            using (var container = new RhetosTestContainer())
            {
                var log = new List<string>();
                container.AddLogMonitor(log);
                container.AddIgnoreClaims();

                var readCommandError = new ReadCommandInfo() { DataSource = "TestRowPermissions.ErrorData", ReadRecords = true, Filters = new[] { new FilterCriteria("duplicateSecondItem") } };
                var processingEngine = container.Resolve<IProcessingEngine>();
                var processingEngineResult = processingEngine.Execute(new[] { readCommandError });
                Assert.IsFalse(processingEngineResult.Success);
                TestUtility.AssertContains(processingEngineResult.SystemMessage, new[] { "Internal server error occurred", "IndexOutOfRangeException" });

                var excected = new ListOfTuples<string, IEnumerable<string>>()
                {
                    { "error info", new[] { "Command failed: ReadCommandInfo TestRowPermissions.ErrorData", "Index was outside the bounds of the array", "stack trace" } },
                    { "CommandsWithServerError xml", new[] { "ProcessingEngine CommandsWithServerError", "TestRowPermissions.ErrorData</DataSource>" } },
                    { "CommandsWithServerError result", new[] { "ProcessingEngine CommandsWithServerError", "<SystemMessage>", "Internal server error" } },
                };

                foreach (var test in excected)
                    Assert.IsTrue(log.Any(line => test.Item2.All(pattern => line.Contains(pattern))), "Missing a log entry for test '" + test.Item1 + "'.");

                var notExpected = new[] { "CommandsWithClientError" };

                foreach (var test in notExpected)
                    Assert.IsFalse(log.Any(line => line.Contains(test)), "Unexpected log entry for test '" + test + "'.");
            }
        }

        [TestMethod]
        public void ActionRollbackOnError()
        {
            string user1 = "TestRollbackOnError";
            string user2 = "TestRollbackOnError_x";
            var usernames = new[] { user1, user2 };

            DeleteUsers(user1, user2);
            Assert.AreEqual("", TestUtility.DumpSorted(ReadUsers(usernames), p => p.Name));

            ExectureRollbackOnError(user1);

            TestUtility.ShouldFail<ApplicationException>(
                () => ExectureRollbackOnError(user2),
                "The username should not end with x.");

            Assert.AreEqual(user1, TestUtility.DumpSorted(ReadUsers(usernames), p => p.Name), "Action that inserts 'user2' shouldn't have been committed because of the exception.");
            DeleteUsers(user1, user2);
        }

        private void ExectureRollbackOnError(string username)
        {
            var command = new ExecuteActionCommandInfo
            {
                Action = new TestAction.RollbackOnError { NewUsername = username }
            };
            Exec(command);
        }

        private IPrincipal[] ReadUsers(params string[] usernames)
        {
            var command = new ReadCommandInfo
            {
                DataSource = typeof(Common.Principal).FullName,
                Filters = new[] { new FilterCriteria("Name", "in", usernames) },
                ReadRecords = true
            };
            var result = Exec<ReadCommandResult>(command);
            return (IPrincipal[])result.Records;
        }

        private void DeleteUsers(params string[] usernames)
        {
            var users = ReadUsers(usernames);
            if (users.Length > 0)
            {
                var command = new SaveEntityCommandInfo
                {
                    Entity = typeof(Common.Principal).FullName,
                    DataToDelete = users
                };
                Exec(command);
            }
        }

        private void Exec(ICommandInfo command)
        {
            Exec<object>(command);
        }

        private TResult Exec<TResult>(ICommandInfo command)
        {
            using (var container = new RhetosTestContainer(commitChanges: true))
            {
                container.AddIgnoreClaims();
                var processingEngine = container.Resolve<IProcessingEngine>();
                var result = processingEngine.Execute(new[] { command });
                if (!result.Success)
                    throw new ApplicationException(result.UserMessage + ", " + result.SystemMessage);
                return (TResult)result.CommandResults.Single().Data?.Value;
            }
        }
    }
}
