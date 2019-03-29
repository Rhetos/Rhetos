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
using Rhetos.TestCommon;
using Rhetos.Configuration.Autofac;
using Rhetos.Utilities;
using Rhetos.Dom.DefaultConcepts;

namespace CommonConcepts.Test
{
    [TestClass]
    public class HardcodedEntityTest
    {
        [TestMethod]
        public void DataInDatabseTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                var inprogressData = repository.TestHardcodedEntity.MessageStatus.Query().FirstOrDefault(x => x.ID == TestHardcodedEntity.MessageStatus.InProgress);
                Assert.AreEqual("In progress", inprogressData.UserDescription);
                Assert.AreEqual(false, inprogressData.VisibleToUser);

                var deliveredData = repository.TestHardcodedEntity.MessageStatus.Query().FirstOrDefault(x => x.ID == TestHardcodedEntity.MessageStatus.Delivered);
                Assert.AreEqual("Delivered", deliveredData.UserDescription);
                Assert.AreEqual(true, deliveredData.VisibleToUser);

                var readData = repository.TestHardcodedEntity.MessageStatus.Query().FirstOrDefault(x => x.ID == TestHardcodedEntity.MessageStatus.Read);
                Assert.AreEqual("Read", readData.UserDescription);
                Assert.AreEqual(true, readData.VisibleToUser);

                var deletedData = repository.TestHardcodedEntity.MessageStatus.Query().FirstOrDefault(x => x.ID == TestHardcodedEntity.MessageStatus.Deleted);
                Assert.AreEqual("Deleted", deletedData.UserDescription);
                Assert.AreEqual(false, deletedData.VisibleToUser);
            }
        }

        [TestMethod]
        public void SimpleTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                repository.TestHardcodedEntity.Message.Insert(new TestHardcodedEntity.Message {
                    Content = "Message 1",
                    MessageStatusID = TestHardcodedEntity.MessageStatus.Delivered
                });
                repository.TestHardcodedEntity.Message.Insert(new TestHardcodedEntity.Message
                {
                    Content = "Message 2",
                    MessageStatusID = TestHardcodedEntity.MessageStatus.Delivered
                });

                Assert.AreEqual(2, repository.TestHardcodedEntity.Message.Query().Count(x => x.MessageStatusID == TestHardcodedEntity.MessageStatus.Delivered));

                repository.TestHardcodedEntity.MarkAllMessagesAsRead.Execute(new TestHardcodedEntity.MarkAllMessagesAsRead());
                Assert.AreEqual(2, repository.TestHardcodedEntity.Message.Query().Count(x => x.MessageStatusID == TestHardcodedEntity.MessageStatus.Read));
            }
        }

        [TestMethod]
        public void SqlTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                repository.TestHardcodedEntity.Message.Insert(new TestHardcodedEntity.Message
                {
                    Content = "Message 1",
                    MessageStatusID = TestHardcodedEntity.MessageStatus.InProgress
                });
                repository.TestHardcodedEntity.Message.Insert(new TestHardcodedEntity.Message
                {
                    Content = "Message 2",
                    MessageStatusID = TestHardcodedEntity.MessageStatus.Delivered
                });

                Assert.AreEqual(1, repository.TestHardcodedEntity.UnreadMessage.Query().Count());
            }
        }
    }
}
