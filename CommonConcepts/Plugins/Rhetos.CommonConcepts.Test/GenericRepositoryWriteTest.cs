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
using Rhetos.Dom;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Rhetos.CommonConcepts.Test.Mocks;
using System.Linq.Expressions;

namespace Rhetos.CommonConcepts.Test
{
    [TestClass]
    public class GenericRepositoryWriteTest
    {
        interface ISimpleEntity : IEntity
        {
            string Name { get; set; }
            decimal? Size { get; set; }
            ParentEntity Parent { get; set; }
        }

        class SimpleEntity : ISimpleEntity
        {
            public Guid ID { get; set; }
            public string Name { get; set; }
            public decimal? Size { get; set; }
            public ParentEntity Parent { get; set; }
            public override string ToString()
            {
                return
                    (Name != null ? Name : "<null>")
                    + (Size != null ? " " + Size.Value.ToString(CultureInfo.InvariantCulture) : "")
                    + (Parent != null && Parent.Name != null ? " " + Parent.Name : "");
            }
        }

        class ParentEntity : IEntity
        {
            public Guid ID { get; set; }
            public string Name { get; set; }
        }

        static Guid Id(int i)
        {
            return new Guid((uint)i, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        }

        static TestGenericRepository<ISimpleEntity, SimpleEntity> NewSimpleRepos(IEnumerable<SimpleEntity> items = null)
        {
            return new TestGenericRepository<ISimpleEntity, SimpleEntity>(items);
        }


        // ================================================================================

        [TestMethod]
        public void Save()
        {
            var genericRepository = NewSimpleRepos();
            var simpleEntityRepository = genericRepository.RepositoryMock;

            int queryCount = 0;

            var insertArray = new SimpleEntity[] { new SimpleEntity { ID = Guid.NewGuid(), Name = "ins" } };
            var updateList = new List<SimpleEntity> { new SimpleEntity { ID = Guid.NewGuid(), Name = "upd" } };
            var deleteQuery = new[] { new SimpleEntity { ID = Guid.NewGuid(), Name = "del" } }.Select(item => { queryCount++; return item; });

            Assert.AreEqual(0, queryCount);

            genericRepository.Save(insertArray, updateList, deleteQuery);

            Assert.AreSame(insertArray, simpleEntityRepository.InsertedGroups.Single(), "Performance issue: No array copying should have happened.");
            Assert.AreSame(updateList, simpleEntityRepository.UpdatedGroups.Single(), "Performance issue: No list copying should have happened.");
            Assert.AreEqual(1, queryCount, "Performance issue: Provided LINQ query should have been evaluated (and materialized) only once.");
        }

        [TestMethod]
        public void InsertOrReadId_KeyName()
        {
            var oldItems = new[] {
                new SimpleEntity { ID = Id(1), Name = "n1", Size = 1 },
                new SimpleEntity { ID = Id(2), Name = "n2", Size = 2 },
                new SimpleEntity { ID = Id(3), Name = "n3" },
                new SimpleEntity { ID = Id(4) },
            };

            var newItems = new[] {
                new SimpleEntity { ID = Id(101), Name = "n2", Size = 2.5m },
                new SimpleEntity { ID = Id(102), Name = "n4", Size = 4 },
                new SimpleEntity { ID = Id(103), Name = "n5" },
            };

            var repos = NewSimpleRepos(oldItems);
            repos.InsertOrReadId(newItems, item => item.Name);

            Assert.AreEqual("INSERT n4 4, INSERT n5", repos.RepositoryMock.Log.ToString());
            Assert.AreEqual(2.5m, newItems[0].Size);
            Assert.AreEqual(Id(2), newItems[0].ID);
            Assert.AreEqual(Id(102), newItems[1].ID);
            Assert.AreEqual(Id(103), newItems[2].ID);
        }

        [TestMethod]
        public void InsertOrReadId_KeyNewNull()
        {
            var oldItems = new[] {
                new SimpleEntity { ID = Guid.NewGuid(), Name = "a", Size = 1 },
            };

            var newItems = new[] {
                new SimpleEntity { ID = Id(1), Name = null, Size = 2.5m },
                new SimpleEntity { ID = Guid.NewGuid(), Name = "b", Size = 3 },
            };

            var repos = NewSimpleRepos(oldItems);
            repos.InsertOrReadId(newItems, item => item.Name);

            Assert.AreEqual("INSERT <null> 2.5, INSERT b 3", repos.RepositoryMock.Log.ToString());
            Assert.AreEqual(Id(1), newItems[0].ID);
        }

        [TestMethod]
        public void InsertOrReadId_KeyBothNull()
        {
            var oldItems = new[] {
                new SimpleEntity { ID = Guid.NewGuid(), Name = "a", Size = 1 },
                new SimpleEntity { ID = Guid.NewGuid(), Name = null, Size = 2 },
            };

            var newItems = new[] {
                new SimpleEntity { ID = Guid.NewGuid(), Name = null, Size = 2.5m },
                new SimpleEntity { ID = Guid.NewGuid(), Name = "b", Size = 3 },
            };

            var repos = NewSimpleRepos(oldItems);
            repos.InsertOrReadId(newItems, item => item.Name);

            Assert.AreEqual("INSERT b 3", repos.RepositoryMock.Log.ToString());
            Assert.AreEqual(oldItems[1].ID, newItems[0].ID);
        }

        [TestMethod]
        public void InsertOrReadId_KeyId()
        {
            var oldItems = new[] {
                new SimpleEntity { ID = Guid.NewGuid(), Name = "o0" },
                new SimpleEntity { ID = Guid.NewGuid(), Name = "o1" },
            };

            var newItems = new[] {
                new SimpleEntity { ID = oldItems[0].ID, Name = "n0" },
                new SimpleEntity { ID = Id(1), Name = "n1" },
                new SimpleEntity { ID = default(Guid), Name = "n2" },
            };

            var repos = NewSimpleRepos(oldItems);
            repos.InsertOrReadId(newItems, item => item.ID);

            Assert.AreEqual("INSERT n1, INSERT n2", repos.RepositoryMock.Log.ToString());
            Assert.AreEqual(Id(1), newItems[1].ID);
        }

        [TestMethod]
        public void InsertOrReadId_DuplicateKey()
        {
            var oldItems = new[] {
                new SimpleEntity { ID = Id(1), Name = "a" },
                new SimpleEntity { ID = Id(2), Name = "b" },
                new SimpleEntity { ID = Id(3), Name = "a" },
            };

            var newItems = new[] {
                new SimpleEntity { ID = Id(101), Name = "a" },
                new SimpleEntity { ID = Id(102), Name = "b"},
                new SimpleEntity { ID = Id(103), Name = "c"},
            };

            var repos = NewSimpleRepos(oldItems);
            repos.InsertOrReadId(newItems, item => item.Name);

            Assert.AreEqual("INSERT c", repos.RepositoryMock.Log.ToString());
            Assert.IsTrue(new[] { Id(1), Id(3) }.Contains(newItems[0].ID));
            Assert.AreEqual(Id(2), newItems[1].ID);
            Assert.AreEqual(Id(103), newItems[2].ID);
        }


        [TestMethod]
        public void InsertOrReadId_MultipleKey()
        {
            var oldItems = new[] {
                new SimpleEntity { ID = Id(1), Name = "a", Size = 1 },
                new SimpleEntity { ID = Id(2), Name = "b", Size = 2 },
                new SimpleEntity { ID = Id(3), Name = "b", Size = 1 },
            };

            var newItems = new[] {
                new SimpleEntity { ID = Id(4), Name = "a", Size = 3 },
                new SimpleEntity { ID = Id(5), Name = "c", Size = 1 },
                new SimpleEntity { ID = Id(6), Name = "a", Size = 1 },
            };

            var repos = NewSimpleRepos(oldItems);
            repos.InsertOrReadId(newItems, item => new { item.Name, item.Size });

            Assert.AreEqual("INSERT a 3, INSERT c 1", repos.RepositoryMock.Log.ToString());
            Assert.AreEqual(Id(4), newItems[0].ID);
            Assert.AreEqual(Id(5), newItems[1].ID);
            Assert.AreEqual(Id(1), newItems[2].ID);
        }

        [TestMethod]
        public void InsertOrReadId_MultipleKeyNulls()
        {
            var oldItems = new[] {
                new SimpleEntity { ID = Id(1), Name = "a", Size = null },
                new SimpleEntity { ID = Id(2), Name = null, Size = 1 },
                new SimpleEntity { ID = Id(3), Name = null, Size = null },
            };

            var newItems = new[] {
                new SimpleEntity { ID = Id(4), Name = "a", Size = 3 },
                new SimpleEntity { ID = Id(5), Name = "c", Size = 1 },
                new SimpleEntity { ID = Id(6), Name = null, Size = null },
            };

            var repos = NewSimpleRepos(oldItems);
            repos.InsertOrReadId(newItems, item => new { item.Name, item.Size });

            Assert.AreEqual("INSERT a 3, INSERT c 1", repos.RepositoryMock.Log.ToString());
            Assert.AreEqual(Id(4), newItems[0].ID);
            Assert.AreEqual(Id(5), newItems[1].ID);
            Assert.AreEqual(Id(3), newItems[2].ID);
        }

        [TestMethod]
        public void InsertOrReadId_Reference()
        {
            var oldItems = new[] {
                new SimpleEntity { ID = Id(1), Name = "a", Parent = new ParentEntity { Name = "p1" } },
                new SimpleEntity { ID = Id(2), Name = "b", Parent = new ParentEntity {} },
                new SimpleEntity { ID = Id(3), Name = "c", Parent = new ParentEntity { Name = "p1" } },
                new SimpleEntity { ID = Id(4), Name = "d", Parent = new ParentEntity { Name = "p2" } },
            };

            var newItems = new[] {
                new SimpleEntity { ID = Id(101), Name = "a2", Parent = new ParentEntity { Name = "p1" } },
                new SimpleEntity { ID = Id(102), Name = "b2", Parent = new ParentEntity {} },
                new SimpleEntity { ID = Id(103), Name = "e2", Parent = new ParentEntity { Name = "p3" } },
            };

            var repos = NewSimpleRepos(oldItems);
            repos.InsertOrReadId(newItems, item => item.Parent.Name);

            Assert.AreEqual("INSERT e2 p3", repos.RepositoryMock.Log.ToString());
            Assert.IsTrue(new[] { Id(1), Id(3) }.Contains(newItems[0].ID));
            Assert.AreEqual(Id(2), newItems[1].ID);
            Assert.AreEqual(Id(103), newItems[2].ID);
        }

        [TestMethod]
        public void InsertOrReadId_ComplexReference()
        {
            var oldItems = new[] {
                new SimpleEntity { ID = Id(1), Name = "a", Parent = new ParentEntity { Name = "p1" } },
                new SimpleEntity { ID = Id(2), Name = "b", Parent = new ParentEntity {} },
                new SimpleEntity { ID = Id(3), Name = "c", Parent = new ParentEntity { Name = "p1" } },
                new SimpleEntity { ID = Id(4), Name = "d", Parent = new ParentEntity { Name = "p2" } },
            };

            var newItems = new[] {
                new SimpleEntity { ID = Id(101), Name = "c", Parent = new ParentEntity { Name = "p1" } },
                new SimpleEntity { ID = Id(102), Name = "b", Parent = new ParentEntity {} },
                new SimpleEntity { ID = Id(103), Name = "e", Parent = new ParentEntity { Name = "p2" } },
                new SimpleEntity { ID = Id(104), Name = "b", Parent = new ParentEntity { Name = "p2" } },
            };

            var repos = NewSimpleRepos(oldItems);
            repos.InsertOrReadId(newItems, item => new { item.Name, ParentName = item.Parent.Name });

            Assert.AreEqual("INSERT e p2, INSERT b p2", repos.RepositoryMock.Log.ToString());
            Assert.AreEqual(Id(3), newItems[0].ID);
            Assert.AreEqual(Id(2), newItems[1].ID);
            Assert.AreEqual(Id(103), newItems[2].ID);
            Assert.AreEqual(Id(104), newItems[3].ID);
        }

        [TestMethod]
        public void InsertOrReadId_NullReference()
        {
            var oldItems = new[] {
                new SimpleEntity { ID = Id(1), Name = "a", Parent = new ParentEntity { Name = "p1" } },
                new SimpleEntity { ID = Id(2), Name = "b", Parent = null },
                new SimpleEntity { ID = Id(3), Name = "c", Parent = new ParentEntity { Name = null } },
            };

            var newItems = new[] {
                new SimpleEntity { ID = Id(101), Name = "a", Parent = null },
                new SimpleEntity { ID = Id(102), Name = "b", Parent = null },
                new SimpleEntity { ID = Id(103), Name = "c", Parent = null },
            };

            var repos = NewSimpleRepos(oldItems);
            repos.InsertOrReadId(newItems, item => new { item.Name, ParentName = item.Parent != null ? item.Parent.Name : null });

            Assert.AreEqual("INSERT a", repos.RepositoryMock.Log.ToString());
            Assert.AreEqual(Id(101), newItems[0].ID);
            Assert.AreEqual(Id(2), newItems[1].ID);
            Assert.AreEqual(Id(3), newItems[2].ID);
        }

        //=========================================================

        [TestMethod]
        public void InsertOrUpdateReadId()
        {
            var oldItems = new[] {
                new SimpleEntity { ID = Id(1), Name = "a", Size = 1, Parent = new ParentEntity { Name = "p1" } },
                new SimpleEntity { ID = Id(2), Name = "b", Size = 2, Parent = null },
                new SimpleEntity { ID = Id(3), Name = "c", Size = 3, Parent = new ParentEntity { Name = null } },
                new SimpleEntity { ID = Id(4), Name = "d", Size = 4, Parent = new ParentEntity { Name = "p4" } },
                new SimpleEntity { ID = Id(5), Name = "e", Size = 5, Parent = new ParentEntity { Name = "p5" } },
            };

            var newItems = new[] {
                new SimpleEntity { ID = Id(101), Name = "a", Size = 101, Parent = null },
                new SimpleEntity { ID = Id(102), Name = "b", Size = 102, Parent = null },
                new SimpleEntity { ID = Id(103), Name = "c", Size = 103, Parent = null },
                new SimpleEntity { ID = Id(104), Name = "d", Size = 104, Parent = new ParentEntity { Name = "p4" } },
                new SimpleEntity { ID = Id(105), Name = "e", Size = 5, Parent = new ParentEntity { Name = "p5" } },
            };

            var repos = NewSimpleRepos(oldItems);
            repos.InsertOrUpdateReadId(newItems,
                item => new { item.Name, ParentName = item.Parent != null ? item.Parent.Name : null },
                item => item.Size);

            Assert.AreEqual("INSERT a 101, UPDATE b 102, UPDATE c 103, UPDATE d 104 p4", repos.RepositoryMock.Log.ToString());

            Assert.AreEqual("a 1 p1", oldItems[0].ToString());
            Assert.AreEqual("b 102", oldItems[1].ToString());
            Assert.AreEqual("c 103", oldItems[2].ToString());
            Assert.AreEqual("d 104 p4", oldItems[3].ToString());
            Assert.AreEqual("e 5 p5", oldItems[4].ToString());

            Assert.AreEqual(Id(101), newItems[0].ID);
            Assert.AreEqual(Id(2), newItems[1].ID);
            Assert.AreEqual(Id(3), newItems[2].ID);
            Assert.AreEqual(Id(4), newItems[3].ID);
            Assert.AreEqual(Id(5), newItems[4].ID);
        }


        [TestMethod]
        public void InsertOrUpdateReadId_NotCoveredAllProperties()
        {
            var oldItems = new[] {
                new SimpleEntity { ID = Id(1), Name = "a", Size = 1, Parent = new ParentEntity { Name = "p1" } },
            };

            var newItems = new[] {
                new SimpleEntity { ID = Id(101), Name = "a", Size = 0, Parent = new ParentEntity { Name = "p101" } },
                new SimpleEntity { ID = Id(102), Name = "b", Size = 102, Parent = new ParentEntity { Name = "p102" } },
            };

            var repos = NewSimpleRepos(oldItems);
            repos.InsertOrUpdateReadId(newItems, item => item.Name, item => item.Parent);

            // The old item's Size property is not covered with keySelector nor propertySelector. It should remain unchanged.
            Assert.AreEqual("UPDATE a 1 p101, INSERT b 102 p102", repos.RepositoryMock.Log.ToString());
            Assert.AreEqual("a 1 p101", oldItems[0].ToString());
            Assert.AreEqual(Id(1), newItems[0].ID);
        }

        //=========================================================

        class SimpleEntityCompareName : IComparer<ISimpleEntity>
        {
            public int Compare(ISimpleEntity x, ISimpleEntity y) { return string.Compare(x.Name, y.Name, StringComparison.InvariantCultureIgnoreCase); }
        }

        static bool SimpleEntityEqualSize(ISimpleEntity x, ISimpleEntity y) { return x.Size == y.Size; }

        [TestMethod]
        public void InsertOrUpdateOrDelete()
        {
            var oldItems = new[] {
                new SimpleEntity { ID = Id(1), Name = "a", Size = 1 },
                new SimpleEntity { ID = Id(2), Name = "b", Size = 2, Parent = new ParentEntity { Name = "p2" } },
            };

            var newItems = new[] {
                new SimpleEntity { ID = Id(102), Name = "b", Size = 22, Parent = new ParentEntity { Name = "p22" } },
                new SimpleEntity { ID = Id(103), Name = "c", Size = 33, Parent = new ParentEntity { Name = "p33" } },
            };

            var repos = NewSimpleRepos(oldItems);
            repos.InsertOrUpdateOrDelete(newItems, new SimpleEntityCompareName(), SimpleEntityEqualSize, new FilterAll(),
                (dest, src) => { dest.Name = src.Name; dest.Size = src.Size; });

            Assert.AreEqual("DELETE a 1, UPDATE b 22 p2, INSERT c 33 p33", repos.RepositoryMock.Log.ToString());
            // Actually, the Parent property (p33) should not be updated or inserted because it is not considered a key nor a value property in the provided comparers and assigners. The current behavior has better performance for large inserts.

            Assert.AreEqual(Id(2), (repos.RepositoryMock.UpdatedGroups[0] as IEnumerable<SimpleEntity>).First().ID);
            Assert.AreEqual(Id(103), (repos.RepositoryMock.InsertedGroups[0] as IEnumerable<SimpleEntity>).First().ID);
        }

        [TestMethod]
        public void InsertOrUpdateOrDelete_FilterLoad()
        {
            var oldItems = new[] {
                new SimpleEntity { ID = Id(1), Name = "a", Size = 1 },
                new SimpleEntity { ID = Id(2), Name = "b", Size = 2, Parent = new ParentEntity { Name = "p2" } },
                new SimpleEntity { ID = Id(3), Name = "c", Size = 3 },
                new SimpleEntity { ID = Id(4), Name = "d", Size = 4, Parent = new ParentEntity { Name = "p4" } },
            };

            var newItems = new[] {
                new SimpleEntity { ID = Id(102), Name = "b", Size = 22 },
                new SimpleEntity { ID = Id(103), Name = "c", Size = 33 },
            };

            var repos = NewSimpleRepos(oldItems);
            repos.InsertOrUpdateOrDelete(newItems, new SimpleEntityCompareName(), SimpleEntityEqualSize,
                (Expression<Func<ISimpleEntity, bool>>)((ISimpleEntity item) => item.Parent != null),
                (dest, src) => { dest.Name = src.Name; dest.Size = src.Size; });

            Assert.AreEqual("DELETE d 4 p4, UPDATE b 22 p2, INSERT c 33", repos.RepositoryMock.Log.ToString());
        }

        [TestMethod]
        public void InsertOrUpdateOrDelete_DuplicateOldKeys()
        {
            var oldItems = new[] {
                new SimpleEntity { ID = Id(1), Name = "a", Size = 1 },
                new SimpleEntity { ID = Id(2), Name = "a", Size = 2 },
                new SimpleEntity { ID = Id(3), Name = "a", Size = 3 },
                new SimpleEntity { ID = Id(4), Name = "b", Size = 4 },
                new SimpleEntity { ID = Id(5), Name = "b", Size = 5 },
            };

            var newItems = new[] {
                new SimpleEntity { ID = Id(101), Name = "b", Size = 101 },
                new SimpleEntity { ID = Id(102), Name = "c", Size = 102 },
            };

            var repos = NewSimpleRepos(oldItems);
            repos.InsertOrUpdateOrDelete(newItems, new SimpleEntityCompareName(), SimpleEntityEqualSize, new FilterAll(),
                (dest, src) => { dest.Name = src.Name; dest.Size = src.Size; });

            string expectedOptions = "<DELETE a 1, a 2, a 3, b 5, UPDATE b 101, INSERT c 102> or <DELETE a 1, a 2, a 3, b 4, UPDATE b 101, INSERT c 102>";
            TestUtility.AssertContains(expectedOptions, "<" + repos.RepositoryMock.Log.ToString() + ">");
        }

        [TestMethod]
        public void InsertOrUpdateOrDelete_DuplicateNewKeys()
        {
            var newItems = new[] {
                new SimpleEntity { ID = Id(101), Name = "a", Size = 101 },
                new SimpleEntity { ID = Id(102), Name = "a", Size = 102 },
            };

            var repos = NewSimpleRepos(new SimpleEntity[] {});
            repos.InsertOrUpdateOrDelete(newItems, new SimpleEntityCompareName(), SimpleEntityEqualSize, new FilterAll(),
                (dest, src) => { dest.Name = src.Name; dest.Size = src.Size; });

            // An error might also be acceptable behavior.
            Assert.AreEqual("INSERT a 101, a 102", repos.RepositoryMock.Log.ToString());
        }

        [TestMethod]
        public void InsertOrUpdateOrDelete_NullKeys()
        {
            var oldItems = new[] {
                new SimpleEntity { ID = Id(1), Name = null, Size = 1 },
            };

            var newItems = new[] {
                new SimpleEntity { ID = Id(101), Name = null, Size = 101 },
            };

            var repos = NewSimpleRepos(oldItems);
            repos.InsertOrUpdateOrDelete(newItems, new SimpleEntityCompareName(), SimpleEntityEqualSize, new FilterAll(),
                (dest, src) => { dest.Name = src.Name; dest.Size = src.Size; });

            Assert.AreEqual("UPDATE <null> 101", repos.RepositoryMock.Log.ToString(),
                "Parent property should not be updated or inserted because it is not considered a key nor a value property in the provided comparers and assigners.");

            Assert.AreEqual(Id(1), (repos.RepositoryMock.UpdatedGroups[0] as IEnumerable<SimpleEntity>).First().ID);
        }

        class CaseInsensitiveName : IComparer<ISimpleEntity>
        {
            public int Compare(ISimpleEntity x, ISimpleEntity y) { return string.Compare(x.Name, y.Name, StringComparison.InvariantCultureIgnoreCase); }
        }

        static bool CaseSensitiveNameSize(ISimpleEntity x, ISimpleEntity y) { return string.Equals(x.Name, y.Name, StringComparison.Ordinal) && x.Size == y.Size; }

        [TestMethod]
        public void InsertOrUpdateOrDelete_UpdateCaseChangeOnCaseInsensitiveKeyProperty()
        {
            var oldItems = new[] {
                new SimpleEntity { ID = Id(1), Name = "a", Size = 1 },
                new SimpleEntity { ID = Id(2), Name = "b", Size = 2 },
                new SimpleEntity { ID = Id(3), Name = "cC", Size = 3 },
                new SimpleEntity { ID = Id(31), Name = "Cc", Size = 3 },
                new SimpleEntity { ID = Id(4), Name = "D", Size = 4 },
            };

            var newItems = new[] {
                new SimpleEntity { ID = Id(101), Name = "A", Size = 1 },
                new SimpleEntity { ID = Id(102), Name = "b", Size = 102 },
                new SimpleEntity { ID = Id(103), Name = "CC", Size = 3 },
                new SimpleEntity { ID = Id(104), Name = "D", Size = 4 },
            };

            var repos = NewSimpleRepos(oldItems);
            repos.InsertOrUpdateOrDelete(newItems, new CaseInsensitiveName(), CaseSensitiveNameSize, new FilterAll(),
                (dest, src) => { dest.Name = src.Name; dest.Size = src.Size; });

            // This combination of comparers and assigners may be used when a case-insensitive key property (Name, e.g.)
            // is allowed to change character case, expecting that the change results in update of the Name property, not
            // deleting the old instance and inserting the new one, nor leaving the old property value in database unchanged.

            Assert.AreEqual("DELETE Cc 3, UPDATE A 1, b 102, CC 3", repos.RepositoryMock.Log.ToString());
        }

        //================================================================

        interface IDeacEntity : IEntity, IDeactivatable
        {
            string Name { get; set; }
        }

        class DeacEntity : IDeacEntity
        {
            public Guid ID { get; set; }
            public string Name { get; set; }
            public bool? Active { get; set; }

            public override string ToString()
            {
                return
                    (Name != null ? Name : "<null>")
                    + (Active != null ? " " + Active : "");
            }
        }

        class DeacCompareName : IComparer<IDeacEntity>
        {
            public int Compare(IDeacEntity x, IDeacEntity y) { return string.Compare(x.Name, y.Name, StringComparison.InvariantCultureIgnoreCase); }
        }

        static bool DeacCompareNameValue(IDeacEntity x, IDeacEntity y) { return string.Equals(x.Name, y.Name, StringComparison.Ordinal) && x.Active == y.Active; }

        class DeacEntityList : List<DeacEntity>
        {
            static int idCounter = 1;
            public void Add(string name, bool? active) { Add(new DeacEntity { Name = name, Active = active, ID = Id(idCounter++) }); }
        }

        [TestMethod]
        public void InsertOrUpdateOrDeleteOrDeactivate()
        {
            var oldItems = new DeacEntityList {
                {"a1", true }, {"a2", true }, {"a3", true },
                {"d1", false }, {"d2", false }, {"d3", false },
                {"o1", null }, {"o2", true }, {"o3", false },
                {"c1", null }, {"c2", null }, {"c3", true }, {"c4", false },
            };

            var newItems = new DeacEntityList {
                { "a1", null }, { "a2", true }, { "a3", false },
                { "d1", null }, { "d2", true }, { "d3", false },
                { "n1", null }, { "n2", true }, { "n3", false },
                { "c1", true }, { "c2", false }, { "c3", false }, { "c4", true },
            };

            var repos = new TestGenericRepository<IDeacEntity, DeacEntity>(oldItems);
            repos.InsertOrUpdateOrDeleteOrDeactivate(newItems, new DeacCompareName(), DeacCompareNameValue, new FilterAll(),
                (dest, src) => { dest.Name = src.Name; dest.Active = src.Active; },
                new FilterAll());

            Assert.AreEqual("UPDATE a3 False, c1 True, c2 False, c3 False, c4 True, d1 True, d2 True, o1 False, o2 False, INSERT n1 True, n2 True, n3 False",
                repos.RepositoryMock.Log.ToString());

            TestUtility.AssertContains(repos.RepositoryMock.InsertedGroups.Single().GetType().FullName,
                "System.Collections.Generic.List`1[[Rhetos.CommonConcepts.Test.GenericRepositoryWriteTest+DeacEntity",
                "GenericRepository should prepare native type for saving to the entity's repository.");
            TestUtility.AssertContains(repos.RepositoryMock.UpdatedGroups.Single().GetType().FullName,
                "System.Collections.Generic.List`1[[Rhetos.CommonConcepts.Test.GenericRepositoryWriteTest+DeacEntity",
                "GenericRepository should prepare native type for saving to the entity's repository.");
        }

        [TestMethod]
        public void InsertOrUpdateOrDeleteOrDeactivate_LoadFilter()
        {
            var oldItems = new DeacEntityList {
                {"a1", true }, {"a2", true }, {"a3", true },
                {"d1", false }, {"d2", false }, {"d3", false },
                {"o1", null }, {"o2", true }, {"o3", false },
                {"c1", null }, {"c2", null }, {"c3", true }, {"c4", false },
            };

            var newItems = new DeacEntityList {
                { "a1", null }, { "a2", true }, { "a3", false },
                { "d1", null }, { "d2", true }, { "d3", false },
                { "n1", null }, { "n2", true }, { "n3", false },
                { "c1", true }, { "c2", false }, { "c3", false }, { "c4", true },
            };

            var hideOldItems = new[] { "a1", "d1", "o1", "c1", "c2" };

            var repos = new TestGenericRepository<IDeacEntity, DeacEntity>(oldItems);
            repos.InsertOrUpdateOrDeleteOrDeactivate(newItems, new DeacCompareName(), DeacCompareNameValue,
                (Expression<Func<IDeacEntity, bool>>)(item => !hideOldItems.Contains(item.Name)),
                (dest, src) => { dest.Name = src.Name; dest.Active = src.Active; },
                new FilterAll());

            Assert.AreEqual("UPDATE a3 False, c3 False, c4 True, d2 True, o2 False, INSERT a1 True, c1 True, c2 False, d1 True, n1 True, n2 True, n3 False",
                repos.RepositoryMock.Log.ToString());

            TestUtility.AssertContains(repos.RepositoryMock.InsertedGroups.Single().GetType().FullName,
                "System.Collections.Generic.List`1[[Rhetos.CommonConcepts.Test.GenericRepositoryWriteTest+DeacEntity",
                "GenericRepository should prepare native type for saving to the entity's repository.");
            TestUtility.AssertContains(repos.RepositoryMock.UpdatedGroups.Single().GetType().FullName,
                "System.Collections.Generic.List`1[[Rhetos.CommonConcepts.Test.GenericRepositoryWriteTest+DeacEntity",
                "GenericRepository should prepare native type for saving to the entity's repository.");
        }

        [TestMethod]
        public void InsertOrUpdateOrDeleteOrDeactivate_DeactivatedFilter()
        {
            var oldItems = new DeacEntityList {
                {"a1", true }, {"a2", true }, {"a3", true },
                {"d1", false }, {"d2", false }, {"d3", false },
                {"o1", null }, {"o2", true }, {"o3", false },
                {"u1", null }, {"u2", true }, {"u3", false },
                {"c1", null }, {"c2", null }, {"c3", true }, {"c4", false },
            };

            var newItems = new DeacEntityList {
                { "a1", null }, { "a2", true }, { "a3", false },
                { "d1", null }, { "d2", true }, { "d3", false },
                { "n1", null }, { "n2", true }, { "n3", false },
                { "c1", true }, { "c2", false }, { "c3", false }, { "c4", true },
            };

            var deactivateDontDeleteUsedOldItems = new[] { "u1", "u2", "u3" };

            var repos = new TestGenericRepository<IDeacEntity, DeacEntity>(oldItems);
            repos.InsertOrUpdateOrDeleteOrDeactivate(newItems, new DeacCompareName(), DeacCompareNameValue, new FilterAll(),
                (dest, src) => { dest.Name = src.Name; dest.Active = src.Active; },
                (Func<IDeacEntity, bool>)(item => deactivateDontDeleteUsedOldItems.Contains(item.Name)));

            Assert.AreEqual("DELETE o1, o2 True, o3 False"
                + ", UPDATE a3 False, c1 True, c2 False, c3 False, c4 True, d1 True, d2 True, u1 False, u2 False"
                + ", INSERT n1 True, n2 True, n3 False",
                repos.RepositoryMock.Log.ToString());

            TestUtility.AssertContains(repos.RepositoryMock.InsertedGroups.Single().GetType().FullName,
                "System.Collections.Generic.List`1[[Rhetos.CommonConcepts.Test.GenericRepositoryWriteTest+DeacEntity",
                "GenericRepository should prepare native type for saving to the entity's repository.");
            TestUtility.AssertContains(repos.RepositoryMock.UpdatedGroups.Single().GetType().FullName,
                "System.Collections.Generic.List`1[[Rhetos.CommonConcepts.Test.GenericRepositoryWriteTest+DeacEntity",
                "GenericRepository should prepare native type for saving to the entity's repository.");
            TestUtility.AssertContains(repos.RepositoryMock.DeletedGroups.Single().GetType().FullName,
                "System.Collections.Generic.List`1[[Rhetos.CommonConcepts.Test.GenericRepositoryWriteTest+DeacEntity",
                "GenericRepository should prepare native type for saving to the entity's repository.");
        }
    }
}
