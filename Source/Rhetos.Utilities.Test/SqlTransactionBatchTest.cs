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
using Rhetos.TestCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rhetos.Utilities.Test
{
    [TestClass]
    public class SqlTransactionBatchTest
    {
        [TestMethod]
        public void Batches()
        {
            var tests = new List<Tuple<string[], string>>
            {
                Tuple.Create(new[] { "a", "#a", "a", "a", "a", "#a", "#a" }, "1t, 1n, 3t, 2n"),
                Tuple.Create(new[] { "a" }, "1t"),
                Tuple.Create(new[] { "#a" }, "1n"),
                Tuple.Create(new string[] {}, ""),
                Tuple.Create(new[] { "#" }, ""),
                Tuple.Create(new[] { "a", "a", "a#", "a#a" }, "4t"),
                Tuple.Create(new[] { "#a", "#a" }, "2n"),
                Tuple.Create(new[] { "a", "a", "#", "a", "a" }, "4t"),
                Tuple.Create(new[] { "#a", "#a", "#", "#a", "#a" }, "4n"),
            };

            foreach (var test in tests)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                var batches = SqlTransactionBatch.GroupByTransaction(test.Item1.Select(sql => sql.Replace("#", SqlUtility.NoTransactionTag)));
#pragma warning restore CS0618 // Type or member is obsolete
                string report = TestUtility.Dump(batches, batch => batch.Count + (batch.UseTransacion ? "t" : "n"));
                Assert.AreEqual(test.Item2, report, "Test: " + TestUtility.Dump(test.Item1) + ".");
            }
        }
    }
}
