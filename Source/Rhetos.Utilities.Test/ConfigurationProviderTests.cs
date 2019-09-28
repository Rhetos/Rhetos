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
using Rhetos.Utilities.ApplicationConfiguration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Utilities.Test
{
    [TestClass]
    public class ConfigurationProviderTests
    {
        [TestMethod]
        public void AllKeys()
        {
            var provider = new ConfigurationBuilder()
                .AddKeyValue("App__TestSection__StringValue", "hello")
                .AddKeyValue("RootValue", "world")
                .Build();

            var keys = string.Join(",", provider.AllKeys);
            Assert.AreEqual("App__TestSection__StringValue,RootValue", keys);
        }

        [TestMethod]
        public void GetByPathAndName()
        {
            var provider = new ConfigurationBuilder()
                .AddKeyValue("App__TestSection__StringValue", "hello")
                .AddKeyValue("RootValue", "world")
                .Build();

            Assert.AreEqual("n/a", provider.GetValue("StringValue", "n/a"));
            Assert.AreEqual("hello", provider.GetValue("StringValue", "n/a", "App__TestSection"));
            Assert.AreEqual("n/a", provider.GetValue("stringvalue", "n/a", "App__TestSection"));

            Assert.AreEqual("n/a", provider.GetValue("RootValue", "n/a", "App__TestSection"));
            Assert.AreEqual("world", provider.GetValue("RootValue", "n/a"));
        }

        public enum TestEnum
        {
            None,
            ValueA,
            ValueB
        }

        [TestMethod]
        public void ImplicitDefaults()
        {
            var provider = new ConfigurationBuilder()
                .AddKeyValue("App__TestSection__StringValue", "hello")
                .Build();

            Assert.AreEqual(null, provider.GetValue<string>("StringValue"));
            Assert.AreEqual(0, provider.GetValue<int>("IntValue"));
            Assert.AreEqual(0, provider.GetValue<double>("DoubleValue"));
            Assert.AreEqual(false, provider.GetValue<bool>("BoolValue"));
            Assert.AreEqual(TestEnum.None, provider.GetValue<TestEnum>("EnumValue"));
        }

        public enum FakeEnum
        {
            None
        }

        [TestMethod]
        public void CorrectlyHandleTypes()
        {
            var provider = new ConfigurationBuilder()
                .AddKeyValue("StringValue", "hello")
                .AddKeyValue("IntValue", 5)
                .AddKeyValue("BoolValue", true)
                .AddKeyValue("BoolValueFalse", false)
                .AddKeyValue("DoubleValue", 3.14)
                .AddKeyValue("EnumNone", TestEnum.None)
                .AddKeyValue("EnumA", TestEnum.ValueA)
                .AddKeyValue("EnumB", TestEnum.ValueB)
                .Build();

            TestUtility.ShouldFail<FrameworkException>(() => provider.GetValue("IntValue", "n/a"), "Can't convert");

            Assert.AreEqual(-1, provider.GetValue("IntValueNA", -1));
            Assert.AreEqual(5, provider.GetValue("IntValue", -1));

            Assert.AreEqual(false, provider.GetValue("BoolValueNA", false));
            Assert.AreEqual(true, provider.GetValue("BoolValue", false));
            Assert.AreEqual(false, provider.GetValue("BoolValueFalse", false));

            Assert.AreEqual(-1.5, provider.GetValue("DoubleValueNA", -1.5));
            Assert.AreEqual(3.14, provider.GetValue("DoubleValue", -1.5));

            // enums should fail when mixed
            TestUtility.ShouldFail<FrameworkException>(() => provider.GetValue("EnumNone", FakeEnum.None), "Can't convert");

            Assert.AreEqual(TestEnum.None, provider.GetValue("EnumNA", TestEnum.None));
            Assert.AreEqual(TestEnum.ValueA, provider.GetValue("EnumA", TestEnum.None));
            Assert.AreEqual(TestEnum.ValueB, provider.GetValue("EnumB", TestEnum.None));
        }

        public class PocoOptions
        {
            public string StringProp { get; set; } = "defaultString";
            public string StringProp2 { get; set; } = "defaultString";
            public int IntProp { get; set; } = 100;
            public int IntProp2 { get; set; } = 100;
            public bool BoolValue { get; set; } = false;
            public double DoubleValueComma { get; set; } = -1.5;
            public double DoubleValueDot { get; set; } = -1.5;
            public double DoubleValueObject { get; set; } = -1.5;
            public TestEnum EnumValueString { get; set; } = TestEnum.None;
            public TestEnum EnumValueObject { get; set; } = TestEnum.None;
            public int JustAField = 0;
        }

        [TestMethod]
        public void CorrectlyBindsOptions()
        {
            var provider = new ConfigurationBuilder()
                .AddKeyValue("App__StringProp", "stringConfigured")
                .AddKeyValue("StringProp2", "stringConfigured")
                .AddKeyValue("App__IntProp", "5")
                .AddKeyValue("IntProp2", 5)
                .AddKeyValue("App__BoolValue", true)
                .AddKeyValue("App__DoubleValueComma", "3,14")
                .AddKeyValue("App__DoubleValueDot", "3.15")
                .AddKeyValue("App__DoubleValueObject", 3.16)
                .AddKeyValue("App__EnumValueString", "ValueA")
                .AddKeyValue("App__EnumValueObject", TestEnum.ValueB)
                .Build();

            TestUtility.ShouldFail<FrameworkException>(() => provider.ConfigureOptions<PocoOptions>("App", true), "requires all members");
            var options = provider.ConfigureOptions<PocoOptions>("App");
            Assert.AreEqual("stringConfigured", options.StringProp);
            Assert.AreEqual("defaultString", options.StringProp2);
            Assert.AreEqual(5, options.IntProp);
            Assert.AreEqual(100, options.IntProp2);
            Assert.AreEqual(true, options.BoolValue);
            Assert.AreEqual(3.14, options.DoubleValueComma);
            Assert.AreEqual(3.15, options.DoubleValueDot);
            Assert.AreEqual(3.16, options.DoubleValueObject);
            Assert.AreEqual(TestEnum.ValueA, options.EnumValueString);
            Assert.AreEqual(TestEnum.ValueB, options.EnumValueObject);
        }

        [TestMethod]
        public void FailsBindOnConversion()
        {
            {
                var provider = new ConfigurationBuilder()
                    .AddKeyValue("App__EnumValueString", "ValueC")
                    .Build();

                TestUtility.ShouldFail<FrameworkException>(() => provider.ConfigureOptions<PocoOptions>("App"), "Type conversion failed");
            }

            {
                var provider = new ConfigurationBuilder()
                    .AddKeyValue("App__IntProp", "120_not_int")
                    .Build();

                TestUtility.ShouldFail<FrameworkException>(() => provider.ConfigureOptions<PocoOptions>("App"), "Type conversion failed");
            }
        }

        [TestMethod]
        public void BindsFields()
        {
            var provider = new ConfigurationBuilder()
                .AddKeyValue("JustAField", 1337)
                .Build();

            var options = provider.ConfigureOptions<PocoOptions>();
            Assert.AreEqual(options.JustAField, 1337);
        }

        public class Poco2
        {
            public string opt1 { get; set; }
            public string opt2;
        }

        [TestMethod]
        public void CorrectlyRequiresAllMembers()
        {
            var provider = new ConfigurationBuilder()
                .AddKeyValue("opt1", "1")
                .AddKeyValue("section__opt1", "101")
                .AddKeyValue("section__opt2", "102")
                .Build();

            TestUtility.ShouldFail(() => provider.ConfigureOptions<Poco2>(requireAllMembers: true), "requires all members", "opt2");
            var poco2 = provider.ConfigureOptions<Poco2>("section", true);

            Assert.AreEqual("101", poco2.opt1);
            Assert.AreEqual("102", poco2.opt2);
        }

        [TestMethod]
        public void SystemConfigurationSource()
        {
            var provider = new ConfigurationBuilder()
                .AddSystemConfiguration()
                .Build();

            Assert.IsTrue(provider.AllKeys.Contains("ConnectionStrings__ServerConnectionString__Name"));
            Assert.AreEqual(30, provider.GetValue("SqlCommandTimeout", 0));
            Assert.AreEqual("TestSettingValue", provider.GetValue("AdditionalTestSetting", "", "TestSection"));
        }
    }
}
