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
using Rhetos.Configuration.Autofac;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Linq;

namespace CommonConcepts.Test
{
    [TestClass]
    public class HardcodedEntityTest
    {
        [TestMethod]
        public void HardcodedEntityWithDefinedAllPropertiesTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var statusWithoutIntPropertyDefined = repository.TestHardcodedEntity.SimpleHardcodedEntity.Query().Where(x => x.ID == TestHardcodedEntity.SimpleHardcodedEntity.StatusWithDefinedAllPropertity).Single();

                Assert.AreEqual("Status with defined all properties", statusWithoutIntPropertyDefined.Description);
                Assert.AreEqual(false, statusWithoutIntPropertyDefined.BoolProperty);
                Assert.AreEqual(2, statusWithoutIntPropertyDefined.IntProperty);
            }
        }

        [TestMethod]
        public void NotDefinedPropertiesShouldBeNullTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var statusWithoutIntPropertyDefined = repository.TestHardcodedEntity.SimpleHardcodedEntity.Query().Where(x => x.ID == TestHardcodedEntity.SimpleHardcodedEntity.StatusWithoutIntPropertyDefined).Single();

                Assert.AreEqual("Status with undefined int property", statusWithoutIntPropertyDefined.Description);
                Assert.AreEqual(true, statusWithoutIntPropertyDefined.BoolProperty);
                Assert.AreEqual(null, statusWithoutIntPropertyDefined.IntProperty);
            }
        }

        [TestMethod]
        public void SqlTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                repository.TestHardcodedEntity.ReferenceToHardcodedEntity.Insert(new TestHardcodedEntity.ReferenceToHardcodedEntity
                {
                    Content = "Message 1",
                    SimpleHardcodedEntityID = TestHardcodedEntity.SimpleHardcodedEntity.StatusWithDefinedAllPropertity
                });
                repository.TestHardcodedEntity.ReferenceToHardcodedEntity.Insert(new TestHardcodedEntity.ReferenceToHardcodedEntity
                {
                    Content = "Message 2",
                    SimpleHardcodedEntityID = TestHardcodedEntity.SimpleHardcodedEntity.StatusWithoutIntPropertyDefined
                });
                
                Assert.AreEqual(1, repository.TestHardcodedEntity.HardcodedEntityInSqlTest.Query().Count());
            }
        }

        [TestMethod]
        public void SpecialDescription()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var item = repository.TestHardcodedEntity.SimpleHardcodedEntity.Load(x => x.Name == "SpecialDescription").Single();
                Assert.AreEqual("a\r\n'\r\nb", item.Description);
            }
        }

        [TestMethod]
        public void ModifyingHardcodedEntityTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                TestUtility.ShouldFail<Rhetos.UserException>(() => {
                    repository.TestHardcodedEntity.SimpleHardcodedEntity.Insert(new TestHardcodedEntity.SimpleHardcodedEntity
                    {
                        Description = "Test"
                    });
                }, "It is not allowed to modify hard-coded data", "TestHardcodedEntity.SimpleHardcodedEntity");
            }
        }

        [TestMethod]
        public void HardcodedEntityAndPolymorphicTests()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                var implementation1 = new TestHardcodedEntity.ReferenceToHardcodedImplementation1 { ID = Guid.NewGuid() };
                repository.TestHardcodedEntity.ReferenceToHardcodedImplementation1.Insert(implementation1);
                var implementation2 = new TestHardcodedEntity.ReferenceToHardcodedImplementation2 { ID = Guid.NewGuid() };
                repository.TestHardcodedEntity.ReferenceToHardcodedImplementation2.Insert(implementation2);

                Assert.AreEqual(TestHardcodedEntity.SimpleHardcodedEntity.SpecialDescription, repository.TestHardcodedEntity.ReferenceToHardcoded.Query(x => x.ID == implementation1.ID).First().SimpleHardcodedEntityID.Value);
                Assert.AreEqual(TestHardcodedEntity.SimpleHardcodedEntity.StatusWithoutIntPropertyDefined, repository.TestHardcodedEntity.ReferenceToHardcoded.Query(x => x.ID == implementation2.ID).First().SimpleHardcodedEntityID.Value);
            }
        }

        [TestMethod]
        public void HardcodedEntityWithCustomEntryIdentifierTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                Assert.AreEqual(new Guid("32AA7C09-ECFE-45E9-81B7-CB761D28F351"), repository.TestHardcodedEntity.HardcodedWithCustomIdentifier.Query().Single(x => x.Name == "Entry1").ID);
                Assert.AreEqual(new Guid("213E77EE-F622-4487-8709-B30D280FB31E"), repository.TestHardcodedEntity.HardcodedWithCustomIdentifier.Query().Single(x => x.Name == "Entry2").ID);
                Assert.AreEqual(TestHardcodedEntity.HardcodedWithCustomIdentifier.Entry2, repository.TestHardcodedEntity.HardcodedWithCustomIdentifier.Query().Single(x => x.Name == "Entry2").ID);
            }
        }

        [TestMethod]
        public void HardcodedEntityWithGeneratedEntryIdentifierTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                var entry3Id = repository.TestHardcodedEntity.HardcodedWithCustomIdentifier.Query().Single(x => x.Name == "Entry3").ID;
                Assert.AreEqual(CsUtility.GenerateGuid("Entry3"), entry3Id);
                Assert.AreEqual(TestHardcodedEntity.HardcodedWithCustomIdentifier.Entry3, entry3Id);
                // The generated ID should not be changed in different version and environments, to make sure existing applications can be easily upgraded:
                Assert.AreEqual(new Guid("F138A49A-9CEF-8D2D-64FF-4AD929C85327"), entry3Id);
            }
        }
    }
}
