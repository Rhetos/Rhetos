using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Configuration.Autofac;
using Rhetos.TestCommon;
using System.Data.SqlClient;
using Common;

namespace CommonConcepts.Test
{
    [TestClass]
    public class StorageTest
    {
        private void AssertAreEqual(Storage.AllProperties expected, Storage.AllProperties actual)
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

                var entity = new Storage.AllProperties
                {
                    ID = Guid.NewGuid(),
                    BinaryProperty = Encoding.ASCII.GetBytes("Test"),
                    BoolProperty = true,
                    DateProperty = new DateTime(2020, 1, 1),
                    DateTimeProperty = new DateTime(2020, 3, 2, 2, 3, 1),
                    DecimalProperty = 101.23423m,
                    GuidProperty = new Guid("05CE273A-C644-485F-82D7-8A3B6922F276"),
                    IntegerProperty = 301,
                    MoneyProperty = 23432.23m,
                    ShortStringProperty = "Test",
                    LongStringProperty = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur."
                };
                context.PersistanceStorage.Insert(entity);
                AssertAreEqual(entity, context.Repository.Storage.AllProperties.Load(x => x.ID == entity.ID).Single());

                entity.BinaryProperty = Encoding.ASCII.GetBytes("Test1");
                entity.BoolProperty = false;
                entity.DateProperty = new DateTime(2020, 2, 2);
                entity.DateTimeProperty = new DateTime(2020, 7, 2, 2, 3, 10);
                entity.DecimalProperty = 523.23353m;
                entity.GuidProperty = new Guid("EE4193C4-580A-4295-B47B-4C2B56F64002");
                entity.IntegerProperty = 34;
                entity.MoneyProperty = 1.23m;
                entity.ShortStringProperty = "Test1";
                entity.LongStringProperty = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore.";
                context.PersistanceStorage.Update(entity);
                AssertAreEqual(entity, context.Repository.Storage.AllProperties.Load(x => x.ID == entity.ID).Single());

                context.PersistanceStorage.Delete(entity);
                Assert.IsFalse(context.Repository.Storage.AllProperties.Load(x => x.ID == entity.ID).Any());
            }
        }

        [TestMethod]
        public void InsertAllPropertiesWithNullValueTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var context = container.Resolve<Common.ExecutionContext>();

                var entity = new Storage.AllProperties
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
                context.PersistanceStorage.Insert(entity);
                var entityLoadedFromDatabase = context.Repository.Storage.AllProperties.Load(x => x.ID == entity.ID).Single();
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

                var entity = new Storage.Simple
                {
                    ID = entityID,
                    Name = "Test"
                };
                context.PersistanceStorage.Insert(new List<Storage.Simple> { entity });
                context.PersistenceTransaction.DiscardChanges();
            }

            using (var container = new RhetosTestContainer(commitChanges: false))
            {
                var context = container.Resolve<Common.ExecutionContext>();
                Assert.AreEqual(0, context.Repository.Storage.Simple.Load(x => x.ID == entityID).Count());
            }
        }

        [TestMethod]
        public void DataStructureWithNoMappingTest()
        {
            var entityID = Guid.NewGuid();
            using (var container = new RhetosTestContainer())
            {
                var context = container.Resolve<Common.ExecutionContext>();

                var entity = new Storage.DataStructureWithNoSaveMapping
                {
                    ID = entityID,
                    Name = "Test"
                };
                TestUtility.ShouldFail(
                    () => context.PersistanceStorage.Insert(entity),
                    "There is no mapping");
            }
        }

        [TestMethod]
        public void UpdateNotExecutedOnEntityWithNoProperty()
        {
            using (var container = new RhetosTestContainer())
            {
                var context = container.Resolve<Common.ExecutionContext>();

                var entityID = Guid.NewGuid();

                context.PersistanceStorage.Insert(new List<Storage.EntityWithNoProperty> { new Storage.EntityWithNoProperty { ID = entityID } });

                var rowsAffected1 = context.PersistanceStorage.StartBatch()
                    .Add(new Storage.EntityWithNoProperty { ID = entityID }, PersistanceStorageCommandType.Update)
                    .Execute();
                Assert.AreEqual(0, rowsAffected1, "The entity does not have any property except ID which is the key of the entity so an update command is not required.");

                var rowsAffected2 = context.PersistanceStorage.StartBatch()
                    .Add(new Storage.Simple { ID = Guid.NewGuid() }, PersistanceStorageCommandType.Insert)
                    .Add(new Storage.EntityWithNoProperty { ID = entityID }, PersistanceStorageCommandType.Update)
                    .Execute();

                Assert.AreEqual(1, rowsAffected2, "The entity does not have any property except ID which is the key of the entity so an update command is not required, only the insert command will be executed.");

            }
        }

        [TestMethod]
        public void MoneyPropertyCutDecimalPostionTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var context = container.Resolve<Common.ExecutionContext>();

                var entity1 = new Storage.AllProperties
                {
                    ID = Guid.NewGuid(),
                    MoneyProperty = 12.34100m
                };
                context.PersistanceStorage.Insert(entity1);
                Assert.AreEqual(12.34m, context.Repository.Storage.AllProperties.Load(x => x.ID == entity1.ID).Single().MoneyProperty,
                    "The money property should be cut off on the second decimal position.");

                var entity2 = new Storage.AllProperties
                {
                    ID = Guid.NewGuid(),
                    MoneyProperty = 12.34900m
                };
                context.PersistanceStorage.Insert(entity2);
                Assert.AreEqual(12.34m, context.Repository.Storage.AllProperties.Load(x => x.ID == entity2.ID).Single().MoneyProperty,
                    "The money property should be cut off on the second decimal position.");

            }
        }

        [TestMethod]
        public void DecimalPropertyCutDecimalPostionTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var context = container.Resolve<Common.ExecutionContext>();

                var entity1 = new Storage.AllProperties
                {
                    ID = Guid.NewGuid(),
                    DecimalProperty = 12.34000000001m
                };
                context.PersistanceStorage.Insert(entity1);
                Assert.AreEqual(12.34m, context.Repository.Storage.AllProperties.Load(x => x.ID == entity1.ID).Single().DecimalProperty,
                    "The money property should be cut off on the 10th decimal position.");

                var entity2 = new Storage.AllProperties
                {
                    ID = Guid.NewGuid(),
                    DecimalProperty = 12.34000000009m
                };
                context.PersistanceStorage.Insert(entity2);
                Assert.AreEqual(12.34m, context.Repository.Storage.AllProperties.Load(x => x.ID == entity2.ID).Single().DecimalProperty,
                    "The money property should be cut off on the 10th decimal position.");

            }
        }

        [TestMethod]
        public void ShortStringPropertyTruncationErrorTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var context = container.Resolve<Common.ExecutionContext>();

                var entity1 = new Storage.AllProperties
                {
                    ID = Guid.NewGuid(),
                    ShortStringProperty = "Sed ut perspiciatis unde omnis iste natus error sit voluptatem accusantium doloremque laudantium, totam rem aperiam, eaque ipsa quae ab illo inventore veritatis et quasi architecto beatae vitae dicta sunt explicabo. Nemo enim ipsam voluptatem quia voluptas sit aspernatur aut odit aut fugit, sed quia consequuntur magni dolores eos qui ratione voluptatem sequi nesciunt."
                };
                TestUtility.ShouldFail<SqlException>(() => context.PersistanceStorage.Insert(entity1),
                    "data would be truncated");
            }
        }

        [TestMethod]
        public void RoundDateTimePropertyTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var context = container.Resolve<Common.ExecutionContext>();

                var sampleDateTime = new DateTime(2020, 1, 1, 1, 1, 1, 1);
                var entity = new Storage.AllProperties
                {
                    ID = Guid.NewGuid(),
                    DateTimeProperty = sampleDateTime
                };
                context.PersistanceStorage.Insert(entity);

                var loadedEntity = context.Repository.Storage.AllProperties.Load(x => x.ID == entity.ID).Single();
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
                var entity = new Storage.AllProperties
                {
                    ID = Guid.NewGuid(),
                    DateProperty = sampleDateTime
                };
                context.PersistanceStorage.Insert(entity);

                var loadedEntity = context.Repository.Storage.AllProperties.Load(x => x.ID == entity.ID).Single();
                Assert.AreEqual(new DateTime(2020, 1, 1, 0, 0, 0), loadedEntity.DateProperty);
            }
        }

        [TestMethod]
        public void BatchNumberCountTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var context = container.Resolve<Common.ExecutionContext>();
                var persistanceStorageMapping = container.Resolve<IPersistanceStorageObjectMappings>();
                var batchExecutionReport = new List<Tuple<int, string>>();
                var commandBatch = new SqlCommandBatch(context.PersistenceTransaction, persistanceStorageMapping, 3, (rowsAffected, command)=> {
                    batchExecutionReport.Add(new Tuple<int, string>(rowsAffected, command.CommandText));
                });

                {
                    var entites = GenerateSimpleEntites(2);
                    commandBatch.Add(entites[0], PersistanceStorageCommandType.Insert)
                        .Add(entites[1], PersistanceStorageCommandType.Insert)
                        .Execute();
                    AssertRowsAffected(batchExecutionReport, new[] { 2 });
                    AssertItemsExists(context.Repository, entites);
                }

                {
                    var entites = GenerateSimpleEntites(3);
                    batchExecutionReport.Clear();
                    commandBatch.Add(entites[0], PersistanceStorageCommandType.Insert)
                        .Add(entites[1], PersistanceStorageCommandType.Insert)
                        .Add(entites[2], PersistanceStorageCommandType.Insert)
                        .Execute();
                    AssertRowsAffected(batchExecutionReport, new[] { 3 });
                    AssertItemsExists(context.Repository, entites);
                }

                {
                    var entites = GenerateSimpleEntites(4);
                    batchExecutionReport.Clear();
                    commandBatch.Add(entites[0], PersistanceStorageCommandType.Insert)
                        .Add(entites[1], PersistanceStorageCommandType.Insert)
                        .Add(entites[2], PersistanceStorageCommandType.Insert)
                        .Add(entites[3], PersistanceStorageCommandType.Insert)
                        .Execute();
                    AssertRowsAffected(batchExecutionReport, new[] { 3, 1 });
                    AssertItemsExists(context.Repository, entites);
                }
                {
                    var entites = GenerateSimpleEntites(1);
                    batchExecutionReport.Clear();
                    commandBatch.Add(entites[0], PersistanceStorageCommandType.Insert)
                        .Add(entites[0], PersistanceStorageCommandType.Update)
                        .Add(entites[0], PersistanceStorageCommandType.Update)
                        .Execute();
                    AssertRowsAffected(batchExecutionReport, new[] { 3 });
                    AssertItemsExists(context.Repository, entites);
                }
            }
        }

        private Storage.Simple[] GenerateSimpleEntites(int count)
        {
            var entites = new Storage.Simple[count];
            for (var i = 0; i < count; i++)
                entites[i] = new Storage.Simple {
                    ID = Guid.NewGuid()
                };
            return entites;
        }

        private void AssertRowsAffected(List<Tuple<int, string>> report, int[] rowsAffectedInBatches)
        {
            Assert.AreEqual(report.Count, rowsAffectedInBatches.Length);
            for (var i = 0; i < report.Count; i++)
                Assert.AreEqual(report[i].Item1, rowsAffectedInBatches[i]);
        }

        private void AssertItemsExists(DomRepository repository, params Storage.Simple[] entites)
        {
            var nonexistentIds = new List<Guid>();
            foreach (var entity in entites)
            {
                if (!repository.Storage.Simple.Query(x => x.ID == entity.ID).Any())
                    nonexistentIds.Add(entity.ID);
            }

            if(nonexistentIds.Any())
                Assert.Fail($"Records with ids {string.Join(",", nonexistentIds)} are expected in the table Storage.Simple.");
        }
    }
}
