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

using Autofac;
using CommonConcepts.Test.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Configuration.Autofac;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.TestCommon;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace CommonConcepts.Test
{
    [TestClass]
    public class StorageTest
    {
        private void AssertAreEqual(TestStorage.AllProperties expected, TestStorage.AllProperties actual)
        {
            if (expected.BinaryProperty == null)
                Assert.IsNull(actual.BinaryProperty);
            else
                Assert.IsTrue(expected.BinaryProperty.SequenceEqual(actual.BinaryProperty));
            Assert.AreEqual(expected.BoolProperty, actual.BoolProperty);
            Assert.AreEqual(expected.DateProperty, actual.DateProperty);
            Assert.AreEqual(expected.DateTimeProperty, actual.DateTimeProperty);
            Assert.AreEqual(expected.DecimalProperty, actual.DecimalProperty);
            Assert.AreEqual(expected.GuidProperty, actual.GuidProperty);
            Assert.AreEqual(expected.IntegerProperty, actual.IntegerProperty);
            Assert.AreEqual(expected.MoneyProperty, actual.MoneyProperty);
            Assert.AreEqual(expected.ShortStringProperty, actual.ShortStringProperty);
            Assert.AreEqual(expected.LongStringProperty, actual.LongStringProperty);
        }

        [TestMethod]
        public void SaveAllPropertiesTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var context = container.Resolve<Common.ExecutionContext>();

                var entity = new TestStorage.AllProperties
                {
                    ID = Guid.NewGuid(),
                    BinaryProperty = Encoding.ASCII.GetBytes("Test"),
                    BoolProperty = true,
                    DateProperty = new DateTime(2020, 1, 1),
                    DateTimeProperty = new DateTime(2020, 3, 2, 2, 3, 1),
                    DecimalProperty = 101.23423m,
                    GuidProperty = new Guid("05CE273A-C644-485F-82D7-8A3B6922F276"),
                    IntegerProperty = -2_147_483_648,
                    MoneyProperty = 23432.23m,
                    ShortStringProperty = "Test",
                    LongStringProperty = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur."
                };
                context.PersistenceStorage.Insert(entity);
                AssertAreEqual(entity, context.Repository.TestStorage.AllProperties.Load(x => x.ID == entity.ID).Single());

                entity.BinaryProperty = new byte[] { };
                entity.BoolProperty = false;
                entity.DateProperty = new DateTime(2020, 2, 2);
                entity.DateTimeProperty = new DateTime(2020, 7, 2, 2, 3, 10, 150);
                entity.DecimalProperty = 523.23353m;
                entity.GuidProperty = new Guid("EE4193C4-580A-4295-B47B-4C2B56F64002");
                entity.IntegerProperty = 34;
                entity.MoneyProperty = 1.23m;
                entity.ShortStringProperty = "Test1";
                entity.LongStringProperty = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore.";
                context.PersistenceStorage.Update(entity);
                AssertAreEqual(entity, context.Repository.TestStorage.AllProperties.Load(x => x.ID == entity.ID).Single());

                entity.BinaryProperty = null;
                entity.BoolProperty = null;
                entity.DateProperty = null;
                entity.DateTimeProperty = null;
                entity.DecimalProperty = null;
                entity.GuidProperty = null;
                entity.IntegerProperty = null;
                entity.MoneyProperty = null;
                entity.ShortStringProperty = null;
                entity.LongStringProperty = null;
                context.PersistenceStorage.Update(entity);
                AssertAreEqual(entity, context.Repository.TestStorage.AllProperties.Load(x => x.ID == entity.ID).Single());

                context.PersistenceStorage.Delete(entity);
                Assert.IsFalse(context.Repository.TestStorage.AllProperties.Load(x => x.ID == entity.ID).Any());
            }
        }

        [TestMethod]
        public void InsertAllPropertiesWithNullValueTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var context = container.Resolve<Common.ExecutionContext>();

                var entity = new TestStorage.AllProperties
                {
                    ID = Guid.NewGuid(),
                    BinaryProperty = null,
                    BoolProperty = null,
                    DateProperty = null,
                    DateTimeProperty = null,
                    DecimalProperty = null,
                    GuidProperty = null,
                    IntegerProperty = null,
                    MoneyProperty = null,
                    ShortStringProperty = null,
                    LongStringProperty = null
                };
                context.PersistenceStorage.Insert(entity);
                var entityLoadedFromDatabase = context.Repository.TestStorage.AllProperties.Load(x => x.ID == entity.ID).Single();
                AssertAreEqual(entity, entityLoadedFromDatabase);
            }
        }

        [TestMethod]
        public void RollbackTransactionOnErrorTest()
        {
            var entityID = Guid.NewGuid();
            using (var container = new RhetosTestContainer(commitChanges: true))
            {
                var context = container.Resolve<Common.ExecutionContext>();

                var entity = new TestStorage.Simple
                {
                    ID = entityID,
                    Name = "Test"
                };
                context.PersistenceStorage.Insert(new List<TestStorage.Simple> { entity });
                Assert.AreEqual(1, context.Repository.TestStorage.Simple.Load(x => x.ID == entityID).Count());
                context.PersistenceTransaction.DiscardChanges();
            }

            using (var container = new RhetosTestContainer(commitChanges: false))
            {
                var context = container.Resolve<Common.ExecutionContext>();
                Assert.AreEqual(0, context.Repository.TestStorage.Simple.Load(x => x.ID == entityID).Count());
            }
        }

        [TestMethod]
        public void DataStructureWithNoMappingTest()
        {
            var entityID = Guid.NewGuid();
            using (var container = new RhetosTestContainer())
            {
                var context = container.Resolve<Common.ExecutionContext>();

                var entity = new TestStorage.DataStructureWithNoSaveMapping
                {
                    ID = entityID,
                    Name = "Test"
                };
                TestUtility.ShouldFail(
                    () => context.PersistenceStorage.Insert(entity),
                    "There is no mapping", "TestStorage.DataStructureWithNoSaveMapping");
            }
        }

        [TestMethod]
        public void UpdateOnEntityWithNoProperty()
        {
            using (var container = new RhetosTestContainer())
            {
                var context = container.Resolve<Common.ExecutionContext>();

                var entityID = Guid.NewGuid();

                context.PersistenceStorage.Insert(new List<TestStorage.EntityWithNoProperty> { new TestStorage.EntityWithNoProperty { ID = entityID } });

                var sqlCommandBatch = container.Resolve<IPersistenceStorageCommandBatch>();

                int rowCount1 = sqlCommandBatch
                    .Execute(new[]
                    {
                        new PersistenceStorageCommand
                        {
                            CommandType = PersistenceStorageCommandType.Update,
                            Entity = new TestStorage.EntityWithNoProperty { ID = entityID },
                            EntityType = typeof(TestStorage.EntityWithNoProperty)
                        }
                    });
                Assert.AreEqual(1, rowCount1, "Event though update is not required, it should be executed for consistency, to verify if the record exists.");

                int rowCount2 = sqlCommandBatch
                    .Execute(new[]
                    {
                        new PersistenceStorageCommand
                        {
                            CommandType = PersistenceStorageCommandType.Insert,
                            Entity = new TestStorage.Simple { ID = Guid.NewGuid() },
                            EntityType = typeof(TestStorage.Simple)
                        },
                        new PersistenceStorageCommand
                        {
                            CommandType = PersistenceStorageCommandType.Update,
                            Entity = new TestStorage.EntityWithNoProperty { ID = entityID },
                            EntityType = typeof(TestStorage.EntityWithNoProperty)
                        }
                    });
                Assert.AreEqual(2, rowCount2, "Multiple updates.");

                // Event if update is not needed, repository.Update() should not throw an exception.
                context.Repository.TestStorage.EntityWithNoProperty.Update(new TestStorage.EntityWithNoProperty { ID = entityID });
            }
        }

        [TestMethod]
        public void UpdateOnEntityWithNoPropertyWithRepository()
        {
            using (var container = new RhetosTestContainer())
            {
                var context = container.Resolve<Common.ExecutionContext>();

                var item = new TestStorage.EntityWithNoProperty();
                context.Repository.TestStorage.EntityWithNoProperty.Insert(item);
                Assert.AreEqual(1, context.Repository.TestStorage.EntityWithNoProperty.Query(new[] { item.ID }).Count());

                context.Repository.TestStorage.EntityWithNoProperty.Update(item);
                Assert.AreEqual(1, context.Repository.TestStorage.EntityWithNoProperty.Query(new[] { item.ID }).Count());

                context.Repository.TestStorage.EntityWithNoProperty.Delete(item);
                Assert.AreEqual(0, context.Repository.TestStorage.EntityWithNoProperty.Query(new[] { item.ID }).Count());
            }
        }

        [TestMethod]
        public void MoneyPropertySizeAndDecimals()
        {
            using (var container = new RhetosTestContainer())
            {
                var context = container.Resolve<Common.ExecutionContext>();

                var tests = new List<(decimal Save, decimal Load)>
                {
                    (12.34100m, 12.34m),
                    (12.34900m, 12.34m),
                    (-12.3410m, -12.34m),
                    (-12.3490m, -12.34m),
                    (-922337203685477.58m, -922337203685477.58m), // T-SQL money limits.
                    (922337203685477.58m, 922337203685477.58m), // T-SQL money limits.
                    (0m, 0m),
                    (0.001m, 0m),
                    (0.009m, 0m),
                    (0.019m, 0.01m),
                    (-0.001m, 0m),
                    (-0.009m, 0m),
                    (-0.019m, -0.01m),
                };

                foreach (var test in tests)
                {
                    var entity = new TestStorage.AllProperties
                    {
                        ID = Guid.NewGuid(),
                        MoneyProperty = test.Save
                    };
                    context.PersistenceStorage.Insert(entity);
                    Assert.AreEqual(test.Load, context.Repository.TestStorage.AllProperties.Load(x => x.ID == entity.ID).Single().MoneyProperty,
                        $"The money property should be cut off on the second decimal position ({test.Save}).");
                }
            }
        }

        [TestMethod]
        public void DecimalPropertySizeAndDecimals()
        {
            using (var container = new RhetosTestContainer())
            {
                var context = container.Resolve<Common.ExecutionContext>();

                var tests = new List<(decimal Save, decimal Load)>
                {
                    (12.34000000001m, 12.34m),
                    (12.34000000009m, 12.34m),
                    (-12.34000000001m, -12.34m),
                    (-12.34000000009m, -12.34m),
                    (12.34000000011m, 12.3400000001m),
                    (12.34000000019m, 12.3400000001m),
                    (-12.34000000011m, -12.3400000001m),
                    (-12.34000000019m, -12.3400000001m),
                    (923456789012345678.0123456789m, 923456789012345678.0123456789m), // decimal(28,10) should allow 18 digits on left and 10 digits on right side of the decimal point.
                    (-923456789012345678.0123456789m, -923456789012345678.0123456789m), // decimal(28,10) should allow 18 digits on left and 10 digits on right side of the decimal point.
                    (0m, 0m),
                    (0.00000000001m, 0m),
                    (0.00000000009m, 0m),
                    (0.00000000019m, 0.0000000001m),
                    (-0.00000000001m, 0m),
                    (-0.00000000009m, 0m),
                    (-0.00000000019m, -0.0000000001m),
                };

                foreach (var test in tests)
                {
                    var entity1 = new TestStorage.AllProperties
                    {
                        ID = Guid.NewGuid(),
                        DecimalProperty = test.Save
                    };
                    context.PersistenceStorage.Insert(entity1);
                    Assert.AreEqual(test.Load, context.Repository.TestStorage.AllProperties.Load(x => x.ID == entity1.ID).Single().DecimalProperty,
                        $"The money property should be cut off on the 10th decimal position ({test.Save}).");
                }
            }
        }

        [TestMethod]
        public void ShortStringPropertySave()
        {
            using (var container = new RhetosTestContainer())
            {
                var context = container.Resolve<Common.ExecutionContext>();

                var tests = new List<string>
                {
                    "abc",
                    "ABC",
                    "",
                    null,
                    @"čćšđžČĆŠĐŽ",
                    @"`~!@#$%^&*()_+-=[]\{}|;':"",./<>?",
                    new string('a', 256),
                };

                foreach (var test in tests)
                {
                    var entity1 = new TestStorage.AllProperties
                    {
                        ID = Guid.NewGuid(),
                        ShortStringProperty = test
                    };
                    context.PersistenceStorage.Insert(entity1);
                    Assert.AreEqual(test, context.Repository.TestStorage.AllProperties.Load(x => x.ID == entity1.ID).Single().ShortStringProperty);
                }
            }
        }

        [TestMethod]
        public void ShortStringPropertyTruncationErrorTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var context = container.Resolve<Common.ExecutionContext>();

                var entity1 = new TestStorage.AllProperties
                {
                    ID = Guid.NewGuid(),
                    ShortStringProperty = "Sed ut perspiciatis unde omnis iste natus error sit voluptatem accusantium doloremque laudantium, totam rem aperiam, eaque ipsa quae ab illo inventore veritatis et quasi architecto beatae vitae dicta sunt explicabo. Nemo enim ipsam voluptatem quia voluptas sit aspernatur aut odit aut fugit, sed quia consequuntur magni dolores eos qui ratione voluptatem sequi nesciunt."
                };
                TestUtility.ShouldFail<SqlException>(() => context.PersistenceStorage.Insert(entity1),
                    "data would be truncated");
            }
        }

        [TestMethod]
        public void LongStringPropertySave()
        {
            using (var container = new RhetosTestContainer())
            {
                var context = container.Resolve<Common.ExecutionContext>();

                var tests = new List<string>
                {
                    "abc",
                    "ABC",
                    "",
                    null,
                    @"čćšđžČĆŠĐŽ",
                    @"`~!@#$%^&*()_+-=[]\{}|;':"",./<>?",
                    new string('a', 256),
                    new string('a', 17000),
                };

                foreach (var test in tests)
                {
                    var entity1 = new TestStorage.AllProperties
                    {
                        ID = Guid.NewGuid(),
                        LongStringProperty = test
                    };
                    context.PersistenceStorage.Insert(entity1);
                    Assert.AreEqual(test, context.Repository.TestStorage.AllProperties.Load(x => x.ID == entity1.ID).Single().LongStringProperty);
                }
            }
        }

        [TestMethod]
        public void DateTimeSave()
        {
            using (var container = new RhetosTestContainer())
            {
                var context = container.Resolve<Common.ExecutionContext>();
                bool usingDateTime2 = !container.Resolve<CommonConceptsDatabaseSettings>().UseLegacyMsSqlDateTime;

                var tests = new List<DateTime>
                {
                    new DateTime(1900, 1, 1),
                    new DateTime(1900, 1, 1, 1, 1, 1, 300),
                    new DateTime(3000, 12, 31),
                    new DateTime(3000, 12, 31, 23, 59, 59, 300),
                    new DateTime(2001, 2, 3, 4, 5, 6, 3),
                    new DateTime(2001, 2, 3, 4, 5, 6, 7),
                    new DateTime(2001, 2, 3, 4, 5, 6, 10),
                    new DateTime(2001, 2, 3, 4, 5, 6, 13),
                };

                foreach (var test in tests)
                {
                    var entity1 = new TestStorage.AllProperties
                    {
                        ID = Guid.NewGuid(),
                        DateTimeProperty = test
                    };
                    context.PersistenceStorage.Insert(entity1);
                    DateTime? loaded = context.Repository.TestStorage.AllProperties.Load(x => x.ID == entity1.ID).Single().DateTimeProperty;
                    if (!usingDateTime2)
                        Assert.AreEqual(test, loaded);
                    else // Old DateTime column type has 3 ms precision.
                    {
                        string info = $"Saved value: '{test}'. Loaded value: '{loaded}'.";
                        Console.WriteLine(info);
                        Assert.IsTrue(Math.Abs(test.Subtract(loaded.Value).TotalMilliseconds) <= 3, info);
                    }
                }
            }
        }

        [TestMethod]
        public void RoundDateTimePropertyTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var context = container.Resolve<Common.ExecutionContext>();

                var sampleDateTime = new DateTime(2020, 1, 1, 1, 1, 1, 1);
                var entity = new TestStorage.AllProperties
                {
                    ID = Guid.NewGuid(),
                    DateTimeProperty = sampleDateTime
                };
                context.PersistenceStorage.Insert(entity);

                var loadedEntity = context.Repository.TestStorage.AllProperties.Load(x => x.ID == entity.ID).Single();
                Assert.AreEqual(new DateTime(2020, 1, 1, 1, 1, 1, 0), loadedEntity.DateTimeProperty);
            }
        }

        [TestMethod]
        public void RoundDatePropertyTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var context = container.Resolve<Common.ExecutionContext>();

                var sampleDateTime = new DateTime(2020, 1, 1, 1, 1, 1);
                var entity = new TestStorage.AllProperties
                {
                    ID = Guid.NewGuid(),
                    DateProperty = sampleDateTime
                };
                context.PersistenceStorage.Insert(entity);

                var loadedEntity = context.Repository.TestStorage.AllProperties.Load(x => x.ID == entity.ID).Single();
                Assert.AreEqual(new DateTime(2020, 1, 1, 0, 0, 0), loadedEntity.DateProperty);
            }
        }

        [TestMethod]
        public void BatchNumberCountTest()
        {
            var commandBatchesLog = new PersistenceCommandsDecorator.Log();

            using (var container = RhetosProcessHelper.CreateTransactionScopeContainer(builder =>
                {
                    builder.RegisterDecorator<PersistenceCommandsDecorator, IPersistenceStorageCommandBatch>();
                    builder.RegisterInstance(commandBatchesLog);
                    builder.RegisterInstance(new CommonConceptsRuntimeOptions { SaveSqlCommandBatchSize = 3 });
                }
            ))
            {
                var context = container.Resolve<Common.ExecutionContext>();
                var persistenceStorage = container.Resolve<IPersistenceStorage>();

                {
                    var entites = GenerateSimpleEntites(2);
                    commandBatchesLog.Clear();
                    persistenceStorage.Insert(entites[0], entites[1]);

                    Assert.AreEqual("2", TestUtility.Dump(commandBatchesLog, batch => batch.Count));
                    AssertItemsExists(context.Repository, entites);
                }

                {
                    var entites = GenerateSimpleEntites(3);
                    commandBatchesLog.Clear();
                    persistenceStorage.Insert(entites[0], entites[1], entites[2]);

                    Assert.AreEqual("3", TestUtility.Dump(commandBatchesLog, batch => batch.Count));
                    AssertItemsExists(context.Repository, entites);
                }

                {
                    var entites = GenerateSimpleEntites(4);
                    commandBatchesLog.Clear();
                    persistenceStorage.Insert(entites[0], entites[1], entites[2], entites[3]);

                    Assert.AreEqual("3, 1", TestUtility.Dump(commandBatchesLog, batch => batch.Count));
                    AssertItemsExists(context.Repository, entites);
                }
                {
                    var entites = GenerateSimpleEntites(1);
                    commandBatchesLog.Clear();
                    persistenceStorage.Save(new[] { entites[0] }, new[] { entites[0], entites[0] }, null);

                    Assert.AreEqual("2, 1", TestUtility.Dump(commandBatchesLog, batch => batch.Count));
                    AssertItemsExists(context.Repository, entites);
                }
            }
        }

        private TestStorage.Simple[] GenerateSimpleEntites(int count)
        {
            var entites = new TestStorage.Simple[count];
            for (var i = 0; i < count; i++)
                entites[i] = new TestStorage.Simple {
                    ID = Guid.NewGuid()
                };
            return entites;
        }

        private void AssertItemsExists(Common.DomRepository repository, params TestStorage.Simple[] entites)
        {
            var nonexistentIds = new List<Guid>();
            foreach (var entity in entites)
            {
                if (!repository.TestStorage.Simple.Query(x => x.ID == entity.ID).Any())
                    nonexistentIds.Add(entity.ID);
            }

            if(nonexistentIds.Any())
                Assert.Fail($"Records with ids {string.Join(",", nonexistentIds)} are expected in the table TestStorage.Simple.");
        }

        [TestMethod]
        public void SortBySelfReferencingDependenciesInsert()
        {
            var a = new TestStorage.SelfReferencing { ID = Guid.NewGuid(), Name = "a" };
            var b = new TestStorage.SelfReferencing { ID = Guid.NewGuid(), Name = "b", ParentID = a.ID };
            var c = new TestStorage.SelfReferencing { ID = Guid.NewGuid(), Name = "c", ParentID = b.ID };
            var d = new TestStorage.SelfReferencing { ID = Guid.NewGuid(), Name = "d", ParentID = c.ID };

            var ids = new[] { a.ID, b.ID, c.ID, d.ID };

            using (var container = RhetosProcessHelper.CreateTransactionScopeContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                // Insert would fail if the records are inserted to database in incorrect order,
                // because of self-referencing FK constraint on column ParentID.
                repository.TestStorage.SelfReferencing.Insert(d, c, b, a);

                // Check if the records are in the database.
                var query = repository.TestStorage.SelfReferencing.Query(ids)
                    .Select(item => new { item.Name, ParentName = item.Parent.Name });
                Assert.AreEqual("a:, b:a, c:b, d:c", TestUtility.Dump(query.ToList(), item => $"{item.Name}:{item.ParentName}"));

                repository.TestStorage.SelfReferencing.Delete(d, c, b, a);
                Assert.AreEqual("", TestUtility.Dump(query.ToList(), item => $"{item.Name}:{item.ParentName}"));
            }

            using (var container = RhetosProcessHelper.CreateTransactionScopeContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                // Testing with opposite initial ordering, just in case.
                repository.TestStorage.SelfReferencing.Insert(a, b, c, d);

                // Check if the records are in the database.
                var query = repository.TestStorage.SelfReferencing.Query(ids)
                    .Select(item => new { item.Name, ParentName = item.Parent.Name });
                Assert.AreEqual("a:, b:a, c:b, d:c", TestUtility.Dump(query.ToList(), item => $"{item.Name}:{item.ParentName}"));

                repository.TestStorage.SelfReferencing.Delete(a, b, c, d);
                Assert.AreEqual("", TestUtility.Dump(query.ToList(), item => $"{item.Name}:{item.ParentName}"));
            }
        }

        [TestMethod]
        public void SortBySelfReferencingDependenciesWithMock()
        {
            // NOTE:
            // Sorting of updated and deleted record works only with the information provided in the given records.
            // It will not load fresh records from database to analyze the dependencies.

            var commandsMock = new PersistenceCommandsMock();

            using (var container = RhetosProcessHelper.CreateTransactionScopeContainer(
                builder => builder.RegisterInstance<IPersistenceStorageCommandBatch>(commandsMock)))
            {
                var persistenceStorage = container.Resolve<IPersistenceStorage>();

                // Input is list for records: "Name:ParentName" or "Name" if it has no parent.
                var tests = new (string Input, string ExpectedOrder)[]
                {
                    ("a, b:a, c:b, d:c", "a, b, c, d"),
                    ("a:b, b:c, c:d, d", "d, c, b, a"),
                    ("a, b, c", "a, b, c"), // Keep initial order if there are no dependencies.
                    ("c, b, a", "c, b, a"), // Keep initial order if there are no dependencies.
                };

                foreach (var test in tests)
                {
                    Console.WriteLine($"Test input: {test.Input}");

                    var records = test.Input.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(r => r.Split(':'))
                        .Select(r => new
                        {
                            ID = Guid.NewGuid(),
                            Name = r[0],
                            ParentName = r.Length > 1 ? r[1] : null
                        })
                        .ToDictionary(r => r.Name);

                    var entities = records.Values
                        .Select(r => new TestStorage.SelfReferencing
                        {
                            ID = r.ID,
                            Name = r.Name,
                            ParentID = !string.IsNullOrEmpty(r.ParentName) ? (Guid?)records[r.ParentName].ID : null
                        }).ToList();

                    commandsMock.CommandBatchesLog.Clear();

                    persistenceStorage.Insert(entities);

                    var saved = commandsMock.CommandBatchesLog
                        .SelectMany(batch => batch.Select(command => ((TestStorage.SelfReferencing)command.Entity).Name));

                    Assert.AreEqual(test.ExpectedOrder, TestUtility.Dump(saved));
                }
            }
        }
    }
}
