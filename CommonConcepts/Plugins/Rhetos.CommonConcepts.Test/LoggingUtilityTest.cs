using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System.Text.RegularExpressions;
using System.Text;

namespace Rhetos.CommonConcepts.Test
{
    [TestClass]
    public class LoggingUtilityTest
    {
        [TestMethod]
        public void GetSummaryTest()
        {
            Assert.AreEqual("", LoggingUtility.GetSummary("Insert", @"<PREVIOUS IDPredmeta=""201684"" IDPriloga=""-1"" TehnickaJedinicaID=""72894D9C-DC35-44F5-B4FE-2CA32CBD3DF1"" />"));

            Assert.AreEqual("IDPredmeta, IDPriloga, TehnickaJedinicaID", LoggingUtility.GetSummary("Update", @"<PREVIOUS IDPredmeta=""201684"" IDPriloga=""-1"" TehnickaJedinicaID=""72894D9C-DC35-44F5-B4FE-2CA32CBD3DF1"" />"));
            Assert.AreEqual("", LoggingUtility.GetSummary("Update", @"<PREVIOUS />"));
            Assert.AreEqual("", LoggingUtility.GetSummary("Update", @"<PREVIOUS  />"));
            Assert.AreEqual("", LoggingUtility.GetSummary("Update", @""));
            Assert.AreEqual("", LoggingUtility.GetSummary("Update", null));
            Assert.AreEqual("IDPredmeta, IDPriloga, TehnickaJedinicaID", LoggingUtility.GetSummary("Update", @"<PREVIOUS IDPredmeta=""201684"" IDPriloga=""-1"" TehnickaJedinicaID=""72894D"));
            Assert.AreEqual("IDPredmeta, IDPriloga", LoggingUtility.GetSummary("Update", @"<PREVIOUS IDPredmeta=""201684"" IDPriloga=""-1"""));
            Assert.AreEqual("IDPredmeta", LoggingUtility.GetSummary("Update", @"<PREVIOUS IDPredmeta=""201684"" IDPriloga="));
        }

        [TestMethod]
        public void ExtractUserInfoTestFormat()
        {
            var initialUserInfo = new TestUserInfo("os\ab", "cd.ef", true);
            var processedUserInfo = LoggingUtility.ExtractUserInfo(SqlUtility.UserContextInfoText(initialUserInfo));
            Assert.AreEqual(
                initialUserInfo.UserName + "|" + initialUserInfo.Workstation,
                processedUserInfo.UserName + "|" + processedUserInfo.Workstation);
        }

        [TestMethod]
        public void ExtractUserInfoTest()
        {
            var tests = new Dictionary<string, string>
            {
                { @"Alpha:OS\aa,bb-cc.hr", @"OS\aa|bb-cc.hr" },
                { @"Rhetos:Bob,Some workstation", @"Bob|Some workstation" },
                { @"Rhetos:OS\aa,192.168.113.108 port 49271", @"OS\aa|192.168.113.108 port 49271" },
                { @"Rhetos:aa,b.c", @"aa|b.c" },
                { @"Rhetos:verylongdomainname\extremelylongusernamenotcompl", @"verylongdomainname\extremelylongusernamenotcompl|null" },
                { @"Rhetos:asdf", @"asdf|null" },
                { @"Rhetos:1,2,3", @"1|2,3" },
                { @"Rhetos:1 1 , 2 2 ", @"1 1|2 2" },
                { "<null>", @"null|null" },
                { @"", @"null|null" },
                { @"Rhetos:", @"null|null" },
                { @"Rhetos:   ", @"null|null" },
                { @"Rhetos:  , ", @"null|null" },
                { @"1:2,3", @"null|null" }
            };

            foreach (var test in tests)
            {
                var result = LoggingUtility.ExtractUserInfo(test.Key == "<null>" ? null : test.Key);
                Assert.AreEqual(test.Value, (result.UserName ?? "null") + "|" + (result.Workstation ?? "null"), "Input: " + test.Key);
            }
        }

        class EventDescriptionList : List<LoggingUtility.EventDescription>
        {
            public void Add(string description)
            {
                Add(new LoggingUtility.EventDescription { LogID = Guid.NewGuid(), Action = "Update", Description = description });
            }
        }

        private static string EmptyDash(string s)
        {
            if (s == null)
                return "null";
            if (s == "")
                return "-";
            if (string.IsNullOrWhiteSpace(s))
                return "__";
            return s;
        }

        private static string Report(IEnumerable<LoggingUtility.ReconstructedDataModifications> items)
        {
            var result = new StringBuilder();
            Guid lastLogId = Guid.Empty;
            foreach (var item in items)
            {
                if (item.LogID != lastLogId && lastLogId != Guid.Empty)
                    result.AppendLine();
                result.AppendLine(item.Property + " " + EmptyDash(item.OldValue) + " " + EmptyDash(item.NewValue) + " " + item.Modified);
                lastLogId = item.LogID;
            }
            Console.WriteLine(items.Count() + " property events:");
            if (result.Length < 10000)
                Console.WriteLine(result.ToString());
            else
                Console.WriteLine("<text length " + result.Length + ">");
            return result.ToString();
        }

        [TestMethod]
        public void ReconstructDataModificationsTest()
        {
            string currentItemDescription = @"<PREVIOUS a=""5"" c=""2"" />";

            var eventDescriptions = new EventDescriptionList
            {
                @"<PREVIOUS a=""4"" />",
                @"<PREVIOUS a=""1"" b=""6"" />",
                @"<PREVIOUS b=""3"" />",
                @"<PREVIOUS />"
            };
            eventDescriptions.Last().Action = "Insert";

            var report = Report(LoggingUtility.ReconstructDataModifications(eventDescriptions, currentItemDescription));

            string expected = @"
                a 4 5 True
                b - - False
                c 2 2 False

                a 1 4 True
                b 6 - True
                c 2 2 False

                a 1 1 False
                b 3 6 True
                c 2 2 False

                a - 1 True
                b - 3 True
                c - 2 True";

            TestUtility.AssertAreEqualByLine(ClearText(expected), ClearText(report));
        }

        private static Regex ClearTextPrefix = new Regex(@"^\s+", RegexOptions.Multiline);
        private static string ClearText(string text)
        {
            return ClearTextPrefix.Replace(text, "").Trim();
        }

        [TestMethod]
        public void ReconstructDataModificationsTestNullsWithoutInsertEvent()
        {
            string currentItemDescription = @"<PREVIOUS a="""" b="""" c="""" f="""" />";

            var eventDescriptions = new EventDescriptionList
            {
                @"<PREVIOUS a=""1"" b="""" />",
                @"<PREVIOUS d="""" />",
                @"<PREVIOUS e="""" />",
                @"<PREVIOUS e=""1"" />",
            };

            var report = Report(LoggingUtility.ReconstructDataModifications(eventDescriptions, currentItemDescription));

            string expected = @"
                a 1 - True
                b - - True
                c - - False
                d - - False
                e - - False
                f - - False

                a 1 1 False
                b - - False
                c - - False
                d - - True
                e - - False
                f - - False

                a 1 1 False
                b - - False
                c - - False
                d - - False
                e - - True
                f - - False

                a 1 1 False
                b - - False
                c - - False
                d - - False
                e 1 - True
                f - - False";

            TestUtility.AssertAreEqualByLine(ClearText(expected), ClearText(report));
        }

        [TestMethod]
        public void ReconstructDataModificationsTestDelete()
        {
            string currentItemDescription = null;

            var eventDescriptions = new List<LoggingUtility.EventDescription>
            {
                new LoggingUtility.EventDescription { LogID = Guid.NewGuid(), Action = "Delete", Description = @"<PREVIOUS a=""1"" c=""5"" />" },
                new LoggingUtility.EventDescription { LogID = Guid.NewGuid(), Action = "Insert", Description = @"<PREVIOUS />" },
                new LoggingUtility.EventDescription { LogID = Guid.NewGuid(), Action = "Delete", Description = @"<PREVIOUS a=""2"" b=""4"" />" },
                new LoggingUtility.EventDescription { LogID = Guid.NewGuid(), Action = "Update", Description = @"<PREVIOUS a=""3"" />" },
                new LoggingUtility.EventDescription { LogID = Guid.NewGuid(), Action = "Insert", Description = @"<PREVIOUS />" },
            };

            var report = Report(LoggingUtility.ReconstructDataModifications(eventDescriptions, currentItemDescription));

            string expected = @"
                a 1 - True
                b - - False
                c 5 - True

                a - 1 True
                b - - False
                c - 5 True

                a 2 - True
                b 4 - True
                c - - False

                a 3 2 True
                b 4 4 False
                c - - False

                a - 3 True
                b - 4 True
                c - - False";

            TestUtility.AssertAreEqualByLine(ClearText(expected), ClearText(report));
        }

        [TestMethod]
        public void ReconstructDataModificationsTestError()
        {
            string currentItemDescription = @"xx";

            var eventDescriptions = new EventDescriptionList
            {
                @"<PREVIOUS a=""1"" />",
                @"<PREVIO />",
                @"<PREVIOUS a=""2"" />",
                @"<PREVIOUS a=""3"" b=""4""",
                @"<PREVIOUS ",
                @"<PREVIOUS a=""5"" />",
            };

            var report = Report(LoggingUtility.ReconstructDataModifications(eventDescriptions, currentItemDescription));

            string expected = @"
                a 1 <Invalid event Description format> True

                a <Invalid event Description format> 1 True

                a 2 <Invalid event Description format> True

                a <Invalid event Description format> 2 True

                a <Invalid event Description format> <Invalid event Description format> True

                a 5 <Invalid event Description format> True";

            TestUtility.AssertAreEqualByLine(ClearText(expected), ClearText(report));
        }
    }
}
