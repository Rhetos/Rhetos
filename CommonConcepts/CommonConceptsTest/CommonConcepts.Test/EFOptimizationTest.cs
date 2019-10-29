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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Configuration.Autofac;
using Rhetos.Utilities;
using Rhetos.Dom.DefaultConcepts;
using System.Linq.Expressions;
using Rhetos.TestCommon;

namespace CommonConcepts.Test
{
    [TestClass]
    public class EFOptimizationTest
    {
        [TestMethod]
        public void WhereContainsTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                var id1 = Guid.NewGuid();
                var id2 = Guid.NewGuid();
                var id3 = Guid.NewGuid();
                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        $@"INSERT INTO Test12.Entity1 (ID) SELECT '{id1}';",
                        $@"INSERT INTO Test12.Entity2 (ID, Entity1ID) SELECT '{id2}', '{id1}';",
                        $@"INSERT INTO Test12.Entity3 (ID, Entity2ID) SELECT '{id3}', '{id2}';"
                    });

                var record = repository.Test12.Entity3.Query().WhereContains(new List<Guid>() { id1, id2 }, x => x.Entity2.Entity1.ID).Single();
                Assert.AreEqual(id3, record.ID);
            }
        }

        [TestMethod]
        public void MultipleWhereContainsTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                var id1 = Guid.NewGuid();
                var id2 = Guid.NewGuid();
                var id3 = Guid.NewGuid();

                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        $@"INSERT INTO Test12.Entity1 (ID) SELECT '{id1}';",
                        $@"INSERT INTO Test12.Entity2 (ID, Entity1ID) SELECT '{id2}', '{id1}';",
                        $@"INSERT INTO Test12.Entity3 (ID, Entity2ID) SELECT '{id3}', '{id2}';"
                    });

                var records = repository.Test12.Entity3.Query()
                    .WhereContains(new List<Guid>() { id1 }, e3 => e3.Entity2.Entity1.ID)
                    .WhereContains(new List<Guid>() { id2 }, e3 => e3.Entity2.ID)
                    .Select(e3 => e3.ID);
                Console.WriteLine(records.Expression.ToString());
                Console.WriteLine(records.ToString());
                Assert.AreEqual(id3.ToString(), TestUtility.DumpSorted(records));
            }
        }

        [TestMethod]
        public void WhereEmptyContainsTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                var id1 = Guid.NewGuid();
                var id2 = Guid.NewGuid();
                var id3 = Guid.NewGuid();

                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        $@"INSERT INTO Test12.Entity1 (ID) SELECT '{id1}';",
                        $@"INSERT INTO Test12.Entity2 (ID, Entity1ID) SELECT '{id2}', '{id1}';",
                        $@"INSERT INTO Test12.Entity3 (ID, Entity2ID) SELECT '{id3}', '{id2}';"
                    });

                var records = repository.Test12.Entity3.Query()
                    .WhereContains(new List<Guid>() { }, e3 => e3.Entity2.Entity1.ID)
                    .Select(e3 => e3.ID);
                Console.WriteLine(records.Expression.ToString());
                Console.WriteLine(records.ToString());
                Assert.AreEqual("", TestUtility.DumpSorted(records));
            }
        }

        [TestMethod]
        public void OptimizeContainsIdsTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                var id1 = Guid.NewGuid();
                var id2 = Guid.NewGuid();
                var id3 = Guid.NewGuid();
                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        $@"INSERT INTO Test12.Entity1 (ID) SELECT '{id1}';",
                        $@"INSERT INTO Test12.Entity2 (ID, Entity1ID) SELECT '{id2}', '{id1}';",
                        $@"INSERT INTO Test12.Entity3 (ID, Entity2ID) SELECT '{id3}', '{id2}';"
                    });

                var listIds = new List<Guid> { id1 };

                var containsExpression = EFExpression.OptimizeContains<Common.Queryable.Test12_Entity3>(x => listIds.Contains(x.Entity2.Entity1ID.Value));
                var whereContainsSql = repository.Test12.Entity3.Query().Where(containsExpression).ToString();
                Console.WriteLine(whereContainsSql);
                Assert.IsFalse(whereContainsSql.ToLower().Contains(id1.ToString().ToLower()));
                Assert.AreEqual(id3, repository.Test12.Entity3.Query().Where(containsExpression).Single().ID);
            }
        }

        [TestMethod]
        public void OptimizeContainsNullableIdsTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                var id1 = Guid.NewGuid();
                var id2 = Guid.NewGuid();
                var id3 = Guid.NewGuid();
                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        $@"INSERT INTO Test12.Entity1 (ID) SELECT '{id1}';",
                        $@"INSERT INTO Test12.Entity2 (ID, Entity1ID) SELECT '{id2}', '{id1}';",
                        $@"INSERT INTO Test12.Entity3 (ID, Entity2ID) SELECT '{id3}', '{id2}';"
                    });

                var listIds = new List<Guid?> { id1 };

                var containsExpression = EFExpression.OptimizeContains<Common.Queryable.Test12_Entity3>(x => listIds.Contains(x.Entity2.Entity1ID.Value));
                var whereContainsSql = repository.Test12.Entity3.Query().Where(containsExpression).ToString();
                Assert.IsFalse(whereContainsSql.ToLower().Contains(id1.ToString().ToLower()));
                Assert.AreEqual(id3, repository.Test12.Entity3.Query().Where(containsExpression).Single().ID);

                var containsExpression2 = EFExpression.OptimizeContains<Common.Queryable.Test12_Entity3>(x => listIds.Contains(x.Entity2.Entity1ID));
                var whereContainsSql2 = repository.Test12.Entity3.Query().Where(containsExpression2).ToString();
                Assert.IsFalse(whereContainsSql2.ToLower().Contains(id1.ToString().ToLower()));
                Assert.AreEqual(id3, repository.Test12.Entity3.Query().Where(containsExpression2).Single().ID);
            }
        }

        [TestMethod]
        public void OptimizeContainsNullableWithNullValueTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                var id1 = Guid.NewGuid();
                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        $@"INSERT INTO Test12.Entity1 (ID) SELECT '{id1}';",
                    });

                var listWithNullValue = new List<Guid?> { null };

                var record1 = repository.Test12.Entity1.Query().Where(EFExpression.OptimizeContains<Common.Queryable.Test12_Entity1>(x => listWithNullValue.Contains(x.GuidProperty))).Single();
                Assert.AreEqual(id1, record1.ID);
            }
        }

        [TestMethod]
        public void OptimizeContainsNullableWithMixedNullValueTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                var id1 = Guid.NewGuid();
                var id2 = Guid.NewGuid();
                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        $@"INSERT INTO Test12.Entity1 (ID) SELECT '{id1}';",
                        $@"INSERT INTO Test12.Entity1 (ID) SELECT '{id2}';",
                    });
                Console.WriteLine($"ID1: {id1}");
                Console.WriteLine($"ID2: {id2}");

                var listWithNullValue = new List<Guid?> { null, id1 };

                {
                    var basicQuery = repository.Test12.Entity1.Query().Where(x => listWithNullValue.Contains(x.GuidProperty));
                    var optimizedQuery = repository.Test12.Entity1.Query().Where(EFExpression.OptimizeContains<Common.Queryable.Test12_Entity1>(x => listWithNullValue.Contains(x.GuidProperty)));
                    Console.WriteLine(basicQuery.ToString());
                    Console.WriteLine(optimizedQuery.ToString());

                    Assert.AreEqual(
                        TestUtility.DumpSorted(basicQuery, item => item.ID),
                        TestUtility.DumpSorted(optimizedQuery, item => item.ID));
                    Assert.AreEqual(
                        TestUtility.DumpSorted(basicQuery.ToSimple().ToList(), item => item.ID),
                        TestUtility.DumpSorted(optimizedQuery.ToSimple().ToList(), item => item.ID));
                }
                {
                    var basicQuery = repository.Test12.Entity1.Query().Where(x => listWithNullValue.Contains(x.ID));
                    var optimizedQuery = repository.Test12.Entity1.Query().Where(EFExpression.OptimizeContains<Common.Queryable.Test12_Entity1>(x => listWithNullValue.Contains(x.ID)));
                    Console.WriteLine(basicQuery.ToString());
                    Console.WriteLine(optimizedQuery.ToString());

                    Assert.AreEqual(
                        TestUtility.DumpSorted(basicQuery, item => item.ID),
                        TestUtility.DumpSorted(optimizedQuery, item => item.ID));
                    Assert.AreEqual(
                        TestUtility.DumpSorted(basicQuery.ToSimple().ToList(), item => item.ID),
                        TestUtility.DumpSorted(optimizedQuery.ToSimple().ToList(), item => item.ID));
                }
            }
        }

        [TestMethod]
        public void UsingListConstructorWotChangeExpressionTest()
        {
            var id1 = Guid.NewGuid();
            Expression<Func<Common.Queryable.Test12_Entity1, bool>> originalExpression = x => new List<Guid?> { id1 }.Contains(x.GuidProperty);
            var optimizedExpression = EFExpression.OptimizeContains<Common.Queryable.Test12_Entity1>(originalExpression);
            Assert.AreEqual(originalExpression, optimizedExpression);
        }
    }
}
