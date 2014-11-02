using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.TestCommon;
using Rhetos.Configuration.Autofac;
using Rhetos.Utilities;
using Rhetos.Processing;
using System.Collections.Generic;
using System;

namespace CommonConcepts.Test
{
    [TestClass]
    public class RowPermissionsTest
    {
        [TestMethod]
        public void TestOfTest()
        {
            using (var container = new RhetosTestContainer(true))
            {
                var repository = container.Resolve<Common.DomRepository>();
                Random rnd = new System.Random();

                List<RPTestModule.TestEntity> list = new List<RPTestModule.TestEntity>();
                for (int i = 0; i < 5; i++)
                {
                    RPTestModule.TestEntity entity = new RPTestModule.TestEntity()
                    {
                        name = "sasa",
                        value = rnd.Next(0, 100),
                    };
                    list.Add(entity);
                }

                repository.RPTestModule.TestEntity.Insert(list);
            }
        }
    }
}
