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
    public class DslSyntaxFileTest
    {
        [TestMethod]
        public void LoadDslSyntaxFile()
        {
            using (var scope = TestScope.Create(builder =>
            {
                builder.RegisterType<DslSyntaxFile>();
                builder.RegisterInstance(new RhetosBuildEnvironment { CacheFolder = $@"..\..\..\..\{TestAppSettings.TestAppName}\obj\Rhetos" });
            }))
            {
                var dslSyntaxFile = scope.Resolve<DslSyntaxFile>();
                var dslSyntax = dslSyntaxFile.Load();

                var conceptTypesInDslModel = scope.Resolve<IDslModel>().Concepts.Select(c => c.GetType()).Distinct().Count();

                Assert.AreNotEqual(0, dslSyntax.ConceptTypes.Count, "dslSyntax.ConceptTypes");
                Assert.AreNotEqual(0, conceptTypesInDslModel, "conceptTypesInDslModel");
                Assert.IsTrue(
                    dslSyntax.ConceptTypes.Count >= conceptTypesInDslModel,
                    $"dslSyntax.ConceptTypes {dslSyntax.ConceptTypes.Count}, conceptTypesInDslModel {conceptTypesInDslModel}");
            }
        }
    }
}
