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
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.TestCommon;
using Rhetos.Configuration.Autofac;
using Rhetos.Utilities;

namespace CommonConcepts.Test
{
    [TestClass]
    public class QueryableExtensionTest
    {
        [TestMethod]
        public void Simple()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                var id1 = Guid.NewGuid();
                var id2 = Guid.NewGuid();
                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        "DELETE FROM Test11.Source;",
                        "INSERT INTO Test11.Source (ID, Name) SELECT '" + id1 + "', 'a';",
                        "INSERT INTO Test11.Source (ID, Name) SELECT '" + id2 + "', 'b';"
                    });

                var all = repository.Test11.QE.Query().ToArray();
                Array.Sort(all, (a, b) => string.Compare(a.Info, b.Info));

                Assert.AreEqual(2, all.Length);

                Assert.AreEqual("ax", all[0].Info);
                Assert.AreEqual(id1, all[0].Base.ID);
                Assert.AreEqual("a", all[0].Base.Name);
                Assert.AreEqual(id1, all[0].ID);

                Assert.AreEqual("bx", all[1].Info);
                Assert.AreEqual(id2, all[1].Base.ID);
                Assert.AreEqual("b", all[1].Base.Name);
                Assert.AreEqual(id2, all[1].ID);
            }
        }

        [TestMethod]
        public void QueryableFilterUsingBase()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        "DELETE FROM Test11.Source;",
                        "INSERT INTO Test11.Source (Name) SELECT 'a';",
                        "INSERT INTO Test11.Source (Name) SELECT 'b';"
                    });

                var filtered = repository.Test11.QE.Query().Where(qe => qe.Base.Name == "b");
                Assert.AreEqual("bx", filtered.Single().Info);
            }
        }

        [TestMethod]
        public void SelectUsingBase()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        "DELETE FROM Test11.Source;",
                        "INSERT INTO Test11.Source (Name) SELECT 'a';",
                        "INSERT INTO Test11.Source (Name) SELECT 'b';"
                    });

                var q = repository.Test11.QE.Query()
                    .Select(qe => new { qe.Info, qe.Base.Name });
                Assert.AreEqual("a ax, b bx", TestUtility.DumpSorted(q.ToArray(), item => item.Name + " " + item.Info));
            }
        }

        [TestMethod]
        public void BrowseUsingBase()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        "DELETE FROM Test11.Source;",
                        "INSERT INTO Test11.Source (Name) SELECT 'a';",
                        "INSERT INTO Test11.Source (Name) SELECT 'b';"
                    });

                var browse = repository.Test11.QEBrowse.Query();
                Assert.AreEqual("a ax, b bx", TestUtility.DumpSorted(browse, item => item.Name + " " + item.Info));
            }
        }

        [TestMethod]
        public void UseExecutionContext()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        "DELETE FROM Test11.Source;",
                        "INSERT INTO Test11.Source (Name) SELECT 'a';",
                        "INSERT INTO Test11.Source (Name) SELECT 'b';"
                    });

                var userInfo = container.Resolve<IUserInfo>();
                Assert.IsFalse(string.IsNullOrEmpty(userInfo.UserName));
                string expected = string.Format("a {0}, b {0}", userInfo.UserName);
                Console.WriteLine(expected);

                Assert.AreEqual(expected, TestUtility.DumpSorted(repository.Test11.QEContext.Query(), item => item.UserInfo));
            }
        }
    }
}
