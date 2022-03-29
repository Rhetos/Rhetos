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

using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rhetos.Utilities.Test.Helpers
{
    /// <summary>
    /// An extension to the [Ignore] attribute. Instead of using test lists / test categories to conditionally
    /// skip tests, allow a [TestClass] or [TestMethod] to specify a method to run. If the method returns
    /// `true` the test method will be skipped. The "ignore criteria" method must be `static`, return a single
    /// `bool` value, and not accept any parameters. By default, it is named "IgnoreIf".
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class IgnoreIfAttribute : Attribute
    {
        public string IgnoreCriteriaMethodName { get; }

        public IgnoreIfAttribute(string ignoreCriteriaMethodName = "IgnoreIf")
        {
            IgnoreCriteriaMethodName = ignoreCriteriaMethodName;
        }

        internal bool ShouldIgnore(ITestMethod testMethod)
        {
            try
            {
                // Search for the method specified by name in this class or any parent classes.
                var searchFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.Static;
                var method = testMethod.MethodInfo.DeclaringType.GetMethod(IgnoreCriteriaMethodName, searchFlags);
                return (bool) method.Invoke(null, null);
            }
            catch (Exception e)
            {
                var message = $"Conditional ignore method {IgnoreCriteriaMethodName} not found. Ensure the method is in the same class as the test method, marked as `static`, returns a `bool`, and doesn't accept any parameters.";
                throw new ArgumentException(message, e);
            }
        }
    }
}