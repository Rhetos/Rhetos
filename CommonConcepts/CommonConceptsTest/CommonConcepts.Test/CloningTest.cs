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
using Rhetos.Configuration.Autofac;
using Rhetos.Utilities;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;

namespace CommonConcepts.Test
{
    [TestClass]
    public class CloningTest
    {
        [TestMethod]
        public void CreatedColumns()
        {
            using (var container = new RhetosTestContainer())
            {
                var sql = @"SELECT
                        OBJECT_NAME(object_id) + '.' + name
                    FROM
                        sys.columns
                    WHERE
                        object_id IN (OBJECT_ID('TestCloning.Clone1'), OBJECT_ID('TestCloning.Clone2'), OBJECT_ID('TestCloning.Clone3'))
                    ORDER BY
                        1";

                var expected =
@"Clone1.ID
Clone1.Start
Clone2.ID
Clone2.Name
Clone2.ParentID
Clone2.Start
Clone3.Code
Clone3.ID
Clone3.Name
Clone3.ParentID
Clone3.Start
";
                var actual = new StringBuilder();
                container.Resolve<ISqlExecuter>().ExecuteReader(sql, reader => actual.AppendLine(reader.GetString(0)));

                Assert.AreEqual(expected, actual.ToString());
            }
        }

        [TestMethod]
        public void CreatedIndexes()
        {
            using (var container = new RhetosTestContainer())
            {
                var sql = @"SELECT
                        i.name + '.' + c.name
                    FROM
                        sys.indexes i
                        INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
                        INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                    WHERE
                        c.object_id IN (OBJECT_ID('TestCloning.Clone1'), OBJECT_ID('TestCloning.Clone2'), OBJECT_ID('TestCloning.Clone3'))
                    ORDER BY
                        i.name, ic.key_ordinal";

                string[] expected =
                    ("IX_Clone2_Parent.ParentID IX_Clone2_Start_Parent.Start IX_Clone2_Start_Parent.ParentID"
                    + " IX_Clone3_Code.Code IX_Clone3_Parent.ParentID IX_Clone3_Start_Parent.Start"
                    + " IX_Clone3_Start_Parent.ParentID PK_Clone1.ID PK_Clone2.ID PK_Clone3.ID").Split(' ');

                var actual = new List<string>();
                container.Resolve<ISqlExecuter>().ExecuteReader(sql, reader => actual.Add(reader.GetString(0)));

                Assert.AreEqual(TestUtility.DumpSorted(expected), TestUtility.DumpSorted(actual));
            }
        }

        [TestMethod]
        public void ClonesSimpleBusinessLogic()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        "DELETE FROM TestCloning.Source",
                        "DELETE FROM TestCloning.Parent",
                        "DELETE FROM TestCloning.Base"
                    });
                var repository = container.Resolve<Common.DomRepository>();

                var b = new TestCloning.Base { ID = Guid.NewGuid(), Name = "b" };
                var b2 = new TestCloning.Base { ID = Guid.NewGuid(), Name = "b2" };
                var b3 = new TestCloning.Base { ID = Guid.NewGuid(), Name = "b3" };
                var p = new TestCloning.Parent { ID = Guid.NewGuid(), Name = "p" };
                var p2 = new TestCloning.Parent { ID = Guid.NewGuid(), Name = "p2" };
                var p3 = new TestCloning.Parent { ID = Guid.NewGuid(), Name = "p3" };
                var c = new TestCloning.Clone3 { ID = b.ID, Name = "c", ParentID = p.ID };
                var c2 = new TestCloning.Clone3 { ID = b2.ID, Name = "c2", ParentID = p2.ID };
                var c3 = new TestCloning.Clone3 { ID = b3.ID, Name = "c3", ParentID = p3.ID };
                repository.TestCloning.Base.Insert(new[] { b, b2, b3 });
                repository.TestCloning.Parent.Insert(new[] { p, p2, p3 });
                repository.TestCloning.Clone3.Insert(new[] { c, c2, c3 });

                Func<string> readClone3 = () => TestUtility.DumpSorted(repository.TestCloning.Clone3.Query()
                        .Select(item => item.Name + " " + item.Base.Name + " " + item.Parent.Name));

                Assert.AreEqual("c b p, c2 b2 p2, c3 b3 p3", readClone3());

                repository.TestCloning.Base.Delete(new[] { b2 });
                repository.TestCloning.Parent.Delete(new[] { p3 });
                Assert.AreEqual("c b p", readClone3());

                c.Name = null;
                TestUtility.ShouldFail(() => repository.TestCloning.Clone3.Update(new[] { c }), "required", "Name");
            }
        }


        [TestMethod]
        public void CloneExtension()
        {
            using (var container = new RhetosTestContainer())
            {
                var dslModel = container.Resolve<IDslModel>();
                var clone3 = (EntityInfo)dslModel.FindByKey("DataStructureInfo TestCloning.Clone3");

                var properties = dslModel.FindByReference<PropertyInfo>(p => p.DataStructure, clone3);
                Assert.AreEqual("DateTime Start, Integer Code, Reference Parent, ShortString Name",
                    TestUtility.DumpSorted(properties, p => $"{p.GetKeywordOrTypeName()} {p.Name}"));

                var extends = dslModel.FindByReference<DataStructureExtendsInfo>(e => e.Extension, clone3).Single();
                Assert.AreEqual("Rhetos.Dsl.DefaultConcepts.DataStructureExtendsInfo TestCloning.Clone3 TestCloning.Base", extends.GetFullDescription());

                var cascadeDelete = dslModel.FindByReference<UniqueReferenceCascadeDeleteInfo>(cd => cd.UniqueReference, extends).SingleOrDefault();
                Assert.IsNotNull(cascadeDelete);
            }
        }

        [TestMethod]
        public void CloneUniqueReference()
        {
            using (var container = new RhetosTestContainer())
            {
                var dslModel = container.Resolve<IDslModel>();
                var cloneUR = (EntityInfo)dslModel.FindByKey("DataStructureInfo TestCloning.CloneUR");

                var properties = dslModel.FindByReference<PropertyInfo>(p => p.DataStructure, cloneUR);
                Assert.AreEqual("ShortString Name",
                    TestUtility.DumpSorted(properties, p => $"{p.GetKeywordOrTypeName()} {p.Name}"));

                var uniqueReference = dslModel.FindByReference<UniqueReferenceInfo>(ur => ur.Extension, cloneUR).Single();
                Assert.AreEqual("Rhetos.Dsl.DefaultConcepts.UniqueReferenceInfo TestCloning.CloneUR TestCloning.Base", uniqueReference.GetFullDescription());

                var cascadeDelete = dslModel.FindByReference<UniqueReferenceCascadeDeleteInfo>(cd => cd.UniqueReference, uniqueReference).SingleOrDefault();
                Assert.IsNotNull(cascadeDelete, "cascadeDelete");
            }
        }
    }
}
