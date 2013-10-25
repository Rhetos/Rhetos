using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.TestCommon;
using Rhetos.Utilities;

namespace Rhetos.CommonConcepts.Test
{
    [TestClass]
    public class LoggingUtilityTest
    {
        [TestMethod]
        public void GetSummaryTest()
        {
            Assert.AreEqual("Insert.", LoggingUtility.GetSummary("Insert", @"<PREVIOUS IDPredmeta=""201684"" IDPriloga=""-1"" TehnickaJedinicaID=""72894D9C-DC35-44F5-B4FE-2CA32CBD3DF1"" />"));

            Assert.AreEqual("Update: IDPredmeta, IDPriloga, TehnickaJedinicaID.", LoggingUtility.GetSummary("Update", @"<PREVIOUS IDPredmeta=""201684"" IDPriloga=""-1"" TehnickaJedinicaID=""72894D9C-DC35-44F5-B4FE-2CA32CBD3DF1"" />"));
            Assert.AreEqual("Update.", LoggingUtility.GetSummary("Update", @"<PREVIOUS />"));
            Assert.AreEqual("Update.", LoggingUtility.GetSummary("Update", @"<PREVIOUS  />"));
            Assert.AreEqual("Update.", LoggingUtility.GetSummary("Update", @""));
            Assert.AreEqual("Update.", LoggingUtility.GetSummary("Update", null));
            Assert.AreEqual("Update: IDPredmeta, IDPriloga, TehnickaJedinicaID.", LoggingUtility.GetSummary("Update", @"<PREVIOUS IDPredmeta=""201684"" IDPriloga=""-1"" TehnickaJedinicaID=""72894D"));
            Assert.AreEqual("Update: IDPredmeta, IDPriloga.", LoggingUtility.GetSummary("Update", @"<PREVIOUS IDPredmeta=""201684"" IDPriloga=""-1"""));
            Assert.AreEqual("Update: IDPredmeta.", LoggingUtility.GetSummary("Update", @"<PREVIOUS IDPredmeta=""201684"" IDPriloga="));
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
    }
}
