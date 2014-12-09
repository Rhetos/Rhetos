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
    public class ReplaceWithReferenceTest
    {
        class Item 
        {
            public ItemMaster master { get; set; }
            public string name { get; set; }
        }

        class ItemMaster 
        {
            public Parent parent { get; set; }
            public int id { get; set; }
        }

        class Parent 
        {
            public Guid id { get; set; } 
        }

        [TestMethod]
        public void TestTrivial()
        {
            {
                Expression<Func<ItemMaster, bool>> expTrue = a => true;
                var rep = new ReplaceWithReference<ItemMaster, Item>(expTrue, "master", "item").NewExpression;
                Assert.AreEqual("item => True", rep.ToString());
                Assert.AreEqual(typeof(Item), rep.Parameters.Single().Type);
            }

            {
                Expression<Func<ItemMaster, bool>> expFalse = b => false;
                var rep = new ReplaceWithReference<ItemMaster, Item>(expFalse, "master", "b").NewExpression; 
                Assert.AreEqual("b => False", rep.ToString());
            }
            {
                Expression<Func<ItemMaster, bool>> expTaut = bliblo => bliblo == bliblo;
                var rep = new ReplaceWithReference<ItemMaster, Item>(expTaut, "master", "item").NewExpression;
                Assert.AreEqual("item => (item == item)", rep.ToString());
            }
        }

        [TestMethod]
        public void TestInvalid()
        {
            {
                Expression<Func<ItemMaster, bool>> exp = a => a.id > 5;
                TestUtility.ShouldFail(() => new ReplaceWithReference<ItemMaster, Item>(exp, "noreference", "item"));
            }

            {
                Expression<Func<ItemMaster, bool>> exp = a => a.id > 5;
                TestUtility.ShouldFail(() => new ReplaceWithReference<ItemMaster, Item>(exp, "invalid reference format", "item"));
            }
        }

        [TestMethod]
        public void TestNonTrivial()
        {
            {
                Expression<Func<ItemMaster, bool>> exp = a => a.id > 5 && a.id < 10;
                var res = new ReplaceWithReference<ItemMaster, Item>(exp, "master", "item").NewExpression;
                Assert.AreEqual("item => ((item.master.id > 5) AndAlso (item.master.id < 10))", res.ToString());
            }
            
            {
                Expression<Func<ItemMaster, bool>> exp = a => int.Equals(0, a.id + a.id * Math.Abs(a.id - 2 * a.id));
                var res = new ReplaceWithReference<ItemMaster, Item>(exp, "master", "b").NewExpression;
                Assert.AreEqual("b => Equals(Convert(0), Convert((b.master.id + (b.master.id * Abs((b.master.id - (2 * b.master.id)))))))", res.ToString());

                // lets try to execute it
                ItemMaster master = new ItemMaster() { id = 5 };
                Item item = new Item() { master = master };
                var func = res.Compile();
                Assert.AreEqual(false, func(item));
            }

            // parameter same name as member with double nested reference
            {
                Expression<Func<ItemMaster, bool>> exp = parent => parent.parent.id != Guid.Empty && parent.id > 0;
                var res = new ReplaceWithReference<ItemMaster, Item>(exp, "master", "master").NewExpression;
                Assert.AreEqual("master => ((master.master.parent.id != Guid.Empty) AndAlso (master.master.id > 0))", res.ToString());
            }
        }
    }
}
