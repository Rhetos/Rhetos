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
using Rhetos.Dom.DefaultConcepts;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Rhetos.CommonConcepts.Test.Mocks;
using System.Linq.Expressions;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Dsl;

namespace Rhetos.CommonConcepts.Test
{
    [TestClass]
    public class AutomaticSubtypeImplementationTest
    {
        public static IDslModel GenerateTestDslModel()
        {
            var module = new ModuleInfo { Name = "PolymorphicTest" };

            var polymorphic = new PolymorphicInfo { Module = module, Name = "SimplePolymorphic" };
            var polymorphicNameProperty = new ShortStringPropertyInfo { DataStructure = polymorphic, Name = "Name" };
            var polymorphicDaysProperty = new IntegerPropertyInfo { DataStructure = polymorphic, Name = "Days" };
            var polymorphicSubtypeProperty = new ShortStringPropertyInfo { DataStructure = polymorphic, Name = "Subtype" };

            var simple1 = new EntityInfo { Module = module, Name = "Simple1" };
            var simple1NameProperty = new ShortStringPropertyInfo { DataStructure = simple1, Name = "Name" };
            var isSubtypeOfInfo1 = new IsSubtypeOfInfo { Subtype = simple1, Supertype = polymorphic, ImplementationName = "SimplePolymorphicImplementation" };

            var polymorphicSimpleProperty = new ReferencePropertyInfo { DataStructure = polymorphic, Referenced = simple1 ,Name = "Simple"};
            

            var poylmorphicUnionViewInfo = new PolymorphicUnionViewInfo { Module = module, Name = "SimplePolymorphic", CreateSql = "", RemoveSql = "" };
            var extensibleSubtypeSqlViewInfo = new ExtensibleSubtypeSqlViewInfo { IsSubtypeOf = isSubtypeOfInfo1, Module = module, Name = "Simple_As_SimplePolymorphic", ViewSource = "" };
            var subtypeExtendPolymorphicInfo = new SubtypeExtendPolymorphicInfo { IsSubtypeOf = isSubtypeOfInfo1, PolymorphicUnionView = poylmorphicUnionViewInfo, SubtypeImplementationView = extensibleSubtypeSqlViewInfo };

            var polymorphicNamePropertyImplementation = new PolymorphicPropertyInfo { Property = polymorphicNameProperty, Dependency_PolymorphicUnionView = poylmorphicUnionViewInfo };
            var polymorphicDaysPropertyImplementation = new PolymorphicPropertyInfo { Property = polymorphicDaysProperty, Dependency_PolymorphicUnionView = poylmorphicUnionViewInfo };
            var polymorphicSimplePropertyImplementation = new PolymorphicPropertyInfo { Property = polymorphicSimpleProperty, Dependency_PolymorphicUnionView = poylmorphicUnionViewInfo };

            var subtypeImplementsPropertyInfo = new SubtypeImplementsPropertyInfo { Dependency_ImplementationView = extensibleSubtypeSqlViewInfo, Expression = "Name", IsSubtypeOf = isSubtypeOfInfo1, Property = polymorphicNameProperty };
            var subtypeImplementsPropertyInfo2 = new SubtypeImplementsPropertyInfo { Dependency_ImplementationView = extensibleSubtypeSqlViewInfo, Expression = "Days", IsSubtypeOf = isSubtypeOfInfo1, Property = polymorphicDaysProperty };

            return new DslModelMock {
                module, polymorphic, polymorphicNameProperty, polymorphicDaysProperty, polymorphicSubtypeProperty, polymorphicNamePropertyImplementation,
                polymorphicDaysPropertyImplementation, simple1, simple1NameProperty, isSubtypeOfInfo1, polymorphicSimpleProperty, polymorphicSimplePropertyImplementation,
                poylmorphicUnionViewInfo, extensibleSubtypeSqlViewInfo, subtypeExtendPolymorphicInfo, subtypeImplementsPropertyInfo, subtypeImplementsPropertyInfo2
            };
        }
        
        [TestMethod]
        public void InheritingNonexistentPropertyTest()
        {
            var configuration = new MockConfiguration();
            configuration.Add("CommonConcepts.Legacy.AutoGeneratePolymorphicProperty", false);
            var dslModel = GenerateTestDslModel();

            var extensibleSubtypeSqlViewMacro = new ExtensibleSubtypeSqlViewMacro();
            var createdConcepts  = extensibleSubtypeSqlViewMacro.CreateNewConcepts(
                new ExtensibleSubtypeSqlViewInfo {
                    IsSubtypeOf = dslModel.FindByType<IsSubtypeOfInfo>().First()
                }
                , dslModel);
        }
    }
}
