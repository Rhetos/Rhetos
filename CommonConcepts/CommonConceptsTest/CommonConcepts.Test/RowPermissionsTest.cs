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

namespace CommonConcepts.Test
{
    [TestClass]
    public class RowPermissionsTest
    {
        static string _exceptionText = "Insufficient permissions to access some or all of the data requested.";
        static string _rowPermissionsFilterParameter = "Common.RowPermissionsAllowedItems";

        /// <summary>
        /// Slightly redundant, but we still want to check if absence of RowPermissions is properly detected
        /// </summary>
        [TestMethod]
        public void TestReadNoRowPermissions()
        {
            using (var container = new RhetosTestContainer())
            {
                var gRepository = container.Resolve<GenericRepository<NoRP>>();
                gRepository.Save(Enumerable.Range(0, 50).Select(a => new NoRP() { value = a }), null, gRepository.Read());

                {
                    var all = new ReadCommandInfo()
                    {
                        DataSource = "TestRowPermissions.NoRP",
                        ReadRecords = true
                    };
                    var result = gRepository.ExecuteReadCommand(all);
                    Assert.AreNotEqual(null, result);
                    Assert.AreEqual(50, result.Records.Count());
                }

                {
                    var filtered = new ReadCommandInfo()
                    {
                        DataSource = "TestRowPermissions.NoRP",
                        ReadRecords = true,
                        Filters = new FilterCriteria[] { new FilterCriteria() { Filter = "TestRowPermissions.Value30" } }
                    };
                    var result = gRepository.ExecuteReadCommand(filtered);
                    Assert.AreNotEqual(null, result);
                    Assert.AreEqual(19, result.Records.Count());
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
                    var result = gRepository.ExecuteReadCommand(single);
                    Assert.AreNotEqual(null, result);
                    Assert.AreEqual(1, result.Records.Count());
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
            using (var container = new RhetosTestContainer())
            {
                var gRepository = container.Resolve<GenericRepository<SimpleRP>>();
                var items = Enumerable.Range(0, 4001).Select(a => new SimpleRP() { ID = Guid.NewGuid(), value = a }).ToList();
                gRepository.Save(items, null, gRepository.Read());

                {
                    var cReadAll = new ReadCommandInfo()
                    {
                        DataSource = "TestRowPermissions.SimpleRP",
                        ReadRecords = true,
                    };
                    TestUtility.ShouldFail(() => gRepository.ExecuteReadCommand(cReadAll), _exceptionText);
                }

                {
                    var cReadCountOnly = new ReadCommandInfo()
                    {
                        DataSource = "TestRowPermissions.SimpleRP",
                        ReadTotalCount = true,
                    };
                    var result = gRepository.ExecuteReadCommand(cReadCountOnly);
                    Assert.AreNotEqual(null, result);
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
                    var result = gRepository.ExecuteReadCommand(cRead1500_2500);
                    Assert.AreNotEqual(null, result.Records);
                    Assert.AreEqual(1001, result.Records.Count());
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
                    TestUtility.ShouldFail(() => gRepository.ExecuteReadCommand(cRead1501_2501), _exceptionText);
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
                    TestUtility.ShouldFail(() => gRepository.ExecuteReadCommand(cRead1499_2499), _exceptionText);
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
                    TestUtility.ShouldFail(() => gRepository.ExecuteReadCommand(cRead4000), _exceptionText);
                }

                {
                    var cReadFilterFail = new ReadCommandInfo()
                    {
                        DataSource = "TestRowPermissions.SimpleRP",
                        ReadRecords = true,
                        Filters = new FilterCriteria[] { new FilterCriteria() { Property = "value", Operation = "less", Value = 2001 } }
                    };
                    TestUtility.ShouldFail(() => gRepository.ExecuteReadCommand(cReadFilterFail), _exceptionText);
                }

                {
                    var cReadSingleFail = new ReadCommandInfo()
                    {
                        DataSource = "TestRowPermissions.SimpleRP",
                        ReadRecords = true,
                        Filters = new FilterCriteria[] { new FilterCriteria() { Property = "ID", Operation = "equal", Value = items[2501].ID } }
                    };
                    TestUtility.ShouldFail(() => gRepository.ExecuteReadCommand(cReadSingleFail), _exceptionText);
                }

                {
                    var cReadSingleOk = new ReadCommandInfo()
                    {
                        DataSource = "TestRowPermissions.SimpleRP",
                        ReadRecords = true,
                        Filters = new FilterCriteria[] { new FilterCriteria() { Property = "ID", Operation = "equal", Value = items[2500].ID } }
                    };
                    var result = gRepository.ExecuteReadCommand(cReadSingleOk);
                    Assert.AreNotEqual(null, result);
                    Assert.AreEqual(1, result.Records.Count());
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
                    var result = gRepository.ExecuteReadCommand(cReadFilterOk);
                    Assert.AreNotEqual(null, result);
                    Assert.AreEqual(1, result.Records.Count());
                    Assert.AreEqual(items[2500].ID, (result.Records[0] as SimpleRP).ID);
                }

                {
                    var cPermissionFilter = new ReadCommandInfo()
                    {
                        DataSource = "TestRowPermissions.SimpleRP",
                        ReadRecords = true,
                        Filters = new FilterCriteria[] { new FilterCriteria()  
                            { Filter = _rowPermissionsFilterParameter } }
                    };
                    var result = gRepository.ExecuteReadCommand(cPermissionFilter);
                    Assert.AreNotEqual(null, result);
                    Assert.AreEqual(1001, result.Records.Count());
                    var values = ((SimpleRP[])result.Records).Select(a => a.value);
                    Assert.IsTrue(Enumerable.Range(1500, 1001).All(a => values.Contains(a)));
                }
            }
        }

        [TestMethod]
        public void TestReadComplexWithContext()
        {
            using (var container = new RhetosTestContainer())
            {
                var context = container.Resolve<Common.ExecutionContext>();
                var currentUserName = context.UserInfo.UserName; 
                var permRepository = container.Resolve<GenericRepository<TestRowPermissions.ComplexRPPermissions>>();
                ComplexRPPermissions[] perms = new ComplexRPPermissions[]
                {
                    new ComplexRPPermissions() { userName = "__non_existant_user__", minVal = 17, maxVal = 50 },
                    new ComplexRPPermissions() { userName = currentUserName, minVal = 5, maxVal = 90 },
                    new ComplexRPPermissions() { userName = "__non_existant_user2__", minVal = 9, maxVal = 1 },
                };
                permRepository.Save(perms, null, permRepository.Read());

                var gRepository = container.Resolve<GenericRepository<TestRowPermissions.ComplexRP>>();
                var items = Enumerable.Range(0, 101).Select(a => new ComplexRP() { ID = Guid.NewGuid(), value = a }).ToList();
                gRepository.Save(items, null, gRepository.Read());

                // first test results with explicit RP filter calls
                {
                    var cAllowed = new ReadCommandInfo()
                    {
                        DataSource = "TestRowPermissions.ComplexRP",
                        ReadRecords = true,
                        Filters = new FilterCriteria[] { new FilterCriteria() { Filter = _rowPermissionsFilterParameter } }
                    };
                    var result = gRepository.ExecuteReadCommand(cAllowed);
                    Assert.AreNotEqual(null, result);
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
                            new FilterCriteria() { Filter = _rowPermissionsFilterParameter },
                            new FilterCriteria() { Filter = "TestRowPermissions.Value10" }
                        }
                    };
                    var result = gRepository.ExecuteReadCommand(cAllowedFilter);
                    Assert.AreNotEqual(null, result);
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
                    
                    TestUtility.ShouldFail(() => gRepository.ExecuteReadCommand(cInvalidRange), _exceptionText);
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
                    TestUtility.ShouldFail(() => gRepository.ExecuteReadCommand(cInvalidRange2), _exceptionText);
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
                    var result = gRepository.ExecuteReadCommand(cValidRange);
                    Assert.AreNotEqual(null, result);
                    Assert.AreEqual(9, result.Records.Count());
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
                    var result = gRepository.ExecuteReadCommand(cNoRecords);
                    Assert.AreNotEqual(null, result);
                    Assert.AreEqual(0, result.Records.Count());
                }

                {
                    var cTotalCount = new ReadCommandInfo()
                    {
                        DataSource = "TestRowPermissions.ComplexRP",
                        ReadTotalCount = true,
                    };
                    var result = gRepository.ExecuteReadCommand(cTotalCount);
                    Assert.AreNotEqual(null, result);
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
                    var result = gRepository.ExecuteReadCommand(cSingleOk);
                    Assert.AreNotEqual(null, result);
                    Assert.AreEqual(1, result.Records.Count());
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
                    TestUtility.ShouldFail(() => gRepository.ExecuteReadCommand(cSingleFail), _exceptionText);
                }
            }
        }
    }
}
