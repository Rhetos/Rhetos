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
using Rhetos.Dom;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Rhetos.Utilities.Test
{
    class DomainObjectModelMock : IDomainObjectModel
    {
        public IEnumerable<System.Reflection.Assembly> Assemblies => new[] { GetType().Assembly };
    }

    [TestClass]
    public class XmlUtilityTest
    {
        private static XmlUtility _xmlUtility = new XmlUtility(new DomainObjectModelMock());

        public class SimpleClass
        {
            public int a;
            public int b { get; set; }
#pragma warning disable 169, CA1823
            private int c;
#pragma warning restore 169, CA1823
        }

        private static TCopy SerializeDeserialize<TCopy>(object orig)
        {
            var data = _xmlUtility.SerializeToXml(orig, orig.GetType());
            System.Diagnostics.Debug.WriteLine(data.ToString());
            return _xmlUtility.DeserializeFromXml<TCopy>(data);
        }

        [TestMethod]
        public void SerializeToXml_SimpleClass()
        {
            var orig = new SimpleClass { a = 1, b = 2 };
            var copy = SerializeDeserialize<SimpleClass>(orig);
            Assert.AreEqual(orig.a, copy.a);
            Assert.AreEqual(orig.b, copy.b);
        }

        [TestMethod]
        public void SerializeToXml_NonenglishCharacters()
        {
            var orig = "čćšđžČĆŠĐŽ";
            var copy = SerializeDeserialize<string>(orig);
            Assert.AreEqual(orig, copy);
        }

        [TestMethod]
        public void SerializeToXml_SpecialCharacters()
        {
            var orig = @";'[]\,./<>?:""{}|!@#$%^&*()_";
            var copy = SerializeDeserialize<string>(orig);
            Assert.AreEqual(orig, copy);
        }

        public class ClassWithSubclass
        {
            public SimpleClass element;
        }

        [TestMethod]
        public void SerializeToXml_Subclass()
        {
            var orig = new ClassWithSubclass { element = new SimpleClass { a = 1, b = 2 } };
            var copy = SerializeDeserialize<ClassWithSubclass>(orig);
            Assert.AreEqual(orig.element.a, copy.element.a);
            Assert.AreEqual(orig.element.b, copy.element.b);
        }

        [TestMethod]
        public void SerializeToXml_List()
        {
            var orig = new List<int>() { 1, 2 };
            var copy = SerializeDeserialize<List<int>>(orig);
            Assert.AreEqual(orig.Count, copy.Count);
            Assert.AreEqual(orig[0], copy[0]);
            Assert.AreEqual(orig[1], copy[1]);
        }

        [TestMethod]
        public void SerializeToXml_Collection()
        {
            var orig = new Collection<int>() { 1, 2 };
            var copy = SerializeDeserialize<Collection<int>>(orig);
            Assert.AreEqual(orig.Count, copy.Count);
            Assert.AreEqual(orig[0], copy[0]);
            Assert.AreEqual(orig[1], copy[1]);
        }

        [TestMethod]
        public void SerializeToXml_ListOfClasses()
        {
            var orig = new List<SimpleClass>() { new SimpleClass { a = 1, b = 2 } };
            var copy = SerializeDeserialize<List<SimpleClass>>(orig);
            Assert.AreEqual(orig.Count, copy.Count);
            Assert.AreEqual(orig[0].a, copy[0].a);
            Assert.AreEqual(orig[0].b, copy[0].b);
        }

        [TestMethod]
        public void SerializeToXml_ListOfComplexClasses()
        {
            var orig = new List<ClassWithSubclass>() { new ClassWithSubclass() { element = new SimpleClass { a = 1, b = 2 } } };
            var copy = SerializeDeserialize<List<ClassWithSubclass>>(orig);
            Assert.AreEqual(orig.Count, copy.Count);
            Assert.AreEqual(orig[0].element.a, copy[0].element.a);
            Assert.AreEqual(orig[0].element.b, copy[0].element.b);
        }

        public interface ITest
        {
            int a { get; set; }
        }

        public class ClassWithInterface : ITest
        {
            public int a { get; set; }
            public int b { get; set; }
        }

        public class ClassWithMermberWithInterface
        {
            public ITest m;
        }

        public abstract class AbstractClass
        {
            public ITest m;
        }

        public class ConcreteClass : AbstractClass
        {
            public int c;
        }

        public class ClassWithAbstractMermber
        {
            public AbstractClass x;
        }

        public class ClassWithListOfInterfaces
        {
            public int A;
            public string B;
            public ITest[] Items;
        }

        [TestMethod]
        public void SerializeToXml_ClassWithListOfInterfaces()
        {
            var orig = new ClassWithListOfInterfaces();
            orig.A = 1;
            orig.B = "aaaa";
            List<ITest> list = new List<ITest>();
            list.Add(new ClassWithInterface {a = 1, b=1});
            list.Add(new ClassWithInterface {a = 2, b=2});
            orig.Items = list.ToArray();
            var copy = SerializeDeserialize<ClassWithListOfInterfaces>(orig);
            Assert.AreEqual(orig.A,copy.A);
            Assert.AreEqual(orig.B,copy.B);
            Assert.AreEqual(orig.Items.GetType(), copy.Items.GetType());
            Assert.AreEqual(orig.Items.Count(), copy.Items.Count());
            Assert.AreEqual(orig.Items[0].a, copy.Items[0].a);
        }

        [TestMethod]
        public void SerializeToXml_Object()
        {
            object orig = new ClassWithInterface { a = 1, b = 2 };
            var copy = SerializeDeserialize<ClassWithInterface>(orig);
            Assert.AreEqual(1, copy.a);
            Assert.AreEqual(2, copy.b);
        }

        [TestMethod]
        public void SerializeToXml_Interface()
        {
            ITest orig = new ClassWithInterface { a = 1, b = 2 };
            var copy = SerializeDeserialize<ClassWithInterface>(orig);
            Assert.AreEqual(1, copy.a);
            Assert.AreEqual(2, copy.b);
        }

        [TestMethod]
        public void SerializeToXml_MermberWithInterface()
        {
            var orig = new ClassWithMermberWithInterface { m = new ClassWithInterface { a = 1, b = 2 } };
            var copy = SerializeDeserialize<ClassWithMermberWithInterface>(orig);
            Assert.AreEqual(orig.m.a, copy.m.a);
            Assert.AreEqual(((ClassWithInterface)orig.m).b, ((ClassWithInterface)copy.m).b);
        }

        [TestMethod]
        public void SerializeToXml_AbstractMermber()
        {
            var orig = new ClassWithAbstractMermber
            {
                x = new ConcreteClass { m = new ClassWithInterface { a = 1, b = 2 }, c = 3 }
            };
            var copy = SerializeDeserialize<ClassWithAbstractMermber>(orig);
            
            Assert.AreEqual(orig.x.m.a, copy.x.m.a);
            Assert.AreEqual(((ClassWithInterface)orig.x.m).b, ((ClassWithInterface)copy.x.m).b);
            Assert.AreEqual(((ConcreteClass)orig.x).c, ((ConcreteClass)copy.x).c);
        }

        [TestMethod]
        public void SerializeToXml_PolimorphismNative()
        {
            ITest t1 = new ClassWithInterface() { a = 1, b = 11 };
            ITest t2 = new ClassWithInterface() { a = 2, b = 22 };
            var orig = new ITest[] { t1, t2 };

            var data = _xmlUtility.SerializeArrayToXml<ITest>(orig);
            Console.WriteLine(data);
            var copy = _xmlUtility.DeserializeArrayFromXml<ITest>(data);

            Assert.AreEqual(orig.Length, copy.Length);
            Assert.AreEqual(orig[0].a, copy[0].a);
            Assert.AreEqual(orig[1].a, copy[1].a);
            Assert.AreEqual(((ClassWithInterface)orig[0]).b, ((ClassWithInterface)copy[0]).b);
            Assert.AreEqual(((ClassWithInterface)orig[1]).b, ((ClassWithInterface)copy[1]).b);
        }

        [TestMethod]
        public void SerializeToXml_Null()
        {
            SimpleClass orig = null;
            var data = _xmlUtility.SerializeToXml(orig);
            SimpleClass copy = (SimpleClass)_xmlUtility.DeserializeFromXml(typeof(SimpleClass), data);
            Assert.IsNull(copy);
        }

        [TestMethod]
        public void SerializeToXml_ValueType()
        {
            var orig = new DateTime(2009, 12, 31, 1, 2, 3, 456);
            var copy = SerializeDeserialize<DateTime>(orig);
            Assert.AreEqual(orig, copy);
        }

        public class ClassWithEnum
        {
            public enum TestEnum { ValueA, ValueB };
            public TestEnum testEnumValue;
        }

        [TestMethod]
        public void SerializeToXml_EnumTest()
        {
            var orig = new ClassWithEnum { testEnumValue = ClassWithEnum.TestEnum.ValueB };
            var copy = SerializeDeserialize<ClassWithEnum>(orig);
            Assert.AreEqual(orig.testEnumValue, copy.testEnumValue);
        }

        [TestMethod]
        public void Deserialize_CompatibilityWithPersistedData()
        {
            ITest t1 = new ClassWithInterface() { a = 1, b = 11 };
            ITest t2 = new ClassWithInterface() { a = 2, b = 22 };
            var orig = new ITest[] { t1, t2 };

            var data = @"<?xml version=""1.0"" encoding=""utf-16""?>
                <ArrayOfUtilityTest.ClassWithInterface xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/Rhetos.Utilities.Test"">
                    <XmlUtilityTest.ClassWithInterface>
                        <a>1</a>
                        <b>11</b>
                    </XmlUtilityTest.ClassWithInterface>
                    <XmlUtilityTest.ClassWithInterface>
                        <a>2</a>
                        <b>22</b>
                    </XmlUtilityTest.ClassWithInterface>
                </ArrayOfUtilityTest.ClassWithInterface>";

            var copy = _xmlUtility.DeserializeArrayFromXml<ClassWithInterface>(data);

            string msg = "It should be possible to deserialize the old serialized concepts (in table Rhetos.AppliedConcept) with a new version of the class, if its interface is backward compatible.";
            Assert.AreEqual(orig.Length, copy.Length, msg);
            Assert.AreEqual(orig[0].a, copy[0].a, msg);
            Assert.AreEqual(orig[1].a, copy[1].a, msg);
            Assert.AreEqual(((ClassWithInterface)orig[0]).b, ((ClassWithInterface)copy[0]).b, msg);
            Assert.AreEqual(((ClassWithInterface)orig[1]).b, ((ClassWithInterface)copy[1]).b, msg);
        }

        [TestMethod]
        public void GetValueTest_Simple()
        {
            var dictionary = new Dictionary<string, string> {{"a", "b"}};
            Assert.AreEqual("b", dictionary.GetValue("a", "message"));
        }

        [TestMethod]
        [ExpectedException(typeof(FrameworkException))]
        public void GetValueTest_Exception()
        {
            const string message = "Test {0}";
            const string missingKey = "c";
            try
            {
                var dictionary = new Dictionary<string, string> { { "a", "b" } };
                dictionary.GetValue(missingKey, message);
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.Contains(string.Format(message, missingKey)));
                throw;
            }
        }
    }
}
