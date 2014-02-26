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
using Rhetos.Dom.DefaultConcepts;

namespace CommonConcepts.Test
{
    [TestClass]
    public class DatabaseExtensionsTest
    {
        private static string[] BasicNames = new[] { @"abc1", @"bbb2", @".*(\" };

        private static string[][] BasicTests = new string[][]
        {
            new[] { "%b%", "abc1, bbb2" },
            new[] { "b%", "bbb2" },
            new[] { "%1", "abc1" },
            new[] { "%", @".*(\, abc1, bbb2" },
            new[] { "%3%", "" },
            new[] { "", "" },
            new[] { "bbb2", "bbb2" },
            new[] { "abc%1", "abc1" },
            new[] { "%abc1%", "abc1" },
            new[] { "%ABC1%", "abc1" },
            new[] { "ABC1", "abc1" },
            new[] { "abc_", "abc1" },
            new[] { "abc__", "" },
            new[] { "%.%", @".*(\" },
            new[] { "%*%", @".*(\" },
            new[] { "%(%", @".*(\" },
            new[] { @"%\%", @".*(\" },
        };

        [TestMethod]
        public void LikeSql()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestDatabaseExtensions.Simple" });
                executionContext.SqlExecuter.ExecuteSql(BasicNames.Select(name =>
                        "INSERT INTO TestDatabaseExtensions.Simple (Name) SELECT '" + name + "'"));

                var repository = new Common.DomRepository(executionContext);

                foreach (var test in BasicTests)
                {
                    var loaded = repository.TestDatabaseExtensions.Simple.Query().Where(item => item.Name.Like(test[0]));
                    Assert.AreEqual(test[1], TestUtility.DumpSorted(loaded, item => item.Name), "Pattern: '" + test[0] + "'");
                }
            }
        }

        [TestMethod]
        public void LikeCs()
        {
            var items = BasicNames.Select(name => new TestDatabaseExtensions.Simple { Name = name });

            foreach (var test in BasicTests)
            {
                var loaded = items.Where(item => item.Name.Like(test[0]));
                Assert.AreEqual(test[1], TestUtility.DumpSorted(loaded, item => item.Name), "Pattern: '" + test[0] + "'");
            }
        }
    }
}
