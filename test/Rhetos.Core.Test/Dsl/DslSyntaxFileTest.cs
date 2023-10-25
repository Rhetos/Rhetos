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
using Rhetos.Logging;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.IO;

namespace Rhetos.Dsl.Test
{
    [TestClass]
    public class DslSyntaxFileTest
    {
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            TestWorkingFolder = context.TestResultsDirectory;
        }

        static string TestWorkingFolder;

        [TestMethod]
        public void UnsupportedVersionWarning()
        {
            string dslSyntaxFileContent =
@"{
  ""$id"": ""1"",
  ""Version"": ""11.2"",
  ""RhetosVersion"": ""11.11.11-dev1234"",
  ""ExcessDotInKey"": 1,
  ""DatabaseLanguage"": ""PostgreSQL"",
  ""ConceptTypes"": null
}";

            File.WriteAllText(Path.Combine(TestWorkingFolder, DslSyntaxFile.DslSyntaxFileName), dslSyntaxFileContent);

            var environment = new RhetosBuildEnvironment { CacheFolder = TestWorkingFolder };

            var log = new List<string>();
            void logMonitor(EventType eventType, string eventName, Func<string> message)
                => log.Add($"[{eventType}] {eventName}: {message()}");

            var dslSyntaxFile = new DslSyntaxFile(environment, new ConsoleLogProvider(logMonitor));

            var dslSyntax = dslSyntaxFile.Load();

            Assert.AreEqual("11.2", dslSyntax.Version.ToString());
            Assert.IsTrue(dslSyntax.Version > DslSyntax.CurrentVersion);

            TestUtility.AssertContains(string.Join(Environment.NewLine, log),
                new[] { "newer version", "version 11.2", $"version {DslSyntax.CurrentVersion}" });
        }

        [TestMethod]
        public void OldVersionNotDetected()
        {
            string dslSyntaxFileContent =
@"{
  ""$id"": ""1"",
  ""Versioning123"": ""11.2"",
  ""RhetosVersion"": ""11.11.11-dev1234"",
  ""ExcessDotInKey"": 1,
  ""DatabaseLanguage"": ""PostgreSQL"",
  ""ConceptTypes"": null
}";

            File.WriteAllText(Path.Combine(TestWorkingFolder, DslSyntaxFile.DslSyntaxFileName), dslSyntaxFileContent);

            var environment = new RhetosBuildEnvironment { CacheFolder = TestWorkingFolder };

            var log = new List<string>();
            void logMonitor(EventType eventType, string eventName, Func<string> message)
                => log.Add($"[{eventType}] {eventName}: {message()}");

            var dslSyntaxFile = new DslSyntaxFile(environment, new ConsoleLogProvider(logMonitor));

            var dslSyntax = dslSyntaxFile.Load();

            Assert.AreEqual(null, dslSyntax.Version);

            TestUtility.AssertContains(string.Join(Environment.NewLine, log), "Cannot detect");
        }

        [TestMethod]
        public void OldVersionSupported()
        {
            string dslSyntaxFileContent =
@"{
  ""$id"": ""1"",
  ""Version"": ""4.9.1"",
  ""RhetosVersion"": ""5.0.0-dev1234"",
  ""ExcessDotInKey"": 1,
  ""DatabaseLanguage"": ""PostgreSQL"",
  ""ConceptTypes"": null
}";

            File.WriteAllText(Path.Combine(TestWorkingFolder, DslSyntaxFile.DslSyntaxFileName), dslSyntaxFileContent);

            var environment = new RhetosBuildEnvironment { CacheFolder = TestWorkingFolder };

            var log = new List<string>();
            void logMonitor(EventType eventType, string eventName, Func<string> message)
                => log.Add($"[{eventType}] {eventName}: {message}");

            var dslSyntaxFile = new DslSyntaxFile(environment, new ConsoleLogProvider(logMonitor));

            var dslSyntax = dslSyntaxFile.Load();

            Assert.AreEqual("4.9.1", dslSyntax.Version.ToString());
            Assert.IsTrue(dslSyntax.Version < DslSyntax.CurrentVersion);
        }
    }
}
