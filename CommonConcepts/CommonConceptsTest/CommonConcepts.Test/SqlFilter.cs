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
using NHibernate;
using NHibernate.Transform;

namespace CommonConcepts.Test
{
    [TestClass]
    public class SqlFilter
    {
        [TestMethod]
        public void Spike1_SqlExecuteFunction()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var report = new StringBuilder();
                executionContext.SqlExecuter.ExecuteReader(
                    SqlGetSome(new DateTime(2001, 2, 3, 4, 5, 6)),
                    reader => report.AppendLine(reader["Code"].ToString() + ", " + reader.GetDateTime(2).ToString("s")));

                Console.WriteLine(report);
                Assert.AreEqual("1, 2001-02-03T04:05:06\r\n2, 2001-02-04T04:05:06\r\n", report.ToString());
            }
        }

        private static string SqlGetSome(DateTime dateTime)
        {
            return string.Format(
                "SELECT ID, Code, Start FROM TestSqlFilter.GetSome('{0}') ORDER BY Code",
                dateTime.ToString("u").Replace("Z", ""));
        }

        [TestMethod]
        public void Spike2_NHInitializationTime()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestSqlFilter.Simple" });
                var repository = new Common.DomRepository(executionContext);
                var result = repository.TestSqlFilter.Simple.All();
                var report = string.Join("|", result.Select(item => item.Code + ", " + item.Start.Value.ToString("s")));
                Assert.AreEqual("", report);
            }
        }

        [TestMethod]
        public void Spike3_NHExecuteFunction()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var sql = SqlGetSome(new DateTime(2001, 2, 3, 4, 5, 6));
                Console.WriteLine(sql);

                var nhSqlQuery = executionContext.NHibernateSession.CreateSQLQuery(sql)
                    /*.AddScalar("ID", NHibernateUtil.Guid)
                    .AddScalar("Code", NHibernateUtil.Int32)
                    .AddScalar("Start", NHibernateUtil.DateTime)
                    //.AddEntity("TestSqlFilter.Simple")*/
                    .SetResultTransformer(Transformers.AliasToBean(typeof(TestSqlFilter.Simple)));

                var result = nhSqlQuery.List<TestSqlFilter.Simple>();
                var report = string.Join("|", result.Select(item => item.Code + ", " + item.Start.Value.ToString("s")));

                Console.WriteLine(report);
                Assert.AreEqual("1, 2001-02-03T04:05:06|2, 2001-02-04T04:05:06", report);
            }
        }

        private static string SqlGetRef(DateTime dateTime)
        {
            return string.Format(
                "SELECT ID, Name, OtherID, Finish FROM TestSqlFilter.GetRef('{0}') ORDER BY Name",
                dateTime.ToString("u").Replace("Z", ""));
        }

        [TestMethod]
        public void Spike4_NHEntityReference()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var id1 = new Guid("11111111-1111-1111-1111-111111111111");
                var id2 = new Guid("22222222-1111-1111-1111-111111111111");
                executionContext.SqlExecuter.ExecuteSql(new[] 
                {
                    "DELETE FROM TestSqlFilter.Simple",
                    "INSERT INTO TestSqlFilter.Simple (ID, Code, Start) VALUES ('" + id1 + "', 1, GETDATE())",
                    "INSERT INTO TestSqlFilter.Simple (ID, Code, Start) VALUES ('" + id2 + "', 2, GETDATE())"
                });

                var sql = SqlGetRef(new DateTime(2001, 2, 3, 4, 5, 6));
                Console.WriteLine(sql);

                var nhSqlQuery = executionContext.NHibernateSession.CreateSQLQuery(sql)
                    .SetResultTransformer(Transformers.AliasToBean(typeof(TestSqlFilter.Ref)));

                var result = nhSqlQuery.List<TestSqlFilter.Ref>();
                result[0].Other = executionContext.NHibernateSession.Load<TestSqlFilter.Simple>(result[0].Other.ID);
                result[1].Other = executionContext.NHibernateSession.Load<TestSqlFilter.Simple>(result[1].Other.ID);

                var report = string.Join("|", result.Select(item => item.Name + ", " + item.Other.Code + ", " + item.Finish.Value.Day));
                Console.WriteLine(report);
                Assert.AreEqual("name_1, 1, 4|name_2, 2, 5", report);
            }
        }

        [TestMethod]
        public void Spike5_NHEntityReferenceBetter()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var id1 = new Guid("11111111-1111-1111-1111-111111111111");
                var id2 = new Guid("22222222-1111-1111-1111-111111111111");
                executionContext.SqlExecuter.ExecuteSql(new[] 
                {
                    "DELETE FROM TestSqlFilter.Simple",
                    "INSERT INTO TestSqlFilter.Simple (ID, Code, Start) VALUES ('" + id1 + "', 1, GETDATE())",
                    "INSERT INTO TestSqlFilter.Simple (ID, Code, Start) VALUES ('" + id2 + "', 2, GETDATE())"
                });

                var sql = "SELECT * FROM TestSqlFilter.GetRef(:dateTime) ORDER BY Name";
                var sqlParameter = new DateTime(2001, 2, 3, 4, 5, 6);
                Console.WriteLine(sql);

                var nhSqlQuery = executionContext.NHibernateSession.CreateSQLQuery(sql)
                    .AddEntity(typeof(TestSqlFilter.Ref))
                    .SetDateTime("dateTime", sqlParameter);
                var result = nhSqlQuery.List<TestSqlFilter.Ref>();

                var report = string.Join("|", result.Select(item => item.Name + ", " + item.Other.Code + ", " + item.Finish.Value.Day));
                Console.WriteLine(report);
                Assert.AreEqual("name_1, 1, 4|name_2, 2, 5", report);
            }
        }
    }
}
