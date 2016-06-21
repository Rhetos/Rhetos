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
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Utilities;
using Rhetos.TestCommon;
using System.Security.Principal;
using System.Text.RegularExpressions;
using Rhetos.Configuration.Autofac;
using Rhetos.Dom.DefaultConcepts;

namespace CommonConcepts.Test
{
    [TestClass]
    public class LoggingTest
    {
        [TestMethod]
        public void DeleteIntegerStringDataTime()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                var newItem = new TestLogging.Simple { ID = Guid.NewGuid(), Count = -2, Name = "abc", Created = DateTime.Now };
                repository.TestLogging.Simple.Insert(new[] { newItem });

                var logRecord = repository.Common.Log.Query().Where(log => log.ItemId == newItem.ID && log.Action == "Insert").SingleOrDefault();
                Assert.IsNotNull(logRecord, "There should be 'Insert' record in the log.");
                Assert.AreEqual("", logRecord.Description);

                repository.TestLogging.Simple.Delete(new[] { newItem });

                logRecord = repository.Common.Log.Query().Where(log => log.ItemId == newItem.ID && log.Action == "Delete").SingleOrDefault();
                Assert.IsNotNull(logRecord, "There should be 'Delete' record in the log.");

                Assert.AreEqual(SqlUtility.UserContextInfoText(container.Resolve<IUserInfo>()), logRecord.ContextInfo);
                Assert.IsTrue(container.Resolve<IUserInfo>().IsUserRecognized);
                TestUtility.AssertContains(logRecord.ContextInfo, container.Resolve<IUserInfo>().UserName);
                TestUtility.AssertContains(logRecord.ContextInfo, container.Resolve<IUserInfo>().Workstation);

                var now = SqlUtility.GetDatabaseTime(container.Resolve<ISqlExecuter>());
                Assert.IsTrue(logRecord.Created.Value.Subtract(now).TotalSeconds < 5);

                Assert.AreEqual("TestLogging.Simple", logRecord.TableName);

                Assert.IsTrue(!string.IsNullOrWhiteSpace(logRecord.UserName));
                Assert.IsTrue(!string.IsNullOrWhiteSpace(logRecord.Workstation));

                // Description is XML:
                var xmlText = @"<?xml version=""1.0"" encoding=""UTF-16""?>" + Environment.NewLine + logRecord.Description;
                Console.WriteLine(xmlText);
                var xdoc = XDocument.Parse(xmlText);
                Console.WriteLine(string.Join(", ", xdoc.Root.Attributes().Select(a => a.Name + ":" + a.Value)));

                var logCount = int.Parse(xdoc.Root.Attribute("Count").Value);
                Assert.AreEqual(newItem.Count, logCount);

                var logName = xdoc.Root.Attribute("Name").Value;
                Assert.AreEqual(newItem.Name, logName);

                var logCreated = DateTime.Parse(xdoc.Root.Attribute("Created").Value);
                Assert.IsTrue(Math.Abs(newItem.Created.Value.Subtract(logCreated).TotalMilliseconds) <= 1000, "Error made by converting DataTime to XML should be less than a second.");
            }
        }

        [TestMethod]
        public void SpecialCharacters()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                var newItem = new TestLogging.Simple { ID = Guid.NewGuid(), Name = @"<>'""&;[]\\//()čćšđžČĆŠĐŽ]]>" };
                repository.TestLogging.Simple.Insert(new[] { newItem });

                var logRecord = repository.Common.Log.Query().Where(log => log.ItemId == newItem.ID && log.Action == "Insert").SingleOrDefault();
                Assert.IsNotNull(logRecord, "There should be an 'Insert' record in the log.");
                Assert.AreEqual("", logRecord.Description);

                repository.TestLogging.Simple.Delete(new[] { newItem });

                logRecord = repository.Common.Log.Query().Where(log => log.ItemId == newItem.ID && log.Action == "Delete").SingleOrDefault();
                Assert.IsNotNull(logRecord, "There should be a 'Delete' record in the log.");

                var xmlText = @"<?xml version=""1.0"" encoding=""UTF-16""?>" + Environment.NewLine + logRecord.Description;
                Console.WriteLine(xmlText);
                var xdoc = XDocument.Parse(xmlText);
                Console.WriteLine(string.Join(", ", xdoc.Root.Attributes().Select(a => a.Name + ":" + a.Value)));

                var logName = xdoc.Root.Attribute("Name").Value;
                Assert.AreEqual(newItem.Name, logName);
            }
        }

        [TestMethod]
        public void UpdatedOldNullValues()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                var newItem = new TestLogging.Simple { ID = Guid.NewGuid(), Count = null, Name = null, Created = null };
                repository.TestLogging.Simple.Insert(new[] { newItem });

                var logRecord = repository.Common.Log.Query().Where(log => log.ItemId == newItem.ID && log.Action == "Insert").SingleOrDefault();
                Assert.IsNotNull(logRecord, "There should be an 'Insert' record in the log.");
                Assert.AreEqual("", logRecord.Description);

                newItem.Count = 2;
                newItem.Name = "abc";
                newItem.Created = DateTime.Now;
                repository.TestLogging.Simple.Update(new[] { newItem });

                logRecord = repository.Common.Log.Query().Where(log => log.ItemId == newItem.ID && log.Action == "Update").SingleOrDefault();
                Assert.IsNotNull(logRecord, "There should be an 'Update' record in the log.");

                var xmlText = @"<?xml version=""1.0"" encoding=""UTF-16""?>" + Environment.NewLine + logRecord.Description;
                Console.WriteLine(xmlText);
                var xdoc = XDocument.Parse(xmlText);
                Console.WriteLine(string.Join(", ", xdoc.Root.Attributes().Select(a => a.Name + ":" + a.Value)));

                Assert.IsTrue(IsNullOrEmpty(xdoc.Root.Attribute("Count")));
                Assert.IsTrue(IsNullOrEmpty(xdoc.Root.Attribute("Name")));
                Assert.IsTrue(IsNullOrEmpty(xdoc.Root.Attribute("Created")));
            }
        }

        private static bool IsNullOrEmpty(XAttribute attribute)
        {
            // Attributes in XML cannot store null values
            return attribute == null || attribute.Value == "";
        }

        [TestMethod]
        public void DeleteOldNullValues()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                var newItem = new TestLogging.Simple { ID = Guid.NewGuid(), Count = null, Name = null, Created = null };
                repository.TestLogging.Simple.Insert(new[] { newItem });

                var logRecord = repository.Common.Log.Query().Where(log => log.ItemId == newItem.ID && log.Action == "Insert").SingleOrDefault();
                Assert.IsNotNull(logRecord, "There should be an 'Insert' record in the log.");
                Assert.AreEqual("", logRecord.Description);

                newItem.Count = 2;
                newItem.Name = "abc";
                newItem.Created = DateTime.Now;
                repository.TestLogging.Simple.Delete(new[] { newItem });

                logRecord = repository.Common.Log.Query().Where(log => log.ItemId == newItem.ID && log.Action == "Delete").SingleOrDefault();
                Assert.IsNotNull(logRecord, "There should be a 'Delete' record in the log.");

                var xmlText = @"<?xml version=""1.0"" encoding=""UTF-16""?>" + Environment.NewLine + logRecord.Description;
                Console.WriteLine(xmlText);
                var xdoc = XDocument.Parse(xmlText);
                Console.WriteLine(string.Join(", ", xdoc.Root.Attributes().Select(a => a.Name + ":" + a.Value)));

                Assert.IsTrue(IsNullOrEmpty(xdoc.Root.Attribute("Count")));
                Assert.IsTrue(IsNullOrEmpty(xdoc.Root.Attribute("Name")));
                Assert.IsTrue(IsNullOrEmpty(xdoc.Root.Attribute("Created")));
            }
        }

        [TestMethod]
        public void SqlChangeID()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                var id1 = Guid.NewGuid();
                var id2 = Guid.NewGuid();

                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        "DELETE FROM TestLogging.Complex",
                        "DELETE FROM TestLogging.Simple",
                        "INSERT INTO TestLogging.Simple (ID, Name) VALUES (" + SqlUtility.QuoteGuid(id1) + ", 'abc')",
                        "UPDATE TestLogging.Simple SET ID = " + SqlUtility.QuoteGuid(id2)
                    });

                var actual = repository.Common.Log.Query().Where(log => new Guid?[] { id1, id2 }.Contains(log.ItemId)).ToArray();

                var expected = new[]
                {
                    new Common.Log { ItemId = id1, Action = "Delete", Description = "...abc..." },
                    new Common.Log { ItemId = id1, Action = "Insert", Description = "..." },
                    new Common.Log { ItemId = id2, Action = "Insert", Description = "..." },
                };

                Func<Common.Log[], string> report = logRecords =>
                    TestUtility.DumpSorted(logRecords, log => log.ItemId.ToString() + " " + log.Action + " " + log.Description.Contains("abc"));

                Assert.AreEqual(report(expected), report(actual));
            }
        }

        [TestMethod]
        public void Complex()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        "DELETE FROM TestLogging.Complex",
                        "DELETE FROM TestLogging.Simple",
                    });
                var repository = container.Resolve<Common.DomRepository>();
                var id = Guid.NewGuid();

                var simple = new TestLogging.Simple { ID = Guid.NewGuid() };
                repository.TestLogging.Simple.Insert(new[] { simple });

                var complex = new TestLogging.Complex
                {
                    bi = new byte[] { 1, 2, 3 },
                    bo = true,
                    da = new DateTime(2001, 2, 3),
                    t = new DateTime(2001, 2, 3, 4, 5, 6),
                    de = 123.4567m,
                    g = Guid.NewGuid(),
                    ls = "abc",
                    m = 11.22m,
                    rID = simple.ID
                };
                repository.TestLogging.Complex.Insert(new[] { complex });
                complex.ls = "def";
                repository.TestLogging.Complex.Update(new[] { complex });
                repository.TestLogging.Complex.Delete(new[] { complex });

                var ids = new Guid?[] { simple.ID, complex.ID };

                var ins = repository.Common.Log.Query().Where(log => log.TableName == "TestLogging.Complex" && log.Action == "Insert" && ids.Contains(log.ItemId)).Single();
                var upd = repository.Common.Log.Query().Where(log => log.TableName == "TestLogging.Complex" && log.Action == "Update" && ids.Contains(log.ItemId)).Single();
                var del = repository.Common.Log.Query().Where(log => log.TableName == "TestLogging.Complex" && log.Action == "Delete" && ids.Contains(log.ItemId)).Single();

                Assert.AreEqual("", ins.Description);
                Assert.AreEqual(@"<PREVIOUS ls=""abc"" />", upd.Description);

                Console.WriteLine(del.Description);

                var description = del.Description.Split(' ').OrderBy(x => x).ToList();
                Assert.AreEqual(@"/>", description[0]);
                Assert.AreEqual(@"<PREVIOUS", description[1]);
                Assert.AreEqual(@"bi=""0x010203""", description[2]);
                Assert.AreEqual(@"bo=""1""", description[3]);
                Assert.AreEqual(@"da=""2001-02-03""", description[4]);
                Assert.IsTrue(new Regex(@"^de=""123\.45670*""$").IsMatch(description[5]));// optional additional zeros
                Assert.AreEqual(@"g=""" + SqlUtility.GuidToString(complex.g.Value) + @"""", description[6]);
                Assert.AreEqual(@"ls=""def""", description[7]);
                Assert.AreEqual(@"m=""11.2200""", description[8]);
                Assert.AreEqual(@"rID=""" + SqlUtility.GuidToString(simple.ID) + @"""", description[9]);
                Assert.IsTrue(new Regex(@"^t=""2001-02-03T04:05:06(.0+)?""$").IsMatch(description[10]));// optional milliseconds
            }
        }
    }
}
