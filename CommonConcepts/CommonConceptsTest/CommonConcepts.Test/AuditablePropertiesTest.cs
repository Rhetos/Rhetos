/*
    Copyright (C) 2013 Omega software d.o.o.

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

namespace CommonConcepts.Test
{
    [TestClass]
    public class AuditablePropertiesTest
    {
        private static void CheckCreatedDate(CommonTestExecutionContext executionContext, DateTime start, DateTime finish)
        {
            start = new DateTime(start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second); // ORM may trim milliseconds.

            executionContext.NHibernateSession.Clear();
            var repository = new Common.DomRepository(executionContext);
            DateTime? generatedCreationTime = repository.TestAuditable.Simple.All().Single().Started;
            Assert.IsNotNull(generatedCreationTime, "Generated CreationTime is null.");

            var msg = "Generated CreationTime (" + generatedCreationTime.Value.ToString("o") + ") should be between " + start.ToString("o") + " and " + finish.ToString("o") + ".";
            Assert.IsTrue(start <= generatedCreationTime.Value && generatedCreationTime.Value <= finish, msg);
            Console.WriteLine(msg);
        }

        [TestMethod]
        public void CreationTime()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestAuditable.Simple" });
                var repository = new Common.DomRepository(executionContext);

                var start = MsSqlUtility.GetDatabaseTime(executionContext.SqlExecuter);
                repository.TestAuditable.Simple.Insert(new[] { new TestAuditable.Simple { Name = "app" } });
                executionContext.NHibernateSession.Flush();
                var finish = MsSqlUtility.GetDatabaseTime(executionContext.SqlExecuter);

                CheckCreatedDate(executionContext, start, finish);
            }
        }

        [TestMethod]
        public void CreationTime_Explicit()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestAuditable.Simple" });
                var repository = new Common.DomRepository(executionContext);

                var explicitDateTime = new DateTime(2001, 2, 3, 4, 5, 6);
                repository.TestAuditable.Simple.Insert(new[] { new TestAuditable.Simple { Name = "exp", Started = explicitDateTime } });
                executionContext.NHibernateSession.Flush();

                CheckCreatedDate(executionContext, explicitDateTime, explicitDateTime);
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
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] {
                    "DELETE FROM TestAuditable.Simple",
                    "DELETE FROM TestAuditable.Parent",
                    "INSERT INTO TestAuditable.Parent (ID, Name) VALUES ('"+parentID1+"', 'par1')",
                    "INSERT INTO TestAuditable.Parent (ID, Name) VALUES ('"+parentID2+"', 'par2')" });
                var repository = new Common.DomRepository(executionContext);

                {
                    var start = MsSqlUtility.GetDatabaseTime(executionContext.SqlExecuter);
                    repository.TestAuditable.Simple.Insert(new[] { ReadInstance(insertData) });

                    executionContext.NHibernateSession.Flush();
                    executionContext.NHibernateSession.Clear();
                    DateTime? generatedModificationTime = propertySelector(repository.TestAuditable.Simple.All().Single());
                    Assert.IsNotNull(generatedModificationTime, testInfo + " Insert: Generated ModificationTime is null.");

                    var expectedTime = ReadExpectedResult(insertData, start).Value;
                    AssertJustAfter(expectedTime, generatedModificationTime, "After insert");
                }

                if (string.IsNullOrEmpty(updateData))
                    return;

                {
                    var start = MsSqlUtility.GetDatabaseTime(executionContext.SqlExecuter);
                    repository.TestAuditable.Simple.Update(new[] { ReadInstance(updateData) });

                    executionContext.NHibernateSession.Flush();
                    executionContext.NHibernateSession.Clear();
                    DateTime? generatedModificationTime = propertySelector(repository.TestAuditable.Simple.All().Single());
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

        [TestMethod]
        public void ModificationTimeOf_Database()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var start = MsSqlUtility.GetDatabaseTime(executionContext.SqlExecuter);
                executionContext.SqlExecuter.ExecuteSql(new[]
                    {
                        "DELETE FROM TestAuditable.Simple",
                        "DELETE FROM TestAuditable.Parent",
                        "INSERT INTO TestAuditable.Parent (ID, Name) VALUES ('"+parentID1+"', 'par1')",
                        "INSERT INTO TestAuditable.Parent (ID, Name) VALUES ('"+parentID2+"', 'par2')",
                        "INSERT INTO TestAuditable.Simple (Name, ModifiedParentProperty) VALUES ('newInstance', NULL)",
                        "INSERT INTO TestAuditable.Simple (Name, ParentID, ModifiedParentProperty) VALUES ('explicitTime', '" + parentID1 + "', '2001-2-3')",
                        "INSERT INTO TestAuditable.Simple (Name, ParentID, ModifiedParentProperty) VALUES ('modifiedParent', '" + parentID1 + "', '2001-2-3')",
                        "UPDATE TestAuditable.Simple SET ParentID = '" + parentID2 + "' WHERE Name = 'modifiedParent'",
                        "INSERT INTO TestAuditable.Simple (Name, ParentID, ModifiedParentProperty) VALUES ('modifiedNameX', '" + parentID1 + "', '2001-2-3')",
                        "UPDATE TestAuditable.Simple SET Name = 'modifiedName' WHERE Name = 'modifiedNameX'"
                    });
                var repository = new Common.DomRepository(executionContext);

                Func<string, DateTime?> actualModifiedTime = test => repository.TestAuditable.Simple.Query()
                    .Where(item => item.Name == test).Single().ModifiedParentProperty;

                AssertJustAfter(start, actualModifiedTime("newInstance"), "newInstance");
                AssertJustAfter(new DateTime(2001, 2, 3), actualModifiedTime("explicitTime"), "explicitTime");
                AssertJustAfter(start, actualModifiedTime("modifiedParent"), "modifiedParent");
                AssertJustAfter(new DateTime(2001, 2, 3), actualModifiedTime("modifiedName"), "modifiedName");
            }
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
    }
}
