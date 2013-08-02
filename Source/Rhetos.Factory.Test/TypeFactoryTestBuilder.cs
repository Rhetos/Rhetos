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
    public class TypeFactoryTestBuilder
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
        public void TestRegisterTypeAsWithBuilder()
        {
            var factory = CreateFactory();
            Assert.IsNotNull(factory);
            var builder =new TypeFactoryBuilder();
            builder.RegisterBuilderType(new TypeFactoryBuilderType { Type = typeof(DummyClass), AsType = typeof(IDummyInterface) });
            factory.Register(builder);
            var dc1 = factory.Resolve<IDummyInterface>();
            Assert.IsNotNull(dc1);
            var dc2 = factory.Resolve<IDummyInterface>();
            Assert.IsNotNull(dc2);
            Assert.AreNotEqual(dc1, dc2);
        }

        [TestMethod]
        public void TestRegisterTypeAsInstanceWithBuilder()
        {
            var factory = CreateFactory();
            Assert.IsNotNull(factory);
            var builder = new TypeFactoryBuilder();
            builder.RegisterBuilderType(new TypeFactoryBuilderType { Type = typeof(DummyClass), AsType = typeof(IDummyInterface), Singleton = true });
            factory.Register(builder);
            var dc1 = factory.Resolve<IDummyInterface>();
            Assert.IsNotNull(dc1);
            var dc2 = factory.Resolve<IDummyInterface>();
            Assert.IsNotNull(dc2);
            Assert.AreEqual(dc1, dc2);
        }

        [TestMethod]
        public void TestRegisterTypeWithBuilderAndCheckRegistration()
        {
            var factory = CreateFactory();
            Assert.IsNotNull(factory);
            var builder = new TypeFactoryBuilder();
            builder.RegisterBuilderType(new TypeFactoryBuilderType { Type = typeof(DummyClass) });
            factory.Register(builder);
            Assert.IsTrue(factory.IsRegistered(typeof(DummyClass)));
            Assert.IsFalse(factory.IsRegistered(typeof(IDummyInterface)));
        }

        [TestMethod]
        public void TestRegisterTypeAsWithBuilderAndCheckRegistration()
        {
            var factory = CreateFactory();
            Assert.IsNotNull(factory);
            var builder = new TypeFactoryBuilder();
            builder.RegisterBuilderType(new TypeFactoryBuilderType { Type = typeof(DummyClass), AsType = typeof(IDummyInterface) });
            factory.Register(builder);
            Assert.IsFalse(factory.IsRegistered(typeof(DummyClass)));
            Assert.IsTrue(factory.IsRegistered(typeof(IDummyInterface)));
        }

        [TestMethod]
        public void TestRegisterSingletonTypeAsWithBuilderAndCheckRegistration()
        {
            var factory = CreateFactory();
            Assert.IsNotNull(factory);
            var builder = new TypeFactoryBuilder();
            builder.RegisterBuilderType(new TypeFactoryBuilderType { Type = typeof(DummyClass), AsType = typeof(IDummyInterface), Singleton = true });
            factory.Register(builder);
            Assert.IsFalse(factory.IsRegistered(typeof(DummyClass)));
            Assert.IsTrue(factory.IsRegistered(typeof(IDummyInterface)));
        }

        [TestMethod]
        public void TestRegisterInstanceWithBuilder()
        {
            var factory = CreateFactory();
            Assert.IsNotNull(factory);
            var builder = new TypeFactoryBuilder();
            builder.RegisterBuilderInstance(new TypeFactoryBuilderInstance { Instance = new DummyClass() });
            factory.Register(builder);
            var dc1 = factory.Resolve<DummyClass>();
            Assert.IsNotNull(dc1);
            var dc2 = factory.Resolve<DummyClass>();
            Assert.IsNotNull(dc2);
            Assert.AreEqual(dc1, dc2);
        }

        [TestMethod]
        public void TestRegisterInstanceAsWithBuilder()
        {
            var factory = CreateFactory();
            Assert.IsNotNull(factory);
            var builder = new TypeFactoryBuilder();
            builder.RegisterBuilderInstance(new TypeFactoryBuilderInstance { Instance = new DummyClass(), AsType = typeof(IDummyInterface) });
            factory.Register(builder);
            var dc1 = factory.Resolve<IDummyInterface>();
            Assert.IsNotNull(dc1);
            var dc2 = factory.Resolve<IDummyInterface>();
            Assert.IsNotNull(dc2);
            Assert.AreEqual(dc1, dc2);
        }

        [TestMethod]
        public void TestRegisterInstanceWithBuilderAndCheckRegistration()
        {
            var factory = CreateFactory();
            Assert.IsNotNull(factory);
            var builder = new TypeFactoryBuilder();
            builder.RegisterBuilderInstance(new TypeFactoryBuilderInstance { Instance = new DummyClass() });
            factory.Register(builder);
            Assert.IsTrue(factory.IsRegistered(typeof(DummyClass)));
        }

        [TestMethod]
        public void TestRegisterInstanceAsWithBuilderAndCheckRegistration()
        {
            var factory = CreateFactory();
            Assert.IsNotNull(factory);
            var builder = new TypeFactoryBuilder();
            builder.RegisterBuilderInstance(new TypeFactoryBuilderInstance { Instance = new DummyClass(), AsType = typeof(IDummyInterface) });
            factory.Register(builder);
            Assert.IsTrue(factory.IsRegistered(typeof(IDummyInterface)));
        }

    }
}
