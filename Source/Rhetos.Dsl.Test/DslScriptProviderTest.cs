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
    public class DslScriptProviderTest
    {
        class MockDslScriptProvider : DslScriptProvider
        {
            public MockDslScriptProvider() : base(_dslScripts)
            {
            }

            private static readonly DslScript[] _dslScripts = new []
            {
                new DslScript { Name = "name1", Script = "abc" },
                new DslScript { Name = "name2", Script = "123" }
            };
        }

        [TestMethod]
        public void ReportErrorTest()
        {
            var dslScriptProvider = new MockDslScriptProvider();

            TestUtility.ShouldFail(() => dslScriptProvider.ReportError(-1), "Index too low", "out of range", "-1");
            TestUtility.AssertContains(dslScriptProvider.ReportError(0), "before: \"abc\"", "name1");
            TestUtility.AssertContains(dslScriptProvider.ReportError(1), "before: \"bc\"", "name1");
            TestUtility.AssertContains(dslScriptProvider.ReportError(2), "before: \"c\"", "name1");
            TestUtility.AssertContains(dslScriptProvider.ReportError(3), "before: \"\"", "name1");
            TestUtility.ShouldFail(() => dslScriptProvider.ReportError(4), "Invalid position not in any script", "not within a script", "4");
            TestUtility.AssertContains(dslScriptProvider.ReportError(5), "before: \"123\"", "name2");
            TestUtility.AssertContains(dslScriptProvider.ReportError(6), "before: \"23\"", "name2");
            TestUtility.AssertContains(dslScriptProvider.ReportError(7), "before: \"3", "name2");
            TestUtility.AssertContains(dslScriptProvider.ReportError(8), "before: \"\"", "name2");
            TestUtility.ShouldFail(() => dslScriptProvider.ReportError(9), "Index too high", "out of range", "9");
        }
    }
}
