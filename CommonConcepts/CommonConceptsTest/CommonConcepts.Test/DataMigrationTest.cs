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

using CommonConcepts.Test.Helpers;
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using Rhetos.Configuration.Autofac;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Logging;
using Rhetos.Deployment;

namespace CommonConcepts.Test
{
    [TestClass]
    public class DataMigrationTest
    {
        class SimpleScriptsProvider : IDataMigrationScriptsProvider
        {
            readonly List<DataMigrationScript> _scripts;

            public SimpleScriptsProvider(string scriptsDescription)
            {
                _scripts = scriptsDescription.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s))
                    .Select(s =>
                    {
                        // Tag is same as Path is not provided otherwise.
                        var tagPath = s.Contains(":") ? s.Split(':') : new[] { s, s };

                        return new DataMigrationScript
                        {
                            Tag = tagPath[0],
                            Path = tagPath[1] + ".sql",
                            Content = "/*" + tagPath[0] + "*/\r\nPRINT " + SqlUtility.QuoteText(tagPath[1])
                        };
                    })
                    .ToList();
            }

            public List<DataMigrationScript> Load()
            {
                return _scripts;
            }
        }

        private List<string> TestExecuteDataMigrationScripts(string[] scriptsDescriptions, string expectedResult, bool skipScriptsWithWrongOrder = false)
        {
            using (var container = new RhetosTestContainer())
            {
                var log = new List<string>();
                container.AddLogMonitor(log);

                var sqlExecuter = container.Resolve<ISqlExecuter>();
                sqlExecuter.ExecuteSql("DELETE FROM Rhetos.DataMigrationScript");

                var sqlBatches = container.Resolve<SqlTransactionBatches>();

                foreach (string scriptsDescription in scriptsDescriptions)
                {
                    var scriptsProvider = new SimpleScriptsProvider(scriptsDescription);
                    var deployOptions = new DeployOptions() { DataMigration__SkipScriptsWithWrongOrder = skipScriptsWithWrongOrder };
                    var dataMigration = new DataMigrationScripts(sqlExecuter, container.Resolve<ILogProvider>(), scriptsProvider, deployOptions, sqlBatches);
                    dataMigration.Execute();
                }

                var report = new List<string>();
                sqlExecuter.ExecuteReader("SELECT Path, Active FROM Rhetos.DataMigrationScript ORDER BY OrderExecuted",
                    reader => report.Add(reader.GetString(0).Replace(".sql", "") + (reader.GetBoolean(1) ? "" : "-")));
                Assert.AreEqual(expectedResult, string.Join(", ", report));

                return log;
            }
        }

        private void TestExecuteDataMigrationScripts(string scriptsDescription, string expectedResult)
        {
            TestExecuteDataMigrationScripts(new[] { scriptsDescription }, expectedResult);
        }

        [TestMethod]
        public void SimpleScripts()
        {
            TestExecuteDataMigrationScripts(
                @"tag1:package1\script1, tag2:package2\script2",
                @"package1\script1, package2\script2");
        }

        [TestMethod]
        public void DeactiveDeleted()
        {
            TestExecuteDataMigrationScripts(
                new[]
                {
                    @"p1\f1\s1,    p1\f2\s2, p1\f2\s3, p1\f2\s4,    p2\s5, p2\s6",
                    @"p1\f2\s3"
                },
                @"p1\f1\s1-, p1\f2\s2-, p1\f2\s3, p1\f2\s4-, p2\s5-, p2\s6-");
        }

        [TestMethod]
        public void Reactivate()
        {
            TestExecuteDataMigrationScripts(
                new[]
                {
                    @"p1\f1\s1,    p1\f2\s2, p1\f2\s3, p1\f2\s4,    p2\s5, p2\s6",
                    @"",
                    @"p1\f2\s3"
                },
                @"p1\f1\s1-, p1\f2\s2-, p1\f2\s4-, p2\s5-, p2\s6-, p1\f2\s3");
        }

        [TestMethod]
        public void ExecuteSkippedScriptsWithWarning()
        {
            var log = TestExecuteDataMigrationScripts(
                new[]
                {
                    @"p1\s1, p1\s3, p2\ss1, p2\ss3",
                    @"p1\s1, p1\s2, p1\s3, p1\s4, p2\ss1, p2\ss2, p2\ss3, p2\ss4",
                },
                @"p1\s1, p1\s3, p2\ss1, p2\ss3, p1\s2, p1\s4, p2\ss2, p2\ss4");

            var expectedLog = @"
                [Trace] DataMigration: Script on disk p1\s1.sql (tag p1\s1)
                [Trace] DataMigration: Script on disk p1\s3.sql (tag p1\s3)
                [Trace] DataMigration: Script on disk p2\ss1.sql (tag p2\ss1)
                [Trace] DataMigration: Script on disk p2\ss3.sql (tag p2\ss3)
                [Info] DataMigration: Execute p1\s1.sql (tag p1\s1)
                [Info] DataMigration: Execute p1\s3.sql (tag p1\s3)
                [Info] DataMigration: Execute p2\ss1.sql (tag p2\ss1)
                [Info] DataMigration: Execute p2\ss3.sql (tag p2\ss3)
                [Trace] DataMigration: Script on disk p1\s1.sql (tag p1\s1)
                [Trace] DataMigration: Script on disk p1\s2.sql (tag p1\s2)
                [Trace] DataMigration: Script on disk p1\s3.sql (tag p1\s3)
                [Trace] DataMigration: Script on disk p1\s4.sql (tag p1\s4)
                [Trace] DataMigration: Script on disk p2\ss1.sql (tag p2\ss1)
                [Trace] DataMigration: Script on disk p2\ss2.sql (tag p2\ss2)
                [Trace] DataMigration: Script on disk p2\ss3.sql (tag p2\ss3)
                [Trace] DataMigration: Script on disk p2\ss4.sql (tag p2\ss4)
                [Trace] DataMigration: Script in database p1\s1.sql (tag p1\s1)
                [Trace] DataMigration: Script in database p1\s3.sql (tag p1\s3)
                [Trace] DataMigration: Script in database p2\ss1.sql (tag p2\ss1)
                [Trace] DataMigration: Script in database p2\ss3.sql (tag p2\ss3)
                [Trace] DataMigration: Last executed script in 'p1' is 'p1\s3.sql' of new scripts provided.
                [Trace] DataMigration: Last executed script in 'p2' is 'p2\ss3.sql' of new scripts provided.
                [Info] DataMigration: Executing script in an incorrect order p1\s2.sql (tag p1\s2)
                [Info] DataMigration: Executing script in an incorrect order p2\ss2.sql (tag p2\ss2)
                [Info] DataMigration: Execute p1\s2.sql (tag p1\s2)
                [Info] DataMigration: Execute p1\s4.sql (tag p1\s4)
                [Info] DataMigration: Execute p2\ss2.sql (tag p2\ss2)
                [Info] DataMigration: Execute p2\ss4.sql (tag p2\ss4)";

            Assert.AreEqual(expectedLog, string.Concat(log
                .Where(l => l.Contains("] DataMigration:"))
                .Select(l => "\r\n                " + l)));
        }

        [TestMethod]
        public void ExecuteDifferentScriptsWithoutWarning()
        {
            var log = TestExecuteDataMigrationScripts(
                new[]
                {
                    @"p1\s3",
                    @"p1\s1",
                },
                @"p1\s3-, p1\s1");

            var expectedLog = @"
                [Trace] DataMigration: Script on disk p1\s3.sql (tag p1\s3)
                [Info] DataMigration: Execute p1\s3.sql (tag p1\s3)
                [Trace] DataMigration: Script on disk p1\s1.sql (tag p1\s1)
                [Trace] DataMigration: Script in database p1\s3.sql (tag p1\s3)
                [Info] DataMigration: Remove p1\s3.sql (tag p1\s3)
                [Info] DataMigration: Execute p1\s1.sql (tag p1\s1)";

            Assert.AreEqual(expectedLog, string.Concat(log
                .Where(l => l.Contains("] DataMigration:"))
                .Select(l => "\r\n                " + l)));
        }

        [TestMethod]
        public void DontExecuteSkippedScriptsIfBackwardCompatibilityEnabled()
        {
            var log = TestExecuteDataMigrationScripts(
                new[]
                {
                    @"p1\s1, p1\s3, p2\ss1, p2\ss3",
                    @"p1\s1, p1\s2, p1\s3, p1\s4, p2\ss1, p2\ss2, p2\ss3, p2\ss4",
                },
                @"p1\s1, p1\s3, p2\ss1, p2\ss3, p1\s4, p2\ss4",
                skipScriptsWithWrongOrder: true);

            var expectedLog = @"
                [Trace] DataMigration: Script on disk p1\s1.sql (tag p1\s1)
                [Trace] DataMigration: Script on disk p1\s3.sql (tag p1\s3)
                [Trace] DataMigration: Script on disk p2\ss1.sql (tag p2\ss1)
                [Trace] DataMigration: Script on disk p2\ss3.sql (tag p2\ss3)
                [Info] DataMigration: Execute p1\s1.sql (tag p1\s1)
                [Info] DataMigration: Execute p1\s3.sql (tag p1\s3)
                [Info] DataMigration: Execute p2\ss1.sql (tag p2\ss1)
                [Info] DataMigration: Execute p2\ss3.sql (tag p2\ss3)
                [Trace] DataMigration: Script on disk p1\s1.sql (tag p1\s1)
                [Trace] DataMigration: Script on disk p1\s2.sql (tag p1\s2)
                [Trace] DataMigration: Script on disk p1\s3.sql (tag p1\s3)
                [Trace] DataMigration: Script on disk p1\s4.sql (tag p1\s4)
                [Trace] DataMigration: Script on disk p2\ss1.sql (tag p2\ss1)
                [Trace] DataMigration: Script on disk p2\ss2.sql (tag p2\ss2)
                [Trace] DataMigration: Script on disk p2\ss3.sql (tag p2\ss3)
                [Trace] DataMigration: Script on disk p2\ss4.sql (tag p2\ss4)
                [Trace] DataMigration: Script in database p1\s1.sql (tag p1\s1)
                [Trace] DataMigration: Script in database p1\s3.sql (tag p1\s3)
                [Trace] DataMigration: Script in database p2\ss1.sql (tag p2\ss1)
                [Trace] DataMigration: Script in database p2\ss3.sql (tag p2\ss3)
                [Trace] DataMigration: Last executed script in 'p1' is 'p1\s3.sql' of new scripts provided.
                [Trace] DataMigration: Last executed script in 'p2' is 'p2\ss3.sql' of new scripts provided.
                [Info] DataMigration: Skipped older script p1\s2.sql (tag p1\s2)
                [Info] DataMigration: Skipped older script p2\ss2.sql (tag p2\ss2)
                [Info] DataMigration: Execute p1\s4.sql (tag p1\s4)
                [Info] DataMigration: Execute p2\ss4.sql (tag p2\ss4)";

            Assert.AreEqual(expectedLog, string.Concat(log
                .Where(l => l.Contains("] DataMigration:"))
                .Select(l => "\r\n                " + l)));
        }
    }
}
