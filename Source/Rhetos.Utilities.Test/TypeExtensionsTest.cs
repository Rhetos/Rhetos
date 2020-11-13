using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.TestCommon;
using System;
using System.Collections.Generic;
using System.Text;

namespace Rhetos.Utilities.Test
{
    [TestClass]
    public class TypeExtensionsTest
    {
        [TestMethod]
        public void GetSubclassOfRawGenericTest()
        {
            TestUtility.ShouldFail<ArgumentException>(() => typeof(int).GetUnderlyingGenericType(typeof(IEnumerable<>)), "Interfaces are not supported");
            TestUtility.ShouldFail<ArgumentException>(() => typeof(int).GetUnderlyingGenericType(typeof(System.Collections.ArrayList)), "The type must be a generic type");
            TestUtility.ShouldFail<ArgumentException>(() => typeof(int).GetUnderlyingGenericType(typeof(List<string>)), "The generic type should not have any type arguments");
            Assert.IsNull(typeof(int).GetUnderlyingGenericType(typeof(List<>)), "If the method was not able to find a sublcass that implements the generic type it will return null.");
            Assert.AreEqual(typeof(List<string>), typeof(UnderlyingGenericTypeTestClass1).GetUnderlyingGenericType(typeof(List<>)));
            Assert.AreEqual(typeof(List<string>), typeof(UnderlyingGenericTypeTestClass2<string>).GetUnderlyingGenericType(typeof(List<>)));
        }

        public class UnderlyingGenericTypeTestClass1 : List<string>
        {}

        public class UnderlyingGenericTypeTestClass2<T> : List<T>
        { }
    }
}
