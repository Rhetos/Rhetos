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
    public class PessimisticLockingTest
    {
        [TestMethod]
        public void UpdateLocked()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                var groupId = Guid.NewGuid();
                var id1 = Guid.NewGuid();
                var id2 = Guid.NewGuid();
                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        "DELETE FROM TestPessimisticLocking.Article;",
                        "DELETE FROM Common.ExclusiveLock;",
                        "INSERT INTO TestPessimisticLocking.ArticleGroup (ID, Name) SELECT '" + groupId + "', 'ggg';",
                        "INSERT INTO TestPessimisticLocking.Article (ID, Name, ParentID) SELECT '" + id1 + "', 'aaa', '" + groupId + "';",
                        "INSERT INTO TestPessimisticLocking.Article (ID, Name, ParentID) SELECT '" + id2 + "', 'bbb', '" + groupId + "';",
                    });

                var articleRepos = repository.TestPessimisticLocking.Article;
                var lockRepos = repository.Common.ExclusiveLock;

                var articles = articleRepos.Load();

                foreach (var article in articles)
                    article.Name = article.Name + "1";
                articleRepos.Update(articles);
                Assert.AreEqual("aaa1, bbb1", TestUtility.DumpSorted(articleRepos.Query(), item => item.Name), "updated without locks");

                foreach (var article in articles)
                    article.Name = article.Name + "2";
                var myLock = new Common.ExclusiveLock
                {
                    UserName = "OtherUser",
                    Workstation = container.Resolve<IUserInfo>().Workstation,
                    ResourceType = "TestPessimisticLocking.Article",
                    ResourceID = id2,
                    LockStart = DbTime(container),
                    LockFinish = DbTime(container).AddSeconds(10)
                };
                lockRepos.Insert(new[] { myLock });
                TestUtility.ShouldFail(() => articleRepos.Update(articles), id2.ToString(), "OtherUser");

                myLock.UserName = container.Resolve<IUserInfo>().UserName;
                myLock.Workstation = "OtherWorkstation";
                lockRepos.Update(new[] { myLock });
                TestUtility.ShouldFail(() => articleRepos.Update(articles), id2.ToString(), "OtherWorkstation");

                myLock.Workstation = container.Resolve<IUserInfo>().Workstation;
                lockRepos.Update(new[] { myLock });
                articleRepos.Update(articles);
                Assert.AreEqual("aaa12, bbb12", TestUtility.DumpSorted(articleRepos.Query(), item => item.Name), "updated with owned locks");
            }
        }

        private static DateTime DbTime(RhetosTestContainer container)
        {
            return SqlUtility.GetDatabaseTime(container.Resolve<ISqlExecuter>());
        }

        [TestMethod]
        public void ParentLocked()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                var parentId0 = Guid.NewGuid();
                var parentId1 = Guid.NewGuid();
                var id0 = Guid.NewGuid();
                var id1 = Guid.NewGuid();
                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        "DELETE FROM TestPessimisticLocking.Article;",
                        "DELETE FROM TestPessimisticLocking.ArticleGroup;",
                        "DELETE FROM Common.ExclusiveLock;",
                        "INSERT INTO TestPessimisticLocking.ArticleGroup (ID, Name) SELECT '" + parentId0 + "', 'group1';",
                        "INSERT INTO TestPessimisticLocking.ArticleGroup (ID, Name) SELECT '" + parentId1 + "', 'group2';",
                        "INSERT INTO TestPessimisticLocking.Article (ID, Name, ParentID) SELECT '" + id0 + "', 'aaa', '" + parentId0 + "';",
                        "INSERT INTO TestPessimisticLocking.Article (ID, Name, ParentID) SELECT '" + id1 + "', 'bbb', '" + parentId1 + "';"
                    });

                var articleRepos = repository.TestPessimisticLocking.Article;
                var lockRepos = repository.Common.ExclusiveLock;

                var groups = repository.TestPessimisticLocking.ArticleGroup.Load().OrderBy(item => item.Name).ToArray();
                var articles = articleRepos.Load().OrderBy(item => item.Name).ToArray();

                foreach (var article in articles)
                    article.Name = article.Name + "1";
                articleRepos.Update(articles);
                Assert.AreEqual("aaa1, bbb1", TestUtility.DumpSorted(articleRepos.Query(), item => item.Name), "updated without locks");

                // Update detail with locked parent:

                foreach (var article in articles)
                    article.Name = article.Name + "2";
                var myLock = new Common.ExclusiveLock
                {
                    UserName = "OtherUser",
                    Workstation = container.Resolve<IUserInfo>().Workstation,
                    ResourceType = "TestPessimisticLocking.ArticleGroup",
                    ResourceID = parentId0,
                    LockStart = DbTime(container),
                    LockFinish = DbTime(container).AddSeconds(10)
                };
                lockRepos.Insert(new[] { myLock });
                TestUtility.ShouldFail(() => articleRepos.Update(articles), parentId0.ToString(), "OtherUser");

                myLock.UserName = container.Resolve<IUserInfo>().UserName;
                myLock.Workstation = "OtherWorkstation";
                lockRepos.Update(new[] { myLock });
                TestUtility.ShouldFail(() => articleRepos.Update(articles), parentId0.ToString(), "OtherWorkstation");

                myLock.UserName = container.Resolve<IUserInfo>().UserName;
                myLock.Workstation = container.Resolve<IUserInfo>().Workstation;
                lockRepos.Update(new[] { myLock });
                articleRepos.Update(articles);
                Assert.AreEqual("aaa12, bbb12", TestUtility.DumpSorted(articleRepos.Query(), item => item.Name), "updated with OWNED parent lock");

                // Remove detail from locked parent (by deleting or updating Parent reference):

                myLock.UserName = "OtherUser";
                lockRepos.Update(new[] { myLock });
                articles[0].ParentID = groups[1].ID;

                Assert.IsTrue(articles.All(item => item.ParentID != myLock.ResourceID), "New values do not contain locked parents, but old values do");
                TestUtility.ShouldFail(() => articleRepos.Update(articles), parentId0.ToString(), "OtherUser");

                TestUtility.ShouldFail(() => articleRepos.Delete(new[] { articles[0] }), parentId0.ToString(), "OtherUser");

                myLock.UserName = container.Resolve<IUserInfo>().UserName;
                lockRepos.Update(new[] { myLock });
                articleRepos.Update(articles);
                Assert.AreEqual("aaa12, bbb12", TestUtility.DumpSorted(articleRepos.Query(), item => item.Name), "Updated with OWNED old parent lock");

                // Insert new detail into locked parent:

                myLock.UserName = "OtherUser";
                lockRepos.Update(new[] { myLock });
                var newArticle = new TestPessimisticLocking.Article { ID = Guid.NewGuid(), Name = "ccc", ParentID = groups[0].ID };
                TestUtility.ShouldFail(() => articleRepos.Insert(new[] { newArticle }), parentId0.ToString(), "OtherUser");

                myLock.UserName = container.Resolve<IUserInfo>().UserName;
                lockRepos.Update(new[] { myLock });
                articleRepos.Insert(new[] { newArticle });
                Assert.AreEqual("aaa12, bbb12, ccc", TestUtility.DumpSorted(articleRepos.Query(), item => item.Name), "Inserted with OWNED new parent lock");
            }
        }

        [TestMethod]
        public void LockFinish()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                var parentId = Guid.NewGuid();
                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        "DELETE FROM TestPessimisticLocking.Article;",
                        "DELETE FROM TestPessimisticLocking.ArticleGroup;",
                        "DELETE FROM Common.ExclusiveLock;",
                        "INSERT INTO TestPessimisticLocking.ArticleGroup (ID, Name) SELECT '" + parentId + "', 'group1';",
                        "INSERT INTO TestPessimisticLocking.Article (ID, Name, ParentID) SELECT '" + Guid.NewGuid() + "', 'aaa', '" + parentId + "';"
                    });

                var lockRepos = repository.Common.ExclusiveLock;
                var articleRepos = repository.TestPessimisticLocking.Article;

                var group = repository.TestPessimisticLocking.ArticleGroup.Load().Single();
                var article = articleRepos.Load().Single();

                // Active and past lock:

                var myLock = new Common.ExclusiveLock
                {
                    UserName = "OtherUser",
                    Workstation = container.Resolve<IUserInfo>().Workstation,
                    ResourceType = "TestPessimisticLocking.Article",
                    ResourceID = article.ID,
                    LockStart = DbTime(container),
                    LockFinish = DbTime(container).AddSeconds(10)
                };
                lockRepos.Insert(new[] { myLock });
                article.Name = article.Name + "1";
                TestUtility.ShouldFail(() => articleRepos.Update(new[] { article }), article.ID.ToString(), "OtherUser");

                myLock.LockFinish = DbTime(container).AddSeconds(-10);
                lockRepos.Update(new[] { myLock });
                articleRepos.Update(new[] { article });
                Assert.AreEqual("aaa1", TestUtility.DumpSorted(articleRepos.Query(), item => item.Name), "Inactive lock");

                // Active and past lock on parent:

                myLock = new Common.ExclusiveLock
                {
                    UserName = "OtherUser",
                    Workstation = container.Resolve<IUserInfo>().Workstation,
                    ResourceType = "TestPessimisticLocking.ArticleGroup",
                    ResourceID = group.ID,
                    LockStart = DbTime(container),
                    LockFinish = DbTime(container).AddSeconds(10)
                };
                lockRepos.Insert(new[] { myLock });
                article.Name = article.Name + "2";
                TestUtility.ShouldFail(() => articleRepos.Update(new[] { article }), group.ID.ToString(), "OtherUser");

                myLock.LockFinish = DbTime(container).AddSeconds(-10);
                lockRepos.Update(new[] { myLock });
                articleRepos.Update(new[] { article });
                Assert.AreEqual("aaa12", TestUtility.DumpSorted(articleRepos.Query(), item => item.Name), "Inactive parent lock");
            }
        }

        static void AssertInRange(DateTime value, DateTime start, DateTime end)
        {
            const double errorMarginSec = 0.01; // Rounding on saving to database may modify the number.
            Assert.IsTrue(value >= start.AddSeconds(-errorMarginSec), value.ToString("o") + " should be greater than " + start.ToString("o"));
            Assert.IsTrue(value <= end.AddSeconds(errorMarginSec), value.ToString("o") + " should be less than " + end.ToString("o"));
            Console.WriteLine(start.ToString("o") + " <= " + value.ToString("o") + " <= " + end.ToString("o") + " (error margin " + errorMarginSec + " sec)");
        }

        const int defaultLockMinutes = 15;

        [TestMethod]
        public void ActionSetLock_Basic()
        {
            using (var container = new RhetosTestContainer())
            {
                var parentId = Guid.NewGuid();
                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        "DELETE FROM TestPessimisticLocking.Article;",
                        "DELETE FROM TestPessimisticLocking.ArticleGroup;",
                        "DELETE FROM Common.ExclusiveLock;",
                        "INSERT INTO TestPessimisticLocking.ArticleGroup (ID, Name) SELECT '" + parentId + "', 'group1';",
                        "INSERT INTO TestPessimisticLocking.Article (ID, Name, ParentID) SELECT '" + Guid.NewGuid() + "', 'aaa', '" + parentId + "';"
                    });

                var repository = container.Resolve<Common.DomRepository>();
                var article = repository.TestPessimisticLocking.Article.Load().Single();

                TestSetLock(new Common.SetLock { ResourceType = "TestPessimisticLocking.Article", ResourceID = article.ID }, repository, container.Resolve<IUserInfo>());
                var myLock = repository.Common.ExclusiveLock.Load().Single();
                Assert.AreEqual("TestPessimisticLocking.Article", myLock.ResourceType);
                Assert.AreEqual(article.ID, myLock.ResourceID);
                Assert.AreEqual(container.Resolve<IUserInfo>().UserName, myLock.UserName);
                Assert.AreEqual(container.Resolve<IUserInfo>().Workstation, myLock.Workstation);
                var now = DbTime(container);
                AssertInRange(myLock.LockStart.Value, now.AddSeconds(-1), now);
                AssertInRange(myLock.LockFinish.Value, now.AddMinutes(defaultLockMinutes).AddSeconds(-1), now.AddMinutes(defaultLockMinutes));
            }
        }

        [TestMethod]
        public void ActionSetLock_OtherUser()
        {
            using (var container = new RhetosTestContainer())
            {
                var parentId = Guid.NewGuid();
                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        "DELETE FROM TestPessimisticLocking.Article;",
                        "DELETE FROM TestPessimisticLocking.ArticleGroup;",
                        "DELETE FROM Common.ExclusiveLock;",
                        "INSERT INTO TestPessimisticLocking.ArticleGroup (ID, Name) SELECT '" + parentId + "', 'group1';",
                        "INSERT INTO TestPessimisticLocking.Article (ID, Name, ParentID) SELECT '" + Guid.NewGuid() + "', 'aaa', '" + parentId + "';"
                    });

                var repository = container.Resolve<Common.DomRepository>();
                var article = repository.TestPessimisticLocking.Article.Load().Single();

                var oldLock = new Common.ExclusiveLock
                {
                    UserName = "OtherUser",
                    Workstation = container.Resolve<IUserInfo>().Workstation,
                    ResourceType = "TestPessimisticLocking.Article",
                    ResourceID = article.ID,
                    LockStart = DbTime(container),
                    LockFinish = DbTime(container).AddSeconds(10)
                };
                repository.Common.ExclusiveLock.Insert(new[] { oldLock });

                TestUtility.ShouldFail(() => TestSetLock(new Common.SetLock { ResourceType = "TestPessimisticLocking.Article", ResourceID = article.ID }, repository, container.Resolve<IUserInfo>()),
                    "OtherUser", article.ID.ToString());
            }
        }

        [TestMethod]
        public void ActionSetLock_OtherUserObsoleteLock()
        {
            using (var container = new RhetosTestContainer())
            {
                var parentId = Guid.NewGuid();
                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        "DELETE FROM TestPessimisticLocking.Article;",
                        "DELETE FROM TestPessimisticLocking.ArticleGroup;",
                        "DELETE FROM Common.ExclusiveLock;",
                        "INSERT INTO TestPessimisticLocking.ArticleGroup (ID, Name) SELECT '" + parentId + "', 'group1';",
                        "INSERT INTO TestPessimisticLocking.Article (ID, Name, ParentID) SELECT '" + Guid.NewGuid() + "', 'aaa', '" + parentId + "';"
                    });

                var repository = container.Resolve<Common.DomRepository>();
                var article = repository.TestPessimisticLocking.Article.Load().Single();

                var oldLock = new Common.ExclusiveLock
                {
                    UserName = "OtherUser",
                    Workstation = container.Resolve<IUserInfo>().Workstation,
                    ResourceType = "TestPessimisticLocking.Article",
                    ResourceID = article.ID,
                    LockStart = DbTime(container).AddDays(-1),
                    LockFinish = DbTime(container).AddDays(-1).AddSeconds(10)
                };
                repository.Common.ExclusiveLock.Insert(new[] { oldLock });

                TestSetLock(new Common.SetLock { ResourceType = "TestPessimisticLocking.Article", ResourceID = article.ID }, repository, container.Resolve<IUserInfo>());
                var myLock = repository.Common.ExclusiveLock.Load().Single();
                Assert.AreEqual("TestPessimisticLocking.Article", myLock.ResourceType);
                Assert.AreEqual(article.ID, myLock.ResourceID);
                Assert.AreEqual(container.Resolve<IUserInfo>().UserName, myLock.UserName);
                Assert.AreEqual(container.Resolve<IUserInfo>().Workstation, myLock.Workstation);
                var now = DbTime(container);
                AssertInRange(myLock.LockStart.Value, now.AddSeconds(-1), now);
                AssertInRange(myLock.LockFinish.Value, now.AddMinutes(defaultLockMinutes).AddSeconds(-1), now.AddMinutes(defaultLockMinutes));
            }
        }

        [TestMethod]
        public void ActionSetLock_MyRedundantLock()
        {
            using (var container = new RhetosTestContainer())
            {
                var parentId = Guid.NewGuid();
                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        "DELETE FROM TestPessimisticLocking.Article;",
                        "DELETE FROM TestPessimisticLocking.ArticleGroup;",
                        "DELETE FROM Common.ExclusiveLock;",
                        "INSERT INTO TestPessimisticLocking.ArticleGroup (ID, Name) SELECT '" + parentId + "', 'group1';",
                        "INSERT INTO TestPessimisticLocking.Article (ID, Name, ParentID) SELECT '" + Guid.NewGuid() + "', 'aaa', '" + parentId + "';"
                    });

                var repository = container.Resolve<Common.DomRepository>();

                var article = repository.TestPessimisticLocking.Article.Load().Single();

                var oldLock = new Common.ExclusiveLock
                {
                    UserName = container.Resolve<IUserInfo>().UserName,
                    Workstation = container.Resolve<IUserInfo>().Workstation,
                    ResourceType = "TestPessimisticLocking.Article",
                    ResourceID = article.ID,
                    LockStart = DbTime(container).AddSeconds(-10),
                    LockFinish = DbTime(container).AddSeconds(4)
                };
                repository.Common.ExclusiveLock.Insert(new[] { oldLock });

                TestSetLock(new Common.SetLock { ResourceType = "TestPessimisticLocking.Article", ResourceID = article.ID }, repository, container.Resolve<IUserInfo>());
                var myLock = repository.Common.ExclusiveLock.Load().Single();
                Assert.AreEqual("TestPessimisticLocking.Article", myLock.ResourceType);
                Assert.AreEqual(article.ID, myLock.ResourceID);
                Assert.AreEqual(container.Resolve<IUserInfo>().UserName, myLock.UserName);
                Assert.AreEqual(container.Resolve<IUserInfo>().Workstation, myLock.Workstation);
                var now = DbTime(container);
                AssertInRange(myLock.LockStart.Value, now.AddSeconds(-1), now);
                AssertInRange(myLock.LockFinish.Value, now.AddMinutes(defaultLockMinutes).AddSeconds(-1), now.AddMinutes(defaultLockMinutes));
            }
        }

        private void TestSetLock(Common.SetLock parameters, Common.DomRepository repository, Rhetos.Utilities.IUserInfo userInfo)
        {
            repository.Common.SetLock.Execute(parameters);
        }

        [TestMethod]
        public void ActionReleaseLock_Basic()
        {
            using (var container = new RhetosTestContainer())
            {
                var parentId = Guid.NewGuid();
                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        "DELETE FROM TestPessimisticLocking.Article;",
                        "DELETE FROM TestPessimisticLocking.ArticleGroup;",
                        "DELETE FROM Common.ExclusiveLock;",
                        "INSERT INTO TestPessimisticLocking.ArticleGroup (ID, Name) SELECT '" + parentId + "', 'group1';",
                        "INSERT INTO TestPessimisticLocking.Article (ID, Name, ParentID) SELECT '" + Guid.NewGuid() + "', 'aaa', '" + parentId + "';"
                    });

                var repository = container.Resolve<Common.DomRepository>();
                var article = repository.TestPessimisticLocking.Article.Load().Single();

                TestReleaseLock(new Common.ReleaseLock { ResourceType = "TestPessimisticLocking.Article", ResourceID = article.ID }, repository, container.Resolve<IUserInfo>());
                Assert.AreEqual(0, repository.Common.ExclusiveLock.Query().Count());

                var oldLock = new Common.ExclusiveLock
                {
                    UserName = container.Resolve<IUserInfo>().UserName,
                    Workstation = container.Resolve<IUserInfo>().Workstation,
                    ResourceType = "TestPessimisticLocking.Article",
                    ResourceID = article.ID,
                    LockStart = DbTime(container).AddSeconds(-10),
                    LockFinish = DbTime(container).AddSeconds(4)
                };
                repository.Common.ExclusiveLock.Insert(new[] { oldLock });
                Assert.AreEqual(1, repository.Common.ExclusiveLock.Query().Count());

                TestReleaseLock(new Common.ReleaseLock { ResourceType = "TestPessimisticLocking.NonexistingEntity", ResourceID = article.ID }, repository, container.Resolve<IUserInfo>());
                Assert.AreEqual(1, repository.Common.ExclusiveLock.Query().Count());

                TestReleaseLock(new Common.ReleaseLock { ResourceType = "TestPessimisticLocking.Article", ResourceID = Guid.NewGuid() }, repository, container.Resolve<IUserInfo>());
                Assert.AreEqual(1, repository.Common.ExclusiveLock.Query().Count());

                TestReleaseLock(new Common.ReleaseLock { ResourceType = "TestPessimisticLocking.Article", ResourceID = article.ID }, repository, container.Resolve<IUserInfo>());
                Assert.AreEqual(0, repository.Common.ExclusiveLock.Query().Count());
            }
        }

        [TestMethod]
        public void ActionReleaseLock_OtherUser()
        {
            using (var container = new RhetosTestContainer())
            {
                var parentId = Guid.NewGuid();
                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        "DELETE FROM TestPessimisticLocking.Article;",
                        "DELETE FROM TestPessimisticLocking.ArticleGroup;",
                        "DELETE FROM Common.ExclusiveLock;",
                        "INSERT INTO TestPessimisticLocking.ArticleGroup (ID, Name) SELECT '" + parentId + "', 'group1';",
                        "INSERT INTO TestPessimisticLocking.Article (ID, Name, ParentID) SELECT '" + Guid.NewGuid() + "', 'aaa', '" + parentId + "';"
                    });

                var repository = container.Resolve<Common.DomRepository>();
                var article = repository.TestPessimisticLocking.Article.Load().Single();

                var oldLock = new Common.ExclusiveLock
                {
                    UserName = "OtherUser",
                    Workstation = container.Resolve<IUserInfo>().Workstation,
                    ResourceType = "TestPessimisticLocking.Article",
                    ResourceID = article.ID,
                    LockStart = DbTime(container).AddSeconds(0),
                    LockFinish = DbTime(container).AddSeconds(10)
                };
                repository.Common.ExclusiveLock.Insert(new[] { oldLock });
                Assert.AreEqual(1, repository.Common.ExclusiveLock.Query().Count());

                TestReleaseLock(new Common.ReleaseLock { ResourceType = "TestPessimisticLocking.Article", ResourceID = article.ID }, repository, container.Resolve<IUserInfo>());
                Assert.AreEqual(1, repository.Common.ExclusiveLock.Query().Count(), "Server should silently ignore invalid calls to release lock.");
            }
        }

        private void TestReleaseLock(Common.ReleaseLock parameters, Common.DomRepository repository, Rhetos.Utilities.IUserInfo userInfo)
        {
            repository.Common.ReleaseLock.Execute(parameters);
        }
    }
}
