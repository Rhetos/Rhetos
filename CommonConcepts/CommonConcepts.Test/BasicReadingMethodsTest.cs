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
using Rhetos.Dom.DefaultConcepts;
using Rhetos.TestCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using TestReading.Repositories;

namespace CommonConcepts.Test
{
    [TestClass]
    public class BasicReadingMethodsTest
    {
        [TestMethod]
        public void DirectReadMethodsCall()
        {
            var tests = new Func<Basic_Repository, IEnumerable<TestReading.Basic>>[]
            {
                // All these methods are direct implementations of the DSL features, not generic methods that would find the matching implementations.
                r => r.Load(new TestReading.ParameterLoadPrototype { Pattern = "A" }),
                r => r.Load(new TestReading.ParameterLoadExpression { Pattern = "A" }),
                r => r.Query(new TestReading.ParameterQueryPrototype { Pattern = "A" }),
                r => r.Query(new TestReading.ParameterQueryExpression { Pattern = "A" }),
                r => r.Filter(r.Load(), new TestReading.ParameterFilterPrototype { Pattern = "A" }),
                r => r.Filter(r.Load(), new TestReading.ParameterFilterExpression { Pattern = "A" }),
                r => r.Filter(r.Query(), new TestReading.ParameterQueryFilterPrototype { Pattern = "A" }),
                r => r.Filter(r.Query(), new TestReading.ParameterQueryFilterExpression { Pattern = "A" }),
            };

            using (var scope = TestScope.Create())
            {
                var items = new[]
                {
                    new TestReading.Basic { Name = "A1" },
                    new TestReading.Basic { Name = "A2" },
                    new TestReading.Basic { Name = "B1" },
                    new TestReading.Basic { Name = "B2" },
                };

                var repository = scope.Resolve<Common.DomRepository>();
                repository.TestReading.Basic.Insert(items);

                for (int t = 0; t < tests.Length; t++)
                {
                    var readItems = tests[t].Invoke(repository.TestReading.Basic);

                    var report = string.Join(", ",
                        readItems.Select(item => item.Name).OrderBy(x => x));
                    Assert.AreEqual("A1, A2", report, $"Test {t}.");
                }
            }
        }

        [TestMethod]
        public void GenericReadMethods()
        {
            var tests = new object[]
            {
                // Generic read method should find the matching read method implementations by parameter type.
                new TestReading.ParameterLoadPrototype { Pattern = "A" },
                new TestReading.ParameterLoadExpression { Pattern = "A" },
                new TestReading.ParameterQueryPrototype { Pattern = "A" },
                new TestReading.ParameterQueryExpression { Pattern = "A" },
                new TestReading.ParameterFilterPrototype { Pattern = "A" },
                new TestReading.ParameterFilterExpression { Pattern = "A" },
                new TestReading.ParameterQueryFilterPrototype { Pattern = "A" },
                new TestReading.ParameterQueryFilterExpression { Pattern = "A" },
            };

            using (var scope = TestScope.Create())
            {
                var items = new[]
                {
                    new TestReading.Basic { Name = "A1" },
                    new TestReading.Basic { Name = "A2" },
                    new TestReading.Basic { Name = "B1" },
                    new TestReading.Basic { Name = "B2" },
                };

                var repository = scope.Resolve<Common.DomRepository>();
                repository.TestReading.Basic.Insert(items);

                for (int t = 0; t < tests.Length; t++)
                {
                    var readParameter = tests[t];

                    foreach (bool preferQuery in new[] { false, true })
                    {
                        var readItems = repository.TestReading.Basic.Read(readParameter, readParameter.GetType(), preferQuery);

                        var report = string.Join(", ",
                            readItems.Select(item => item.Name).OrderBy(x => x));
                        Assert.AreEqual("A1, A2", report, $"Test {t}, preferQuery {preferQuery}.");
                    }
                }
            }
        }
    }
}
