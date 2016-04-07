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
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.TestCommon;
using Rhetos.Configuration.Autofac;
using Rhetos.Utilities;

namespace CommonConcepts.Test
{
    [TestClass]
    public class HierarchyTest
    {
        private static void PrepareSimpleData(Common.DomRepository repository, string treeDescription)
        {
            repository.TestHierarchy.Simple.Delete(repository.TestHierarchy.Simple.All());

            var items = new Dictionary<string, TestHierarchy.Simple>();

            var inputElements = treeDescription.Split(',').Select(x => x.Trim()).ToArray();
            var rootNodes = inputElements.Where(ni => !ni.Contains('-'));
            var parentChildEdges = inputElements.Where(ni => ni.Contains('-')).Select(edge =>
                {
                    var split = edge.Split('-');
                    return new { Parent = split[0], Child = split[1] };
                });

            foreach (string node in rootNodes)
                items.Add(node, new TestHierarchy.Simple { ID = Guid.NewGuid(), Name = node });

            foreach (var node in parentChildEdges)
                items.Add(node.Child, new TestHierarchy.Simple { ID = Guid.NewGuid(), Name = node.Child });

            foreach (var node in parentChildEdges)
                items[node.Child].Parent = items[node.Parent];

            repository.TestHierarchy.Simple.Insert(items.Values);
        }

        private static string ReportSimple(Common.DomRepository repository)
        {
            return ReportSimple(repository.TestHierarchy.Simple.All());
        }

        private static string ReportSimple(IEnumerable<TestHierarchy.Simple> items)
        {
            return TestUtility.DumpSorted(items, item => item.Name + item.Extension_SimpleParentHierarchy.LeftIndex + "/" + item.Extension_SimpleParentHierarchy.RightIndex);
        }

        [TestMethod]
        public void SimpleHierarchy()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestHierarchy.Simple" });
                var repository = container.Resolve<Common.DomRepository>();

                PrepareSimpleData(repository, "a");
                Assert.AreEqual("a1/2", ReportSimple(repository));

                PrepareSimpleData(repository, "a, a-b");
                Assert.AreEqual("a1/4, b2/3", ReportSimple(repository));

                PrepareSimpleData(repository, "a, a-b, b-c");
                Assert.AreEqual("a1/6, b2/5, c3/4", ReportSimple(repository));
                Assert.AreEqual("c3/4", ReportSimple(repository.TestHierarchy.Simple.Filter(new TestHierarchy.Level2OrDeeper())));

                PrepareSimpleData(repository, "a, a-b, a-c");
                Assert.AreEqual("a1/6, b2/3, c4/5", ReportSimple(repository));
                Assert.AreEqual("", ReportSimple(repository.TestHierarchy.Simple.Filter(new TestHierarchy.Level2OrDeeper())));
            }
        }

        [TestMethod]
        public void Level()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestHierarchy.Simple" });
                var repository = container.Resolve<Common.DomRepository>();

                PrepareSimpleData(repository, "a, a-b, b-c");
                Assert.AreEqual("c3/4", ReportSimple(repository.TestHierarchy.Simple.Filter(new TestHierarchy.Level2OrDeeper())));

                PrepareSimpleData(repository, "a, a-b, a-c");
                Assert.AreEqual("", ReportSimple(repository.TestHierarchy.Simple.Filter(new TestHierarchy.Level2OrDeeper())));
            }
        }

        [TestMethod]
        public void HierarchyAncestorsDescendants()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestHierarchy.Simple" });
                var repository = container.Resolve<Common.DomRepository>();

                var h = new TestHierarchy.Simple2 { ID = Guid.NewGuid(), Name2 = "h", Parent2 = null };
                var h1 = new TestHierarchy.Simple2 { ID = Guid.NewGuid(), Name2 = "h1", Parent2 = h };
                var h2 = new TestHierarchy.Simple2 { ID = Guid.NewGuid(), Name2 = "h2", Parent2 = h };
                var h21 = new TestHierarchy.Simple2 { ID = Guid.NewGuid(), Name2 = "h21", Parent2 = h2 };

                repository.TestHierarchy.Simple2.Insert(new[] { h, h1, h2, h21 });

                var testAncestors = new Dictionary<TestHierarchy.Simple2, string>
                {
                    { h, "" },
                    { h1, "h" },
                    { h2, "h" },
                    { h21, "h, h2" }
                };

                foreach (var test in testAncestors)
                {
                    Console.WriteLine("Testing Ancestors " + test.Key.Name2 + " => " + test.Value);
                    var filtered = repository.TestHierarchy.Simple2.Filter(new TestHierarchy.Parent2HierarchyAncestors { ID = test.Key.ID });
                    Assert.AreEqual(test.Value, TestUtility.DumpSorted(filtered, item => item.Name2));
                }

                var testDescentantds = new Dictionary<TestHierarchy.Simple2, string>
                {
                    { h, "h1, h2, h21" },
                    { h1, "" },
                    { h2, "h21" },
                    { h21, "" }
                };

                foreach (var test in testDescentantds)
                {
                    Console.WriteLine("Testing Descentantds " + test.Key.Name2 + " => " + test.Value);
                    var filtered = repository.TestHierarchy.Simple2.Filter(new TestHierarchy.Parent2HierarchyDescendants { ID = test.Key.ID });
                    Assert.AreEqual(test.Value, TestUtility.DumpSorted(filtered, item => item.Name2));
                }
            }
        }

        [TestMethod]
        public void SingleRootConstraint()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestHierarchy.Simple2" });
                var repository = container.Resolve<Common.DomRepository>();

                var h1 = new TestHierarchy.Simple2 { ID = Guid.NewGuid(), Name2 = "h1", Parent2 = null };
                var h2 = new TestHierarchy.Simple2 { ID = Guid.NewGuid(), Name2 = "h2", Parent2 = null };

                TestUtility.ShouldFail(() => repository.TestHierarchy.Simple2.Insert(new[] { h1, h2 }),
                    "root record", "TestHierarchy.Simple2", "Parent2");
            }
        }

        [TestMethod]
        public void ErrorCircularReference1InsertSingleRoot()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestHierarchy.Simple2" });
                var repository = container.Resolve<Common.DomRepository>();

                var single = new TestHierarchy.Simple2 { ID = Guid.NewGuid(), Name2 = "a" };
                single.Parent2ID = single.ID;

                TestUtility.ShouldFail(() => repository.TestHierarchy.Simple2.Insert(new[] { single }), "not allowed", "circular dependency");
            }
        }

        [TestMethod]
        public void ErrorCircularReference1UpdateSingleRoot()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestHierarchy.Simple2" });
                var repository = container.Resolve<Common.DomRepository>();

                var single = new TestHierarchy.Simple2 { ID = Guid.NewGuid(), Name2 = "a" };
                repository.TestHierarchy.Simple2.Insert(new[] { single });

                single.Parent2ID = single.ID;
                TestUtility.ShouldFail(() => repository.TestHierarchy.Simple2.Update(new[] { single }), "not allowed", "circular dependency");
            }
        }

        [TestMethod]
        public void MultipleRoots()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestHierarchy.Simple" });
                var repository = container.Resolve<Common.DomRepository>();

                PrepareSimpleData(repository, "a, b, b-c");
                Assert.AreEqual("a1/2, b3/6, c4/5", ReportSimple(repository.TestHierarchy.Simple.All()));
            }
        }

        [TestMethod]
        public void DontFailOnEmpty()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestHierarchy.Simple" });
                var repository = container.Resolve<Common.DomRepository>();

                repository.TestHierarchy.SimpleParentHierarchy.Recompute();
                Assert.AreEqual("", ReportSimple(repository), "Initially empty.");

                PrepareSimpleData(repository, "a, a-b");
                Assert.AreEqual("a1/4, b2/3", ReportSimple(repository));

                repository.TestHierarchy.Simple.Delete(repository.TestHierarchy.Simple.All());
                Assert.AreEqual("", ReportSimple(repository), "Empty after delete.");
            }
        }

        [TestMethod]
        public void ErrorCircularReference1Insert()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestHierarchy.Simple" });
                var repository = container.Resolve<Common.DomRepository>();

                var single = new TestHierarchy.Simple { ID = Guid.NewGuid(), Name = "a" };
                single.ParentID = single.ID;

                TestUtility.ShouldFail(() => repository.TestHierarchy.Simple.Insert(new[] { single }), "not allowed", "circular dependency");
            }
        }

        [TestMethod]
        public void ErrorCircularReference1Update()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestHierarchy.Simple" });
                var repository = container.Resolve<Common.DomRepository>();

                PrepareSimpleData(repository, "a");
                Assert.AreEqual("a1/2", ReportSimple(repository));
                var single = repository.TestHierarchy.Simple.Query().Where(item => item.Name == "a").Single();
                single.Parent = single;

                TestUtility.ShouldFail(() => repository.TestHierarchy.Simple.Update(new[] { single }), "not allowed", "circular dependency");
            }
        }

        [TestMethod]
        public void ErrorCircularReference2()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestHierarchy.Simple" });
                var repository = container.Resolve<Common.DomRepository>();

                PrepareSimpleData(repository, "a, a-b");
                Assert.AreEqual("a1/4, b2/3", ReportSimple(repository));
                var root = repository.TestHierarchy.Simple.Query().Where(item => item.Name == "a").Single();
                var leaf = repository.TestHierarchy.Simple.Query().Where(item => item.Name == "b").Single();
                root.Parent = leaf;

                TestUtility.ShouldFail(() => repository.TestHierarchy.Simple.Update(new[] { root }), "not allowed", "circular dependency");
            }
        }

        [TestMethod]
        public void ErrorCircularReference4()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestHierarchy.Simple" });
                var repository = container.Resolve<Common.DomRepository>();

                PrepareSimpleData(repository, "a, a-b, b-c, c-d");
                var root = repository.TestHierarchy.Simple.Query().Where(item => item.Name == "a").Single();
                var leaf = repository.TestHierarchy.Simple.Query().Where(item => item.Name == "d").Single();
                root.Parent = leaf;

                TestUtility.ShouldFail(() => repository.TestHierarchy.Simple.Update(new[] { root }), "not allowed", "circular dependency");
            }
        }

        [TestMethod]
        public void Path()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestHierarchy.WithPath" });
                var repository = container.Resolve<Common.DomRepository>();

                var h1 = new TestHierarchy.WithPath { ID = Guid.NewGuid(), Title = "h1", Group = null };
                var h11 = new TestHierarchy.WithPath { ID = Guid.NewGuid(), Title = "h11", Group = h1 };
                var h12 = new TestHierarchy.WithPath { ID = Guid.NewGuid(), Title = "h12", Group = h1 };
                var h121 = new TestHierarchy.WithPath { ID = Guid.NewGuid(), Title = "h121", Group = h12 };
                var h2 = new TestHierarchy.WithPath { ID = Guid.NewGuid(), Title = "h2", Group = null };

                repository.TestHierarchy.WithPath.Insert(new[] { h1, h11, h12, h121, h2 });

                Assert.AreEqual("h1, h1 - h11, h1 - h12, h1 - h12 - h121, h2",
                    TestUtility.DumpSorted(repository.TestHierarchy.BrowseWithPath.All(), item => item.GroupSequence));

            }
        }

        [TestMethod]
        [Ignore]
        public void LongPath()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                repository.TestHierarchy.WithPath.Delete(repository.TestHierarchy.WithPath.Query());

                var h1 = new TestHierarchy.WithPath { ID = Guid.NewGuid(), Title = new string('a', 256), Group = null };
                var h2 = new TestHierarchy.WithPath { ID = Guid.NewGuid(), Title = new string('b', 256), Group = h1 };
                var h3 = new TestHierarchy.WithPath { ID = Guid.NewGuid(), Title = new string('c', 256), Group = h2 };

                repository.TestHierarchy.WithPath.Insert(new[] { h1, h2, h3 });

                var paths = repository.TestHierarchy.WithPathGroupHierarchy.Query()
                    .Select(h => new { h.ID, h.GroupSequence })
                    .ToList()
                    .ToDictionary(h => h.ID, h => h.GroupSequence);

                Assert.AreEqual("aaa...(256)", TestUtility.CompressReport(paths[h1.ID]));
                Assert.AreEqual("aaa...(256) - bbb...(256)", TestUtility.CompressReport(paths[h2.ID]));
                Assert.AreEqual("aaa...(256) - bbb...(256) - ccc...(256)", TestUtility.CompressReport(paths[h3.ID]));

            }
        }

        [TestMethod]
        public void MultipleHierarchiesOnSameEntity()
        {
            Assert.Inconclusive();
        }

        [TestMethod]
        [Ignore] // TODO: This feature is implemented. Create the unit test.
        public void HierarchyOnComputedDataStructureExtension()
        {
            // Useful for defining a hierarchy on a set of different entities using their union as a list of base items.
            Assert.Inconclusive();
        }

        [TestMethod]
        public void MultiplePathsOnSameEntity()
        {
            Assert.Inconclusive();
        }
    }
}
