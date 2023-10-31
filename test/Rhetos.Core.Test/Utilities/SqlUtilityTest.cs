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
using Rhetos.TestCommon;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Rhetos.Utilities.Test
{
    [TestClass]
    public class SqlUtilityTest
    {
        [TestMethod]
        public void ExtractUserInfoTestFormat()
        {
            var initialUserInfo = new TestUserInfo(@"os\ab", "cd.ef", true);
            var processedUserInfo = SqlUtility.ExtractUserInfo(SqlUtility.UserContextInfoText(initialUserInfo));
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
                { "<null>", @"null|null(unrecognized)" },
                { @"", @"null|null(unrecognized)" },
                { @"Rhetos:", @"null|null(unrecognized)" },
                { @"Rhetos:   ", @"null|null(unrecognized)" },
                { @"Rhetos:  , ", @"null|null(unrecognized)" },
                { @"1:2,3", @"null|null(unrecognized)" }
            };

            foreach (var test in tests)
            {
                var result = SqlUtility.ExtractUserInfo(test.Key == "<null>" ? null : test.Key);
                Assert.AreEqual(test.Value, (result.UserName ?? "null") + "|" + (result.Workstation ?? "null") + (result.IsUserRecognized ? "" : "(unrecognized)"), "Input: " + test.Key);
            }
        }

        [TestMethod]
        public void SplitBatches()
        {
            var tests = new Dictionary<string, string[]>
            {
                { "111\r\nGO\r\n222", new[] { "111", "222" } }, // Simple
                { "111\nGO\n222", new[] { "111", "222" } }, // UNIX EOL
                { "111\r\ngo\r\n222", new[] { "111", "222" } }, // Case insensitive
                { "111\r\n    GO\t\t\t\r\n222", new[] { "111", "222" } }, // Spaces and tabs
                { "GO\r\n111\r\nGO\r\n222\r\nGO", new[] { "111", "222" } }, // Beginning and ending with GO
                { "111\r\nGO\r\n222\r\nGO   ", new[] { "111", "222" } }, // Beginning and ending with GO
                { "\r\n  GO  \r\n\r\n111\r\nGO\r\n222\r\nGO   \r\nGO   \r\n\r\n", new[] { "111", "222" } }, // Beginning and ending with GO
                { "111\r\n222\r\nGO\r\n333\r\n444", new[] { "111\r\n222", "333\r\n444" } }, // Multi-line batches
                { "111\n222\nGO\n333\n444", new[] { "111\n222", "333\n444" } }, // Multi-line batches, UNIX EOL
                { "111\r\nGO GO\r\n		GO   \r\n		go           \r\n222\r\ngoo\r\ngo", new[] { "111\r\nGO GO", "222\r\ngoo" } }, // Complex
                { "", Array.Empty<string>() }, // Empty batches
                { "GO", Array.Empty<string>() }, // Empty batches
                { "\r\n  \r\n  GO  \r\n  \r\n", Array.Empty<string>() } // Empty batches
            };
            foreach (var test in tests)
                Assert.AreEqual(TestUtility.Dump(test.Value), TestUtility.Dump(SqlUtility.SplitBatches(test.Key)), "Input: " + test.Key);
        }
    }
}
