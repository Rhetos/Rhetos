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
using Rhetos.Configuration.Autofac;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonConcepts.Test
{
    [TestClass]
    public class PolymorphicTest
    {
        [TestMethod]
        public void Simple()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                // Initialize data:

                repository.TestPolymorphic.Simple1.Delete(repository.TestPolymorphic.Simple1.All());
                repository.TestPolymorphic.Simple2.Delete(repository.TestPolymorphic.Simple2.All());
                Assert.AreEqual(0, repository.TestPolymorphic.SimpleBase.All().Count());

                repository.TestPolymorphic.Simple1.Insert(new[] {
                    new TestPolymorphic.Simple1 { Name = "a", Days = 1 },
                    new TestPolymorphic.Simple1 { Name = "b", Days = 2 },
                    new TestPolymorphic.Simple1 { Name = "b3", Days = 2.3m },
                    new TestPolymorphic.Simple1 { Name = "b7", Days = 2.7m },
                });
                repository.TestPolymorphic.Simple2.Insert(new[] {
                    new TestPolymorphic.Simple2 { Name1 = "aa", Name2 = 11, Finish = new DateTime(2000, 1, 1) },
                    new TestPolymorphic.Simple2 { Name1 = "bb", Name2 = 22, Finish = new DateTime(2000, 1, 2) },
                    new TestPolymorphic.Simple2 { Name1 = "cc", Name2 = 33, Finish = new DateTime(2000, 1, 3) },
                });

                // Tests reading:

                var all = repository.TestPolymorphic.SimpleBase.All();
                Assert.AreEqual(
                    "a/1, aa-11/1, b/2, b3/2, b7/3, bb-22/2, cc-33/3",
                    TestUtility.DumpSorted(all, item => item.Name + "/" + item.Days),
                    "Property implementations");

                var filterBySubtype = repository.TestPolymorphic.SimpleBase.Query()
                    .Where(item => item.Subtype == "TestPolymorphic.Simple1")
                    .Select(item => item.Name);
                Assert.AreEqual("a, b, b3, b7", TestUtility.DumpSorted(filterBySubtype), "filterBySubtype");

                var filterBySubtypeReference = repository.TestPolymorphic.SimpleBase.Query()
                    .Where(item => item.Simple2 != null)
                    .Select(item => item.Name);
                Assert.AreEqual("aa-11, bb-22, cc-33", TestUtility.DumpSorted(filterBySubtypeReference), "filterBySubtypeReference");

                var filterByProperty = repository.TestPolymorphic.SimpleBase.Query()
                    .Where(item => item.Days == 2)
                    .Select(item => item.Name);
                Assert.AreEqual("b, bb-22", TestUtility.DumpSorted(filterByProperty), "filterByProperty");

                var filterByID = repository.TestPolymorphic.SimpleBase.Query()
                    .Where(item => item.ID == all[0].ID)
                    .Select(item => item.Name);
                Assert.AreEqual("a", TestUtility.DumpSorted(filterByID), "filterByID");

                var filterBySubtypeID = repository.TestPolymorphic.SimpleBase.Query()
                    .Where(item => item.Simple1.ID == all[0].ID)
                    .Select(item => item.Name);
                Assert.AreEqual("a", TestUtility.DumpSorted(filterBySubtypeID), "filterBySubtypeID");

                var filterByOtherSubtypeID = repository.TestPolymorphic.SimpleBase.Query()
                    .Where(item => item.Simple2.ID == all[0].ID)
                    .Select(item => item.Name);
                Assert.AreEqual("", TestUtility.DumpSorted(filterByOtherSubtypeID), "filterByOtherSubtypeID");
            }
        }

        [TestMethod]
        public void Simple_NoExcessSql()
        {
            using (var container = new RhetosTestContainer())
            {
                CheckColumns(container, "ID, Days, Name", "TestPolymorphic", "Simple1_As_SimpleBase");
                CheckColumns(container, "ID, Days, Name", "TestPolymorphic", "Simple2_As_SimpleBase");
                CheckColumns(container, "ID, Days, Name, Subtype, Simple1ID, Simple2ID", "TestPolymorphic", "SimpleBase");
            }
        }

        private static void CheckColumns(RhetosTestContainer container, string expectedColumns, string schema, string table)
        {
            var sqlExecuter = container.Resolve<ISqlExecuter>();
            var actualColumns = new List<string>();
            sqlExecuter.ExecuteReader(
                "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = '" + schema + "' AND TABLE_NAME = '" + table + "'",
                reader => actualColumns.Add(reader[0].ToString()));
            Assert.AreEqual(
                TestUtility.DumpSorted(expectedColumns.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)),
                TestUtility.DumpSorted(actualColumns), schema + "." + table);
        }

        [TestMethod]
        public void Simple_NoExcessCs()
        {
            List<string> baseProperties = typeof(TestPolymorphic.SimpleBase).GetProperties().Select(p => p.Name).ToList();

            string expectedProperties = "ID, Days, Name, Subtype, Simple1, Simple1ID, Simple2, Simple2ID";

            Assert.AreEqual(
                TestUtility.DumpSorted(expectedProperties.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)),
                TestUtility.DumpSorted(baseProperties.Where(p => !p.Contains("_"))));
        }

        [TestMethod]
        public void Simple_Browse()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                // Initialize data:

                repository.TestPolymorphic.Simple1.Delete(repository.TestPolymorphic.Simple1.All());
                repository.TestPolymorphic.Simple2.Delete(repository.TestPolymorphic.Simple2.All());
                Assert.AreEqual(0, repository.TestPolymorphic.SimpleBase.All().Count());

                repository.TestPolymorphic.Simple1.Insert(new[] {
                    new TestPolymorphic.Simple1 { Name = "a", Days = 1 },
                    new TestPolymorphic.Simple1 { Name = "b", Days = 2 },
                    new TestPolymorphic.Simple1 { Name = "b3", Days = 2.3m },
                    new TestPolymorphic.Simple1 { Name = "b7", Days = 2.7m },
                });
                repository.TestPolymorphic.Simple2.Insert(new[] {
                    new TestPolymorphic.Simple2 { Name1 = "aa", Name2 = 11, Finish = new DateTime(2000, 1, 1) },
                    new TestPolymorphic.Simple2 { Name1 = "bb", Name2 = 22, Finish = new DateTime(2000, 1, 2) },
                    new TestPolymorphic.Simple2 { Name1 = "cc", Name2 = 33, Finish = new DateTime(2000, 1, 3) },
                });

                // Tests reading:

                var report = repository.TestPolymorphic.SimpleBrowse.Query()
                    .Where(item => item.Days == 2)
                    .Select(item => item.Name + "/" + item.Days + "(" + item.Simple1Name + "/" + item.Simple2Name1 + "/" + item.Simple2.Name2 + ")")
                    .ToList();

                Assert.AreEqual(
                    "b/2(b//), bb-22/2(/bb/22)",
                    TestUtility.DumpSorted(report));
            }
        }

        [TestMethod]
        public void Empty()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                var loaded = repository.TestPolymorphic.Empty.All();

                Assert.AreEqual("", TestUtility.DumpSorted(loaded, item => item.ID + "-" + item.Subtype));
            }
        }

        [TestMethod]
        public void SecondBase()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                // Initialize data:

                repository.TestPolymorphic.Simple1.Delete(repository.TestPolymorphic.Simple1.All());
                repository.TestPolymorphic.Simple2.Delete(repository.TestPolymorphic.Simple2.All());
                repository.TestPolymorphic.Second1.Delete(repository.TestPolymorphic.Second1.All());
                Assert.AreEqual(0, repository.TestPolymorphic.SecondBase.All().Count());

                repository.TestPolymorphic.Simple1.Insert(new[] {
                    new TestPolymorphic.Simple1 { Name = "a", Days = 1 },
                });
                repository.TestPolymorphic.Simple2.Insert(new[] {
                    new TestPolymorphic.Simple2 { Name1 = "b", Name2 = 2, Finish = new DateTime(2000, 1, 22) },
                });
                repository.TestPolymorphic.Second1.Insert(new[] {
                    new TestPolymorphic.Second1 { Info = "c" },
                });

                // Tests reading:

                var all = repository.TestPolymorphic.SecondBase.All();
                Assert.AreEqual(
                    "a/1.0000000000, b/2/2000-01-22T00:00:00, c",
                    TestUtility.DumpSorted(all, item => item.Info));
            }
        }
    }
}
