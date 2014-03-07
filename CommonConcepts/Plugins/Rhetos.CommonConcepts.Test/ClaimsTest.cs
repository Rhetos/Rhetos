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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using Rhetos.Security;
using Rhetos.Dom.DefaultConcepts;

namespace Rhetos.CommonConcepts.Test
{
    class CC : ICommonClaim
    {
        public CC() { ID = Guid.NewGuid(); }
        public Guid ID { get; set; }
        public string ClaimResource { get; set; }
        public string ClaimRight { get; set; }
        public bool? Active { get; set; }
    }

    [TestClass]
    public class ClaimsTest
    {
        private static Claim c(string claimInfo)
        {
            var parts = claimInfo.Split('/');
            Assert.AreEqual(2, parts.Count());
            return new Claim(parts[0], parts[1]);
        }

        private static Claim[] ca(string claimInfo2)
        {
            var parts = claimInfo2.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Select(p => c(p)).ToArray();
        }

        [TestMethod]
        public void ClaimComparison()
        {
            var equals = new[]
            {
                "a/b a/b",
                "a/b a/B",
                "a/b A/B",
                "a/ A/",
            };

            foreach (var test in equals.Select(t => ca(t)))
                Assert.AreEqual(test[0], test[1]);

            var notEquals = new[]
            {
                "a/b a.b/",
                "a/b.c a.b/c",
                "a/b /a.b",
                "a/b aa/bb",
                "a/ a/b",
            };

            foreach (var test in notEquals.Select(t => ca(t)))
                Assert.AreNotEqual(test[0], test[1]);
        }
    }
}
