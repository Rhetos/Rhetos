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

namespace Rhetos.Utilities.Test
{
    [TestClass]
    public class UserExceptionTest
    {
        [TestMethod]
        public void SafeFormatMessage()
        {
            var tests = new ListOfTuples<Exception, string>
            {
                { new Exception("abc"), "abc" },
                { new UserException("abc"), "abc" },
                { new UserException("abc", null, null, null), "abc" },
                { new UserException("abc", new object[] { 123 }, null, null), "abc" },
                { new UserException("a{0}bc", new object[] { 123, 456 }, null, null), "a123bc" },
                { new UserException("a{1}bc", new object[] { 123 }, null, null), "Invalid error message format. Message: \"a{1}bc\", Parameters: \"123\", FormatException: Index (zero based) must be greater than or equal to zero and less than the size of the argument list." },
                { new UserException("a{0}bc", null, null, null), "Invalid error message format. Message: \"a{0}bc\", Parameters: null, FormatException: Index (zero based) must be greater than or equal to zero and less than the size of the argument list." },
            };

            foreach (var test in tests)
            {
                Console.WriteLine("Test: " + test.Item1.ToString());
                string result = ExceptionsUtility.MessageForLog(test.Item1);
                Assert.AreEqual(test.Item2, result);
            }
        }
    }
}
