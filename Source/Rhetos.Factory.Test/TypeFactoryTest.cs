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
    public class TypeFactoryTest
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

        [TestMethod]
        public void TestCanCreateFactory()
        {
            var factory = CreateFactory();
            Assert.IsNotNull(factory);
        }

        [TestMethod]
        public void TestCanCreateInnerFactory()
        {
            var factory = CreateFactory();
            Assert.IsNotNull(factory);
            var inner = factory.CreateInnerTypeFactory();
            Assert.IsNotNull(inner);
        }

        [TestMethod]
        public void TestInnerFactoryIsNotSame()
        {
            var factory = CreateFactory();
            Assert.IsNotNull(factory);
            var inner = factory.CreateInnerTypeFactory();
            Assert.IsNotNull(inner);
            Assert.AreNotEqual(factory, inner);
        }

        [TestMethod]
        public void TestInnerFactoriesAreNotSame()
        {
            var factory = CreateFactory();
            Assert.IsNotNull(factory);
            var inner1 = factory.CreateInnerTypeFactory();
            Assert.IsNotNull(inner1);
            var inner2 = factory.CreateInnerTypeFactory();
            Assert.IsNotNull(inner2);
            Assert.AreNotEqual(inner1, inner2);
        }
    }
}
