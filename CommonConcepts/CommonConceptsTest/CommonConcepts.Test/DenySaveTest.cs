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

using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.TestCommon;

namespace CommonConcepts.Test
{
    [TestClass]
    public class DenySaveTest
    {
        private static void AssertData(Common.DomRepository repository, string expected)
        {
            Assert.AreEqual(expected, TestUtility.DumpSorted(repository.TestDenySave.Simple.All(), item => item.Name));
        }

        private static TestDenySave.Simple[] CreateSimple(params int[] data)
        {
            return data.Select(e => new TestDenySave.Simple { Name = "s" + e, Count = e }).ToArray();
        }

        [TestMethod]
        public void InsertInvalidData()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestDenySave.Simple;" });
                var repository = new Common.DomRepository(executionContext);

                repository.TestDenySave.Simple.Insert(CreateSimple(3));
                AssertData(repository, "s3");

                TestUtility.ShouldFail(() => repository.TestDenySave.Simple.Insert(CreateSimple(300)), "larger than 100");
            }
        }

        [TestMethod]
        public void InsertValidAndInvalidData()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestDenySave.Simple;" });
                var repository = new Common.DomRepository(executionContext);

                TestUtility.ShouldFail(() => repository.TestDenySave.Simple.Insert(CreateSimple(3, 300)), "larger than 100");
            }
        }

        [TestMethod]
        public void UpdateInvalidData()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestDenySave.Simple;" });
                var repository = new Common.DomRepository(executionContext);

                var s3 = CreateSimple(3).Single();
                repository.TestDenySave.Simple.Insert(new[] { s3 });

                s3.Name = "s3b";
                s3.Count = 33;
                repository.TestDenySave.Simple.Update(new[] { s3 });

                AssertData(repository, "s3b");
                s3.Name = "s3c";
                s3.Count = 300;
                TestUtility.ShouldFail(() => repository.TestDenySave.Simple.Update(new[] { s3 }), "larger than 100");
            }
        }

        [TestMethod]
        public void UpdateInvalidDataWithValidInsertAndDelete()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestDenySave.Simple;" });
                var repository = new Common.DomRepository(executionContext);

                var s1 = CreateSimple(1).Single();
                var s2 = CreateSimple(2).Single();
                var s3 = CreateSimple(3).Single();
                repository.TestDenySave.Simple.Insert(new[] { s1, s2, s3 });
                s3.Name = "s3b";
                s3.Count = 33;
                repository.TestDenySave.Simple.Update(new[] { s3 });
                repository.TestDenySave.Simple.Delete(new[] { s1 });

                AssertData(repository, "s2, s3b");
                s3.Name = "s3d";
                s3.Count = 333;
                TestUtility.ShouldFail(() => repository.TestDenySave.Simple.Save(CreateSimple(5), new[] { s3 }, new[] { s2 }), "larger than 100");
            }
        }

        [TestMethod]
        public void InsertInvalidDataWithValidUpdateAndDelete()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[] { "DELETE FROM TestDenySave.Simple;" });
                var repository = new Common.DomRepository(executionContext);

                var s1 = CreateSimple(1).Single();
                var s2 = CreateSimple(2).Single();
                var s3 = CreateSimple(3).Single();
                repository.TestDenySave.Simple.Insert(new[] { s1, s2, s3 });
                s3.Name = "s3b";
                s3.Count = 33;
                repository.TestDenySave.Simple.Update(new[] { s3 });
                repository.TestDenySave.Simple.Delete(new[] { s1 });

                AssertData(repository, "s2, s3b");
                s3.Name = "s3e";
                s3.Count = 33;
                TestUtility.ShouldFail(() => repository.TestDenySave.Simple.Save(CreateSimple(555), new[] { s3 }, new[] { s2 }), "larger than 100");
            }
        }
    }
}
