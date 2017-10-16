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
using System.Linq;
using System.Collections.Generic;
using Rhetos.TestCommon;

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
    }
}
