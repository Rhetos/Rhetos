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
using Rhetos.Utilities;
using Rhetos.Deployment;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.Deployment.Test
{
    [TestClass()]
    public class DataMigrationTest
    {
        private static List<DataMigrationScript> ParseDms(string text)
        {
            return text.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s))
                .Select(s => { var pair = s.Split(':'); return new DataMigrationScript { Tag = pair[0], Path = pair[1] }; })
                .ToList();
        }

        private static string Dump(IEnumerable<DataMigrationScript> scripts)
        {
            var result = string.Join(", ", scripts.Select(s => s.Tag).OrderBy(x => x));
            Console.WriteLine(result);
            return result;
        }

        [TestMethod]
        [DeploymentItem("Rhetos.Deployment.dll")]
        public void SkipOldScriptsThatAreNotExecuted()
        {
            var oldIndex = new HashSet<string> { "s2", "s4", "s6" };
            var newScripts = new List<DataMigrationScript>(ParseDms(@"s1:1\0001, s2:1\0002, s3:1\0003, s4:2\0005, s5:2\0006, s6:2\0007"));

            DataMigration_Accessor dataMigrationAccessor = new DataMigration_Accessor();
            List<DataMigrationScript> skippedOldUnexecutesScripts = dataMigrationAccessor.SkipOlderScriptsInEachFolder(oldIndex, newScripts);
            Assert.AreEqual("s1, s5", Dump(skippedOldUnexecutesScripts));
        }

        [TestMethod]
        [DeploymentItem("Rhetos.Deployment.dll")]
        public void SkipOldScriptsThatAreNotExecuted_SubfoldersAndNumericSort()
        {
            var oldIndex = new HashSet<string> { "s2", "s5" };
            var newScripts = new List<DataMigrationScript>(ParseDms(@"s1:1\9\c, s2:1\10\b, s3:1\11\a, s4:1\99\c, s5:1\100\b, s6:1\101\a"));

            DataMigration_Accessor dataMigrationAccessor = new DataMigration_Accessor();
            List<DataMigrationScript> skippedOldUnexecutesScripts = dataMigrationAccessor.SkipOlderScriptsInEachFolder(oldIndex, newScripts);
            Assert.AreEqual("s1, s3, s4", Dump(skippedOldUnexecutesScripts));
        }

        private static string GetSkipped(string executedScript, string newScriptsText)
        {
            var oldIndex = new HashSet<string> { executedScript };
            var newScripts = new List<DataMigrationScript>(ParseDms(newScriptsText));

            DataMigration_Accessor dataMigrationAccessor = new DataMigration_Accessor();
            List<DataMigrationScript> skippedOldUnexecutesScripts = dataMigrationAccessor.SkipOlderScriptsInEachFolder(oldIndex, newScripts);
            return Dump(skippedOldUnexecutesScripts);
        }

        [TestMethod]
        [DeploymentItem("Rhetos.Deployment.dll")]
        public void SkipOldScriptsThatAreNotExecuted_ComplexNumeric()
        {
            const string newScripts = @"s1:1\9.9\x, s2:1\9.10\x, s3:1\10.9\x, s4:1\10.10\x, s5:1\11\x, s6:1\11.next\x";

            Assert.AreEqual("s1", GetSkipped("s2", newScripts));
            Assert.AreEqual("s1, s2", GetSkipped("s3", newScripts));
            Assert.AreEqual("s1, s2, s3", GetSkipped("s4", newScripts));
            Assert.AreEqual("s1, s2, s3, s4", GetSkipped("s5", newScripts));
        }
    }
}
