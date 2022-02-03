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
using Rhetos.Deployment;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CommonConcepts.Test
{
    [TestClass]
    public class DataMigrationTest
    {
        private DataMigrationScripts ParseDataMigrationScriptsFromScriptsDescription(string scriptsDescription)
        {
            var scripts = scriptsDescription.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s))
                .Select(s =>
                {
                    string downgradeSuffix = ".down";
                    bool hasDown = s.EndsWith(downgradeSuffix);
                    if (hasDown)
                        s = s.Substring(0, s.Length - downgradeSuffix.Length);

                    // Tag is same as Path is not provided otherwise.
                    var tagPath = s.Contains(":") ? s.Split(':') : new[] { s, s };

                    return new DataMigrationScript
                    {
                        Tag = tagPath[0],
                        Path = tagPath[1] + ".sql",
                        Content = $"/*{tagPath[0]}*/\r\nPRINT {SqlUtility.QuoteText(tagPath[1])}",
                        Down = hasDown ? $"/*{tagPath[0]}-DOWN*/\r\nPRINT {SqlUtility.QuoteText(tagPath[1])} + '-DOWN'" : null,
                    };
                })
                .ToList();

            return new DataMigrationScripts { Scripts = scripts };
        }

        private string TestExecuteDataMigrationScripts(string[] scriptsDescriptions, bool skipScriptsWithWrongOrder = false)
        {
            return TestExecuteDataMigrationScripts(scriptsDescriptions, out _, out _, skipScriptsWithWrongOrder);
        }

        private string TestExecuteDataMigrationScripts(string[] scriptsDescriptions, out List<string> log, out List<string> sqlLog, bool skipScriptsWithWrongOrder = false)
        {
            var systemLog = new List<string>();
            log = systemLog;

            var sqlExecuterLog = new SqlExecuterLog();
            sqlLog = sqlExecuterLog;

            using (var scope = TestScope.Create(builder => builder
                .ConfigureOptions<SqlTransactionBatchesOptions>(o => o.ExecuteOnNewConnection = false)
                .ConfigureLogMonitor(systemLog)
                .ConfigureSqlExecuterMonitor(sqlExecuterLog)))
            {
                var sqlExecuter = scope.Resolve<ISqlExecuter>();
                sqlExecuter.ExecuteSql("DELETE FROM Rhetos.DataMigrationScript");

                var sqlBatches = scope.Resolve<ISqlTransactionBatches>();

                int deployment = 0;
                foreach (string scriptsDescription in scriptsDescriptions)
                {
                    sqlExecuter.ExecuteSql($"--DBUpdate: {++deployment}");
                    var dbUpdateOptions = new DbUpdateOptions() { DataMigrationSkipScriptsWithWrongOrder = skipScriptsWithWrongOrder };
                    var dataMigration = new DataMigrationScriptsExecuter(scope.Resolve<ILogProvider>(),
                        ParseDataMigrationScriptsFromScriptsDescription(scriptsDescription), dbUpdateOptions, sqlBatches, sqlExecuter);
                    dataMigration.Execute();
                }

                var report = new List<string>();
                sqlExecuter.ExecuteReader("SELECT Path, Active FROM Rhetos.DataMigrationScript ORDER BY OrderExecuted",
                    reader => report.Add(reader.GetString(0).Replace(".sql", "") + (reader.GetBoolean(1) ? "" : "-")));
                return string.Join(", ", report);
            }
        }

        [TestMethod]
        public void SimpleScripts()
        {
            Assert.AreEqual(
                @"package1\script1, package2\script2",
                TestExecuteDataMigrationScripts(
                    new[]
                    {
                        @"tag1:package1\script1, tag2:package2\script2"
                    }));
        }

        [TestMethod]
        public void DeactiveDeleted()
        {
            Assert.AreEqual(
                @"p1\f1\s1-, p1\f2\s2-, p1\f2\s3, p1\f2\s4-, p2\s5-, p2\s6-",
                TestExecuteDataMigrationScripts(
                    new[]
                    {
                        @"p1\f1\s1,    p1\f2\s2, p1\f2\s3, p1\f2\s4,    p2\s5, p2\s6",
                        @"p1\f2\s3"
                    }));
        }

        [TestMethod]
        public void Reactivate()
        {
            Assert.AreEqual(
                @"p1\f1\s1-, p1\f2\s2-, p1\f2\s4-, p2\s5-, p2\s6-, p1\f2\s3",
                TestExecuteDataMigrationScripts(
                    new[]
                    {
                        @"p1\f1\s1,    p1\f2\s2, p1\f2\s3, p1\f2\s4,    p2\s5, p2\s6",
                        @"",
                        @"p1\f2\s3"
                    }));
        }

        [TestMethod]
        public void ExecuteSkippedScriptsWithWarning()
        {
            Assert.AreEqual(
                @"p1\s1, p1\s3, p2\ss1, p2\ss3, p1\s2, p1\s4, p2\ss2, p2\ss4",
                TestExecuteDataMigrationScripts(
                    new[]
                    {
                        @"p1\s1, p1\s3, p2\ss1, p2\ss3",
                        @"p1\s1, p1\s2, p1\s3, p1\s4, p2\ss1, p2\ss2, p2\ss3, p2\ss4",
                    }, out var log, out _));

            var expectedLog = @"
                [Trace] DataMigration: Script on disk p1\s1.sql (tag p1\s1)
                [Trace] DataMigration: Script on disk p1\s3.sql (tag p1\s3)
                [Trace] DataMigration: Script on disk p2\ss1.sql (tag p2\ss1)
                [Trace] DataMigration: Script on disk p2\ss3.sql (tag p2\ss3)
                [Info] DataMigration: Executing p1\s1.sql (tag p1\s1)
                [Info] DataMigration: Executing p1\s3.sql (tag p1\s3)
                [Info] DataMigration: Executing p2\ss1.sql (tag p2\ss1)
                [Info] DataMigration: Executing p2\ss3.sql (tag p2\ss3)
                [Info] DataMigration: Executed 4 of 4 scripts.
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
                [Warning] DataMigration: Executing script in an incorrect order p1\s2.sql (tag p1\s2)
                [Warning] DataMigration: Executing script in an incorrect order p2\ss2.sql (tag p2\ss2)
                [Info] DataMigration: Executing p1\s2.sql (tag p1\s2)
                [Info] DataMigration: Executing p1\s4.sql (tag p1\s4)
                [Info] DataMigration: Executing p2\ss2.sql (tag p2\ss2)
                [Info] DataMigration: Executing p2\ss4.sql (tag p2\ss4)
                [Info] DataMigration: Executed 4 of 8 scripts.";

            Assert.AreEqual(expectedLog, string.Concat(log
                .Where(l => l.Contains("] DataMigration:"))
                .Select(l => "\r\n                " + l)));
        }

        [TestMethod]
        public void ExecuteDifferentScriptsWithoutWarning()
        {
            Assert.AreEqual(
                @"p1\s3-, p1\s1",
                TestExecuteDataMigrationScripts(
                    new[]
                    {
                        @"p1\s3",
                        @"p1\s1",
                    },
                    out var log, out _));

            var expectedLog = @"
                [Trace] DataMigration: Script on disk p1\s3.sql (tag p1\s3)
                [Info] DataMigration: Executing p1\s3.sql (tag p1\s3)
                [Info] DataMigration: Executed 1 of 1 scripts.
                [Trace] DataMigration: Script on disk p1\s1.sql (tag p1\s1)
                [Trace] DataMigration: Script in database p1\s3.sql (tag p1\s3)
                [Info] DataMigration: Removing p1\s3.sql (tag p1\s3)
                [Info] DataMigration: Executing p1\s1.sql (tag p1\s1)
                [Info] DataMigration: Executed 1 of 1 scripts.";

            Assert.AreEqual(expectedLog, string.Concat(log
                .Where(l => l.Contains("] DataMigration:"))
                .Select(l => "\r\n                " + l)));
        }

        [TestMethod]
        public void DontExecuteSkippedScriptsIfBackwardCompatibilityEnabled()
        {
            Assert.AreEqual(
                @"p1\s1, p1\s3, p2\ss1, p2\ss3, p1\s4, p2\ss4",
                TestExecuteDataMigrationScripts(
                    new[]
                    {
                        @"p1\s1, p1\s3, p2\ss1, p2\ss3",
                        @"p1\s1, p1\s2, p1\s3, p1\s4, p2\ss1, p2\ss2, p2\ss3, p2\ss4",
                    },
                    out var log, out _,
                    skipScriptsWithWrongOrder: true));

            var expectedLog = @"
                [Trace] DataMigration: Script on disk p1\s1.sql (tag p1\s1)
                [Trace] DataMigration: Script on disk p1\s3.sql (tag p1\s3)
                [Trace] DataMigration: Script on disk p2\ss1.sql (tag p2\ss1)
                [Trace] DataMigration: Script on disk p2\ss3.sql (tag p2\ss3)
                [Info] DataMigration: Executing p1\s1.sql (tag p1\s1)
                [Info] DataMigration: Executing p1\s3.sql (tag p1\s3)
                [Info] DataMigration: Executing p2\ss1.sql (tag p2\ss1)
                [Info] DataMigration: Executing p2\ss3.sql (tag p2\ss3)
                [Info] DataMigration: Executed 4 of 4 scripts.
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
                [Warning] DataMigration: Skipped older script p1\s2.sql (tag p1\s2)
                [Warning] DataMigration: Skipped older script p2\ss2.sql (tag p2\ss2)
                [Info] DataMigration: Executing p1\s4.sql (tag p1\s4)
                [Info] DataMigration: Executing p2\ss4.sql (tag p2\ss4)
                [Info] DataMigration: Executed 2 of 8 scripts. 2 older skipped.";

            Assert.AreEqual(expectedLog, string.Concat(log
                .Where(l => l.Contains("] DataMigration:"))
                .Select(l => "\r\n                " + l)));
        }

        [TestMethod]
        public void DowngradeScripts()
        {
            Assert.AreEqual(
                @"p1\s1-, p1\s2-, p2\s1-, p2\s2-, p1\s3-",
                TestExecuteDataMigrationScripts(
                    new[]
                    {
                        @"p1\s1, p1\s2.down, p2\s1.down, p2\s2.down",
                        @"p1\s2.down, p1\s3.down",
                        @"",
                    }, out var log, out var sqlLog));

            var expectedLog = @"
                [Trace] DataMigration: Script on disk p1\s1.sql (tag p1\s1)
                [Trace] DataMigration: Script on disk p1\s2.sql (tag p1\s2, with downgrade)
                [Trace] DataMigration: Script on disk p2\s1.sql (tag p2\s1, with downgrade)
                [Trace] DataMigration: Script on disk p2\s2.sql (tag p2\s2, with downgrade)
                [Info] DataMigration: Executing p1\s1.sql (tag p1\s1)
                [Info] DataMigration: Executing p1\s2.sql (tag p1\s2, with downgrade)
                [Info] DataMigration: Executing p2\s1.sql (tag p2\s1, with downgrade)
                [Info] DataMigration: Executing p2\s2.sql (tag p2\s2, with downgrade)
                [Info] DataMigration: Executed 4 of 4 scripts.

                [Trace] DataMigration: Script on disk p1\s2.sql (tag p1\s2, with downgrade)
                [Trace] DataMigration: Script on disk p1\s3.sql (tag p1\s3, with downgrade)
                [Trace] DataMigration: Script in database p1\s1.sql (tag p1\s1)
                [Trace] DataMigration: Script in database p1\s2.sql (tag p1\s2, with downgrade)
                [Trace] DataMigration: Script in database p2\s1.sql (tag p2\s1, with downgrade)
                [Trace] DataMigration: Script in database p2\s2.sql (tag p2\s2, with downgrade)
                [Trace] DataMigration: Last executed script in 'p1' is 'p1\s2.sql' of new scripts provided.
                [Info] DataMigration: Removing p2\s2.sql (tag p2\s2, with downgrade)
                [Info] DataMigration: Removing p2\s1.sql (tag p2\s1, with downgrade)
                [Info] DataMigration: Removing p1\s1.sql (tag p1\s1)
                [Info] DataMigration: Executing p1\s3.sql (tag p1\s3, with downgrade)
                [Info] DataMigration: Executed 1 of 2 scripts. Executed 2 downgrade scripts.

                [Trace] DataMigration: Script in database p1\s2.sql (tag p1\s2, with downgrade)
                [Trace] DataMigration: Script in database p1\s3.sql (tag p1\s3, with downgrade)
                [Info] DataMigration: Removing p1\s3.sql (tag p1\s3, with downgrade)
                [Info] DataMigration: Removing p1\s2.sql (tag p1\s2, with downgrade)
                [Info] DataMigration: Executed 0 of 0 scripts. Executed 2 downgrade scripts.";

            Assert.AreEqual(
                RemoveIndentation(expectedLog),
                string.Join("\r\n", log.Where(l => l.Contains("] DataMigration:"))));

            var expectedSqlLog = @"
                --DBUpdate: 1
                --Name: p1\s1.sql /*p1\s1*/ PRINT 'p1\s1'
                --Name: p1\s2.sql /*p1\s2*/ PRINT 'p1\s2'
                --Name: p2\s1.sql /*p2\s1*/ PRINT 'p2\s1'
                --Name: p2\s2.sql /*p2\s2*/ PRINT 'p2\s2'

                --DBUpdate: 2
                --Name: p2\s2.sql /*p2\s2-DOWN*/ PRINT 'p2\s2' + '-DOWN'
                --Name: p2\s1.sql /*p2\s1-DOWN*/ PRINT 'p2\s1' + '-DOWN'
                --Name: p1\s3.sql /*p1\s3*/ PRINT 'p1\s3'

                --DBUpdate: 3
                --Name: p1\s3.sql /*p1\s3-DOWN*/ PRINT 'p1\s3' + '-DOWN'
                --Name: p1\s2.sql /*p1\s2-DOWN*/ PRINT 'p1\s2' + '-DOWN'";

            Assert.AreEqual(
                RemoveIndentation(expectedSqlLog),
                string.Join("\r\n", sqlLog
                    .Where(sql => sql.StartsWith("--Name:") || sql.StartsWith("--DBUpdate"))
                    .Select(sql => sql.Replace("\r\n", " ").Replace("\n", " "))));
        }

        private string RemoveIndentation(string expectedLog)
        {
            return string.Join("\r\n",
                expectedLog.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(line => line.Trim())
                    .Where(line => !string.IsNullOrEmpty(line)));
        }

        [TestMethod]
        public void UpdateMetadata1()
        {
            Assert.AreEqual(
                @"p1\s1-",
                TestExecuteDataMigrationScripts(
                    new[]
                    {
                        @"p1\s1",
                        @"p1\s1.down",
                        @"",
                    }, out var log, out var sqlLog));

            var expectedLog = @"
                [Trace] DataMigration: Script on disk p1\s1.sql (tag p1\s1)
                [Info] DataMigration: Executing p1\s1.sql (tag p1\s1)
                [Info] DataMigration: Executed 1 of 1 scripts.

                [Trace] DataMigration: Script on disk p1\s1.sql (tag p1\s1, with downgrade)
                [Trace] DataMigration: Script in database p1\s1.sql (tag p1\s1)
                [Trace] DataMigration: Last executed script in 'p1' is 'p1\s1.sql' of new scripts provided.
                [Info] DataMigration: Updating metadata p1\s1.sql (tag p1\s1, with downgrade)
                [Info] DataMigration: Executed 0 of 1 scripts.

                [Trace] DataMigration: Script in database p1\s1.sql (tag p1\s1, with downgrade)
                [Info] DataMigration: Removing p1\s1.sql (tag p1\s1, with downgrade)
                [Info] DataMigration: Executed 0 of 0 scripts. Executed 1 downgrade scripts.";

            Assert.AreEqual(
                RemoveIndentation(expectedLog),
                string.Join("\r\n", log.Where(l => l.Contains("] DataMigration:"))));

            var expectedSqlLog = @"
                --DBUpdate: 1
                --Name: p1\s1.sql /*p1\s1*/ PRINT 'p1\s1'
                --DBUpdate: 2
                --DBUpdate: 3
                --Name: p1\s1.sql /*p1\s1-DOWN*/ PRINT 'p1\s1' + '-DOWN'";

            Assert.AreEqual(
                RemoveIndentation(expectedSqlLog),
                string.Join("\r\n", sqlLog
                    .Where(sql => sql.StartsWith("--Name:") || sql.StartsWith("--DBUpdate"))
                    .Select(sql => sql.Replace("\r\n", " ").Replace("\n", " "))));
        }

        [TestMethod]
        public void UpdateMetadata2()
        {
            Assert.AreEqual(
                @"p1\s1-",
                TestExecuteDataMigrationScripts(
                    new[]
                    {
                        @"p1\s1.down",
                        @"p1\s1",
                        @"",
                    }, out var log, out var sqlLog));

            var expectedLog = @"
                [Trace] DataMigration: Script on disk p1\s1.sql (tag p1\s1, with downgrade)
                [Info] DataMigration: Executing p1\s1.sql (tag p1\s1, with downgrade)
                [Info] DataMigration: Executed 1 of 1 scripts.

                [Trace] DataMigration: Script on disk p1\s1.sql (tag p1\s1)
                [Trace] DataMigration: Script in database p1\s1.sql (tag p1\s1, with downgrade)
                [Trace] DataMigration: Last executed script in 'p1' is 'p1\s1.sql' of new scripts provided.
                [Info] DataMigration: Updating metadata p1\s1.sql (tag p1\s1)
                [Info] DataMigration: Executed 0 of 1 scripts.

                [Trace] DataMigration: Script in database p1\s1.sql (tag p1\s1)
                [Info] DataMigration: Removing p1\s1.sql (tag p1\s1)
                [Info] DataMigration: Executed 0 of 0 scripts.";

            Assert.AreEqual(
                RemoveIndentation(expectedLog),
                string.Join("\r\n", log.Where(l => l.Contains("] DataMigration:"))));

            var expectedSqlLog = @"
                --DBUpdate: 1
                --Name: p1\s1.sql /*p1\s1*/ PRINT 'p1\s1'
                --DBUpdate: 2
                --DBUpdate: 3";

            Assert.AreEqual(
                RemoveIndentation(expectedSqlLog),
                string.Join("\r\n", sqlLog
                    .Where(sql => sql.StartsWith("--Name:") || sql.StartsWith("--DBUpdate"))
                    .Select(sql => sql.Replace("\r\n", " ").Replace("\n", " "))));
        }
    }
}
