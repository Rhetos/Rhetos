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
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.Utilities.Test
{
    [TestClass]
    public class UserExceptionTest
    {
        [TestMethod]
        public void InvalidMessageFormat()
        {
            var tests = new List<(Action Input, string Output)>
            {
                (() => throw new InvalidOperationException("abc"), "abc"),
                (() => throw new UserException("abc"), "abc"),
                (() => throw new UserException("abc", null, null, null), "abc"),
                (() => throw new UserException("abc", new object[] { 123 }, null, null), "abc"),
                (() => throw new UserException("a{0}bc", new object[] { 123, 456 }, null, null), "a123bc"),
                (() => throw new UserException("a{1}bc", new object[] { 123 }, null, null), "Invalid error message format. Message: \"a{1}bc\", Parameters: \"123\". Index (zero based) must be greater than or equal to zero and less than the size of the argument list."),
                (() => throw new UserException("a{0}bc", null, null, null), "Invalid error message format. Message: \"a{0}bc\", Parameters: null. Index (zero based) must be greater than or equal to zero and less than the size of the argument list."),
                (() => throw new UserException(null, new object[] { 123 }, null, null), "Invalid error message format. Message: null, Parameters: \"123\". --> ArgumentNullException"),
                (() => throw new UserException(null, null, null, null), "Exception of type 'Rhetos.UserException' was thrown."), // Standard default message for exceptions.
                (() => throw new UserException(), "Exception of type 'Rhetos.UserException' was thrown."), // Default constructor should always work as expected.
            };

            for (int t = 0; t < tests.Count; t++)
            {
                var test = tests[t];
                try
                {
                    test.Input();
                    Assert.Fail($"Exception expected in test {t}.");
                }
                catch (Exception ex)
                {
                    string report = ExceptionsUtility.MessageForLog(ex);
                    if (ex.InnerException != null)
                        report += " --> " + ex.InnerException.GetType().Name;
                    Assert.AreEqual(test.Output, report, $"Test {t}.");
                }
            }
        }
    }
}
