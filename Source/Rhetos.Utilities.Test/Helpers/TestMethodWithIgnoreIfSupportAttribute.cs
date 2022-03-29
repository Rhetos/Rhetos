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

using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Rhetos.Utilities.Test.Helpers
{
    /// <summary>
    /// An extension to the [TestMethod] attribute. It walks the method and class hierarchy looking
    /// for an [IgnoreIf] attribute. If one or more are found, they are each evaluated, if the attribute
    /// returns `true`, evaluation is short-circuited, and the test method is skipped.
    /// </summary>
    public class TestMethodWithIgnoreIfSupportAttribute : TestMethodAttribute
    {
        public override TestResult[] Execute(ITestMethod testMethod)
        {
            var ignoreAttributes = FindAttributes(testMethod);

            // Evaluate each attribute, and skip if one returns `true`
            foreach (var ignoreAttribute in ignoreAttributes)
            {
                if (ignoreAttribute.ShouldIgnore(testMethod))
                {
                    var message = $"Test not executed. Conditional ignore method '{ignoreAttribute.IgnoreCriteriaMethodName}' evaluated to 'true'.";
                    return new[]
                    {
                        new TestResult
                        {
                            Outcome = UnitTestOutcome.Inconclusive,
                            TestFailureException = new AssertInconclusiveException(message)
                        }
                    };
                }
            }
            return base.Execute(testMethod);
        }

        private IEnumerable<IgnoreIfAttribute> FindAttributes(ITestMethod testMethod)
        {
            // Look for an [IgnoreIf] on the method, including any virtuals this method overrides
            var ignoreAttributes = new List<IgnoreIfAttribute>();
            ignoreAttributes.AddRange(testMethod.GetAttributes<IgnoreIfAttribute>(inherit: true));

            // Walk the class hierarchy looking for an [IgnoreIf] attribute
            var type = testMethod.MethodInfo.DeclaringType;
            while (type != null)
            {
                ignoreAttributes.AddRange(
                    type.GetCustomAttributes(inherit: true)
                        .OfType<IgnoreIfAttribute>());
                type = type.DeclaringType;
            }
            return ignoreAttributes;
        }
    }
}