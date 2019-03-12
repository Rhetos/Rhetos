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
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rhetos.Dsl.Test
{
    [TestClass]
    public class TypeExtensiontest
    {

        public class SimpleConcept1 : IConceptInfo { }

        public class DerivationConcept1 : SimpleConcept1 { }

        public class SimpleConcept2 : IConceptInfo { }

        public interface SimpleTypeExtension<out T> : ITypeExtension<T> where T : IConceptInfo
        {
            string ExtensionForType{ get; }
        }

        public class SimpleTypeExtensionImplementation1 : SimpleTypeExtension<SimpleConcept1>
        {
            public string ExtensionForType { get { return "SimpleConcept1"; } }
        }

        public class DerivationTypeExtensionImplementation1 : SimpleTypeExtension<DerivationConcept1>
        {
            public string ExtensionForType { get { return "DerivationConcept1"; } }
        }

        public class SimpleTypeExtensionImplementation12 : SimpleTypeExtension<SimpleConcept1>
        {
            public string ExtensionForType { get { return "SimpleConcept1"; } }
        }

        public class SimpleTypeExtensionImplementation2 : SimpleTypeExtension<SimpleConcept2>
        {
            public string ExtensionForType { get { return "SimpleConcept2"; } }
        }

        [TestMethod]
        public void RetrieveTypeExtensionTest()
        {
            var extensionProvider = new TypeExtensionProvider(new MockPluginsContainer<ITypeExtension>(new ITypeExtension[] {
                new SimpleTypeExtensionImplementation1(),
                new SimpleTypeExtensionImplementation2(),
                new DerivationTypeExtensionImplementation1()
            }));

            Assert.AreEqual("SimpleConcept1", extensionProvider.Get<SimpleTypeExtension<IConceptInfo>>(typeof(SimpleConcept1)).ExtensionForType);
            Assert.AreEqual("SimpleConcept2", extensionProvider.Get<SimpleTypeExtension<IConceptInfo>>(typeof(SimpleConcept2)).ExtensionForType);
            Assert.AreEqual("DerivationConcept1", extensionProvider.Get<SimpleTypeExtension<IConceptInfo>>(typeof(DerivationConcept1)).ExtensionForType);
        }

        [TestMethod]
        public void MultipleExtensionForSameTypeErrorTest()
        {
            TestUtility.ShouldFail(()=> {
                var extensionProvider = new TypeExtensionProvider(new MockPluginsContainer<ITypeExtension>(new ITypeExtension[] {
                    new SimpleTypeExtensionImplementation1(),
                    new SimpleTypeExtensionImplementation12()
                }));

                var typeExtension =  extensionProvider.Get<SimpleTypeExtension<IConceptInfo>>(typeof(SimpleConcept1));
            }, "There is already an implementation of");
        }

        [TestMethod]
        public void FallbackTypeTest()
        {
            var extensionProvider = new TypeExtensionProvider(new MockPluginsContainer<ITypeExtension>(new ITypeExtension[] {
                new SimpleTypeExtensionImplementation1()
            }));
            
            Assert.AreEqual("SimpleConcept1", extensionProvider.Get<SimpleTypeExtension<IConceptInfo>>(typeof(DerivationConcept1)).ExtensionForType);
        }

        [TestMethod]
        public void NoTypeExtensionTest()
        {
            var extensionProvider = new TypeExtensionProvider(new MockPluginsContainer<ITypeExtension>(new ITypeExtension[] {}));

            TestUtility.ShouldFail(() => {
                var typeExtension = extensionProvider.Get<SimpleTypeExtension<IConceptInfo>>(typeof(SimpleConcept1));
            }, "There is no type extension");
        }
    }
}
