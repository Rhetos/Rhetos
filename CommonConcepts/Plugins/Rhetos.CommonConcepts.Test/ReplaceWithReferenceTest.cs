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
using System.Text.RegularExpressions;

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
#pragma warning disable CS1718 // Comparison made to same variable
                Expression<Func<ItemMaster, bool>> expTaut = bliblo => bliblo == bliblo;
#pragma warning restore CS1718 // Comparison made to same variable
                var rep = new ReplaceWithReference<ItemMaster, Item>(expTaut, "master", "item").NewExpression;
                Assert.AreEqual("item => (item == item)", rep.ToString());
            }
        }

        [TestMethod]
        public void TestInvalid()
        {
            {
                Expression<Func<ItemMaster, bool>> exp = a => a.id > 5;
                TestUtility.ShouldFail(
                    () => { var rep = new ReplaceWithReference<ItemMaster, Item>(exp, "noreference", "item").NewExpression; },
                    "Item", "noreference");
            }

            {
                Expression<Func<ItemMaster, bool>> exp = a => a.id > 5;
                TestUtility.ShouldFail(
                    () => { var rep = new ReplaceWithReference<ItemMaster, Item>(exp, "invalid reference format", "item").NewExpression; },
                    "Item", "invalid reference format");
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

        [TestMethod]
        public void TestContains()
        {
            {
                var parents = new List<Parent>();
                Expression<Func<ItemMaster, bool>> exp = m => parents.Contains(m.parent);
                Assert.AreEqual("m => value(...).parents.Contains(m.parent)", SimpleReport(exp));

                var rep = new ReplaceWithReference<ItemMaster, Item>(exp, "master", "item").NewExpression;
                Assert.AreEqual("item => value(...).parents.Contains(item.master.parent)", SimpleReport(rep));
            }
        }

        [TestMethod]
        public void TestContainsWithoutMemberAccess()
        {
            {
                var masters = new List<ItemMaster>();
                Expression<Func<ItemMaster, bool>> exp = m => masters.Contains(m);
                Assert.AreEqual("m => value(...).masters.Contains(m)", SimpleReport(exp));

                var rep = new ReplaceWithReference<ItemMaster, Item>(exp, "master", "item").NewExpression;
                Assert.AreEqual("item => value(...).masters.Contains(item.master)", SimpleReport(rep));
            }
            {
                var masters = new List<ItemMaster>().AsQueryable();
                Expression<Func<ItemMaster, bool>> exp = m => masters.Contains(m);
                Assert.AreEqual("m => value(...).masters.Contains(m)", SimpleReport(exp));

                var rep = new ReplaceWithReference<ItemMaster, Item>(exp, "master", "item").NewExpression;
                Assert.AreEqual("item => value(...).masters.Contains(item.master)", SimpleReport(rep));
            }
        }

        [TestMethod]
        public void TestUnrelatedParameters()
        {
            var masters = new List<ItemMaster>();
            Expression<Func<ItemMaster, bool>> exp = m => masters.Contains(m) && masters.Where(m2 => masters.Contains(m2)).Select(m3 => m3.id).Any();
            Assert.AreEqual("m => (value(...).masters.Contains(m) AndAlso value(...).masters.Where(m2 => value(...).masters.Contains(m2)).Select(m3 => m3.id).Any())", SimpleReport(exp));

            var rep = new ReplaceWithReference<ItemMaster, Item>(exp, "master", "item").NewExpression;
            Assert.AreEqual("item => (value(...).masters.Contains(item.master) AndAlso value(...).masters.Where(m2 => value(...).masters.Contains(m2)).Select(m3 => m3.id).Any())", SimpleReport(rep));
        }

        private string SimpleReport(Expression rep)
        {
            return new Regex(@"value\(.*?\)").Replace(rep.ToString(), "value(...)");
        }

        class OptimizeChild
        {
            public int A1 { get; set; }
            public int B1 { get; set; }
            public OptimizeParent Parent { get; set; }
        }

        class OptimizeParent
        {
            public int A2 { get; set; }
            public int B2 { get; set; }
        }

        /// <summary>
        /// Data structure that inherits row permissions may have some properties copied from the base data structure (same property value).
        /// Such property may be used directly in the inherited row permissions, simplifying the generated LINQ (and consequently SQL) query.
        /// </summary>
        [TestMethod]
        public void OptimizedCopiedProperties()
        {
            {
                // Control:
                Expression<Func<OptimizeParent, bool>> exp = p => p.A2 > 1 && p.B2 > 2;
                var res = new ReplaceWithReference<OptimizeParent, OptimizeChild>(exp, "Parent", "c").NewExpression;
                Assert.AreEqual("c => ((c.Parent.A2 > 1) AndAlso (c.Parent.B2 > 2))", res.ToString());
            }
            {
                Expression<Func<OptimizeParent, bool>> exp = p => p.A2 > 1 && p.B2 > 2;
                var res = new ReplaceWithReference<OptimizeParent, OptimizeChild>(exp, "Parent", "c", new Tuple<string, string>[] { Tuple.Create("A1", "A2") }).NewExpression;
                Assert.AreEqual("c => ((c.A1 > 1) AndAlso (c.Parent.B2 > 2))", res.ToString());
            }
        }

        class OptimizeBase
        {
            public int A { get; set; }
            public OptimizeExtension1 Extension1 { get; set; }
        }

        class OptimizeExtension1
        {
            public int B { get; set; }
            public OptimizeBase Base { get; set; }
            public OptimizeExtension2 Extension2 { get; set; }
        }

        class OptimizeExtension2
        {
            public int C { get; set; }
            public OptimizeExtension1 Base { get; set; }
        }

        /// <summary>
        /// If row permissions expression on the base data structure references the extension,
        /// then the inherited expression can directly use members of the extension without referencing "item.Base.Extension...".
        /// </summary>
        [TestMethod]
        public void OptimizedExtension()
        {
            {
                // Control:
                Expression<Func<OptimizeBase, bool>> exp = p => p.A > 1 && p.Extension1.B > 2 && p.Extension1.Extension2.C > 3;

                var res = new ReplaceWithReference<OptimizeBase, OptimizeExtension1>(exp, "Base", "ext1").NewExpression;
                Assert.AreEqual("ext1 => (((ext1.Base.A > 1) AndAlso (ext1.Base.Extension1.B > 2)) AndAlso (ext1.Base.Extension1.Extension2.C > 3))", res.ToString());

                var res2 = new ReplaceWithReference<OptimizeExtension1, OptimizeExtension2>(res, "Base", "ext2").NewExpression;
                Assert.AreEqual("ext2 => (((ext2.Base.Base.A > 1) AndAlso (ext2.Base.Base.Extension1.B > 2)) AndAlso (ext2.Base.Base.Extension1.Extension2.C > 3))", res2.ToString());
            }
            {
                // Test:
                Expression<Func<OptimizeBase, bool>> exp = p => p.A > 1 && p.Extension1.B > 2 && p.Extension1.Extension2.C > 3;

                var res = new ReplaceWithReference<OptimizeBase, OptimizeExtension1>(exp, "Base", "ext1", null, "Extension1").NewExpression;
                Assert.AreEqual("ext1 => (((ext1.Base.A > 1) AndAlso (ext1.B > 2)) AndAlso (ext1.Extension2.C > 3))", res.ToString());

                var res2 = new ReplaceWithReference<OptimizeExtension1, OptimizeExtension2>(res, "Base", "ext2", null, "Extension2").NewExpression;
                Assert.AreEqual("ext2 => (((ext2.Base.Base.A > 1) AndAlso (ext2.Base.B > 2)) AndAlso (ext2.C > 3))", res2.ToString());
            }
        }
    }
}
