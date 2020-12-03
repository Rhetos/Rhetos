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
using System.IO;
using System.Text;

namespace Rhetos.Utilities.Test
{
    [TestClass]
    public class FilesUtilityTest
    {
        [TestMethod]
        public void AbsoluteToRelativePath()
        {
            var tests = new ListOfTuples<string, string, string>()
            {
                // Trivial:
                { @"C:\", @"C:\", @"." },
                { @"C:\1", @"C:\", @".." },
                { @"C:\1\", @"C:\", @".." },
                { @"C:\1", @"C:\1", @"." },
                { @"C:\1\", @"C:\1", @"." },
                { @"C:\1", @"C:\1\", @"." },
                { @"C:\1\", @"C:\1\", @"." },
                // Simple:
                { @"C:\", @"C:\1", @"1" },
                { @"C:\", @"C:\1\", @"1" },
                { @"C:\1", @"C:\2", @"..\2" },
                { @"C:\1\", @"C:\2", @"..\2" },
                { @"C:\1", @"C:\2\", @"..\2" },
                { @"C:\1\", @"C:\2\", @"..\2" },
                // Complex:
                { @"C:\11\22\33", @"C:\11\aaa\bbb\ccc", @"..\..\aaa\bbb\ccc" },
                { @"C:\11\22\33\aaa", @"C:\11\aaa\22\33", @"..\..\..\aaa\22\33" },
                // Other disk:
                { @"C:\1", @"D:\1", @"D:\1" },
                { @"C:\1", @"D:\2", @"D:\2" },
            };

            foreach (var test in tests)
                Assert.AreEqual(
                    test.Item3,
                    FilesUtility.AbsoluteToRelativePath(test.Item1, test.Item2),
                    $"base:'{test.Item1}', target:'{test.Item2}'");

        }

        [TestMethod]
        public void ReadAllText_UTF8()
        {
            var log = new List<string>();
            LogMonitor logMonitor = (eventType, eventName, message) =>
            {
                if (eventName == "FilesUtility" && eventType == Logging.EventType.Warning)
                    log.Add($"{message}");
            };

            var testPath = Path.GetTempFileName();
            string sampleText = "111\r\n¤\r\n333";
            File.WriteAllText(testPath, sampleText, Encoding.UTF8);

            var files = new FilesUtility(new ConsoleLogProvider(logMonitor));
            string readText;
            try
            {
                readText = files.ReadAllText(testPath);
            }
            finally
            {
                File.Delete(testPath);
            }

            Assert.AreEqual(sampleText, readText);
            Assert.AreEqual("", TestUtility.Dump(log));
        }

        //TODO: Encoding.Default on .NET Core is laways UTF8 so we should check if this test is needed
        //and also if the check in FilesUtility.ReadAllText is needed on .NET Core
        [TestMethod]
        public void ReadAllText_Default()
        {
            if (Encoding.Default == Encoding.UTF8)
                Assert.Inconclusive("Default encoding is UTF8.");

            var log = new List<string>();
            LogMonitor logMonitor = (eventType, eventName, message) =>
            {
                if (eventName == "FilesUtility" && eventType == Logging.EventType.Warning)
                    log.Add($"{message()}");
            };

            var testPath = Path.GetTempFileName();
            string sampleText = "111\r\n¤\r\n333";
            File.WriteAllText(testPath, sampleText, CodePagesEncodingProvider.Instance.GetEncoding(System.Globalization.CultureInfo.CurrentCulture.TextInfo.ANSICodePage));

            var files = new FilesUtility(new ConsoleLogProvider(logMonitor));
            string readText;
            try
            {
                readText = files.ReadAllText(testPath);
            }
            finally
            {
                File.Delete(testPath);
            }

            Assert.AreEqual(sampleText, readText);
            TestUtility.AssertContains(
                TestUtility.Dump(log),
                new[] { "invalid UTF-8 character at line 2", "Reading with default system encoding" });
        }

        [TestMethod]
        public void IsSameDirectory()
        {
            var groups = new[]
            {
                new[] { @"C:\", @"C:\.", @"C:\temp\..", @"C:\temp\..\" },
                new[] { @"C:\1", @"C:\1\", @"C:\1\.", @"C:\1\2\..", @"C:\1\2\..\.\", @"C:\2\..\1\" },
                new[] { @"C:\a", @"C:\A", @"c:\a\" },
                new[] { @"", @".", Environment.CurrentDirectory },
                new[] { @"1", @"1\", Path.Combine(Environment.CurrentDirectory, "1") },
            };

            // Test if same within a group:

            foreach (var group in groups)
                for (int p1 = 0; p1 < group.Length; p1++)
                    for (int p2 = p1; p2 < group.Length; p2++)
                        Assert.IsTrue(FilesUtility.IsSameDirectory(group[p1], group[p2]), $"Paths should be same: '{group[p1]}' and '{group[p2]}'.");


            // Test if different between groups:

            for (int g1 = 0; g1 < groups.Length; g1++)
                for (int g2 = g1 + 1; g2 < groups.Length; g2++)
                    foreach (string path1 in groups[g1])
                        foreach (string path2 in groups[g2])
                            Assert.IsFalse(FilesUtility.IsSameDirectory(path1, path2), $"Paths should be different: '{path1}' and '{path2}'.");
        }
    }
}
