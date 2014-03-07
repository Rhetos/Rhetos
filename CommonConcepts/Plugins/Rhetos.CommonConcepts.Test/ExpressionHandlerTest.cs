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
using System.Linq.Expressions;
using System.Text;

namespace Rhetos.CommonConcepts.Test
{
    [TestClass]
    public class ExpressionHandlerTest
    {
        public interface I
        {
            int? i { get; set; }
        }

        public class C : I
        {
            public int? i { get; set; }
            public string s { get; set; }
            public P p { get; set; }
            public override string ToString()
            {
                return string.Join(" ", new object[] { i, s, p != null ? p.ps : null }
                    .Where(o => o != null)
                    .Select(o => o.ToString()));
            }
        }

        public class P
        {
            public string ps { get; set; }
        }

        private ExpressionHelper<TEntityInterface, TProperties> NewExpressionHandler<TEntityInterface, TProperties>(
            Expression<Func<TEntityInterface, TProperties>> propertiesSelector)
        {
            return new ExpressionHelper<TEntityInterface, TProperties>(propertiesSelector);
        }

        [TestMethod]
        public void AssingInterface()
        {
            var eh = NewExpressionHandler((I item) => item.i);

            var c1 = new C { i = 1, s = "a" };
            var c2 = new C { i = 2, s = "b" };

            eh.Assign(c1, c2);
            Assert.AreEqual("2 a", c1.ToString());
            Assert.AreEqual("2 b", c2.ToString());
        }

        [TestMethod]
        public void AssingReference()
        {
            var eh = NewExpressionHandler((C item) => new { item.s, item.p });

            var c1 = new C { i = 1, s = "a" };
            var c2 = new C { i = 2, s = "b", p = new P { ps = "p2" } };
            var c3 = new C { i = 3, s = "c" };

            eh.Assign(c1, c2);
            eh.Assign(c2, c3);
            Assert.AreEqual("1 b p2", c1.ToString());
            Assert.AreEqual("2 c", c2.ToString());
            Assert.AreEqual("3 c", c3.ToString());
        }

        [TestMethod]
        public void AssingError()
        {
            var eh = NewExpressionHandler((C item) => new { item.s, ps = item.p.ps + "x" });

            var c1 = new C { i = 1, s = "a", p = new P { ps = "p1" } };
            var c2 = new C { i = 2, s = "b", p = new P { ps = "p2" } };

            TestUtility.ShouldFail(() => eh.Assign(c1, c2), "Assing function supports only simple property selector", "P.ps");
        }

        [TestMethod]
        public void AssingError2()
        {
            var eh = NewExpressionHandler((C item) => new { item.s, ps = item.p.ps });

            var c1 = new C { i = 1, s = "a", p = new P { ps = "p1" } };
            var c2 = new C { i = 2, s = "b", p = new P { ps = "p2" } };

            TestUtility.ShouldFail(() => eh.Assign(c1, c2), "Assing function supports only simple property selector", "item.p");
        }
    }
}
