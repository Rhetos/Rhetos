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
using Autofac;
using Rhetos.Extensibility;

namespace Rhetos.Factory.Test
{
    [TestClass]
    public class TypeFactoryTestClasses
    {
        private class ProxyFactoryMock : IAspectFactory
        {
            public void RegisterAspect<TAspected>(IAspect aspect) { }
            public object CreateProxy(Type type, object value) { return value; }
            public TProxy CreateProxy<TProxy>(object value) { return (TProxy)value; }
        }

        private ITypeFactory CreateFactory()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<ProxyFactoryMock>().As<IAspectFactory>().SingleInstance();
            builder.RegisterType<TypeFactory>().As<ITypeFactory>().SingleInstance();
            var container = builder.Build();
            return container.Resolve<ITypeFactory>();
        }

        private class DummyClass { }

        [TestMethod]
        public void TestRegisterAndResolveType()
        {
            var factory = CreateFactory();
            Assert.IsNotNull(factory);
            factory.RegisterType(typeof(DummyClass));
            var dc = factory.Resolve<DummyClass>();
            Assert.IsNotNull(dc);
        }

        [TestMethod]
        public void TestRegisterAndCheckRegistrationType()
        {
            var factory = CreateFactory();
            Assert.IsNotNull(factory);
            Assert.IsFalse(factory.IsRegistered(typeof(DummyClass)));
            factory.RegisterType(typeof(DummyClass));
            Assert.IsTrue(factory.IsRegistered(typeof(DummyClass)));
            var dc = factory.Resolve<DummyClass>();
            Assert.IsNotNull(dc);
        }

        [TestMethod]
        public void TestRegisterAndResolveTypeCheckDifferentInstance()
        {
            var factory = CreateFactory();
            Assert.IsNotNull(factory);
            factory.RegisterType(typeof(DummyClass));
            var dc1 = factory.Resolve<DummyClass>();
            Assert.IsNotNull(dc1);
            var dc2 = factory.Resolve<DummyClass>();
            Assert.IsNotNull(dc2);
            Assert.AreNotEqual(dc1, dc2);
        }

        [TestMethod]
        public void TestResolveTypeCheckDifferentInstance()
        {
            var factory = CreateFactory();
            Assert.IsNotNull(factory);
            var dc1 = factory.Resolve<DummyClass>();
            Assert.IsNotNull(dc1);
            var dc2 = factory.Resolve<DummyClass>();
            Assert.IsNotNull(dc2);
            Assert.AreNotEqual(dc1, dc2);
        }

        [TestMethod]
        public void TestRegisterAndResolveTypeWithInnerContainer()
        {
            var factory = CreateFactory();
            Assert.IsNotNull(factory);
            factory.RegisterType(typeof(DummyClass));
            var inner = factory.CreateInnerTypeFactory();
            var dc1 = factory.Resolve<DummyClass>();
            Assert.IsNotNull(dc1);
            var dc2 = inner.Resolve<DummyClass>();
            Assert.IsNotNull(dc2);
        }

        [TestMethod]
        public void TestRegisterTypeToInnerAndIsNotInParentContainer()
        {
            var factory = CreateFactory();
            Assert.IsNotNull(factory);
            var inner = factory.CreateInnerTypeFactory();
            inner.RegisterType(typeof(DummyClass));
            var dc1 = inner.Resolve<DummyClass>();
            Assert.IsNotNull(dc1);
            Assert.IsFalse(factory.IsRegistered(typeof(DummyClass)));
        }

        [TestMethod]
        public void TestCanResolveTypeWithoutRegistration()
        {
            var factory = CreateFactory();
            Assert.IsNotNull(factory);
            Assert.IsFalse(factory.IsRegistered(typeof(DummyClass)));
            var dc = factory.Resolve<DummyClass>();
            Assert.IsNotNull(dc);
        }

        [TestMethod]
        public void TestRegisterAndResolveInstance()
        {
            var factory = CreateFactory();
            Assert.IsNotNull(factory);
            factory.RegisterInstance(new DummyClass());
            var dc = factory.Resolve<DummyClass>();
            Assert.IsNotNull(dc);
        }


        [TestMethod]
        public void TestRegisterAndResolveSameInstance()
        {
            var factory = CreateFactory();
            Assert.IsNotNull(factory);
            factory.RegisterInstance(new DummyClass());
            var dc1 = factory.Resolve<DummyClass>();
            Assert.IsNotNull(dc1);
            var dc2 = factory.Resolve<DummyClass>();
            Assert.IsNotNull(dc2);
            Assert.AreEqual(dc1, dc2);
        }

        [TestMethod]
        public void TestRegisterAndCheckIfiItIsRegisteredSameInstance()
        {
            var factory = CreateFactory();
            Assert.IsNotNull(factory);
            Assert.IsFalse(factory.IsRegistered(typeof(DummyClass)));
            factory.RegisterInstance(new DummyClass());
            Assert.IsTrue(factory.IsRegistered(typeof(DummyClass)));
            var dc = factory.Resolve<DummyClass>();
            Assert.IsNotNull(dc);
        }

        [TestMethod]
        public void TestRegisterAndResolveSameInstanceInInnerContainer()
        {
            var factory = CreateFactory();
            Assert.IsNotNull(factory);
            factory.RegisterInstance(new DummyClass());
            var dc1 = factory.Resolve<DummyClass>();
            Assert.IsNotNull(dc1);
            var inner = factory.CreateInnerTypeFactory();
            var dc2 = inner.Resolve<DummyClass>();
            Assert.IsNotNull(dc2);
            Assert.AreEqual(dc1, dc2);
        }

        [TestMethod]
        public void TestRegisterInInnerContainerDifferentThanParentInstance()
        {
            var factory = CreateFactory();
            Assert.IsNotNull(factory);
            var inner = factory.CreateInnerTypeFactory();
            inner.RegisterInstance(new DummyClass());
            var dc2 = inner.Resolve<DummyClass>();
            Assert.IsNotNull(dc2);
            var dc1 = factory.Resolve<DummyClass>();
            Assert.IsNotNull(dc1);
            Assert.AreNotEqual(dc1, dc2);
            var dc3 = factory.Resolve<DummyClass>();
            Assert.IsNotNull(dc3);
            Assert.AreNotEqual(dc2, dc3);
            var dc4 = inner.Resolve<DummyClass>();
            Assert.IsNotNull(dc4);
            Assert.AreEqual(dc2, dc4);
        }

        private class DisposableClass : IDisposable
        {
            public static bool IsDisposed;
            public void Dispose()
            {
                IsDisposed = true;
            }
        }

        [TestMethod]
        public void TestRegisterDisposableType()
        {
            var factory = CreateFactory();
            Assert.IsFalse(DisposableClass.IsDisposed);
            using (var inner = factory.CreateInnerTypeFactory())
            {
                Assert.IsFalse(DisposableClass.IsDisposed);
                inner.RegisterType(typeof(DisposableClass));
                Assert.IsFalse(DisposableClass.IsDisposed);
                var dc1 = inner.Resolve<DisposableClass>();
                Assert.IsFalse(DisposableClass.IsDisposed);
            }
            Assert.IsTrue(DisposableClass.IsDisposed);
        }

    }
}
