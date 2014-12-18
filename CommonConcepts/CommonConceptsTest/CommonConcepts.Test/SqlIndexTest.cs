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
using Rhetos.Utilities;
using Rhetos.TestCommon;
using Rhetos;
using Rhetos.Configuration.Autofac;

namespace CommonConcepts.Test
{
    [TestClass]
    public class SqlIndexTest
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
        public void SqlIndexMultipleClustered()
        {
            using (var container = new RhetosTestContainer())
            {
                var sqlExecuter = container.Resolve<ISqlExecuter>();

                string sqlIndexInfo =
                    @"SELECT
	                    i.type_desc, i.is_unique
                    FROM
	                    sys.indexes i
	                    INNER JOIN sys.objects o ON o.object_id = i.object_id
	                    INNER JOIN sys.schemas s ON s.schema_id = o.schema_id
                    WHERE
	                    s.Name = 'TestSqlWorkarounds'
	                    AND o.Name = 'TestIndex'
	                    AND i.name = 'IX_TestIndex_A_B'";

                Assert.AreEqual(
                    "CLUSTERED, False",
                    ReportSqlQueryResult(sqlExecuter, sqlIndexInfo));

                string sqlIndexColumnsInfo =
                    @"SELECT
	                    ic.key_ordinal, c.name
                    FROM
	                    sys.indexes i
	                    INNER JOIN sys.objects o ON o.object_id = i.object_id
	                    INNER JOIN sys.schemas s ON s.schema_id = o.schema_id
	                    INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
	                    INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                    WHERE
	                    s.Name = 'TestSqlWorkarounds'
	                    AND o.Name = 'TestIndex'
	                    AND i.name = 'IX_TestIndex_A_B'
                    ORDER BY
	                    1, 2";

                Assert.AreEqual(
                    "1, A\r\n2, B",
                    ReportSqlQueryResult(sqlExecuter, sqlIndexColumnsInfo));
            }
        }
    }
}
