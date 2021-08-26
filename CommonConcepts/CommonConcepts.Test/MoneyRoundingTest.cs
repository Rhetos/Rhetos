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

using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.TestCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonConcepts.Test
{
    [TestClass]
    public class MoneyRoundingTest
    {
        [TestMethod]
        public void AutoRoundOn()
        {
            using (var scope = TestScope.Create(builder =>
            {
                var options = new CommonConceptsRuntimeOptions() { AutoRoundMoney = true };
                builder.RegisterInstance(options);
            }))
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                var tests = new List<(decimal ValueToWrite, decimal ExpectedPersistedValue)>()
                {
                    (0.001m, 0m),
                    (0.009m, 0m),
                    (0.019m, 0.01m),
                    (-0.001m, 0m),
                    (-0.009m, 0m),
                    (-0.019m, -0.01m),
                };

                foreach (var test in tests)
                {
                    var entity = new TestStorage.AllProperties
                    {
                        ID = Guid.NewGuid(),
                        MoneyProperty = test.ValueToWrite
                    };
                    context.Repository.TestStorage.AllProperties.Save(new[] { entity }, null, null);
                    var actualPersistedValue = context.Repository.TestStorage.AllProperties.Load(x => x.ID == entity.ID).Single().MoneyProperty;

                    Assert.AreEqual(test.ExpectedPersistedValue, actualPersistedValue);
                }
            }
        }

        [TestMethod]
        public void AutoRoundOff_ThowsIfOverflow()
        {
            // AutoRoundMoney option defaults to false to there's no need to override the options
            using (var scope = TestScope.Create())
            {
                var context = scope.Resolve<Common.ExecutionContext>();

                var tests = new[]
                {
                    0.001m,
                    1.1234m,
                    -0.001m,
                    -1.1234m,
                };

                foreach (var test in tests)
                {
                    var entity = new TestStorage.AllProperties
                    {
                        ID = Guid.NewGuid(),
                        MoneyProperty = test
                    };

                    Action save = () => context.Repository.TestStorage.AllProperties.Save(new[] { entity }, null, null);

                    TestUtility.ShouldFail<UserException>(save, "It is not allowed to enter a money value with more than 2 decimals.");
                }
            }
        }
    }
}
