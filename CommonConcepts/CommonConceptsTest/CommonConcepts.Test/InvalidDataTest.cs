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
using Rhetos.Logging;

namespace CommonConcepts.Test
{
    [TestClass]
    public class InvalidDataTest
    {
        private static void AssertData(Common.DomRepository repository, string expected)
        {
            Assert.AreEqual(expected, TestUtility.DumpSorted(repository.TestInvalidData.Simple.Query(), item => item.Name));
        }

        private static TestInvalidData.Simple[] CreateSimple(params int[] data)
        {
            return data.Select(e => new TestInvalidData.Simple { Name = "s" + e, Count = e }).ToArray();
        }

        [TestMethod]
        public void InsertInvalidData()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestInvalidData.Simple;" });
                var repository = container.Resolve<Common.DomRepository>();

                repository.TestInvalidData.Simple.Insert(CreateSimple(3));
                AssertData(repository, "s3");

                TestUtility.ShouldFail(() => repository.TestInvalidData.Simple.Insert(CreateSimple(300)), "larger than 100");
            }
        }

        [TestMethod]
        public void InsertValidAndInvalidData()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestInvalidData.Simple;" });
                var repository = container.Resolve<Common.DomRepository>();

                TestUtility.ShouldFail(() => repository.TestInvalidData.Simple.Insert(CreateSimple(3, 300)), "larger than 100");
            }
        }

        [TestMethod]
        public void UpdateInvalidData()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestInvalidData.Simple;" });
                var repository = container.Resolve<Common.DomRepository>();

                var s3 = CreateSimple(3).Single();
                repository.TestInvalidData.Simple.Insert(new[] { s3 });

                s3.Name = "s3b";
                s3.Count = 33;
                repository.TestInvalidData.Simple.Update(new[] { s3 });

                AssertData(repository, "s3b");
                s3.Name = "s3c";
                s3.Count = 300;
                TestUtility.ShouldFail(() => repository.TestInvalidData.Simple.Update(new[] { s3 }), "larger than 100");
            }
        }

        [TestMethod]
        public void UpdateInvalidDataWithValidInsertAndDelete()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestInvalidData.Simple;" });
                var repository = container.Resolve<Common.DomRepository>();

                var s1 = CreateSimple(1).Single();
                var s2 = CreateSimple(2).Single();
                var s3 = CreateSimple(3).Single();
                repository.TestInvalidData.Simple.Insert(new[] { s1, s2, s3 });
                s3.Name = "s3b";
                s3.Count = 33;
                repository.TestInvalidData.Simple.Update(new[] { s3 });
                repository.TestInvalidData.Simple.Delete(new[] { s1 });

                AssertData(repository, "s2, s3b");
                s3.Name = "s3d";
                s3.Count = 333;
                TestUtility.ShouldFail(() => repository.TestInvalidData.Simple.Save(CreateSimple(5), new[] { s3 }, new[] { s2 }), "larger than 100");
            }
        }

        [TestMethod]
        public void InsertInvalidDataWithValidUpdateAndDelete()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestInvalidData.Simple;" });
                var repository = container.Resolve<Common.DomRepository>();

                var s1 = CreateSimple(1).Single();
                var s2 = CreateSimple(2).Single();
                var s3 = CreateSimple(3).Single();
                repository.TestInvalidData.Simple.Insert(new[] { s1, s2, s3 });
                s3.Name = "s3b";
                s3.Count = 33;
                repository.TestInvalidData.Simple.Update(new[] { s3 });
                repository.TestInvalidData.Simple.Delete(new[] { s1 });

                AssertData(repository, "s2, s3b");
                s3.Name = "s3e";
                s3.Count = 33;
                TestUtility.ShouldFail(() => repository.TestInvalidData.Simple.Save(CreateSimple(555), new[] { s3 }, new[] { s2 }), "larger than 100");
            }
        }

        [TestMethod]
        public void ErrorMessages()
        {
            var tests = new ListOfTuples<string, string[]>
            {
                { "xa", new[] { "Contains A" } },
                { "xb", new[] { "Contains B (abc, 123)", "Property:Name" } },
                { "xc", new[] { "Contains C (xc, 2)" } },
                { "xdddddd", new[] { "Property 'Simple2-Name' should not contain 'letter D'. The entered text is 'xdddddd', 7 characters long." } },
            };

            foreach (var test in tests)
            {
                using (var container = new RhetosTestContainer())
                {
                    Console.WriteLine("\r\nInput: " + test.Item1);
                    var simple2 = container.Resolve<Common.DomRepository>().TestInvalidData.Simple2;
                    simple2.Delete(simple2.Query());
                    var newItem = new TestInvalidData.Simple2 { Name = test.Item1 };
                    var error = TestUtility.ShouldFail<Rhetos.UserException>(
                        () => simple2.Insert(newItem),
                        test.Item2);
                    Console.WriteLine("ErrorMessage: " + ExceptionsUtility.SafeFormatUserMessage(error));
                    Console.WriteLine("Exception: " + error.ToString());
                }
            }
        }
    }
}
