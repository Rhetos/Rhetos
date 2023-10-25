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
using Rhetos.TestCommon;
using Rhetos.Utilities.ApplicationConfiguration;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.Utilities.Test
{
    [TestClass]
    public class ConfigurationKeyComparerTest
    {
        [TestMethod]
        public void SameKeys()
        {
            var sameKeys = new[]
            {
                new[] { "a", "A" },
                new[] { "a.a.a", "A.a.A", "a:a:a", "A.A:a" },
                new[] { "a a a" },
                new[] { "a a a.a", "a A A:A" },
            };

            var keyWithGroup = sameKeys.SelectMany((keyGroup, group) => keyGroup.Select(key => (key, group)));

            var comparer = new ConfigurationKeyComparer();

            foreach (var key1 in keyWithGroup)
                foreach (var key2 in keyWithGroup)
                    Assert.AreEqual(key1.group == key2.group, comparer.Equals(key1.key, key2.key));
        }

        [TestMethod]
        public void KeyComparerDictionary()
        {
            var sameKeys = new[]
            {
                new[] { "a", "A" },
                new[] { "a.a.a", "A.a.A", "a:a:a", "A.A:a" },
                new[] { "a a a" },
                new[] { "a a a.a", "a A A:A" },
            };

            var dictionary = new Dictionary<string, object>(new ConfigurationKeyComparer());
            foreach (var key in sameKeys.SelectMany(keyGroup => keyGroup))
                dictionary[key] = null;

            var firstInEachGroup = TestUtility.DumpSorted(sameKeys.Select(keyGroup => keyGroup.First()));
            var keysInDictionary = TestUtility.DumpSorted(dictionary.Keys);
            Assert.AreEqual(firstInEachGroup, keysInDictionary);
        }
    }
}
