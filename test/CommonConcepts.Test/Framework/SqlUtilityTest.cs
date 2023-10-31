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
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Utilities;
using System;
using System.Linq;

namespace CommonConcepts.Test.Framework
{
    [TestClass]
    public class SqlUtilityTest
    {
        [TestMethod]
        public void AnonymousUserContextTest()
        {
            using (var scope = TestScope.Create(b => b.ConfigureFakeUser(null)))
            {
                var repository = scope.Resolve<Common.DomRepository>();

                var newEntry = new Common.AddToLog { Action = Guid.NewGuid().ToString() };
                repository.Common.AddToLog.Execute(newEntry);

                var logEntry = repository.Common.LogReader.Load(entry => entry.Action == newEntry.Action).Single();

                Assert.AreEqual("Rhetos:", logEntry.ContextInfo);
            }
        }

        [TestMethod]
        public void GetDatabaseTimeTest()
        {
            // More detailed tests are implemented in the DatabaseTimeCacheTest class.
            // This is only a smoke test for SqlUtility.

            using var scope = TestScope.Create();

            var sqlExecuter = scope.Resolve<ISqlExecuter>();
            var sqlUtility = scope.Resolve<ISqlUtility>();

            _ = Enumerable.Range(0, 4).Select(x => sqlUtility.GetDatabaseTime(sqlExecuter)).ToList(); // Caching initialization.

            var notCachedDatabaseTime = ((MsSqlUtility)sqlUtility).GetDatabaseTimeUncached(sqlExecuter);
            var cachedTime = sqlUtility.GetDatabaseTime(sqlExecuter);

            Console.WriteLine(notCachedDatabaseTime.ToString("o"));
            Console.WriteLine(cachedTime.ToString("o"));

            Assert.IsTrue(notCachedDatabaseTime - cachedTime <= TimeSpan.FromSeconds(0.01));
            Assert.IsTrue(cachedTime - notCachedDatabaseTime <= TimeSpan.FromSeconds(0.01));
        }
    }
}
