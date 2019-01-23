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
using Rhetos.Utilities;
using Rhetos.Configuration.Autofac;
using Rhetos.Dom.DefaultConcepts;

namespace CommonConcepts.Test
{
    [TestClass]
    public class DataStructureTest
    {
        [TestMethod]
        public void StandardClass()
        {
            TestDataStructure.SimpleDataStructure1 item = new TestDataStructure.SimpleDataStructure1();
            Assert.IsNotNull(item);
        }

        [TestMethod]
        public void ShortStringProperty()
        {
            TestDataStructure.SimpleDataStructure2 item = new TestDataStructure.SimpleDataStructure2();
            Assert.IsNull(item.SimpleShortString);

            {
                const string value = "";
                item.SimpleShortString = value;
                Assert.AreEqual(value, item.SimpleShortString);
            }

            {
                const string value = "abc";
                item.SimpleShortString = value;
                Assert.AreEqual(value, item.SimpleShortString);
            }

            item.SimpleShortString = null;
            Assert.IsNull(item.SimpleShortString);

            {
                string value = new string('x', 256);
                item.SimpleShortString = value;
                Assert.AreEqual(value, item.SimpleShortString);
            }
        }

        [TestMethod]
        public void Serialization()
        {
            using (var container = new RhetosTestContainer())
            {
                var item = new TestDataStructure.SimpleDataStructure2 { SimpleShortString = "abc" };
                string xml = container.Resolve<XmlUtility>().SerializeToXml(item);
                Console.WriteLine(xml);

                TestUtility.AssertContains(xml, "TestDataStructure");
                TestUtility.AssertContains(xml, "SimpleDataStructure2");
                TestUtility.AssertContains(xml, "SimpleShortString");
                TestUtility.AssertContains(xml, "abc");

                var item2 = container.Resolve<XmlUtility>().DeserializeFromXml<TestDataStructure.SimpleDataStructure2>(xml);
                Assert.IsNotNull(item2);
                Assert.AreEqual(item.SimpleShortString, item2.SimpleShortString);
            }
        }

        [TestMethod]
        public void SerializationMustNotDependOnClientOrServerDllName()
        {
            using (var container = new RhetosTestContainer())
            {
                var item = new TestDataStructure.SimpleDataStructure2 { SimpleShortString = "abc" };
                string xml = container.Resolve<XmlUtility>().SerializeToXml(item);
                Console.WriteLine(xml);

                var type = typeof(TestDataStructure.SimpleDataStructure2);
                Console.WriteLine();
                Console.WriteLine(type.AssemblyQualifiedName);
                TestUtility.AssertNotContains(xml, type.AssemblyQualifiedName);

                var dllName = type.Assembly.FullName.Split(',')[0];
                Console.WriteLine();
                Console.WriteLine("dll: \"" + dllName + "\"");
                TestUtility.AssertNotContains(xml, dllName);
            }
        }

        [TestMethod]
        public void SerializationOfNull()
        {
            using (var container = new RhetosTestContainer())
            {
                var item = new TestDataStructure.SimpleDataStructure2 { SimpleShortString = null };
                string xml = container.Resolve<XmlUtility>().SerializeToXml(item);
                Console.WriteLine(xml);

                var item2 = container.Resolve<XmlUtility>().DeserializeFromXml<TestDataStructure.SimpleDataStructure2>(xml);
                Assert.IsNotNull(item2);
                Assert.IsNull(item2.SimpleShortString);
            }
        }

        private readonly List<string> StringoviSaOstalimUnicodeZnakovima =
            new List<string>
            {
                @"„ ” “ « » • † ° ÷ © ® ™ … — – ― € £ $ ¥ ¢ ¤ · × ¬ ‰ ± §",
                @"А а Б б В в Г г Д д Ђ ђ Е е Ж ж З з И и Ј ј К к Л л Љ љ М м Н н Њ њ О о П п Р р С с Т т Ћ ћ У у Ф ф Х х Ц ц Ч ч Џ џ Ш ш",
                @"А Б В Г Д Ѓ Е Ж З Ѕ И Ј К Л Љ М Н Њ О П Р С Т Ќ У Ф Х Ц Ч Џ Ш",
                @"Α Β Γ Δ Ε Ζ Η Θ Ι Κ Λ Μ Ν Ξ Ο Π Ρ Σ Τ Υ Φ Χ Ψ Ω",
                @"    غ فـفـف ف ڤـڤـڤ ڤ قـقـق ق كـكـك ك لـلـل ل مـمـم م نـنـن ن هـهـه ه ـة ة ـو و يـيـي ي ( یـیـی ی ) ـى ى"
            };
        [TestMethod]
        public void SerializationCsStringEncoding()
        {
            using (var container = new RhetosTestContainer())
            {
                foreach (string s in StringoviSaOstalimUnicodeZnakovima)
                {
                    var item = new TestDataStructure.SimpleDataStructure2 { SimpleShortString = s };
                    string xml = container.Resolve<XmlUtility>().SerializeToXml(item);
                    Console.WriteLine(xml);

                    var item2 = container.Resolve<XmlUtility>().DeserializeFromXml<TestDataStructure.SimpleDataStructure2>(xml);
                    Assert.IsNotNull(item2);
                    Assert.AreEqual(item.SimpleShortString, item2.SimpleShortString);

                    TestUtility.AssertContains(xml.ToLower(), "utf-16", "C# string is always in UTF-16 encoding.");
                }
            }
        }

        [TestMethod]
        public void SimpleReference()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        "DELETE FROM TestDataStructure.Child",
                        "DELETE FROM TestDataStructure.Parent",
                    });
                var repository = container.Resolve<Common.DomRepository>();

                var parent = new TestDataStructure.Parent { ID = Guid.NewGuid() };
                var child = new TestDataStructure.Child { ID = Guid.NewGuid(), ParentID = parent.ID };

                repository.TestDataStructure.Parent.Insert(new[] { parent });
                repository.TestDataStructure.Child.Insert(new[] { child });

                Assert.AreEqual(child.ID + " " + parent.ID, repository.TestDataStructure.Child.Query().Select(c => c.ID + " " + c.Parent.ID).Single(),
                    "Testing if the Reference concept was properly implemented while using late initialization of the Reference property.");

                repository.TestDataStructure.Parent.Delete(new[] { parent });
                Assert.AreEqual(0, repository.TestDataStructure.Child.Query().Count(),
                    "Testing if the CascadeDelete concept was properly implemented while using a Reference concept with late initialization of the Reference property.");
            }
        }

        [TestMethod]
        public void SimpleMethod()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();

                var test = new TestDataStructure.TestMethod { Name = "test" };
                repository.TestDataStructure.TestMethod.Insert(test);
                
                Assert.IsTrue(repository.TestDataStructure.TestMethod.Filter(new TestDataStructure.Limit5()).Any(x => x.Name == "test"));
            }
        }
    }
}
