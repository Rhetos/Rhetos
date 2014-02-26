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

        private static CC cc(string claimInfo)
        {
            var parts = claimInfo.Split('/');
            Assert.AreEqual(2, parts.Count());
            return new CC { ClaimResource = parts[0], ClaimRight = parts[1] };
        }

        private static CC[] cca(string claimInfo2)
        {
            var parts = claimInfo2.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Select(p => cc(p)).ToArray();
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

        [TestMethod]
        public void ClaimGeneratorDiff()
        {
            TestDiff("c/d e/f g/h", "a/b c/d E/F", "a/b", "E/F", "g/h");
            TestUtility.ShouldFail(() => TestDiff("", "a.b/c a/b.c"), "DslSyntaxException", "Multiple");
            TestUtility.ShouldFail(() => TestDiff("", "a.b/c A/B.C"), "DslSyntaxException", "Multiple");
            TestUtility.ShouldFail(() => TestDiff("", "a.b.c/ A.B/C."), "DslSyntaxException", "Multiple");
            TestDiff("a.b/c a/b.c", "", "", "", "a.b/c, a/b.c");
            TestDiff("a.b/c a/b.c", "A.B/C", "", "A.B/C", "a/b.c");
        }

        private static void TestDiff(string oldClaimsInfo, string newClaimsInfo,
            string insertExpected = null, string updateExpected = null, string deleteExpected = null)
        {
            Console.WriteLine("Old claims: " + oldClaimsInfo);
            Console.WriteLine("New claims: " + newClaimsInfo);
            IList<ICommonClaim> oldClaims = cca(oldClaimsInfo);
            IList<Claim> newClaims = ca(newClaimsInfo);

            IList<Claim> insert;
            IList<ICommonClaim> update, delete;
            new ClaimGeneratorAccessor().DiffClaims(oldClaims, newClaims, out insert, out update, out delete);

            if (insertExpected != null || updateExpected != null || deleteExpected != null)
            {
                Assert.AreEqual(insertExpected, TestUtility.DumpSorted(insert, i => i.Resource + "/" + i.Right), "Inserted");
                Assert.AreEqual(updateExpected, TestUtility.DumpSorted(update, i => i.ClaimResource + "/" + i.ClaimRight), "Updated");
                Assert.AreEqual(deleteExpected, TestUtility.DumpSorted(delete, i => i.ClaimResource + "/" + i.ClaimRight), "Deleted");
            }
        }
    }

    internal class ClaimGeneratorAccessor : ClaimGenerator
    {
        public ClaimGeneratorAccessor()
            : base(null, null, new ConsoleLogProvider(), null)
        {
        }

        new public void DiffClaims(IList<ICommonClaim> oldClaims, IList<Claim> newClaims, out IList<Claim> insert, out IList<ICommonClaim> update, out IList<ICommonClaim> delete)
        {
            base.DiffClaims(oldClaims, newClaims, out insert, out update, out delete);
        }
    }
}
