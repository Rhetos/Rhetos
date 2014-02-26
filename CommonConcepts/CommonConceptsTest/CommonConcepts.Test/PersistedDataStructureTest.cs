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
using System.Linq.Expressions;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.TestCommon;
using Test9._Helper;

namespace CommonConcepts.Test
{
    [TestClass]
    public class PersistedDataStructureTest
    {
        private static string ReportSource(Common.DomRepository repository)
        {
            var loadedData = repository.Test6.Comp.Query().Select(item => item.Name + item.Num.CastToString()).ToList();
            string report = string.Join(", ", loadedData.OrderBy(s => s));
            Console.WriteLine(report);
            return report;
        }

        private static string ReportPersisted(Common.DomRepository repository)
        {
            var loadedData = repository.Test6.Pers.Query().Select(item => item.Name + item.Num.CastToString()).ToList();
            string report = string.Join(", ", loadedData.OrderBy(s => s));
            Console.WriteLine(report);
            return report;
        }

        [TestMethod]
        public void UpdateCache()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM Test6.Pers;" });

                Assert.AreEqual("a1, b2, c3", ReportSource(repository), "source computation");
                Assert.AreEqual("", ReportPersisted(repository), "initial");

                repository.Test6.Pers.Recompute();
                Assert.AreEqual("a1, b2, c3", ReportPersisted(repository), "recompute");

                repository.Test6.Pers.Delete(repository.Test6.Pers.Query().Where(item => item.Num == 1 || item.Num == 3));
                Assert.AreEqual("b2", ReportPersisted(repository), "after delete");
                repository.Test6.Pers.Recompute();
                Assert.AreEqual("a1, b2, c3", ReportPersisted(repository), "after delete recompute");

                repository.Test6.Pers.Insert(new[] { new Test6.Pers { Name = "a", Num = 0 }, new Test6.Pers { Name = "d", Num = 4 } });
                Assert.AreEqual("a0, a1, b2, c3, d4", ReportPersisted(repository), "after insert");
                repository.Test6.Pers.Recompute();
                Assert.AreEqual("a1, b2, c3", ReportPersisted(repository), "after insert recompute");

                var source = repository.Test6.Comp.Query().OrderBy(item => item.Num).ToList();
                repository.Test6.Pers.Update(new[]
                    {
                        new Test6.Pers { ID = source[0].ID, Name = source[0].Name + "x", Num = source[0].Num },
                        new Test6.Pers { ID = source[2].ID, Name = source[2].Name, Num = source[2].Num * 10 }
                    });
                Assert.AreEqual("ax1, b2, c30", ReportPersisted(repository), "after update");
                repository.Test6.Pers.Recompute();
                Assert.AreEqual("a1, b2, c3", ReportPersisted(repository), "after update recompute");

                repository.Test6.Pers.Delete(repository.Test6.Pers.Query());
                for (int i = source.Count - 1; i >= 0; i--)
                    repository.Test6.Pers.Insert(new[] { new Test6.Pers { ID = source[i].ID, Name = source[i].Name, Num = source[i].Num } });
                Assert.AreEqual("c3, b2, a1", string.Join(", ", repository.Test6.Pers.Query().Select(item => item.Name + item.Num.CastToString())), "after reorder");
                repository.Test6.Pers.Recompute();
                Assert.AreEqual("a1, b2, c3", ReportPersisted(repository), "after reorder recompute");
            }
        }

        private static string ReportDocumentCreationInfo(Common.DomRepository repository)
        {
            var loadedData = repository.Test9.DocumentCreationInfo.Query().Select(item => item.Base.Name + ":" + item.Rank).ToList();
            string report = string.Join(", ", loadedData.OrderBy(s => s));
            Console.WriteLine(report);
            return report;
        }

        [TestMethod]
        public void ComputeForNewBaseItems()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);

                var d1ID = Guid.NewGuid();
                executionContext.SqlExecuter.ExecuteSql(new[]
                    {
                        "DELETE FROM Test9.Document;",
                        "DELETE FROM Test9.DocumentCreationInfo;",
                        "INSERT INTO Test9.Document (ID, Name) SELECT '" + d1ID + "', 'd1'"
                    });


                Assert.AreEqual("", ReportDocumentCreationInfo(repository), "initial");
                repository.Test9.DocumentCreationInfo.Recompute();
                Assert.AreEqual("d1:1", ReportDocumentCreationInfo(repository), "initial recalc");

                var documents = repository.Test9.Document;

                var d2ID = Guid.NewGuid();
                documents.Insert(new[] { new Test9.Document { ID = d2ID, Name = "d2" } });
                Assert.AreEqual("d1:1, d2:2", ReportDocumentCreationInfo(repository), "autorecompute after new");

                var d3ID = Guid.NewGuid();
                var d4ID = Guid.NewGuid();
                documents.Insert(new[] { new Test9.Document { ID = d3ID, Name = "d3" }, new Test9.Document { ID = d4ID, Name = "d4" } });
                Assert.AreEqual("d1:1, d2:2, d3:4, d4:4", ReportDocumentCreationInfo(repository), "autorecompute after new2");

                documents.Save(null, new[] { new Test9.Document { ID = d1ID, Name = "d1x" } }, new[] { new Test9.Document { ID = d3ID } });
                Assert.AreEqual("d1x:1, d2:2, d4:4", ReportDocumentCreationInfo(repository), "autorecompute after update&delete");
            }
        }

        [TestMethod]
        [Ignore]
        public void ComputeForNewBaseItems_InvalidCommand()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);

                var d1ID = Guid.NewGuid();
                var d2ID = Guid.NewGuid();
                executionContext.SqlExecuter.ExecuteSql(new[]
                    {
                        "DELETE FROM Test9.Document;",
                        "DELETE FROM Test9.DocumentCreationInfo;",
                        "INSERT INTO Test9.Document (ID, Name) SELECT '" + d1ID + "', 'd1'",
                        "INSERT INTO Test9.Document (ID, Name) SELECT '" + d2ID + "', 'd2'",
                        "INSERT INTO Test9.DocumentCreationInfo (ID, Rank) SELECT '" + d1ID + "', 1",
                        "INSERT INTO Test9.DocumentCreationInfo (ID, Rank) SELECT '" + d2ID + "', 2",
                    });


                Assert.AreEqual("d1:1, d2:2", ReportDocumentCreationInfo(repository), "initial");

                var documents = repository.Test9.Document;

                TestUtility.ShouldFail(() => documents.Insert(new[] { new Test9.Document { ID = d1ID, Name = "d1" } }), "existing");
                Assert.AreEqual("d1:1, d2:2", ReportDocumentCreationInfo(repository), "creation info of previously inserted documents shoud not be changed");

                // TODO: Instead of using the wrapper 'Save' function to check data validations, we should handle insert/update/delete events (NHibernate event listener) for faster and more reliable validations.
                TestUtility.ShouldFail(() => documents.Update(new[] { new Test9.Document { ID = Guid.NewGuid(), Name = "d3" } }));
                Assert.AreEqual("d1:1, d2:2", ReportDocumentCreationInfo(repository), "creation info of previously inserted documents shoud not be changed");
            }
        }

        private static string ReportDocumentAggregates(Common.DomRepository repository)
        {
            var loadedData = repository.Test9.DocumentAggregates.Query().Select(item => item.NameNumParts).ToList();
            string report = string.Join(", ", loadedData.OrderBy(s => s));
            Console.WriteLine(report);
            return report;
        }

        private static int SimpleNumParts(Common.DomRepository repository, string documentName)
        {
            return repository.Test9.Document.Query()
                .Where(d => d.Name == documentName)
                .Select(d => d.Extension_DocumentSimpleAggregate.NumParts)
                .Single().Value;
        }

        [TestMethod]
        public void KeepSynchronizedSimple()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);

                var doc1 = new Test9.Document { Name = "doc1" };
                var doc2 = new Test9.Document { Name = "doc2" };
                repository.Test9.Document.Insert(new[] { doc1, doc2 });
                executionContext.NHibernateSession.Clear();

                Assert.AreEqual(0, SimpleNumParts(repository, "doc1"), "initial");
                executionContext.NHibernateSession.Clear();

                var st1 = new Test9.Part { Head = doc1, Name = "st1" };
                repository.Test9.Part.Insert(new[] { st1 });
                executionContext.NHibernateSession.Clear();

                Assert.AreEqual(1, SimpleNumParts(repository, "doc1"), "after insert detail");
                executionContext.NHibernateSession.Clear();

                var st2 = new Test9.Part { Head = doc1, Name = "st2" };
                repository.Test9.Part.Insert(new[] { st2 });
                executionContext.NHibernateSession.Clear();

                Assert.AreEqual(2, SimpleNumParts(repository, "doc1"), "after insert detail 2");
                executionContext.NHibernateSession.Clear();

                st1.Head = doc2;
                repository.Test9.Part.Update(new[] { st1 });
                executionContext.NHibernateSession.Clear();

                Assert.AreEqual(1, SimpleNumParts(repository, "doc1"), "after update detail");
                executionContext.NHibernateSession.Clear();

                repository.Test9.Part.Delete(new[] { st2 });
                executionContext.NHibernateSession.Clear();

                Assert.AreEqual(0, SimpleNumParts(repository, "doc1"), "after delete detail 2");
                executionContext.NHibernateSession.Clear();
            }
        }

        [TestMethod]
        public void KeepSynchronized()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);

                var d1ID = Guid.NewGuid();
                var d2ID = Guid.NewGuid();
                var s11ID = Guid.NewGuid();
                executionContext.SqlExecuter.ExecuteSql(new[]
                    {
                        "DELETE FROM Test9.DocumentAggregates;",
                        "DELETE FROM Test9.Part;",
                        "DELETE FROM Test9.Document;",
                        "INSERT INTO Test9.Document (ID, Name) SELECT '" + d1ID + "', 'd1';",
                        "INSERT INTO Test9.Document (ID, Name) SELECT '" + d2ID + "', 'd2';",
                        "INSERT INTO Test9.Part (ID, HeadID, Name) SELECT '" + s11ID + "', '" + d1ID + "', 's11';"
                    });


                Assert.AreEqual("", ReportDocumentAggregates(repository), "initial");
                repository.Test9.DocumentAggregates.Recompute();
                Assert.AreEqual("d1:1, d2:0", ReportDocumentAggregates(repository), "initial recalc");

                var documents = repository.Test9.Document;
                var parts = repository.Test9.Part;

                var d3ID = Guid.NewGuid();
                documents.Save(new[] { new Test9.Document { ID = d3ID, Name = "d3" } }, null, null);
                Assert.AreEqual("d1:1, d2:0, d3:0", ReportDocumentAggregates(repository), "autorecompute after insert");

                documents.Save(null, new[] { new Test9.Document { ID = d2ID, Name = "d2x" } }, null);
                Assert.AreEqual("d1:1, d2x:0, d3:0", ReportDocumentAggregates(repository), "autorecompute after update");

                var s12ID = Guid.NewGuid();
                var s13ID = Guid.NewGuid();
                parts.Save(new[] { new Test9.Part { ID = s12ID, HeadID = d1ID, Name = "s12" }, new Test9.Part { ID = s13ID, HeadID = d1ID, Name = "s13" } }, null, null);
                Assert.AreEqual("d1:3, d2x:0, d3:0", ReportDocumentAggregates(repository), "autorecompute after insert detail 2");

                var s21ID = Guid.NewGuid();
                parts.Save(
                    new[] { new Test9.Part { ID = s21ID, HeadID = d2ID, Name = "s21" } },
                    new[] { new Test9.Part { ID = s12ID, HeadID = d3ID, Name = "s12x" } },
                    new[] { new Test9.Part { ID = s13ID } });
                Assert.AreEqual("d1:1, d2x:1, d3:1", ReportDocumentAggregates(repository), "autorecompute after insert&update&delete detail");

                var d4ID = Guid.NewGuid();
                documents.Save(new[] { new Test9.Document { ID = d4ID, Name = "d4 locked" } }, null, null);
                Assert.AreEqual("d1:1, d2x:1, d3:1", ReportDocumentAggregates(repository), "autorecompute after insert locked");

                documents.Save(null, new[] { new Test9.Document { ID = d2ID, Name = "d2xx" }, new Test9.Document { ID = d4ID, Name = "d4x locked" } }, null);
                Assert.AreEqual("d1:1, d2xx:1, d3:1", ReportDocumentAggregates(repository), "autorecompute after update locked");

                documents.Save(null, null, new[] { new Test9.Document { ID = d3ID }, new Test9.Document { ID = d4ID } });
                Assert.AreEqual("d1:1, d2xx:1", ReportDocumentAggregates(repository), "autorecompute after delete locked");
            }
        }
    }
}
