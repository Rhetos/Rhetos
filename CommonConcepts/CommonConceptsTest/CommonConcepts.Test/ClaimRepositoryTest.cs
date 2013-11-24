/*
    Copyright (C) 2013 Omega software d.o.o.

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
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.TestCommon;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Security;

namespace CommonConcepts.Test
{
    [TestClass]
    public class ClaimRepositoryTest
    {
        [TestMethod]
        public void CRUD()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);
                var unitTestClaims = repository.Common.Claim.Query().Where(c => c.ClaimResource.StartsWith("unittest_")).ToList();
                Console.WriteLine("Delete old: " + TestUtility.DumpSorted(unitTestClaims, c => c.ClaimResource + "." + c.ClaimRight) + ".");
                repository.Common.Claim.Delete(unitTestClaims);
                
                IClaimRepository cr = repository.Common.Claim;
                var c1 = new Claim("unittest_c1", "c11");
                var c2 = new Claim("unittest_c2", "c22");
                var c3 = new Claim("unittest_c3", "c33");
                cr.SaveClaims(new[] { c1, c2, c3 }, new ICommonClaim[] {}, new ICommonClaim[] {});

                var loaded = cr.LoadClaims().Where(c => c.ClaimResource.StartsWith("unittest_")).ToList();
                loaded.Sort((cl1, cl2) => cl1.ClaimResource.CompareTo(cl2.ClaimResource));
                Assert.AreEqual("c11, c22, c33", TestUtility.Dump(loaded, c => c.ClaimRight));

                loaded[0].ClaimRight = loaded[0].ClaimRight.ToUpper();
                var c4 = new Claim("unittest_c4", "c44");
                cr.SaveClaims(new[] { c4 }, new[] { loaded[0] }, new[] { loaded[1] });

                loaded = cr.LoadClaims().Where(c => c.ClaimResource.StartsWith("unittest_")).ToList();
                loaded.Sort((cl1, cl2) => cl1.ClaimResource.CompareTo(cl2.ClaimResource));
                Assert.AreEqual("C11, c33, c44", TestUtility.Dump(loaded, c => c.ClaimRight));
            }
        }
    }
}
