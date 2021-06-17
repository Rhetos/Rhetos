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

using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Dsl;
using Rhetos.Utilities;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CommonConcepts.Test
{
    [TestClass]
    public class DslDocumentationFileTest
    {
        [TestMethod]
        public void LoadDslDocumentationFile()
        {
            using (var scope = TestScope.Create(builder =>
            {
                builder.RegisterType<DslDocumentationFile>();
                builder.RegisterInstance(new RhetosBuildEnvironment { CacheFolder = @"..\..\..\obj\Rhetos" });
            }))
            {
                var dslDocumentationFile = scope.Resolve<DslDocumentationFile>();
                var dslDocumentation = dslDocumentationFile.Load();

                var conceptTypesInDslModel = scope.Resolve<IDslModel>().Concepts.Select(c => c.GetType()).Distinct().Count();

                Assert.AreNotEqual(0, dslDocumentation.Concepts, "dslDocumentation.Concepts");
                Assert.AreNotEqual(0, conceptTypesInDslModel, "conceptTypesInDslModel");
                Assert.IsTrue(
                    dslDocumentation.Concepts.Count >= conceptTypesInDslModel,
                    $"dslDocumentation.Concepts {dslDocumentation.Concepts.Count}, conceptTypesInDslModel {conceptTypesInDslModel}");
            }
        }
    }
}
