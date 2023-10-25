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

using System.Data;
using System.Data.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using Rhetos.Utilities;
using Rhetos.DatabaseGenerator;
using Rhetos.TestCommon;

namespace Rhetos.Dsl.Test
{
    [TestClass]
    public class DslScriptTest
    {
        [TestMethod]
        public void ReportPositionTest()
        {
            var dslScript= new DslScript { Name = "name1", Script = "abc", Path = "name1" };

            TestUtility.ShouldFail(() => dslScript.ReportPosition(-1), "out of range", "-1"); // Index too low
            TestUtility.AssertContains(dslScript.ReportPosition(0), "before: \"abc\"", "name1");
            TestUtility.AssertContains(dslScript.ReportPosition(1), "before: \"bc\"", "name1");
            TestUtility.AssertContains(dslScript.ReportPosition(2), "before: \"c\"", "name1");
            TestUtility.AssertContains(dslScript.ReportPosition(3), "before: \"\"", "name1");
            TestUtility.ShouldFail(() => dslScript.ReportPosition(4), "out of range", "4"); // Invalid position not in any script
        }
    }
}
