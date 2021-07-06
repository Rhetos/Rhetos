﻿/*
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

using CommonConcepts.Test;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.TestCommon;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;

namespace Rhetos.Persistence.Test
{
    [TestClass]
    public class BaseSqlExecuterTest
    {
        private static string Report(params Common.Principal[] principals)
            => string.Join(", ", principals.OrderBy(p => p.ID).Select(p => $"ID:{p.ID} Name:{p.Name}"));

        [TestMethod]
        public void ExecuteReaderTest()
        {
            using (var scope = TestScope.Create())
            {
                var persistenceTransaction = scope.Resolve<IPersistenceTransaction>();
                var executionContext = scope.Resolve<Common.ExecutionContext>();

                var principalNamePrefix = "BaseExecuter_";
                var principalNameSuffix = "_Test";
                var principal1 = new Common.Principal { ID = Guid.NewGuid(), Name = principalNamePrefix + Guid.NewGuid().ToString() };
                var principal2 = new Common.Principal { ID = Guid.NewGuid(), Name = principalNamePrefix + Guid.NewGuid().ToString() + principalNameSuffix };
                executionContext.Repository.Common.Principal.Insert(principal1, principal2);

                var baseSqlExecuter = new BaseSqlExecuter(persistenceTransaction);

                var results = new List<Common.Principal>();
                baseSqlExecuter.ExecuteReaderRaw("SELECT ID, Name FROM Common.Principal WHERE Name LIKE {0}+'%' AND Name LIKE '%'+@suffix",
                    new object[] { principalNamePrefix, new SqlParameter("@suffix", principalNameSuffix) },
                    reader => results.Add(new Common.Principal { ID = reader.GetGuid(0), Name = reader.GetString(1) }));

                Assert.AreEqual(Report(principal2), Report(results.ToArray()));
            }
        }

        [TestMethod]
        public void ExecuteSqlTest()
        {
            using (var scope = TestScope.Create())
            {
                var persistenceTransaction = scope.Resolve<IPersistenceTransaction>();
                var executionContext = scope.Resolve<Common.ExecutionContext>();

                var principalNamePrefix = "BaseExecuter_";
                var principal1 = new Common.Principal { ID = Guid.NewGuid(), Name = principalNamePrefix + Guid.NewGuid().ToString() };
                var principal2 = new Common.Principal { ID = Guid.NewGuid(), Name = principalNamePrefix + Guid.NewGuid().ToString() };

                var baseSqlExecuter = new BaseSqlExecuter(persistenceTransaction);

                baseSqlExecuter.ExecuteSqlRaw(@"
                        INSERT INTO Common.Principal (ID, Name) VALUES({0}, {1});
                        INSERT INTO Common.Principal (ID, Name) VALUES(@principal2ID, @principal2Name);
                    ", new object[] { principal1.ID, principal1.Name, new SqlParameter("@principal2ID", principal2.ID),
                    new SqlParameter("@principal2Name", principal2.Name) });

                var results = executionContext.Repository.Common.Principal.Query(x => x.Name.StartsWith(principalNamePrefix)).ToSimple().ToArray();

                Assert.AreEqual(Report(principal1, principal2), Report(results));
            }
        }

        [TestMethod]
        public void NullParameterTest()
        {
            using (var scope = TestScope.Create())
            {
                var persistenceTransaction = scope.Resolve<IPersistenceTransaction>();
                var executionContext = scope.Resolve<Common.ExecutionContext>();

                var principalID = Guid.NewGuid();
                var baseSqlExecuter = new BaseSqlExecuter(persistenceTransaction);

                baseSqlExecuter.ExecuteSqlRaw("INSERT INTO TestEntity.Principal (ID, Name) VALUES({0}, {1});", new object[] { principalID, null });

                var result = executionContext.Repository.TestEntity.Principal.Query(x => x.ID == principalID).ToSimple().First();

                Assert.IsNull(result.Name);
            }
        }

        [TestMethod]
        public void InvalidParameterNameTest()
        {
            using (var scope = TestScope.Create())
            {
                var persistenceTransaction = scope.Resolve<IPersistenceTransaction>();
                var executionContext = scope.Resolve<Common.ExecutionContext>();
                var baseSqlExecuter = new BaseSqlExecuter(persistenceTransaction);

                var sqlParameter = new SqlParameter("@__p0", "Test");
                TestUtility.ShouldFail<ArgumentException>(
                    () => baseSqlExecuter.ExecuteSqlRaw("INSERT INTO TestEntity.Principal (Name) VALUES(@__p0);", new object[] { sqlParameter }),
                    "parameter name should not start with", "@__p");
            }
        }
    }
}