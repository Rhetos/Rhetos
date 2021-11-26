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
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.TestCommon;
using TestReport;
using Rhetos.Configuration.Autofac;
using Rhetos.Utilities;
using Rhetos.Dom.DefaultConcepts;
using CommonConcepts.Test.Helpers;

namespace CommonConcepts.Test
{
    [TestClass]
    public class PartialRepositoryTest
    {
        [TestMethod]
        public void SimpleFilterA()
        {
            using (var scope = TestScope.Create(builder => builder.ConfigureFakeUser("user123")))
            {
                var repository = scope.Resolve<Common.DomRepository>();
                int claimsCount = repository.Common.Claim.Query().Count();
                Assert.IsTrue(claimsCount > 0, claimsCount.ToString());

                var loaded = repository.TestReading.Simple.Load(new TestReading.FilterA());
                Assert.AreEqual(
                    $"A1 {claimsCount}, A2 user123",
                    TestUtility.DumpSorted(loaded, item => $"{item.Name} {item.Data}"));
            }
        }

        [TestMethod]
        public void SimpleFilterB()
        {
            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();

                var items = new[] { "a", "b", "c" }
                    .Select(name => new TestReading.Simple { Name = name })
                    .ToList();
                repository.TestReading.Simple.Insert(items);

                var loaded = repository.TestReading.Simple.Load(new TestReading.FilterB());
                Assert.AreEqual(
                    "b",
                    TestUtility.DumpSorted(loaded, item => item.Name));
            }
        }

        [TestMethod]
        public void SimpleLoadString()
        {
            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();

                var loaded = repository.TestReading.Simple.Load(new[] { "a", "b", "c" });
                Assert.AreEqual(
                    "a, b, c",
                    TestUtility.DumpSorted(loaded, item => item.Name));
            }
        }

        [TestMethod]
        public void SimpleQueryFilterPrefix()
        {
            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();

                var items = new[] { "a1", "a2", "b1", "b2" }
                    .Select(name => new TestReading.Simple { Name = name })
                    .ToList();
                repository.TestReading.Simple.Insert(items);

                var filters = new[]
                {
                    new FilterCriteria(items.Select(item => item.ID).ToList()),
                    new FilterCriteria(new TestReading.Prefix { Pattern = "A" })
                };

                var loaded = repository.TestReading.Simple.Query(filters).ToSimple().ToList();
                Assert.AreEqual(
                    "a1, a2",
                    TestUtility.DumpSorted(loaded, item => item.Name));
            }
        }
    }
}
