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
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System.Text.RegularExpressions;
using Rhetos.Configuration.Autofac;
using Rhetos.Dom.DefaultConcepts;

namespace CommonConcepts.Test
{
    [TestClass]
    public class AuditablePropertiesTest
    {
        private static void CheckCreatedDate(RhetosTestContainer container, DateTime start, DateTime finish)
        {
            start = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second); // ORM may trim milliseconds.

            var repository = container.Resolve<Common.DomRepository>();
            DateTime? generatedCreationTime = repository.TestAuditable.Simple.Load().Single().Started;
            Assert.IsNotNull(generatedCreationTime, "Generated CreationTime is null.");

            var msg = "Generated CreationTime (" + generatedCreationTime.Value.ToString("o") + ") should be between " + start.ToString("o") + " and " + finish.ToString("o") + ".";
            Assert.IsTrue(start <= generatedCreationTime.Value && generatedCreationTime.Value <= finish, msg);
            Console.WriteLine(msg);
        }

        [TestMethod]
        public void CreationTime()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestAuditable.Simple" });
                var repository = container.Resolve<Common.DomRepository>();

                var start = SqlUtility.GetDatabaseTime(container.Resolve<ISqlExecuter>());
                repository.TestAuditable.Simple.Insert(new[] { new TestAuditable.Simple { Name = "app" } });
                var finish = SqlUtility.GetDatabaseTime(container.Resolve<ISqlExecuter>());

                CheckCreatedDate(container, start, finish);
            }
        }

        [TestMethod]
        public void CreationTime_Explicit()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestAuditable.Simple" });
                var repository = container.Resolve<Common.DomRepository>();

                var explicitDateTime = new DateTime(2001, 2, 3, 4, 5, 6);
                repository.TestAuditable.Simple.Insert(new[] { new TestAuditable.Simple { Name = "exp", Started = explicitDateTime } });

                CheckCreatedDate(container, explicitDateTime, explicitDateTime);
            }
        }

        private static Guid parentID1 = Guid.NewGuid();
        private static Guid parentID2 = Guid.NewGuid();
        private static DateTime oldTime1 = new DateTime(2001, 2, 3, 4, 5, 6);
        private static DateTime oldTime2 = new DateTime(2002, 2, 3, 4, 5, 6);

        private static Regex testParser = new Regex(@"^name(.+?)-par(.+?)-time(.+?)(-time(.+?))?=>time(.+?)$");
        private static Dictionary<string, Guid?> parentParser = new Dictionary<string, Guid?>
            { { "1", parentID1 }, { "2", parentID2 }, { "Null", null }, { "", null } };
        private static Dictionary<string, DateTime?> timeParser = new Dictionary<string, DateTime?>
            { { "1", oldTime1 }, { "2", oldTime2 }, { "Null", null }, { "", null }, { "Now", DateTime.MaxValue } };
        private static Guid simpleID1 = Guid.NewGuid();

        private static TestAuditable.Simple ReadInstance(string data)
        {
            var match = testParser.Match(data);
            return new TestAuditable.Simple
            {
                ID = simpleID1,
                Name = match.Groups[1].Value,
                ParentID = parentParser[match.Groups[2].Value],
                ModifiedParentProperty = timeParser[match.Groups[3].Value],
                ModifiedNameOrParentModification = timeParser[match.Groups[5].Value],
            };
        }

        private static DateTime? ReadExpectedResult(string data, DateTime dbTime)
        {
            var match = testParser.Match(data);
            var result = timeParser[match.Groups[6].Value];
            if (result == DateTime.MaxValue)
                result = dbTime;
            return result;
        }

        private static void TestModificationTimeOf(Func<TestAuditable.Simple, DateTime?> propertySelector, string insertData, string updateData, string testInfo)
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] {
                    "DELETE FROM TestAuditable.Simple",
                    "DELETE FROM TestAuditable.Parent",
                    "INSERT INTO TestAuditable.Parent (ID, Name) VALUES ('"+parentID1+"', 'par1')",
                    "INSERT INTO TestAuditable.Parent (ID, Name) VALUES ('"+parentID2+"', 'par2')" });
                var repository = container.Resolve<Common.DomRepository>();

                {
                    var start = SqlUtility.GetDatabaseTime(container.Resolve<ISqlExecuter>());
                    repository.TestAuditable.Simple.Insert(new[] { ReadInstance(insertData) });

                    DateTime? generatedModificationTime = propertySelector(repository.TestAuditable.Simple.Load().Single());
                    Assert.IsNotNull(generatedModificationTime, testInfo + " Insert: Generated ModificationTime is null.");

                    var expectedTime = ReadExpectedResult(insertData, start).Value;
                    AssertJustAfter(expectedTime, generatedModificationTime, "After insert");
                }

                if (string.IsNullOrEmpty(updateData))
                    return;

                {
                    var start = SqlUtility.GetDatabaseTime(container.Resolve<ISqlExecuter>());
                    repository.TestAuditable.Simple.Update(new[] { ReadInstance(updateData) });

                    DateTime? generatedModificationTime = propertySelector(repository.TestAuditable.Simple.Load().Single());
                    Assert.IsNotNull(generatedModificationTime, testInfo + " Update: Generated ModificationTime is null.");

                    var expectedTime = ReadExpectedResult(updateData, start).Value;
                    AssertJustAfter(expectedTime, generatedModificationTime, "After update");
                }
            }
        }

        [TestMethod]
        public void ModificationTimeOf()
        {
            Func<TestAuditable.Simple, DateTime?> p = item => item.ModifiedParentProperty;
            TestModificationTimeOf(p, "nameA-par1-time1=>time1", "nameA-par2-time1=>timeNow", "Changed parent, update time.");
            TestModificationTimeOf(p, "nameA-par1-time1=>time1", "nameB-par1-time1=>time1", "Changed name, keep old time.");
            TestModificationTimeOf(p, "nameA-par1-time1=>time1", "nameA-parNull-time1=>timeNow", "Removed parent, update time.");
            TestModificationTimeOf(p, "nameA-parNull-time1=>time1", "nameA-par1-time1=>timeNow", "Initialized parent, update time.");
            TestModificationTimeOf(p, "nameA-parNull-time1=>time1", "nameA-parNull-time1=>time1", "No change.");
            TestModificationTimeOf(p, "nameA-par1-time1=>time1", "nameA-par1-time2=>time2", "Changed time.");
            TestModificationTimeOf(p, "nameA-par1-time1=>time1", "nameA-par2-time2=>timeNow", "Changed parent and time, update time.");
            TestModificationTimeOf(p, "nameA-par1-timeNull=>timeNow", null, "Initialize parent, update time.");
            TestModificationTimeOf(p, "nameA-parNull-timeNull=>timeNow", null, "Initialize without parent, update time.");
        }

        private static void AssertJustAfter(DateTime expected, DateTime? actual, string errorMessage)
        {
            expected = new DateTime(expected.Year, expected.Month, expected.Day, expected.Hour, expected.Minute, expected.Second); // ORM may trim milliseconds.
            var msg = "Expected " + expected.ToString("o") + ", actual " + actual.Value.ToString("o") + ".";
            Assert.IsTrue(expected <= actual.Value && actual.Value <= expected.AddSeconds(10), errorMessage + " " + msg);
            Console.WriteLine(msg);
        }

        [TestMethod]
        public void ModificationTimeOf_Multiple()
        {
            Func<TestAuditable.Simple, DateTime?> p = item => item.ModifiedNameOrParentModification;
            TestModificationTimeOf(p, "nameA-par1-time1-time2=>time2", "nameA-par2-time1-time2=>timeNow", "Changed parent, update time.");
            TestModificationTimeOf(p, "nameA-par1-time1-time2=>time2", "nameB-par1-time1-time2=>timeNow", "Changed name, update time.");
            TestModificationTimeOf(p, "nameA-par1-time1-time2=>time2", "nameA-par1-time2-time2=>timeNow", "Changed parent modification time, update time.");
            TestModificationTimeOf(p, "nameA-par1-time1-time2=>time2", "nameA-par1-time1-time2=>time2", "No change.");
            TestModificationTimeOf(p, "nameA-par1-time1-time2=>time2", "nameNull-par1-time1-time2=>timeNow", "Removed name, update time.");
            TestModificationTimeOf(p, "nameA-par1-time1-time2=>time2", "nameA-parNull-time1-time2=>timeNow", "Removed parent, update time.");
            TestModificationTimeOf(p, "nameA-parNull-time1-time2=>time2", "nameA-par1-time1-time2=>timeNow", "Initialized parent, update time.");
            TestModificationTimeOf(p, "nameA-parNull-time1-time2=>time2", "nameA-parNull-time1-time2=>time2", "No change.");
            TestModificationTimeOf(p, "nameA-par1-time1-time2=>time2", "nameA-par2-time2-time2=>timeNow", "Changed parent and time, update time.");
            TestModificationTimeOf(p, "nameA-par1-timeNull-timeNull=>timeNow", null, "Initialize1, update time.");
            TestModificationTimeOf(p, "nameA-parNull-time1-timeNull=>timeNow", null, "Initialize2, update time.");
        }

        public void AssertLessOrEqual(DateTime? t1, DateTime? t2, string message)
        {
            Assert.IsTrue(t1.Value <= t2.Value, $"Failed condition {t1.Value.ToString("o")} <= {t2.Value.ToString("o")}. {message}");
        }

        [TestMethod]
        public void ModificationTime_ChronologyOnInsert()
        {
            const int testRuns = 5;
            const int testBatch = 5;
            TimeSpan sqlPrecision = TimeSpan.FromMilliseconds(5); // SQL DateTime precision.

            var errors = new MultiDictionary<string, string>();

            for (int testRun = 0; testRun < testRuns; testRun++)
            {
                using (var container = new RhetosTestContainer())
                {
                    var context = container.Resolve<Common.ExecutionContext>();
                    var repository = context.Repository;
                    repository.TestAuditable.Simple2.Insert(new TestAuditable.Simple2 { }); // Cold start.

                    var items = Enumerable.Range(0, testBatch)
                        .Select(x => new TestAuditable.Simple2 { Name = x.ToString() })
                        .ToArray();

                    foreach (var item in items)
                        repository.TestAuditable.Simple2.Insert(item); // Inserting one by one to make sure that each is processed separately.

                    for (int i = 0; i < items.Count(); i++)
                        items[i] = repository.TestAuditable.Simple2.Load(new[] { items[i].ID }).Single();

                    Console.WriteLine(string.Join("\r\n", items.Select((item, x) => $"{x}" +
                        $" {item.Created.Value.ToString("o")}" +
                        $" created\r\n{x} {item.Modified.Value.ToString("o")} modified")));

                    for (int i = 1; i < items.Count(); i++)
                        AssertLessOrEqual(items[i].Created, items[i].Modified, $"Item should be created before modified ({testRun}/{i}).");

                    for (int i = 1; i < items.Count(); i++)
                        AssertLessOrEqual(items[i-1].Modified, items[i].Created, $"The previous item should be modified before the next is created ({testRun}/{i}).");
                }
            }
        }
    }
}
