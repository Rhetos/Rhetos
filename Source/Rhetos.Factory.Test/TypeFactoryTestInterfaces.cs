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
    public class TypeFactoryTestInterfaces
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

        private interface IDummyInterface { }
        private class DummyClass : IDummyInterface { }

        [TestMethod]
        public void TestRegisterAndResolveInstanceWithExplicitly()
        {
            var factory = CreateFactory();
            Assert.IsNotNull(factory);
            factory.RegisterInstance<IDummyInterface>(new DummyClass());
            var dc = factory.Resolve<IDummyInterface>();
            Assert.IsNotNull(dc);
        }

        [TestMethod]
        public void TestRegisterAndResolveInstanceWithCasting()
        {
            var factory = CreateFactory();
            Assert.IsNotNull(factory);
            factory.RegisterInstance(new DummyClass() as IDummyInterface);
            var dc = factory.Resolve<IDummyInterface>();
            Assert.IsNotNull(dc);
        }

        [TestMethod]
        public void TestRegisterAndResolveInstanceFromTypeInference()
        {
            var factory = CreateFactory();
            Assert.IsNotNull(factory);
            IDummyInterface dummy = new DummyClass();
            factory.RegisterInstance(dummy);
            var dc = factory.Resolve<IDummyInterface>();
            Assert.IsNotNull(dc);
        }

        [TestMethod]
        public void TestRegisterAndResolveFunc()
        {
            var factory = CreateFactory();
            Assert.IsNotNull(factory);
            factory.RegisterFunc<IDummyInterface>(tf => new DummyClass());
            var dc = factory.Resolve<IDummyInterface>();
            Assert.IsNotNull(dc);
        }

        [TestMethod]
        public void TestRegisterTypeAndResolveFunc()
        {
            var factory = CreateFactory();
            Assert.IsNotNull(factory);
            factory.RegisterType(typeof(DummyClass));
            factory.RegisterFunc<IDummyInterface>(tf => tf.Resolve<DummyClass>());
            var dc = factory.Resolve<IDummyInterface>();
            Assert.IsNotNull(dc);
        }

        [TestMethod]
        [ExpectedException(typeof(MissingMethodException))]
        public void TestRegisterTypeWithoutInterface()
        {
            var factory = CreateFactory();
            Assert.IsNotNull(factory);
            factory.RegisterType(typeof(DummyClass));
            var dc = factory.Resolve<IDummyInterface>();
            Assert.IsNotNull(dc);
        }

        [TestMethod]
        [ExpectedException(typeof(MissingMethodException))]
        public void TestRegisterInInnerContainerAndResolveInParent()
        {
            var factory = CreateFactory();
            Assert.IsNotNull(factory);
            var inner = factory.CreateInnerTypeFactory();
            inner.RegisterInstance<IDummyInterface>(new DummyClass());
            var dc = factory.Resolve<IDummyInterface>();
            Assert.IsNotNull(dc);
        }

        [TestMethod]
        public void TestRegisterInInnerContainerAndResolveInInner()
        {
            var factory = CreateFactory();
            Assert.IsNotNull(factory);
            var inner = factory.CreateInnerTypeFactory();
            inner.RegisterInstance<IDummyInterface>(new DummyClass());
            var dc = inner.Resolve<IDummyInterface>();
            Assert.IsNotNull(dc);
        }

        private class DisposableClass : IDummyInterface, IDisposable
        {
            public static bool IsDisposed;
            public void Dispose()
            {
                IsDisposed = true;
            }
        }

        [TestMethod]
        public void TestRegisterDisposableInterface()
        {
            var factory = CreateFactory();
            Assert.IsFalse(DisposableClass.IsDisposed);
            using (var inner = factory.CreateInnerTypeFactory())
            {
                Assert.IsFalse(DisposableClass.IsDisposed);
                inner.RegisterInstance<IDummyInterface>(new DisposableClass());
                Assert.IsFalse(DisposableClass.IsDisposed);
                var dc = inner.Resolve<IDummyInterface>();
                Assert.IsNotNull(dc);
                Assert.IsFalse(DisposableClass.IsDisposed);
            }
            Assert.IsTrue(DisposableClass.IsDisposed);
        }
    }
}
