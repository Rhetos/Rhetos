using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Configuration.Autofac;
using Rhetos.Dom.DefaultConcepts;
using System;
using System.Linq;

namespace CommonConcepts.Test
{
    [TestClass]
    public class RhetosTestContainerTest
    {
        private const string TestNamePrefix = "RhetosTestContainerTest_";

        [TestMethod]
        public void CommitOnDisposeTest()
        {
            var id = Guid.NewGuid();
#pragma warning disable CS0618 // Type or member is obsolete
            using (var rhetos = new RhetosTestContainer("CommonConcepts.Test.dll", true))
            {
#pragma warning restore CS0618
                var repository = rhetos.Resolve<Common.DomRepository>();
                repository.TestEntity.BaseEntity.Insert(new TestEntity.BaseEntity { ID = id, Name = TestNamePrefix + "e1" });
            }

            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();
                Assert.IsTrue(repository.TestEntity.BaseEntity.Query().Any(x => x.ID == id));
            }
        }

        [TestMethod]
        public void RollbackOnDisposeTest()
        {
            var id = Guid.NewGuid();
#pragma warning disable CS0618 // Type or member is obsolete
            using (var rhetos = new RhetosTestContainer("CommonConcepts.Test.dll", false))
            {
#pragma warning restore CS0618
                var repository = rhetos.Resolve<Common.DomRepository>();
                repository.TestEntity.BaseEntity.Insert(new TestEntity.BaseEntity { ID = id, Name = TestNamePrefix + "e2" });
            }

            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();
                Assert.IsFalse(repository.TestEntity.BaseEntity.Query().Any(x => x.ID == id));
            }
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            using (var scope = TestScope.Create())
            {
                var context = scope.Resolve<Common.ExecutionContext>();
                var testItems = context.Repository.TestEntity.BaseEntity.Load(item => item.Name.StartsWith(TestNamePrefix));
                context.Repository.TestEntity.BaseEntity.Delete(testItems);
                scope.CommitAndClose();
            }
        }
    }
}
