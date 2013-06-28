/*
    Copyright (C) 2013 Omega software d.o.o.

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
using Rhetos.Utilities;
using Rhetos.TestCommon;

namespace CommonConcepts.Test
{
    [TestClass]
    public class SqlWorkaroundsTest
    {
        private static string ReportSqlQueryResult(ISqlExecuter sqlExecuter, string sql)
        {
            var rows = new List<string>();
            sqlExecuter.ExecuteReader(sql,
                reader =>
                {
                    var fields = new List<string>();
                    for (int c = 0; c < reader.FieldCount; c++)
                        fields.Add(reader[c].ToString());
                    rows.Add(string.Join(", ", fields));
                });
            return string.Join("\r\n", rows);
        }

        [TestMethod]
        public void SqlFunction()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                Assert.AreEqual("11", ReportSqlQueryResult(executionContext.SqlExecuter, "SELECT * FROM TestSqlWorkarounds.Fun2(10)"));
            }
        }

        [TestMethod]
        public void SqlObject()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[]
                {
                    "DELETE FROM TestSqlWorkarounds.E",
                    "INSERT INTO TestSqlWorkarounds.E (I) VALUES (100)"
                });

                string report = "";
                executionContext.SqlExecuter.ExecuteReader(
                    @"SELECT E.I, V1.I1, V2.I2
                        FROM TestSqlWorkarounds.E
                        INNER JOIN TestSqlWorkarounds.V1 ON V1.ID = E.ID
                        INNER JOIN TestSqlWorkarounds.V2 ON V2.ID = E.ID",
                    reader => report += reader.GetInt32(0) + ", " + reader.GetInt32(1) + ", " + reader.GetInt32(2) + ".");
                Assert.AreEqual("100, 101, 102.", report);

                report = "";
                executionContext.SqlExecuter.ExecuteReader(
                    @"SELECT X FROM TestSqlWorkarounds.V3 ORDER BY X",
                    reader => report += reader.GetInt32(0) + ".");
                Assert.AreEqual("101.102.", report);
            }
        }

        [TestMethod]
        public void ExecuteSqlProcedure()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestSqlWorkarounds.Person" });
                executionContext.SqlExecuter.ExecuteSql(Enumerable.Range(1, 100).Select(x =>
                    "INSERT INTO TestSqlWorkarounds.Person (Name) VALUES ('User" + x.ToString() +"')"));

                var repository = new Common.DomRepository(executionContext);
                TestUtility.ShouldFail(() => repository.TestSqlWorkarounds.PersonInfo.All(), "Filter must be used", "filter", "PersonFilter", "must be used");

                var result = repository.TestSqlWorkarounds.PersonInfo.Filter(new TestSqlWorkarounds.PersonFilter { NamePattern = "%1%", LimitResultCount = 4 });
                Assert.AreEqual("User1 5, User10 6, User100 7, User11 6", TestUtility.Dump(result, item => item.Person.Name + " " + item.NameLength));
            }
        }
    }
}
