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
using Rhetos;
using Rhetos.Dom.DefaultConcepts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CommonConcepts.Test
{
    [TestClass]
    public class UnitOfWorkFactoryTest
    {
        private const string LogActionName = "UnitOfWorkFactoryTest";

        [TestMethod]
        public void IndependantDatabaseTransaction()
        {
            var report = new List<string>();

            foreach (bool commit1 in new[] { false, true })
                foreach (bool commit2 in new[] { false, true })
                {
                    Guid id1 = Guid.NewGuid();
                    Guid id2 = Guid.NewGuid();

                    using (var scope1 = TestScope.Create())
                    {
                        var repository1 = scope1.Resolve<Common.DomRepository>();
                        repository1.Common.AddToLog.Execute(new Common.AddToLog { Action = LogActionName, TableName = "", ItemId = id1 });

                        var unitOfWorkFactory = scope1.Resolve<IUnitOfWorkFactory>();
                        using (var scope2 = unitOfWorkFactory.CreateScope())
                        {
                            var repository2 = scope2.Resolve<Common.DomRepository>();
                            repository2.Common.AddToLog.Execute(new Common.AddToLog { Action = LogActionName, TableName = "", ItemId = id2 });

                            if (commit2)
                                scope2.CommitAndClose();
                            else
                                scope2.RollbackAndClose();

                            using (var scope = TestScope.Create())
                                Console.WriteLine(scope.Resolve<Common.DomRepository>().Common.Log.Query(l => l.ItemId == id2).Any());
                        }

                        if (commit1)
                            scope1.CommitAndClose();
                    }

                    using (var scope = TestScope.Create())
                    {
                        var repository = scope.Resolve<Common.DomRepository>();
                        bool saved1 = repository.Common.Log.Query(l => l.ItemId == id1).Any();
                        bool saved2 = repository.Common.Log.Query(l => l.ItemId == id2).Any();
                        report.Add($"{(commit1 ? "1" : "-")}{(commit2 ? "2" : "-")}:{(saved1 ? "1" : "-")}{(saved2 ? "2" : "-")}");
                    }
                }

            Assert.AreEqual(
                // Each element shows which unit-of-work is committed, and which data was actually saved in database.
                // Element format "commit1 commit2 : saved1 saved2"
                "--:--, -2:-2, 1-:1-, 12:12",
                string.Join(", ", report));

            // Cleanup:
            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();
                var testData = repository.Common.Log.Query(l => l.Action == LogActionName && l.TableName == ""); // 'TableName' is included in filter because it is required for SQL Server to use the available index.
                repository.Common.Log.Delete(testData);
            }
        }
    }
}
