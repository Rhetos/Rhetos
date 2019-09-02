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
    public class ConceptMetadataTest
    {

        public class SimpleConcept1 : IConceptInfo { }

        public class DerivationConcept1 : SimpleConcept1 { }

        public class SimpleConcept2 : IConceptInfo { }

        public interface ISimpleConceptMetadata<out T> : IConceptMetadataExtension<T> where T : IConceptInfo
        {
            string ExtensionForType{ get; }
        }

        public class SimpleConceptMetadataImplementation1 : ISimpleConceptMetadata<SimpleConcept1>
        {
            public string ExtensionForType { get { return "SimpleConcept1"; } }
        }

        public class DerivationConceptMetadataImplementation1 : ISimpleConceptMetadata<DerivationConcept1>
        {
            public string ExtensionForType { get { return "DerivationConcept1"; } }
        }

        public class SimpleConceptMetadataImplementation12 : ISimpleConceptMetadata<SimpleConcept1>
        {
            public string ExtensionForType { get { return "SimpleConcept1"; } }
        }

        public class SimpleConceptMetadataImplementation2 : ISimpleConceptMetadata<SimpleConcept2>
        {
            public string ExtensionForType { get { return "SimpleConcept2"; } }
        }

        [TestMethod]
        public void RetrieveConceptMetadataTest()
        {
            var metadataProvider = new ConceptMetadata(new MockPluginsContainer<IConceptMetadataExtension>(new IConceptMetadataExtension[] {
                new SimpleConceptMetadataImplementation1(),
                new SimpleConceptMetadataImplementation2(),
                new DerivationConceptMetadataImplementation1()
            }));

            Assert.AreEqual("SimpleConcept1", metadataProvider.Get<ISimpleConceptMetadata<IConceptInfo>>(typeof(SimpleConcept1)).ExtensionForType);
            Assert.AreEqual("SimpleConcept2", metadataProvider.Get<ISimpleConceptMetadata<IConceptInfo>>(typeof(SimpleConcept2)).ExtensionForType);
            Assert.AreEqual("DerivationConcept1", metadataProvider.Get<ISimpleConceptMetadata<IConceptInfo>>(typeof(DerivationConcept1)).ExtensionForType);
        }

        [TestMethod]
        public void MultipleExtensionForSameTypeErrorTest()
        {
            TestUtility.ShouldFail(()=> {
                var metadataProvider = new ConceptMetadata(new MockPluginsContainer<IConceptMetadataExtension>(new IConceptMetadataExtension[] {
                    new SimpleConceptMetadataImplementation1(),
                    new SimpleConceptMetadataImplementation12()
                }));

                metadataProvider.Get<ISimpleConceptMetadata<IConceptInfo>>(typeof(SimpleConcept1));
            },
                "There are multiple implementations of", "SimpleConceptMetadataImplementation1", "SimpleConceptMetadataImplementation12",
                "SimpleConceptMetadata", "SimpleConcept1");
        }

        [TestMethod]
        public void FallbackTypeTest()
        {
            var metadataProvider = new ConceptMetadata(new MockPluginsContainer<IConceptMetadataExtension>(new IConceptMetadataExtension[] {
                new SimpleConceptMetadataImplementation1()
            }));
            
            Assert.AreEqual("SimpleConcept1", metadataProvider.Get<ISimpleConceptMetadata<IConceptInfo>>(typeof(DerivationConcept1)).ExtensionForType);
        }

        [TestMethod]
        public void NoConceptMetadataTest()
        {
            var metadataProvider = new ConceptMetadata(new MockPluginsContainer<IConceptMetadataExtension>(new IConceptMetadataExtension[] {}));

            TestUtility.ShouldFail(() => {
                metadataProvider.Get<ISimpleConceptMetadata<IConceptInfo>>(typeof(SimpleConcept1));
            }, $"There is no {nameof(IConceptMetadataExtension)} plugin", "SimpleConceptMetadata", "SimpleConcept1");
        }
    }
}
