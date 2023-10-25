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
using System.Linq;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.TestCommon;
using Rhetos.Utilities;

namespace CommonConcepts.Test
{
    [TestClass()]
    public class ComputationsUtilityTest
    {
        class SomeEntity
        {
            public string Name;
            public Guid? Reference;
        }

        [TestMethod()]
        public void SortByGivenOrderTest()
        {
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var id3 = Guid.NewGuid();
            var id4 = Guid.NewGuid();

            var expectedOrder = new Guid?[] { id1, id2, id3, id4 };
            var items = new[]
            {
                new SomeEntity { Name = "a", Reference = id3 },
                new SomeEntity { Name = "b", Reference = id2 },
                new SomeEntity { Name = "c", Reference = id1 },
                new SomeEntity { Name = "d", Reference = id2 },
                new SomeEntity { Name = "e", Reference = id4 }
            };

            Graph.SortByGivenOrder(items, expectedOrder, item => item.Reference);
            string result = string.Join(", ", items.Select(item => item.Name));
            const string expectedResult1 = "c, b, d, a, e";
            const string expectedResult2 = "c, d, b, a, e";

            Console.WriteLine("result: " + result);
            Console.WriteLine("expectedResult1: " + expectedResult1);
            Console.WriteLine("expectedResult2: " + expectedResult2);

            Assert.IsTrue(result == expectedResult1 || result == expectedResult2, "Result '" + result + "' is not '" + expectedResult1 + "' nor '" + expectedResult2 + "'.");
        }

        [TestMethod()]
        public void SortByGivenOrderError()
        {
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();

            var expectedOrder = new Guid?[] { id1 };
            var items = new[]
            {
                new SomeEntity { Name = "a", Reference = id1 },
                new SomeEntity { Name = "b", Reference = id2 },
            };

            TestUtility.ShouldFail(() => Graph.SortByGivenOrder(items, expectedOrder, item => item.Reference),
                "SomeEntity", "ComputationsUtilityTest", id2.ToString());
        }
    }
}
