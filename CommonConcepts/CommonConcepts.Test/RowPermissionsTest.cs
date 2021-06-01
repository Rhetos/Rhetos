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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CommonConcepts.Test.Helpers;
using Rhetos.TestCommon;
using Rhetos.Configuration.Autofac;
using Rhetos.Utilities;
using Rhetos.Processing;
using System.Collections.Generic;
using Rhetos.Processing.DefaultCommands;
using Rhetos.Dom.DefaultConcepts;
using System.Linq;
using TestRowPermissions;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos;
using Autofac.Features.Indexed;
using Rhetos.Extensibility;
using System.Diagnostics;

namespace CommonConcepts.Test
{
    [TestClass]
    public class RowPermissionsTest
    {
        static string _readException = "You are not authorized to access";
        static string _writeException = "You are not authorized to write";
        static string _rowPermissionsReadFilter = "Common.RowPermissionsReadItems";
        static string _rowPermissionsWriteFilter = "Common.RowPermissionsWriteItems";

        private static ReadCommandResult ExecuteReadCommand(ReadCommandInfo commandInfo, UnitOfWorkScope scope)
        {
            var commands = scope.Resolve<IIndex<Type, IEnumerable<ICommandImplementation>>>();
            var readCommand = (ReadCommand)commands[typeof(ReadCommandInfo)].Single();
            return (ReadCommandResult)readCommand.Execute(commandInfo).Data.Value;
        }

        /// <summary>
        /// Slightly redundant, but we still want to check if absence of RowPermissions is properly detected
        /// </summary>
        [TestMethod]
        public void TestReadNoRowPermissions()
        {
            using (var scope = TestScope.Create())
            {
                var gRepository = scope.Resolve<GenericRepository<NoRP>>();
                gRepository.Save(Enumerable.Range(0, 50).Select(a => new NoRP() { value = a }), null, gRepository.Load());

                {
                    var all = new ReadCommandInfo()
                    {
                        DataSource = "TestRowPermissions.NoRP",
                        ReadRecords = true
                    };
                    var result = ExecuteReadCommand(all, scope);
                    Assert.AreEqual(50, result.Records.Length);
                }

                {
                    var filtered = new ReadCommandInfo()
                    {
                        DataSource = "TestRowPermissions.NoRP",
                        ReadRecords = true,
                        Filters = new FilterCriteria[] { new FilterCriteria() { Filter = "TestRowPermissions.Value30" } }
                    };
                    var result = ExecuteReadCommand(filtered, scope);
                    Assert.AreEqual(19, result.Records.Length);
                }

                {
                    var guid = Guid.NewGuid();
                    gRepository.Save(new NoRP[] { new NoRP() { ID = guid, value = 51 } }, null, null);

                    var single = new ReadCommandInfo()
                    {
                        DataSource = "TestRowPermissions.NoRP",
                        ReadRecords = true,
                        Filters = new FilterCriteria[] { new FilterCriteria() { Property = "ID", Operation = "equal", Value = guid } }
                    };
                    var result = ExecuteReadCommand(single, scope);
                    Assert.AreEqual(1, result.Records.Length);
                    Assert.AreEqual(51, (result.Records[0] as NoRP).value);
                }
            }
        }

        /// <summary>
        /// Tests simple case, but with >2000 records, testing batch functionality of RowPermission mechanism
        /// </summary>
        [TestMethod]
        public void TestReadSimpleManyRecords()
        {
            CancelTestOnSlowServer();

            using (var scope = TestScope.Create())
            {
                var gRepository = scope.Resolve<GenericRepository<SimpleRP>>();
                var items = Enumerable.Range(0, 4001).Select(a => new SimpleRP() { ID = Guid.NewGuid(), value = a }).ToList();
                gRepository.Save(items, null, gRepository.Load());

                {
                    var cReadAll = new ReadCommandInfo()
                    {
                        DataSource = "TestRowPermissions.SimpleRP",
                        ReadRecords = true,
                    };
                    TestUtility.ShouldFail(() => ExecuteReadCommand(cReadAll, scope), _readException);
                }

                {
                    var cReadAll = new ReadCommandInfo()
                    {
                        DataSource = "TestRowPermissions.SimpleRP",
                        ReadRecords = true,
                        Filters = Array.Empty<FilterCriteria>()
                    };
                    TestUtility.ShouldFail(() => ExecuteReadCommand(cReadAll, scope), _readException);
                }

                {
                    var cReadCountOnly = new ReadCommandInfo()
                    {
                        DataSource = "TestRowPermissions.SimpleRP",
                        ReadTotalCount = true,
                    };
                    var result = ExecuteReadCommand(cReadCountOnly, scope);
                    Assert.AreEqual(4001, result.TotalCount);
                }


                var orderByValue = new OrderByProperty[] { new OrderByProperty() { Property = "value", Descending = false } };

                {
                    var cRead1500_2500 = new ReadCommandInfo()
                    {
                        DataSource = "TestRowPermissions.SimpleRP",
                        ReadRecords = true,
                        Skip = 1500,
                        Top = 1001,
                        OrderByProperties = orderByValue,
                    };
                    var result = ExecuteReadCommand(cRead1500_2500, scope);
                    Assert.AreEqual(1001, result.Records.Length);
                }

                {
                    var cRead1501_2501 = new ReadCommandInfo()
                    {
                        DataSource = "TestRowPermissions.SimpleRP",
                        ReadRecords = true,
                        Skip = 1501,
                        Top = 1001,
                        OrderByProperties = orderByValue,
                    };
                    TestUtility.ShouldFail(() => ExecuteReadCommand(cRead1501_2501, scope), _readException);
                }

                {
                    var cRead1499_2499 = new ReadCommandInfo()
                    {
                        DataSource = "TestRowPermissions.SimpleRP",
                        ReadRecords = true,
                        Skip = 1499,
                        Top = 1001,
                        OrderByProperties = orderByValue,
                    };
                    TestUtility.ShouldFail(() => ExecuteReadCommand(cRead1499_2499, scope), _readException);
                }

                {
                    var cRead4000 = new ReadCommandInfo()
                    {
                        DataSource = "TestRowPermissions.SimpleRP",
                        ReadRecords = true,
                        Skip = 4000,
                        Top = 1,
                        OrderByProperties = orderByValue,
                    };
                    TestUtility.ShouldFail(() => ExecuteReadCommand(cRead4000, scope), _readException);
                }

                {
                    var cReadFilterFail = new ReadCommandInfo()
                    {
                        DataSource = "TestRowPermissions.SimpleRP",
                        ReadRecords = true,
                        Filters = new FilterCriteria[] { new FilterCriteria() { Property = "value", Operation = "less", Value = 2001 } }
                    };
                    TestUtility.ShouldFail(() => ExecuteReadCommand(cReadFilterFail, scope), _readException);
                }

                {
                    var cReadSingleFail = new ReadCommandInfo()
                    {
                        DataSource = "TestRowPermissions.SimpleRP",
                        ReadRecords = true,
                        Filters = new FilterCriteria[] { new FilterCriteria() { Property = "ID", Operation = "equal", Value = items[2501].ID } }
                    };
                    TestUtility.ShouldFail(() => ExecuteReadCommand(cReadSingleFail, scope), _readException);
                }

                {
                    var cReadSingleOk = new ReadCommandInfo()
                    {
                        DataSource = "TestRowPermissions.SimpleRP",
                        ReadRecords = true,
                        Filters = new FilterCriteria[] { new FilterCriteria() { Property = "ID", Operation = "equal", Value = items[2500].ID } }
                    };
                    var result = ExecuteReadCommand(cReadSingleOk, scope);
                    Assert.AreEqual(1, result.Records.Length);
                }

                {
                    var cReadFilterOk = new ReadCommandInfo()
                    {
                        DataSource = "TestRowPermissions.SimpleRP",
                        ReadRecords = true,
                        Filters = new FilterCriteria[]
                            {
                                new FilterCriteria() { Property = "value", Operation = "greater", Value = 2499 },
                                new FilterCriteria() { Property = "value", Operation = "less", Value = 2501 }
                            }
                    };
                    var result = ExecuteReadCommand(cReadFilterOk, scope);
                    Assert.AreEqual(1, result.Records.Length);
                    Assert.AreEqual(items[2500].ID, (result.Records[0] as SimpleRP).ID);
                }

                {
                    var cPermissionFilter = new ReadCommandInfo()
                    {
                        DataSource = "TestRowPermissions.SimpleRP",
                        ReadRecords = true,
                        Filters = new FilterCriteria[] { new FilterCriteria()
                            { Filter = _rowPermissionsReadFilter } }
                    };
                    var result = ExecuteReadCommand(cPermissionFilter, scope);
                    Assert.AreEqual(1001, result.Records.Length);
                    var values = ((SimpleRP[])result.Records).Select(a => a.value);
                    Assert.IsTrue(Enumerable.Range(1500, 1001).All(a => values.Contains(a)));
                }
            }
        }

        [TestMethod]
        public void TestReadComplexWithContext()
        {
            using (var scope = TestScope.Create())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                var currentUserName = context.UserInfo.UserName;
                var permRepository = scope.Resolve<GenericRepository<TestRowPermissions.ComplexRPPermissions>>();

                ComplexRPPermissions[] perms = new ComplexRPPermissions[]
                {
                    new ComplexRPPermissions() { userName = "__non_existant_user__", minVal = 17, maxVal = 50 },
                    new ComplexRPPermissions() { userName = currentUserName, minVal = 5, maxVal = 90 },
                    new ComplexRPPermissions() { userName = "__non_existant_user2__", minVal = 9, maxVal = 1 },
                };
                permRepository.Save(perms, null, permRepository.Load());

                var gRepository = scope.Resolve<GenericRepository<TestRowPermissions.ComplexRP>>();
                var items = Enumerable.Range(0, 101).Select(a => new ComplexRP() { ID = Guid.NewGuid(), value = a }).ToList();
                gRepository.Save(items, null, gRepository.Load());

                // first test results with explicit RP filter calls
                {
                    var cAllowed = new ReadCommandInfo()
                    {
                        DataSource = "TestRowPermissions.ComplexRP",
                        ReadRecords = true,
                        Filters = new FilterCriteria[] { new FilterCriteria() { Filter = _rowPermissionsReadFilter } }
                    };
                    var result = ExecuteReadCommand(cAllowed, scope);
                    var values = ((ComplexRP[])result.Records).Select(a => a.value);
                    Assert.IsTrue(Enumerable.Range(5, 86).All(a => values.Contains(a)));
                }

                // add item filter
                {
                    var cAllowedFilter = new ReadCommandInfo()
                    {
                        DataSource = "TestRowPermissions.ComplexRP",
                        ReadRecords = true,
                        Filters = new FilterCriteria[]
                        {
                            new FilterCriteria() { Filter = _rowPermissionsReadFilter },
                            new FilterCriteria() { Filter = "TestRowPermissions.Value10" }
                        }
                    };
                    var result = ExecuteReadCommand(cAllowedFilter, scope);
                    var values = ((ComplexRP[])result.Records).Select(a => a.value);
                    Assert.IsTrue(Enumerable.Range(11, 80).All(a => values.Contains(a)));
                }

                // try invalid range
                {
                    var cInvalidRange = new ReadCommandInfo()
                    {
                        DataSource = "TestRowPermissions.ComplexRP",
                        ReadRecords = true,
                        Filters = new FilterCriteria[]
                        {
                            new FilterCriteria() { Property = "value", Operation = "greater", Value = 50 },
                        }
                    };

                    TestUtility.ShouldFail(() => ExecuteReadCommand(cInvalidRange, scope), _readException);
                }

                {
                    var cInvalidRange2 = new ReadCommandInfo()
                    {
                        DataSource = "TestRowPermissions.ComplexRP",
                        ReadRecords = true,
                        Filters = new FilterCriteria[]
                        {
                            new FilterCriteria() { Property = "value", Operation = "less", Value = 2 },
                        }
                    };
                    TestUtility.ShouldFail(() => ExecuteReadCommand(cInvalidRange2, scope), _readException);
                }

                {
                    var cValidRange = new ReadCommandInfo()
                    {
                        DataSource = "TestRowPermissions.ComplexRP",
                        ReadRecords = true,
                        Filters = new FilterCriteria[]
                        {
                            new FilterCriteria() { Property = "value", Operation = "less", Value = 60 },
                            new FilterCriteria() { Property = "value", Operation = "greater", Value = 50 },                        }
                    };
                    var result = ExecuteReadCommand(cValidRange, scope);
                    Assert.AreEqual(9, result.Records.Length);
                }

                {
                    var cNoRecords = new ReadCommandInfo()
                    {
                        DataSource = "TestRowPermissions.ComplexRP",
                        ReadRecords = true,
                        Filters = new FilterCriteria[]
                        {
                            new FilterCriteria() { Property = "value", Operation = "greater", Value = 200 },
                        }
                    };
                    var result = ExecuteReadCommand(cNoRecords, scope);
                    Assert.AreEqual(0, result.Records.Length);
                }

                {
                    var cTotalCount = new ReadCommandInfo()
                    {
                        DataSource = "TestRowPermissions.ComplexRP",
                        ReadTotalCount = true,
                    };
                    var result = ExecuteReadCommand(cTotalCount, scope);
                    Assert.AreEqual(101, result.TotalCount);
                }

                {
                    var cSingleOk = new ReadCommandInfo()
                    {
                        DataSource = "TestRowPermissions.ComplexRP",
                        ReadRecords = true,
                        Filters = new FilterCriteria[]
                        {
                            new FilterCriteria() { Property = "ID", Operation = "equal", Value = items[90].ID },
                        }
                    };
                    var result = ExecuteReadCommand(cSingleOk, scope);
                    Assert.AreEqual(1, result.Records.Length);
                }

                {
                    var cSingleFail = new ReadCommandInfo()
                    {
                        DataSource = "TestRowPermissions.ComplexRP",
                        ReadRecords = true,
                        Filters = new FilterCriteria[]
                        {
                            new FilterCriteria() { Property = "ID", Operation = "equal", Value = items[91].ID },
                        }
                    };
                    TestUtility.ShouldFail(() => ExecuteReadCommand(cSingleFail, scope), _readException);
                }
            }
        }

        [TestMethod]
        public void Browse()
        {
            using (var scope = TestScope.Create())
            {
                var gr = scope.Resolve<GenericRepository<SimpleRP>>();

                var items = new[] { 1000, 2000 }.Select(a => new SimpleRP() { ID = Guid.NewGuid(), value = a }).ToList();
                gr.Save(items, null, gr.Load());

                {
                    var cReadAll = new ReadCommandInfo()
                    {
                        DataSource = "TestRowPermissions.SimpleRPBrowse",
                        ReadRecords = true,
                    };
                    TestUtility.ShouldFail(() => ExecuteReadCommand(cReadAll, scope), _readException);
                }
                {
                    var cReadAll = new ReadCommandInfo()
                    {
                        DataSource = "TestRowPermissions.SimpleRPBrowse",
                        ReadRecords = true,
                        Filters = new[] { new FilterCriteria("Value2", "less", 1900) }
                    };
                    TestUtility.ShouldFail(() => ExecuteReadCommand(cReadAll, scope), _readException);
                }
                {
                    var cReadAll = new ReadCommandInfo()
                    {
                        DataSource = "TestRowPermissions.SimpleRPBrowse",
                        ReadRecords = true,
                        Filters = new[] { new FilterCriteria("Value2", "greater", 1900) }
                    };
                    dynamic result = ExecuteReadCommand(cReadAll, scope).Records.Single();
                    Assert.AreEqual(2000, result.Value2);
                }
            }
        }

        [TestMethod]
        public void AutoApplyFilter()
        {
            using (var scope = TestScope.Create())
            {
                var gr = scope.Resolve<GenericRepository<TestRowPermissions.AutoFilter>>();
                var logFilterQuery = scope.Resolve<Common.DomRepository>().Common.Log.Query()
                    .Where(log => log.TableName == "TestRowPermissions.AutoFilter" && log.Action == "RowPermissionsReadItems filter");

                var testData = new[] { "a1", "a2", "b1", "b2" }.Select(name => new TestRowPermissions.AutoFilter { Name = name });
                gr.Save(testData, null, gr.Load());

                {
                    var readCommand = new ReadCommandInfo
                    {
                        DataSource = "TestRowPermissions.AutoFilter",
                        ReadRecords = true
                    };
                    int lastFilterCount = logFilterQuery.Count();
                    var readResult = (TestRowPermissions.AutoFilter[])ExecuteReadCommand(readCommand, scope).Records;
                    Assert.AreEqual("a1, a2", TestUtility.DumpSorted(readResult, item => item.Name));

                    Assert.AreEqual(1, logFilterQuery.Count() - lastFilterCount,
                        "Row permission filter should be automatically applied on reading, no need to be applied again on result permission validation.");
                }

                {
                    var readCommand = new ReadCommandInfo
                    {
                        DataSource = "TestRowPermissions.AutoFilter",
                        ReadRecords = true,
                        Filters = new FilterCriteria[] { new FilterCriteria("Name", "contains", "2") }
                    };
                    int lastFilterCount = logFilterQuery.Count();
                    var readResult = (TestRowPermissions.AutoFilter[])ExecuteReadCommand(readCommand, scope).Records;
                    Assert.AreEqual("a2", TestUtility.DumpSorted(readResult, item => item.Name));

                    Assert.AreEqual(1, logFilterQuery.Count() - lastFilterCount,
                        "Row permission filter should be automatically applied on reading, no need to be use it again for result permission validation.");
                }

                {
                    var readCommand = new ReadCommandInfo
                    {
                        DataSource = "TestRowPermissions.AutoFilter",
                        ReadRecords = true,
                        Filters = new FilterCriteria[] { new FilterCriteria("Name", "contains", "2"), new FilterCriteria(typeof(Common.RowPermissionsReadItems)) }
                    };
                    int lastFilterCount = logFilterQuery.Count();
                    var readResult = (TestRowPermissions.AutoFilter[])ExecuteReadCommand(readCommand, scope).Records;
                    Assert.AreEqual("a2", TestUtility.DumpSorted(readResult, item => item.Name));

                    Assert.AreEqual(1, logFilterQuery.Count() - lastFilterCount,
                        "Row permission filter should be automatically applied on reading, no need to be use it again for result permission validation.");
                }

                {
                    var readCommand = new ReadCommandInfo
                    {
                        DataSource = "TestRowPermissions.AutoFilter",
                        ReadRecords = true,
                        Filters = new FilterCriteria[] { new FilterCriteria(typeof(Common.RowPermissionsReadItems)), new FilterCriteria("Name", "contains", "2") }
                    };
                    int lastFilterCount = logFilterQuery.Count();
                    var readResult = (TestRowPermissions.AutoFilter[])ExecuteReadCommand(readCommand, scope).Records;
                    Assert.AreEqual("a2", TestUtility.DumpSorted(readResult, item => item.Name));

                    Assert.AreEqual(2, logFilterQuery.Count() - lastFilterCount,
                        "Row permission filter is not the last filter applied on reading. It will be use again for result permission validation to make sure other filters did not expand the result set.");
                }
            }
        }

        string ReadErrorData(UnitOfWorkScope scope, string testName)
        {
            Console.WriteLine("Test: " + testName);
            var readCommand = new ReadCommandInfo() { DataSource = "TestRowPermissions.ErrorData", ReadRecords = true, Filters = new[] { new FilterCriteria(testName) } };
            var loaded = ExecuteReadCommand(readCommand, scope).Records;
            string report = TestUtility.DumpSorted(loaded, item => ((ErrorData)item).Name);
            Console.WriteLine("Result: " + report);
            return report;
        }

        [TestMethod]
        public void ErrorHandling_SourceHasDuplicateID()
        {
            using (var scope = TestScope.Create())
            {
                var gr = scope.Resolve<GenericRepository<ErrorData>>();
                var newItems = new[] { "a", "b", "c" }.Select(name => new ErrorData { ID = Guid.NewGuid(), Name = name }).ToList();
                gr.Save(newItems, null, gr.Load());

                Assert.AreEqual("a, b, c", ReadErrorData(scope, ""));

                TestUtility.ShouldFail<FrameworkException>(() => ReadErrorData(scope, "duplicateSecondItem"),
                    "duplicate IDs", "ErrorData", newItems[1].ID.ToString());
            }
        }

        [TestMethod]
        public void ErrorHandling_RowPermissionsFilterReturnsDuplicateID()
        {
            using (var scope = TestScope.Create(builder => builder.ConfigureIgnoreClaims()))
            {
                var repository = scope.Resolve<Common.DomRepository>();
                var readCommand = scope.Resolve<IPluginsContainer<ICommandImplementation>>().GetPlugins().OfType<ReadCommand>().Single();
                Guid testRun = Guid.NewGuid();

                // The following DuplicateIdViewID GUIDs are hard-coded in SqlQueryable DuplicateIdView.
                // The user has permissions to read itemAllowed, does not have permissions for itemNotAllowed,
                // but the row permissions filter returns duplicate instances for itemAllowed because of the (intentional)
                // error in SqlQueryable DuplicateIdView.
                var itemAllowed = new EntityWithDuplicateIdFilter { DuplicateIdViewID = new Guid("11111111-c757-4757-9850-231c800393a7"), TestRun = testRun };
                var itemNotAllowed = new EntityWithDuplicateIdFilter { DuplicateIdViewID = new Guid("22222222-c757-4757-9850-231c800393a7"), TestRun = testRun };
                repository.TestRowPermissions.EntityWithDuplicateIdFilter.Insert(itemAllowed, itemNotAllowed);

                var command = new ReadCommandInfo
                {
                    DataSource = typeof(EntityWithDuplicateIdFilter).FullName,
                    Filters = new[] { new FilterCriteria("TestRun", "equal", testRun) },
                    ReadRecords = true
                };

                TestUtility.ShouldFail<FrameworkException>(
                    () => readCommand.Execute(command),
                    new[] { "Row permissions filter error", "duplicate IDs", command.DataSource, "RowPermissionsReadItems" });
            }
        }

        private void ExecuteSaveCommand(SaveEntityCommandInfo saveInfo, UnitOfWorkScope scope)
        {
            var commandImplementations = scope.Resolve<IPluginsContainer<ICommandImplementation>>();
            var saveCommand = commandImplementations.GetImplementations(saveInfo.GetType()).Single();
            saveCommand.Execute(saveInfo);
        }

        private T[] TestWrite<T>(T[] initial, T[] insertItems, T[] updateItems, T[] deleteItems, string expectedException) where T : class, IEntity
        {
            // initialize and persist
            using (var scope = TestScope.Create())
            {
                var gRepository = scope.Resolve<GenericRepository<T>>();
                // clear the repository
                gRepository.Save(null, null, gRepository.Load());

                // save initial data
                gRepository.Save(initial, null, null);

                scope.CommitAndClose(); // We need to write the test data to validate rollbacks on bad inserts and updates.
            }

            // attempt to write test data
            using (var scope = TestScope.Create())
            {
                // construct and execute SaveEntityCommand
                var saveCommand = new SaveEntityCommandInfo()
                {
                    Entity = typeof(T).FullName,
                    DataToInsert = insertItems,
                    DataToUpdate = updateItems,
                    DataToDelete = deleteItems
                };

                if (string.IsNullOrEmpty(expectedException))
                    ExecuteSaveCommand(saveCommand, scope);
                else
                    TestUtility.ShouldFail(() => ExecuteSaveCommand(saveCommand, scope), expectedException);

                scope.CommitAndClose();
            } // closing the scope makes transactions rollback for failed commands

            // read final state and cleanup
            using (var scope = TestScope.Create())
            {
                var finalRepository = scope.Resolve<GenericRepository<T>>();
                var allData = finalRepository.Load().ToArray();

                // cleanup
                finalRepository.Save(null, null, allData);

                scope.CommitAndClose();
                // return state of repository before cleanup
                return allData;
            }
        }

        [TestMethod]
        public void TestWriteNoRowPermissions()
        {
            var items = Enumerable.Range(0, 40).Select(a => new NoRP() { ID = Guid.NewGuid(), value = a }).ToList();

            // test insert
            TestWrite(null, items.ToArray(), null, null, null);

            var initial = items.ToArray();
            var id25 = items.Single(a => a.value == 25).ID;
            items.ForEach(a => a.value = a.value * 2);

            // update items
            {
                var result = TestWrite(initial, null, items.ToArray(), null, null);
                Assert.AreEqual(50, result.Single(a => a.ID == id25).value);
            }

            // delete all
            {
                var result = TestWrite(initial, null, null, items.ToArray(), null);
                Assert.AreEqual(0, result.Length);
            }
        }

        [TestMethod]
        public void TestWriteSimpleManyRecords()
        {
            CancelTestOnSlowServer();

            var notLegal = Enumerable.Range(0, 2005).Select(a => new SimpleRP() { ID = Guid.NewGuid(), value = a }).ToArray();
            var legal1 = Enumerable.Range(600, 300).Select(a => new SimpleRP() { ID = Guid.NewGuid(), value = a }).ToArray();

            // failed insert
            {
                var result = TestWrite(null, notLegal, null, null, _writeException);
                Assert.AreEqual(0, result.Length);
            }

            // failed update
            {
                var result = TestWrite(notLegal, null, notLegal, null, _writeException);
                Assert.AreEqual(notLegal.Length, result.Length);
            }

            // failed delete
            {
                var result = TestWrite(notLegal, null, null, notLegal, _writeException);
                Assert.AreEqual(notLegal.Length, result.Length);
            }

            // legal insert
            {
                var result = TestWrite(null, legal1, null, null, null);
                Assert.AreEqual(legal1.Length, result.Length);
            }

            // legal update
            {
                var update = legal1.Select(a => new SimpleRP() { ID = a.ID, value = 1999 }).ToArray();
                var result = TestWrite(legal1, null, update, null, null);
                Assert.AreEqual(legal1.Length, result.Length);
                Assert.IsTrue(result.All(a => a.value == 1999));
            }

            // legal delete
            {
                var delete = legal1.Take(50).ToArray();
                var result = TestWrite(legal1, null, null, delete, null);
                Assert.AreEqual(legal1.Length - 50, result.Length);
                var resIDs = result.Select(a => a.ID).ToList();
                Assert.IsTrue(delete.All(a => !resIDs.Contains(a.ID)));
            }
        }

        private void CancelTestOnSlowServer()
        {
            using (var scope = TestScope.Create())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                var scripts = Enumerable.Range(1, 10).Select(x => $"print {x}");
                context.SqlExecuter.ExecuteSql(scripts.First()); // Cold start.

                var sw = Stopwatch.StartNew();
                context.SqlExecuter.ExecuteSql(scripts);
                var testDuration = sw.Elapsed;

                Console.WriteLine($"CheckServerPerformance test duration: {testDuration}");
                const int limitMs = 200;
                const int usualMs = 2;
                if (testDuration.TotalMilliseconds > limitMs)
                    Assert.Inconclusive($"This test is suppressed on slow servers." +
                        $" The baseline durations is {testDuration.TotalMilliseconds}ms, should be under {limitMs}ms." +
                        $" The usual duration is around {usualMs}ms.");
            }
        }

        [TestMethod]
        public void TestWriteComplexAndImplicitReadWrite()
        {
            CancelTestOnSlowServer();

            var items = Enumerable.Range(0, 101).Select(a => new ComplexRP() { ID = Guid.NewGuid(), value = a }).ToArray();

            using (var scope = TestScope.Create())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                var currentUserName = context.UserInfo.UserName;
                var permRepository = scope.Resolve<GenericRepository<TestRowPermissions.ComplexRPPermissions>>();

                ComplexRPPermissions[] perms = new ComplexRPPermissions[]
                {
                    new ComplexRPPermissions() { userName = "__non_existant_user__", minVal = 17, maxVal = 50 },
                    new ComplexRPPermissions() { userName = currentUserName, minVal = 5, maxVal = 90 },
                    new ComplexRPPermissions() { userName = "__non_existant_user2__", minVal = 9, maxVal = 1 },
                };
                permRepository.Save(perms, null, permRepository.Load());

                var gRepository = scope.Resolve<GenericRepository<TestRowPermissions.ComplexRP>>();
                gRepository.Save(items, null, gRepository.Load());

                // first test results with explicit RP write filter calls
                {
                    var cAllowed = new ReadCommandInfo()
                    {
                        DataSource = "TestRowPermissions.ComplexRP",
                        ReadRecords = true,
                        Filters = new FilterCriteria[] { new FilterCriteria() { Filter = _rowPermissionsWriteFilter } }
                    };
                    var result = ExecuteReadCommand(cAllowed, scope);
                    var values = ((ComplexRP[])result.Records).Select(a => a.value);
                    Assert.IsTrue(Enumerable.Range(5, 86).All(a => values.Contains(a)));
                }

                scope.CommitAndClose();
            }

            // illegal insert
            {
                var result = TestWrite(null, items, null, null, _writeException);
                Assert.AreEqual(0, result.Length);
            }

            // illegal update subset
            {
                var toUpdate = items.Where(a => a.value > 80).ToArray();
                var result = TestWrite(items, null, toUpdate, null, _writeException);
                Assert.AreEqual(items.Length, result.Length);
            }

            // illegal delete subset
            {
                var toDelete = items.Where(a => a.value < 10).ToArray();
                var result = TestWrite(items, null, null, toDelete, _writeException);
                Assert.AreEqual(items.Length, result.Length);
            }

            var legal = items.Where(a => a.value >= 10 && a.value < 80).ToArray();

            // legal insert
            {
                var result = TestWrite(null, legal, null, null, null);
                Assert.AreEqual(legal.Length, result.Length);
            }

            // legal update
            {
                var update = legal.Select(a => new ComplexRP() { ID = a.ID, value = 50 }).ToArray();
                var result = TestWrite(legal, null, update, null, null);
                Assert.AreEqual(legal.Length, result.Length);
                Assert.IsTrue(result.All(a => a.value == 50));
            }

            // legal delete
            {
                var toDelete = legal.Take(10).ToArray();
                var result = TestWrite(legal, null, null, toDelete, null);
                Assert.AreEqual(legal.Length - 10, result.Length);
                var resIDs = result.Select(a => a.ID).ToList();
                Assert.IsTrue(toDelete.All(a => !resIDs.Contains(a.ID)));
            }
        }

        [TestMethod]
        public void TestUpdateIntoLegalValue()
        {
            Guid illegalID = Guid.NewGuid();
            SimpleRP[] illegal = new SimpleRP[]
            {
                new SimpleRP() { ID = illegalID, value = 100 }
            };

            SimpleRP[] updateToLegal = new SimpleRP[]
            {
                new SimpleRP() { ID = illegalID, value = 600}
            };

            var result = TestWrite(illegal, null, updateToLegal, null, _writeException);
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(100, result.First().value);
        }

        [TestMethod]
        public void SaveInvalidRecordWithPermission_Insert()
        {
            using (var scope = TestScope.Create(builder => builder.ConfigureIgnoreClaims()))
            {
                var processingEngine = scope.Resolve<IProcessingEngine>();

                var item1 = new SimpleRP { value = 1000, ID = Guid.NewGuid() };
                var item2 = new SimpleRP { value = 1001, ID = item1.ID };

                var command1 = new SaveEntityCommandInfo { Entity = item1.GetType().FullName, DataToInsert = new[] { item1 } };
                var response1 = processingEngine.Execute(new[] { command1 });
                Assert.IsTrue(response1.Success);

                var command2 = new SaveEntityCommandInfo { Entity = item2.GetType().FullName, DataToInsert = new[] { item2 } };
                var response2 = processingEngine.Execute(new[] { command2 });
                Assert.IsFalse(response2.Success);
                TestUtility.AssertContains(response2.UserMessage, new[] { "Operation could not be completed because the request sent to the server was not valid or not properly formatted." });
                TestUtility.AssertContains(response2.SystemMessage, new[] { "Inserting a record that already exists in database.", item2.ID.ToString() });
            }
        }

        [TestMethod]
        public void SaveInvalidRecordWithPermission_Update()
        {
            using (var scope = TestScope.Create(builder => builder.ConfigureIgnoreClaims()))
            {
                var processingEngine = scope.Resolve<IProcessingEngine>();

                var item = new SimpleRP { value = 1000, ID = Guid.NewGuid() };
                var command = new SaveEntityCommandInfo { Entity = item.GetType().FullName, DataToUpdate = new[] { item } };

                var response = processingEngine.Execute(new[] { command });
                Assert.IsFalse(response.Success);
                TestUtility.AssertContains(response.UserMessage, new[] { "Operation could not be completed because the request sent to the server was not valid or not properly formatted." });
                TestUtility.AssertContains(response.SystemMessage, new[] { "Updating a record that does not exist in database.", item.ID.ToString() });
            }
        }

        [TestMethod]
        public void SaveInvalidRecordWithPermission_Delete()
        {
            using (var scope = TestScope.Create(builder => builder.ConfigureIgnoreClaims()))
            {
                var processingEngine = scope.Resolve<IProcessingEngine>();

                var item = new SimpleRP { value = 1000, ID = Guid.NewGuid() };
                var command = new SaveEntityCommandInfo { Entity = item.GetType().FullName, DataToDelete = new[] { item } };

                var response = processingEngine.Execute(new[] { command });
                Assert.IsFalse(response.Success);
                TestUtility.AssertContains(response.UserMessage, new[] { "Operation could not be completed because the request sent to the server was not valid or not properly formatted." });
                TestUtility.AssertContains(response.SystemMessage, new[] { "Deleting a record that does not exist in database.", item.ID.ToString() });
            }
        }

        [TestMethod]
        public void SaveInvalidRecordWithoutPermission_Insert()
        {
            using (var scope = TestScope.Create(builder => builder.ConfigureIgnoreClaims()))
            {
                var processingEngine = scope.Resolve<IProcessingEngine>();

                var item1 = new SimpleRP { value = 1000, ID = Guid.NewGuid() };
                var item2 = new SimpleRP { value = 1, ID = item1.ID };

                var command1 = new SaveEntityCommandInfo { Entity = item1.GetType().FullName, DataToInsert = new[] { item1 } };
                var response1 = processingEngine.Execute(new[] { command1 });
                Assert.IsTrue(response1.Success);

                var command2 = new SaveEntityCommandInfo { Entity = item2.GetType().FullName, DataToInsert = new[] { item2 } };
                var response2 = processingEngine.Execute(new[] { command2 });
                Assert.IsFalse(response2.Success);
                TestUtility.AssertContains(response2.UserMessage, new[] { "Operation could not be completed because the request sent to the server was not valid or not properly formatted." });
                TestUtility.AssertContains(response2.SystemMessage, new[] { "Inserting a record that already exists in database.", item2.ID.ToString() });
            }
        }

        [TestMethod]
        public void SaveInvalidRecordWithoutPermission_Update()
        {
            using (var scope = TestScope.Create(builder => builder.ConfigureIgnoreClaims()))
            {
                var processingEngine = scope.Resolve<IProcessingEngine>();

                var item = new SimpleRP { value = 1, ID = Guid.NewGuid() };
                var command = new SaveEntityCommandInfo { Entity = item.GetType().FullName, DataToUpdate = new[] { item } };

                var response = processingEngine.Execute(new[] { command });
                Assert.IsFalse(response.Success);
                TestUtility.AssertContains(response.UserMessage, new[] { "Operation could not be completed because the request sent to the server was not valid or not properly formatted." });
                TestUtility.AssertContains(response.SystemMessage, new[] { "Updating a record that does not exist in database.", item.ID.ToString() });
            }
        }

        [TestMethod]
        public void SaveInvalidRecordWithoutPermission_Delete()
        {
            using (var scope = TestScope.Create(builder => builder.ConfigureIgnoreClaims()))
            {
                var processingEngine = scope.Resolve<IProcessingEngine>();

                var item = new SimpleRP { value = 1, ID = Guid.NewGuid() };
                var command = new SaveEntityCommandInfo { Entity = item.GetType().FullName, DataToDelete = new[] { item } };

                var response = processingEngine.Execute(new[] { command });
                Assert.IsFalse(response.Success);
                TestUtility.AssertContains(response.UserMessage, new[] { "Operation could not be completed because the request sent to the server was not valid or not properly formatted." });
                TestUtility.AssertContains(response.SystemMessage, new[] { "Deleting a record that does not exist in database.", item.ID.ToString() });
            }
        }

        [TestMethod]
        public void OptimizeExtension()
        {
            using (var scope = TestScope.Create())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                var repository = scope.Resolve<Common.DomRepository>();

                Assert.AreEqual(
                    @"item => item.Extension_OptimizeExtension.NameE.Contains(""allow"")",
                    repository.TestRowPermissions.OptimizeBase.GetRowPermissionsReadExpression(null, repository, context).ToString());
                Assert.AreEqual(
                    @"optimizeExtensionItem => optimizeExtensionItem.NameE.Contains(""allow"")",
                    repository.TestRowPermissions.OptimizeExtension.GetRowPermissionsReadExpression(null, repository, context).ToString());

                var b1 = new OptimizeBase { NameB = "b1" };
                var b2 = new OptimizeBase { NameB = "b2" };
                repository.TestRowPermissions.OptimizeBase.Insert(b1, b2);

                var e1 = new OptimizeExtension { NameE = "e1", ID = b1.ID };
                var e2 = new OptimizeExtension { NameE = "e2allow", ID = b2.ID };
                repository.TestRowPermissions.OptimizeExtension.Insert(e1, e2);

                Assert.AreEqual("b2", TestUtility.DumpSorted(
                    repository.TestRowPermissions.OptimizeBase.Query(new Common.RowPermissionsReadItems()),
                    item => item.NameB));
                Assert.AreEqual("e2allow", TestUtility.DumpSorted(
                    repository.TestRowPermissions.OptimizeExtension.Query(new Common.RowPermissionsReadItems()),
                    item => item.NameE));
            }
        }

        [TestMethod]
        public void InheritRowPermissionsToQuery()
        {
            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();

                Guid groupID = Guid.NewGuid();
                repository.TestRowPermissionsInheritToQuery.Simple.Insert(new[]
                {
                    new TestRowPermissionsInheritToQuery.Simple { Name = "a", GroupID = groupID },
                    new TestRowPermissionsInheritToQuery.Simple { Name = "b", GroupID = groupID },
                });

                Assert.AreEqual("a", TestUtility.DumpSorted(
                    repository.TestRowPermissionsInheritToQuery.Simple.Query(new Common.RowPermissionsReadItems())
                        .Where(item => item.GroupID == groupID)
                        .Select(item => item.Name)));

                Assert.AreEqual("a1, a2", TestUtility.DumpSorted(
                    repository.TestRowPermissionsInheritToQuery.DetailQuery.Query(new Common.RowPermissionsReadItems())
                        .Where(item => item.Simple.GroupID == groupID)
                        .Select(item => item.Info)));
            }
        }

        [TestMethod]
        public void FilterCriteriaRowPermissions()
        {
            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();

                var filter = new FilterCriteria { Filter = "Common.RowPermissionsReadItems" };
                var q = repository.TestRowPermissions.SimpleRP.Query(filter);
                TestUtility.AssertContains(q.ToString(), "1500", "2500"); // Check in the query contains the row permissions filter.
            }
        }
    }
}
