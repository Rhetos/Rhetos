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
using System;
using System.Linq;

namespace Rhetos.DatabaseGenerator.Test
{
    [TestClass]
    public class DataMigrationScriptBuilderTest
    {
        [TestMethod]
        public void EmptyDataMigrationScriptsTest()
        {
            var scriptBuilder = new DataMigrationScriptBuilder();

            var dataMigrationScripts = scriptBuilder.GetDataMigrationScripts();
            Assert.AreEqual(0, dataMigrationScripts.BeforeDataMigration.Count());
            Assert.AreEqual(0, dataMigrationScripts.AfterDataMigration.Count());
        }

        [TestMethod]
        public void ScriptOrderTestTest()
        {
            var scriptBuilder = new DataMigrationScriptBuilder();

            var beforeScript1 = "Before script 1" + Environment.NewLine + "Before script 1 line 2";
            var beforeScript2 = "Before script 2";
            scriptBuilder.AddBeforeDataMigrationScript(beforeScript1);
            scriptBuilder.AddBeforeDataMigrationScript(beforeScript2);

            var afterScript1 = "After script 1";
            var afterScript2 = "After script 2";
            scriptBuilder.AddAfterDataMigrationScript(afterScript1);
            scriptBuilder.AddAfterDataMigrationScript(afterScript2);

            var dataMigrationScripts = scriptBuilder.GetDataMigrationScripts();

            var beforeDataMigratinScripts = dataMigrationScripts.BeforeDataMigration.ToList();
            Assert.AreEqual(2, beforeDataMigratinScripts.Count);
            Assert.AreEqual(beforeScript1, beforeDataMigratinScripts[0]);
            Assert.AreEqual(beforeScript2, beforeDataMigratinScripts[1]);

            var afterDataMigratinScripts = dataMigrationScripts.AfterDataMigration.ToList();
            Assert.AreEqual(2, afterDataMigratinScripts.Count);
            Assert.AreEqual(afterScript1, afterDataMigratinScripts[0]);
            Assert.AreEqual(afterScript2, afterDataMigratinScripts[1]);
        }
    }
}
