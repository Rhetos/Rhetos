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
using Rhetos.CommonConcepts.Test.Mocks;
using Rhetos.Dom.DefaultConcepts.Authorization;
using Rhetos.Utilities;

namespace Rhetos.CommonConcepts.Test
{
    [TestClass]
    public class RequestAndGlobalCacheTest
    {
        [TestMethod]
        public void DifferentConnectionStrings()
        {
            var cs1a = new ConnectionString("1");
            var cs1b = new ConnectionString("1");
            var cs1c = new ConnectionString("1");
            var cs2 = new ConnectionString("2");

            const string key1 = "key1";
            const string value1a = "value1a";
            const string value1b = "value1b";
            const string key2 = "key2";
            const string value2a = "value2a";
            const string value2b = "value2b";

            using (var t1a = new FakePersistenceTransaction())
            {
                var cache = new RequestAndGlobalCache(new ConsoleLogProvider(), new RhetosAppOptions(), t1a, cs1a);
                cache.GetOrAdd(key1, () => value1a, immutable: false);
                cache.GetOrAdd(key2, () => value2a, immutable: true);
                Assert.AreEqual(value1a, cache.Get<string>(key1));
                Assert.AreEqual(value2a, cache.Get<string>(key2));
                t1a.RollbackAndClose();
            }

            using (var t1b = new FakePersistenceTransaction())
            {
                var cache = new RequestAndGlobalCache(new ConsoleLogProvider(), new RhetosAppOptions(), t1b, cs1b);
                Assert.AreEqual(null, cache.Get<string>(key1)); // Previous failed transaction should have discarded non-immutable records from the cache.
                Assert.AreEqual(value2a, cache.Get<string>(key2));

                cache.GetOrAdd(key1, () => value1b, immutable: false);
                cache.GetOrAdd(key2, () => value2b, immutable: true);
                Assert.AreEqual(value1b, cache.Get<string>(key1));
                Assert.AreEqual(value2a, cache.Get<string>(key2));

                cache.Set(key2, value2b, immutable: true);
                Assert.AreEqual(value2b, cache.Get<string>(key2));
                t1b.CommitAndClose();
            }

            using (var t1c = new FakePersistenceTransaction())
            {
                var cache = new RequestAndGlobalCache(new ConsoleLogProvider(), new RhetosAppOptions(), t1c, cs1c);
                Assert.AreEqual(value1b, cache.Get<string>(key1));
                Assert.AreEqual(value2b, cache.Get<string>(key2));
            }

            using (var t2 = new FakePersistenceTransaction())
            {
                var cache = new RequestAndGlobalCache(new ConsoleLogProvider(), new RhetosAppOptions(), t2, cs2);
                Assert.AreEqual(null, cache.Get<string>(key1)); // Different connection string (different database) uses a separate cache.
                Assert.AreEqual(null, cache.Get<string>(key2));
            }
        }
    }
}
