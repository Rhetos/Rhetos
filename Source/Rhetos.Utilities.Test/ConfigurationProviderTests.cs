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
using System.Reflection;
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
                .AddKeyValue("App:TestSection:StringValue", "hello")
                .AddKeyValue("RootValue", "world")
                .Build();

            var keys = string.Join(",", provider.AllKeys);
            Assert.AreEqual("app:testsection:stringvalue,rootvalue", keys);
        }

        [TestMethod]
        public void ThrowsOnInvalidKeys()
        {
            {
                var builder = new ConfigurationBuilder()
                    .AddKeyValue("  ", "ble");
                TestUtility.ShouldFail<FrameworkException>(() => builder.Build(), "empty or null configuration key");
            }
            {
                var builder = new ConfigurationBuilder()
                    .AddKeyValue("", "ble");
                TestUtility.ShouldFail<FrameworkException>(() => builder.Build(), "empty or null configuration key");
            }
            {
                var builder = new ConfigurationBuilder()
                    .AddKeyValue(null, "ble");
                TestUtility.ShouldFail<ArgumentNullException>(() => builder.Build());
            }
        }

        [TestMethod]
        public void GetByPathAndName()
        {
            var provider = new ConfigurationBuilder()
                .AddKeyValue("App:TestSection:StringValue", "Hello")
                .AddKeyValue("RootValue", "world")
                .Build();

            Assert.AreEqual("n/a", provider.GetValue("StringValue", "n/a"));
            Assert.AreEqual("Hello", provider.GetValue("StringValue", "n/a", "App:TestSection"));
            Assert.AreEqual("Hello", provider.GetValue("stringvalue", "n/a", "app:testSection"));

            Assert.AreEqual("n/a", provider.GetValue("RootValue", "n/a", "App:TestSection"));
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
                .AddKeyValue("App:TestSection:StringValue", "hello")
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
                .AddKeyValue("App:StringProp", "stringConfigured")
                .AddKeyValue("StringProp2", "stringConfigured")
                .AddKeyValue("App:IntProp", "5")
                .AddKeyValue("IntProp2", 5)
                .AddKeyValue("App:BoolValue", true)
                .AddKeyValue("App:doublevaluecomma", "3,14")
                .AddKeyValue("APP:DOUBLEVALUEDOT", "3.15")
                .AddKeyValue("App:DoubleValueObject", 3.16)
                .AddKeyValue("App:EnumValueString", "ValueA")
                .AddKeyValue("App:EnumValueObject", TestEnum.ValueB)
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
                    .AddKeyValue("App:EnumValueString", "ValueC")
                    .Build();

                TestUtility.ShouldFail<FrameworkException>(() => provider.ConfigureOptions<PocoOptions>("App"), "Type conversion failed");
            }

            {
                var provider = new ConfigurationBuilder()
                    .AddKeyValue("App:IntProp", "120_not_int")
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
                .AddKeyValue("section:opt1", "101")
                .AddKeyValue("section:opt2", "102")
                .Build();

            TestUtility.ShouldFail(() => provider.ConfigureOptions<Poco2>(requireAllMembers: true), "requires all members", "opt2");
            var poco2 = provider.ConfigureOptions<Poco2>("section", true);

            Assert.AreEqual("101", poco2.opt1);
            Assert.AreEqual("102", poco2.opt2);
        }

        [TestMethod]
        [DeploymentItem("ConnectionStrings.config")]
        public void ConfigurationManagerSource()
        {
            var provider = new ConfigurationBuilder()
                .AddConfigurationManagerConfiguration()
                .Build();

            Assert.IsTrue(provider.AllKeys.Contains("connectionstrings:serverconnectionstring:name"));
            Assert.AreEqual(30, provider.GetValue("SqlCommandTimeout", 0));
            Assert.AreEqual("TestSettingValue", provider.GetValue("AdditionalTestSetting", "", "TestSection"));
        }

        [TestMethod]
        [DeploymentItem("TestCfg.config")]
        public void ConfigurationFileSource()
        {
            var provider = new ConfigurationBuilder()
                .AddConfigurationFile("TestCfg.config")
                .Build();

            Assert.IsTrue(provider.AllKeys.Contains("connectionstrings:testconnectionstring:name"));
            Assert.AreEqual(99, provider.GetValue("TestCfgValue", 0));
        }

        [TestMethod]
        [DeploymentItem("Web.config")]
        public void WebConfigurationSource()
        {
            var rootPath = AppDomain.CurrentDomain.BaseDirectory;
            System.Diagnostics.Trace.WriteLine($"Using {rootPath} as rootPath.");
            var provider = new ConfigurationBuilder()
                .AddWebConfiguration(rootPath)
                .Build();

            Assert.IsTrue(provider.AllKeys.Contains("connectionstrings:webconnectionstring:name"));
            Assert.AreEqual(199, provider.GetValue("TestWebValue", 0));
        }

        public class PocoSingleValid
        {
            public int opt = 1;
            private int optPrivate = 2;
            public readonly int optReadOnlyField = 3;
            public int optGetOnlyProperty => 4;
            public int GetOptPrivate() => optPrivate;
        }

        [TestMethod]
        public void IgnoresNonSettable()
        {
            var provider = new ConfigurationBuilder()
                .AddKeyValue("opt", 11)
                .AddKeyValue("optPrivate", 12)
                .AddKeyValue("optReadOnlyField", 13)
                .AddKeyValue("optGetOnlyProperty", 14)
                .Build();

            var options = provider.ConfigureOptions<PocoSingleValid>();

            Assert.AreEqual(11, options.opt);
            Assert.AreEqual(2, options.GetOptPrivate());
            Assert.AreEqual(3, options.optReadOnlyField);
            Assert.AreEqual(4, options.optGetOnlyProperty);

        }

        public class PocoConstructor
        {
            private readonly int a;
            public PocoConstructor(int a)
            {
                this.a = a;
            }
        }

        [TestMethod]
        public void ThrowsOnNoConstructor()
        {
            var provider = new ConfigurationBuilder()
                .Build();

            TestUtility.ShouldFail<MissingMethodException>(() => provider.ConfigureOptions<PocoConstructor>(), "No parameterless constructor");
        }

        [TestMethod]
        public void CommandLineArguments()
        {
            var provider = new ConfigurationBuilder()
                .AddCommandLineArguments(new [] { "/option1", "/Option2", "-option3" }, "/")
                .Build();

            Assert.IsTrue(provider.AllKeys.Contains("option1"));
            Assert.IsTrue(provider.AllKeys.Contains("option2"));
            Assert.IsFalse(provider.AllKeys.Contains("option3"));

            Assert.IsTrue(provider.GetValue("option1", false));
            Assert.IsTrue(provider.GetValue("option2", false));
        }

        [TestMethod]
        public void CommandLineArgumentsPath()
        {
            var provider = new ConfigurationBuilder()
                .AddCommandLineArguments(new[] { "-option1", "/Option2", "/option3" }, "-", "TestSection")
                .Build();

            Assert.IsTrue(provider.AllKeys.Contains("testsection:option1"));
            Assert.IsFalse(provider.AllKeys.Contains("testsection:option2"));
            Assert.IsFalse(provider.AllKeys.Contains("option3"));

            Assert.IsTrue(provider.GetValue("Option1", false, "TestSection"));
        }

        [TestMethod]
        public void CommandLineArgumentsSkipEmpty()
        {
            var provider = new ConfigurationBuilder()
                .AddCommandLineArguments(new [] { "/", "/  ", "/help" }, "/")
                .Build();

            Assert.AreEqual(1, provider.AllKeys.Count());
        }
    }
}
