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

using Rhetos.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Rhetos.Utilities.Test
{

    [TestClass]
    public class ScriptPositionReportingTest
    {
        [TestMethod]
        public void LineTest()
        {
            Assert.AreEqual(1, ScriptPositionReporting.Line("abc\r\n", 0));
            Assert.AreEqual(1, ScriptPositionReporting.Line("abc", 3));
            Assert.AreEqual(1, ScriptPositionReporting.Line("abc\n", 3));
            Assert.AreEqual(2, ScriptPositionReporting.Line("abc\n", 4));
            Assert.AreEqual(2, ScriptPositionReporting.Line("abc\r\n\r\n\r\n", 5));
            Assert.AreEqual(4, ScriptPositionReporting.Line("abc\r\n\r\n\r\ndef", 9));
            Assert.AreEqual(4, ScriptPositionReporting.Line("abc\r\n\r\n\r\ndef", 12));
        }

        [TestMethod]
        public void ColumnTest()
        {
            Assert.AreEqual(1, ScriptPositionReporting.Column("abc\ndef", 0));
            Assert.AreEqual(2, ScriptPositionReporting.Column("abc\ndef", 1));
            Assert.AreEqual(3, ScriptPositionReporting.Column("abc\ndef", 2));
            Assert.AreEqual(4, ScriptPositionReporting.Column("abc\ndef", 3));
            Assert.AreEqual(1, ScriptPositionReporting.Column("abc\ndef", 4));
            Assert.AreEqual(2, ScriptPositionReporting.Column("abc\ndef", 5));

            Assert.AreEqual(4, ScriptPositionReporting.Column("abc", 3));
            Assert.AreEqual(1, ScriptPositionReporting.Column("abc\n", 4));
            Assert.AreEqual(1, ScriptPositionReporting.Column("abc\r\n\r\n\r\n", 5));
            Assert.AreEqual(1, ScriptPositionReporting.Column("abc\r\n\r\n\r\ndef", 9));
            Assert.AreEqual(4, ScriptPositionReporting.Column("abc\r\n\r\n\r\ndef", 12));
        }

        [TestMethod]
        public void FollowingTextTest()
        {
            Assert.AreEqual("a...", ScriptPositionReporting.FollowingText("abc", 0, 1));
            Assert.AreEqual("bc", ScriptPositionReporting.FollowingText("abc", 1, 2));
            Assert.AreEqual("bc", ScriptPositionReporting.FollowingText("abc", 1, 10));
            Assert.AreEqual("", ScriptPositionReporting.FollowingText("abc", 3, 10));
        }

        [TestMethod]
        public void FollowingTextTest_RemoveSeparators()
        {
            Assert.AreEqual("a b", ScriptPositionReporting.FollowingText("a\r\nb", 0, 5));
            Assert.AreEqual("a bbb...", ScriptPositionReporting.FollowingText("a\r\n\r\nbbbbbbbbbbb", 0, 5));
            Assert.AreEqual("b c", ScriptPositionReporting.FollowingText("a\t b\t c", 3, 5));
        }

        /// <summary>
        ///A test for Position
        ///</summary>
        [TestMethod]
        public void PositionTest()
        {
            Assert.AreEqual(0, ScriptPositionReporting.Position("ab", 1, 1));
            Assert.AreEqual(1, ScriptPositionReporting.Position("ab", 1, 2));
            Assert.AreEqual(2, ScriptPositionReporting.Position("ab", 1, 3)); // end of file

            Assert.AreEqual(0, ScriptPositionReporting.Position("a\nbc", 1, 1));
            Assert.AreEqual(1, ScriptPositionReporting.Position("a\nbc", 1, 2));
            Assert.AreEqual(2, ScriptPositionReporting.Position("a\nbc", 2, 1));
            Assert.AreEqual(3, ScriptPositionReporting.Position("a\nbc", 2, 2));
            Assert.AreEqual(4, ScriptPositionReporting.Position("a\nbc", 2, 3)); // end of file

            Assert.AreEqual(0, ScriptPositionReporting.Position("a\r\nbc", 1, 1));
            Assert.AreEqual(1, ScriptPositionReporting.Position("a\r\nbc", 1, 2));
            Assert.AreEqual(3, ScriptPositionReporting.Position("a\r\nbc", 2, 1));
            Assert.AreEqual(4, ScriptPositionReporting.Position("a\r\nbc", 2, 2));
            Assert.AreEqual(5, ScriptPositionReporting.Position("a\r\nbc", 2, 3)); // end of file

            Assert.AreEqual(0, ScriptPositionReporting.Position("\n\nx\n", 1, 1));
            Assert.AreEqual(1, ScriptPositionReporting.Position("\n\nx\n", 2, 1));
            Assert.AreEqual(2, ScriptPositionReporting.Position("\n\nx\n", 3, 1));
            Assert.AreEqual(4, ScriptPositionReporting.Position("\n\nx\n", 4, 1)); // end of file
        }

        [TestMethod]
        public void FollowingTextTest_LineColumn()
        {
            Assert.AreEqual("ab...", ScriptPositionReporting.FollowingText("abc\r\ndefgh", 1, 1, 2));
            Assert.AreEqual("ef...", ScriptPositionReporting.FollowingText("abc\r\ndefgh", 2, 2, 2));
        }

        [TestMethod]
        public void PreviousTextTest()
        {
            Assert.AreEqual("...c", ScriptPositionReporting.PreviousText("abc", 3, 1));
            Assert.AreEqual("ab", ScriptPositionReporting.PreviousText("abc", 2, 2));
            Assert.AreEqual("ab", ScriptPositionReporting.PreviousText("abc", 2, 10));
            Assert.AreEqual("", ScriptPositionReporting.PreviousText("abc", 0, 10));
        }
    }
}
