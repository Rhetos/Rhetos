﻿/*
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

namespace Rhetos.Utilities.Test
{
    [TestClass]
    public class NoLocalizerTest
    {
        [TestMethod]
        public void StringFormat()
        {
            ILocalizer testLocalizer = new NoLocalizer();

            // Expecting the same behavior as string.Format().
            Assert.AreEqual("123", testLocalizer[123]);
            Assert.AreEqual("Hello", testLocalizer["Hello"]);
            Assert.AreEqual("Hello, world.", testLocalizer["Hello, {0}.", "world"]);
            Assert.AreEqual("abc123", testLocalizer["{0}{1}{2}", null, "abc", "123"]);
            Assert.AreEqual("123456", testLocalizer["{0}{1}", "123", 456, 789]);
        }

        [TestMethod]
        public void ErrorHandling()
        {
            ILocalizer testLocalizer = new NoLocalizer();

            // Expecting the same behavior as string.Format().
            TestUtility.ShouldFail<NullReferenceException>(() => { _ = testLocalizer[null]; });
            TestUtility.ShouldFail<FormatException>(() => { _ = testLocalizer["{0}{1}", 123]; });
        }
    }
}
