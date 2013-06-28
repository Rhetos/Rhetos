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
using Rhetos.Dom.DefaultConcepts;
using Rhetos.TestCommon;

namespace CommonConcepts.Test
{
    [TestClass]
    public class ComputedTest
    {
        [TestMethod]
        public void Read()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);

                {
                    var loaded = repository.TestComputed.Simple.All();
                    Assert.AreEqual("a, b", TestUtility.DumpSorted(loaded, item => item.Name));
                }

                {
                    var loaded = repository.TestComputed.Simple.Filter(new FilterAll());
                    Assert.AreEqual("a, b", TestUtility.DumpSorted(loaded, item => item.Name));
                }
            }
        }

        [TestMethod]
        public void SpecialLoad()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);

                var paremeter = new TestComputed.SpecialLoad { SpecialName = "spec" };
                var loaded = repository.TestComputed.Simple.Filter(paremeter);
                Assert.AreEqual("spec", TestUtility.DumpSorted(loaded, item => item.Name));
            }
        }
    }
}
