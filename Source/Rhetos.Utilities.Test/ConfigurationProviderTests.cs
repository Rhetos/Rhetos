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
using Rhetos.Utilities.ApplicationConfiguration.ConfigurationSources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Rhetos.Utilities.Test
{
    [TestClass]
    public class ConfigurationProviderTests
    {
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            // different test runners have different entry assemblies
            // this ensures that our 'app.config' is attached to correct one
            var source = Path.Combine(context.DeploymentDirectory, "app.config");
            var destination = Path.Combine(context.DeploymentDirectory, Path.GetFileName(Assembly.GetEntryAssembly().Location) + ".config");
            File.Copy(source, destination, true);
        }

        [TestMethod]
        public void AllKeys()
        {
            var provider = new ConfigurationBuilder(new ConsoleLogProvider())
                .AddKeyValue("App:TestSection:StringValue", "hello")
                .AddKeyValue("RootValue", "world")
                .Build();

            var keys = string.Join(",", provider.AllKeys);
            Assert.AreEqual("App:TestSection:StringValue,RootValue", keys);
        }

        [TestMethod]
        public void ThrowsOnInvalidKeys()
        {
            {
                var builder = new ConfigurationBuilder(new ConsoleLogProvider())
                    .AddKeyValue("  ", "ble");
                TestUtility.ShouldFail<FrameworkException>(() => builder.Build(), "empty or null configuration key");
            }
            {
                var builder = new ConfigurationBuilder(new ConsoleLogProvider())
                    .AddKeyValue("", "ble");
                TestUtility.ShouldFail<FrameworkException>(() => builder.Build(), "empty or null configuration key");
            }
            {
                var builder = new ConfigurationBuilder(new ConsoleLogProvider())
                    .AddKeyValue(null, "ble");
                TestUtility.ShouldFail<ArgumentNullException>(() => builder.Build());
            }
        }

        [TestMethod]
        public void OldDotConventionInvariance()
        {
            var provider = new ConfigurationBuilder(new ConsoleLogProvider())
                .AddConfigurationManagerConfiguration()
                .Build();

            Assert.AreEqual("DotDot", provider.GetValue<string>("TestSection.OldDotConvention"));
        }

        [TestMethod]
        public void GetByPathAndName()
        {
            var provider = new ConfigurationBuilder(new ConsoleLogProvider())
                .AddKeyValue("App:TestSection:StringValue", "Hello")
                .AddKeyValue("App.TestSection.StringValue", "Hello2")
                .AddKeyValue("RootValue", "world")
                .Build();

            Assert.AreEqual("n/a", provider.GetValue("StringValue", "n/a"));
            Assert.AreEqual("Hello2", provider.GetValue("StringValue", "n/a", "App:TestSection"));
            Assert.AreEqual("Hello2", provider.GetValue("StringValue", "n/a", "App.TestSection"));
            Assert.AreEqual("Hello2", provider.GetValue("stringvalue", "n/a", "app:testSection"));

            Assert.AreEqual("n/a", provider.GetValue("RootValue", "n/a", "App:TestSection"));
            Assert.AreEqual("world", provider.GetValue("RootValue", "n/a"));

            Assert.AreEqual("Hello2", provider.GetValue("App.TestSection.StringValue", "n/a"));
            Assert.AreEqual("Hello2", provider.GetValue("App:TestSection.StringValue", "n/a"));
            Assert.AreEqual("Hello2", provider.GetValue("App.TestSection:StringValue", "n/a"));
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
            var provider = new ConfigurationBuilder(new ConsoleLogProvider())
                .AddKeyValue("App:TestSection:StringValue", "hello")
                .Build();

            Assert.AreEqual(null, provider.GetValue<string>("StringValue"));
            Assert.AreEqual(0, provider.GetValue<int>("IntValue"));
            Assert.AreEqual(0, provider.GetValue<double>("DoubleValue"));
            Assert.AreEqual(false, provider.GetValue<bool>("BoolValue"));
            Assert.AreEqual(TestEnum.None, provider.GetValue<TestEnum>("EnumValue"));
        }

        [TestMethod]
        public void EnumBindingIsVerbose()
        {
            var provider = new ConfigurationBuilder(new ConsoleLogProvider())
                .AddKeyValue("EnumValue", "hello")
                .Build();

            var frameworkException = TestUtility.ShouldFail<FrameworkException>(() => provider.GetValue<TestEnum>("EnumValue"));
            Assert.IsTrue(frameworkException.Message.Contains("Allowed values for TestEnum are: None, ValueA, ValueB"));
        }

        public enum FakeEnum
        {
            None
        }

        [TestMethod]
        public void CorrectlyHandleTypes()
        {
            var provider = new ConfigurationBuilder(new ConsoleLogProvider())
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
            public IEnumerable<string> ArrayOfStrings { get; set; } = new[] { "defaultString" };
            public IEnumerable<string> ArrayOfStringsDefault { get; set; } = new[] { "defaultString" };
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
            var provider = new ConfigurationBuilder(new ConsoleLogProvider())
                .AddKeyValue("App:StringProp", "stringConfigured")
                .AddKeyValue("StringProp2", "stringConfigured")
                .AddKeyValue("App:IntProp", "5")
                .AddKeyValue("IntProp2", 5)
                .AddKeyValue("App:BoolValue", true)
                .AddKeyValue("App:doublevaluecomma", "3,14")
                .AddKeyValue("APP:DOUBLEVALUEDOT", "3.15")
                .AddKeyValue("app:doubleValueDOT", "3.99") // override previous setting
                .AddKeyValue("App:DoubleValueObject", "3.16")
                .AddKeyValue("App:EnumValueString", "ValueA")
                .AddKeyValue("App:EnumValueObject", TestEnum.ValueB)
                .AddKeyValue("App:ArrayOfStrings:0", "A")
                .AddKeyValue("App:ArrayOfStrings:1", "B")
                .Build();

            TestUtility.ShouldFail<FrameworkException>(() => provider.GetOptions<PocoOptions>("App", true), "requires all members");
            var options = provider.GetOptions<PocoOptions>("App");
            Assert.AreEqual("stringConfigured", options.StringProp);
            Assert.AreEqual("defaultString", options.StringProp2);
            Assert.AreEqual(5, options.IntProp);
            Assert.AreEqual(100, options.IntProp2);
            Assert.AreEqual(true, options.BoolValue);
            Assert.AreEqual(3.14, options.DoubleValueComma);
            Assert.AreEqual(3.99, options.DoubleValueDot);
            Assert.AreEqual(3.16, options.DoubleValueObject);
            Assert.AreEqual(TestEnum.ValueA, options.EnumValueString);
            Assert.AreEqual(TestEnum.ValueB, options.EnumValueObject);
            Assert.AreEqual("A, B", TestUtility.Dump(options.ArrayOfStrings));
            Assert.AreEqual("defaultString", TestUtility.Dump(options.ArrayOfStringsDefault));
        }

        [TestMethod]
        public void FailsBindOnConversion()
        {
            {
                var provider = new ConfigurationBuilder(new ConsoleLogProvider())
                    .AddKeyValue("App:EnumValueString", "ValueC")
                    .Build();

                TestUtility.ShouldFail<FrameworkException>(() => provider.GetOptions<PocoOptions>("App"), "Type conversion failed for configuration key 'EnumValueString'");
            }

            {
                var provider = new ConfigurationBuilder(new ConsoleLogProvider())
                    .AddKeyValue("App:IntProp", "120_not_int")
                    .Build();

                TestUtility.ShouldFail<FrameworkException>(() => provider.GetOptions<PocoOptions>("App"), "Type conversion failed for configuration key 'IntProp'");
            }
        }

        [TestMethod]
        public void BindsFields()
        {
            var provider = new ConfigurationBuilder(new ConsoleLogProvider())
                .AddKeyValue("JustAField", 1337)
                .Build();

            var options = provider.GetOptions<PocoOptions>();
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
            var provider = new ConfigurationBuilder(new ConsoleLogProvider())
                .AddKeyValue("opt1", "1")
                .AddKeyValue("section:opt1", "101")
                .AddKeyValue("section:opt2", "102")
                .Build();

            TestUtility.ShouldFail(() => provider.GetOptions<Poco2>(requireAllMembers: true), "requires all members", "opt2");
            var poco2 = provider.GetOptions<Poco2>("section", true);

            Assert.AreEqual("101", poco2.opt1);
            Assert.AreEqual("102", poco2.opt2);
        }

        [TestMethod]
        [DeploymentItem("ConnectionStrings.config")]
        public void ConfigurationManagerSource()
        {
            var provider = new ConfigurationBuilder(new ConsoleLogProvider())
                .AddConfigurationManagerConfiguration()
                .Build();

            Assert.IsTrue(provider.AllKeys.Contains("ConnectionStrings:ServerConnectionString:Name"));
            Assert.AreEqual(31, provider.GetValue("Rhetos:Database:SqlCommandTimeout", 0));
            Assert.AreEqual("TestSettingValue", provider.GetValue("AdditionalTestSetting", "", "TestSection"));
        }

        [TestMethod]
        [DeploymentItem("ConnectionStrings.config")]
        public void BindsConnectionStringOptions()
        {
            var provider = new ConfigurationBuilder(new ConsoleLogProvider())
                .AddConfigurationManagerConfiguration()
                .Build();

            var connectionStringName = provider.GetValue<string>($"ConnectionStrings:ServerConnectionString:Name");
            Assert.AreEqual("ServerConnectionString", connectionStringName);
        }


        [TestMethod]
        [DeploymentItem("TestCfg.config")]
        public void ConfigurationFileSource()
        {
            var provider = new ConfigurationBuilder(new ConsoleLogProvider())
                .AddConfigurationFile("TestCfg.config")
                .Build();

            Assert.IsTrue(provider.AllKeys.Contains("ConnectionStrings:TestConnectionString:Name"));
            Assert.AreEqual(99, provider.GetValue("TestCfgValue", 0));
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
            var provider = new ConfigurationBuilder(new ConsoleLogProvider())
                .AddKeyValue("opt", 11)
                .AddKeyValue("optPrivate", 12)
                .AddKeyValue("optReadOnlyField", 13)
                .AddKeyValue("optGetOnlyProperty", 14)
                .Build();

            var options = provider.GetOptions<PocoSingleValid>();

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
            var provider = new ConfigurationBuilder(new ConsoleLogProvider())
                .Build();

            TestUtility.ShouldFail<MissingMethodException>(() => provider.GetOptions<PocoConstructor>(), "No parameterless constructor");
        }

        public class PocoPath
        {
            public int Section__A__Option1 = -1;
        }

        [TestMethod]
        public void BindsPropertyWithFullPath()
        {
            {
                var provider = new ConfigurationBuilder(new ConsoleLogProvider())
                    .AddKeyValue("section:a:option1", 42)
                    .Build();

                Assert.AreEqual(42, provider.GetOptions<PocoPath>().Section__A__Option1);
            }

            {
                var provider = new ConfigurationBuilder(new ConsoleLogProvider())
                    .AddKeyValue("option1", 42)
                    .Build();

                Assert.AreEqual(-1, provider.GetOptions<PocoPath>().Section__A__Option1);
            }

            {
                var provider = new ConfigurationBuilder(new ConsoleLogProvider())
                    .AddKeyValue("section:a:option1", "notInt")
                    .Build();

                TestUtility.ShouldFail<FrameworkException>(() => provider.GetOptions<PocoPath>(), "Type conversion failed for configuration key 'Section__A__Option1'");
            }

            // add tests for all supported separators
        }

        [TestMethod]
        public void PropertyBindingSupportsMultipleSpecialChars()
        {
            {
                var provider = new ConfigurationBuilder(new ConsoleLogProvider())
                    .AddKeyValue("section:a:option1", 43)
                    .Build();

                Assert.AreEqual(43, provider.GetOptions<PocoPath>().Section__A__Option1);
            }

            {
                var provider = new ConfigurationBuilder(new ConsoleLogProvider())
                    .AddKeyValue("Section__a__Option1", 44)
                    .Build();

                Assert.AreEqual(44, provider.GetOptions<PocoPath>().Section__A__Option1);
            }

            {
                var provider = new ConfigurationBuilder(new ConsoleLogProvider())
                    .AddKeyValue("SECTION.A.Option1", 44)
                    .Build();

                Assert.AreEqual(44, provider.GetOptions<PocoPath>().Section__A__Option1);
            }
        }

        [TestMethod]
        public void PropertyBindingFailsOnMultipleMatches()
        {
            {
                var provider = new ConfigurationBuilder(new ConsoleLogProvider())
                    .AddKeyValue("section:a:option1", 45)
                    .AddKeyValue("Section__a__Option1", 46)
                    .Build();

                TestUtility.ShouldFail<FrameworkException>(() => provider.GetOptions<PocoPath>(), "Found multiple matches while binding configuration value to member 'Section__A__Option1'");
            }

            {
                var provider = new ConfigurationBuilder(new ConsoleLogProvider())
                    .AddKeyValue("section.a.option1", 47)
                    .AddKeyValue("Section__a__Option1", 48)
                    .Build();

                TestUtility.ShouldFail<FrameworkException>(() => provider.GetOptions<PocoPath>(), "Found multiple matches while binding configuration value to member 'Section__A__Option1'");
            }

            {
                var provider = new ConfigurationBuilder(new ConsoleLogProvider())
                    .AddKeyValue("section:a:option1", 49)
                    .AddKeyValue("Section.a.Option1", 50)
                    .Build();

                Assert.AreEqual(50, provider.GetOptions<PocoPath>().Section__A__Option1);
            }

            {
                var provider = new ConfigurationBuilder(new ConsoleLogProvider())
                    .AddKeyValue("section.a.option1", 49)
                    .AddKeyValue("Section:a:Option1", 50)
                    .Build();

                Assert.AreEqual(50, provider.GetOptions<PocoPath>().Section__A__Option1);
            }

            {
                var provider = new ConfigurationBuilder(new ConsoleLogProvider())
                    .AddKeyValue("section:a:option1", 51)
                    .AddKeyValue("Section.a.Option1", 52)
                    .AddKeyValue("Section__a__Option1", 53)
                    .Build();

                TestUtility.ShouldFail<FrameworkException>(() => provider.GetOptions<PocoPath>(), "Found multiple matches while binding configuration value to member 'Section__A__Option1'");
            }
        }

        [TestMethod]
        public void CommandLineArguments()
        {
            var provider = new ConfigurationBuilder(new ConsoleLogProvider())
                .AddCommandLineArguments(new [] { "/option1", "/Option2", "-option3" }, "/")
                .Build();

            Assert.IsTrue(provider.AllKeys.Contains("option1"));
            Assert.IsTrue(provider.AllKeys.Contains("Option2"));
            Assert.IsFalse(provider.AllKeys.Contains("option3"));

            Assert.IsTrue(provider.GetValue("OPTION1", false));
            Assert.IsTrue(provider.GetValue("option2", false));
        }

        [TestMethod]
        public void CommandLineArgumentsPath()
        {
            var provider = new ConfigurationBuilder(new ConsoleLogProvider())
                .AddCommandLineArguments(new[] { "-option1", "/Option2", "/option3" }, "-", "TestSection")
                .Build();

            Assert.IsTrue(provider.AllKeys.Contains("TestSection:option1"));
            Assert.IsFalse(provider.AllKeys.Contains("TestSection:Option2"));
            Assert.IsFalse(provider.AllKeys.Contains("option3"));

            Assert.IsTrue(provider.GetValue("OptioN1", false, "TESTSECTION"));
        }

        [TestMethod]
        public void CommandLineArgumentsSkipEmpty()
        {
            var provider = new ConfigurationBuilder(new ConsoleLogProvider())
                .AddCommandLineArguments(new [] { "/", "/  ", "/help" }, "/")
                .Build();

            Assert.AreEqual(1, provider.AllKeys.Count());
        }

        private class PocoUnsupportedType
        {
            public TimeZone UnsupportedProperty { get; set; }
        }

        [TestMethod]
        public void UnsupportedMemberType()
        {
            var provider = new ConfigurationBuilder(new ConsoleLogProvider())
                .AddKeyValue("UnsupportedProperty", "123")
                .Build();

            TestUtility.ShouldFail<FrameworkException>(() => provider.GetOptions<PocoUnsupportedType>(),
                "not supported", "TimeZone", "UnsupportedProperty");
        }

        [TestMethod]
        [DeploymentItem("JsonConfigurationFile.json")]
        public void JsonConfigurationCorrect()
        {
            var provider = new ConfigurationBuilder(new ConsoleLogProvider())
                .Add(new JsonFileSource("JsonConfigurationFile.json"))
                .Build();

            Assert.AreEqual(3, provider.GetValue("Prop", 0));
            Assert.AreEqual("SectionPropValue", provider.GetValue("SectionProp", "", "Section"));
            Assert.AreEqual("One, Two", TestUtility.Dump(provider.GetValue<string[]>("Array")));
        }

        [TestMethod]
        public void JsonFileNotFoundThrows()
        {
            var builder = new ConfigurationBuilder(new ConsoleLogProvider())
                .Add(new JsonFileSource("NonExistant.json"));

            TestUtility.ShouldFail<FileNotFoundException>(() => builder.Build(), "NonExistant.json");
        }

        [TestMethod]
        [DeploymentItem("JsonConfigurationFile_Invalid.json")]
        public void InvalidJsonFileThrows()
        {
            var builder = new ConfigurationBuilder(new ConsoleLogProvider())
                .Add(new JsonFileSource("JsonConfigurationFile_Invalid.json"));

            TestUtility.ShouldFail<FrameworkException>(() => builder.Build(), "Error parsing JSON contents", "JsonConfigurationFile_Invalid.json");
        }

        [TestMethod]
        public void CorrectlyParsesJson()
        {
            var jsonText = @"
{
    ""IntProp"": 1,
    /* comment */
    ""Section"": 
    {
        ""StringProp"": ""StringValue"",
        ""SubSection"": 
        {
            ""Dot.BoolValue"": 12,
            ""Colon:DoubleValue"": 3.14
        } 
    },
    ""TrailDouble"":  12.9,
    ""GuidAsString"": ""c7653c46-62a2-427b-8841-183bae56d743"",
    ""DateTimeAsString"": ""2019-12-01T15:34:50.7962010Z""
}";

            var provider = new ConfigurationBuilder(new ConsoleLogProvider())
                .Add(new JsonSource(jsonText))
                .Build();

            Assert.AreEqual(1, provider.GetValue("IntProp", 1));
            Assert.AreEqual(12, provider.GetValue("Dot.BoolValue", 0, "Section:SubSection"));
            Assert.AreEqual(3.14, provider.GetValue("Colon:DoubleValue", 0.0, "Section:SubSection"));
            Assert.AreEqual("StringValue", provider.GetValue("StringProp", "", "Section"));
            Assert.AreEqual(12.9, provider.GetValue("TrailDouble", 0.0));
            // ensure some specific string values are kept as strings and not implicitly parsed by json parser
            Assert.AreEqual("c7653c46-62a2-427b-8841-183bae56d743", provider.GetValue("GuidAsString", ""));
            Assert.AreEqual("2019-12-01T15:34:50.7962010Z", provider.GetValue("DateTimeAsString", ""));
            Assert.AreEqual(7, provider.AllKeys.Count());
        }

        [TestMethod]
        public void JsonErrorsThrow()
        {
            Func<string, IConfiguration> buildWithJson = jsonText => new ConfigurationBuilder(new ConsoleLogProvider()).Add(new JsonSource(jsonText)).Build();

            TestUtility.ShouldFail(() => buildWithJson("{"), "Error reading JObject from JsonReader");
            TestUtility.ShouldFail<FrameworkException>(() => buildWithJson("{\"array\": null }"), "JSON token type Null is not allowed");
        }

        private class TestOptions
        {
            private string PrivateField;
            private string PrivateProperty { get; set; }
            public string PublicField;
            public string PublicProperty { get; set; }
            public int PublicPropertyInt { get; set; }
            public object PublicPropertyNull { get; set; }
            public string PublicPropertyGetter { get; }
            public static string StaticField;
            public static string StaticProperty { get; set; }
            public TestOptions()
            {
                PrivateField = "1";
                PrivateProperty = "2";
                PublicField = "3";
                PublicProperty = "4";
                PublicPropertyInt = 5;
                PublicPropertyNull = null;
                PublicPropertyGetter = "6";
                StaticField = "7";
                StaticProperty = "8";
            }
        }

        [TestMethod]
        public void AddOptions()
        {
            var options = new TestOptions();
            var configuration = new ConfigurationBuilder(new ConsoleLogProvider())
                .AddOptions(options)
                .Build();
            Assert.AreEqual("PublicField:3, PublicProperty:4, PublicPropertyGetter:6, PublicPropertyInt:5, PublicPropertyNull:",
                TestUtility.DumpSorted(configuration.AllKeys.Select(key => $"{key}:{configuration.GetValue<object>(key)}")));
        }

        [TestMethod]
        public void AddOptionsWithPrefix()
        {
            var options = new TestOptions();
            var configuration = new ConfigurationBuilder(new ConsoleLogProvider())
                .AddOptions(options, "p")
                .Build();
            Assert.AreEqual("p:PublicField:3, p:PublicProperty:4, p:PublicPropertyGetter:6, p:PublicPropertyInt:5, p:PublicPropertyNull:",
                TestUtility.DumpSorted(configuration.AllKeys.Select(key => $"{key}:{configuration.GetValue<object>(key)}")));
        }

        private class TestPathOptions
        {
            [AbsolutePathOption]
            public string PathConvert { get; set; }
            public string Path { get; set; }
        }


        [TestMethod]
        public void KeyValueSourceConvertingPaths()
        {
            var configuration = new ConfigurationBuilder(new ConsoleLogProvider())
                .AddKeyValue("Path", "relative\\test.json")
                .AddKeyValue("PathConvert", "relative\\testc.json")
                .Build();

            var options = configuration.GetOptions<TestPathOptions>();

            Assert.AreEqual("relative\\test.json", options.Path);

            var expectedConverted = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "relative\\testc.json");
            Assert.AreEqual(expectedConverted, options.PathConvert);
        }

        [TestMethod]
        public void JsonFileSourceConvertsPaths()
        {
            var configuration = new ConfigurationBuilder(new ConsoleLogProvider())
                .AddJsonFile("JsonConfigurationFile.json")
                .Build();

            var options = configuration.GetOptions<TestPathOptions>();

            Assert.AreEqual("relative\\test.json", options.Path);

            var expectedConverted = Path.Combine(Environment.CurrentDirectory, "relative\\testc.json");
            Assert.AreEqual(expectedConverted, options.PathConvert);
        }

        [TestMethod]
        public void ConfigurationManagerSourceConvertsPaths()
        {
            var configuration = new ConfigurationBuilder(new ConsoleLogProvider())
                .AddConfigurationManagerConfiguration()
                .Build();

            var options = configuration.GetOptions<TestPathOptions>();

            Assert.AreEqual("relative\\apptest.json", options.Path);

            var expectedConverted = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "relative\\apptestc.json");
            Assert.AreEqual(expectedConverted, options.PathConvert);
        }

        [TestMethod]
        public void EmptyRelativeFolder()
        {
            var configuration = new ConfigurationBuilder(new ConsoleLogProvider())
                .Add(new KeyValuesSource(new[] {new KeyValuePair<string, object>("PathConvert", "")}))
                .Build();

            var options = configuration.GetOptions<TestPathOptions>();
            Assert.AreEqual(AppDomain.CurrentDomain.BaseDirectory, options.PathConvert);
        }

        private class OverrideOptions
        {
            public string SeparatorTest__SomeConfigurationKey { get; set; }
        }

        [TestMethod]
        public void DifferentSourcesOverrides()
        {
            var configurationBuilder = new ConfigurationBuilder(new ConsoleLogProvider())
                .AddConfigurationManagerConfiguration();

            {
                var keyValueColon = configurationBuilder.Build().GetValue<string>("SeparatorTest:SomeConfigurationKey");
                Assert.AreEqual("OriginalValue", keyValueColon);

                var keyValueDot = configurationBuilder.Build().GetValue<string>("SeparatorTest.SomeConfigurationKey");
                Assert.AreEqual("OriginalValue", keyValueDot);
            }

            var jsonCfg = @"{ ""SeparatorTest"": { ""SomeConfigurationKey"": ""NewValue"" } }";
            configurationBuilder
                .Add(new JsonSource(jsonCfg));

            {
                var configuration = configurationBuilder.Build();

                var keyValueColon = configuration.GetValue<string>("SeparatorTest:SomeConfigurationKey");
                Assert.AreEqual("NewValue", keyValueColon);

                var keyValueDot = configuration.GetValue<string>("SeparatorTest.SomeConfigurationKey");
                Assert.AreEqual("NewValue", keyValueDot);

                var options = configuration.GetOptions<OverrideOptions>();
                Assert.AreEqual("NewValue", options.SeparatorTest__SomeConfigurationKey);
            }
        }

        [Options("MixedPathTest")]
        private class MixedPathOptions
        {
            public string Legacy__AutoGeneratePolymorphicProperty { get; set; }
        }

        [TestMethod]
        public void ResolvesMixedPathOption()
        {
            var configuration = new ConfigurationBuilder(new ConsoleLogProvider())
                .AddConfigurationManagerConfiguration()
                .Build();

            var options = configuration.GetOptions<MixedPathOptions>();
            Assert.AreEqual("MixedPathValue", options.Legacy__AutoGeneratePolymorphicProperty);
        }

        [TestMethod]
        public void DoubleUnderscoreNotInvariant()
        {
            var configuration = new ConfigurationBuilder(new ConsoleLogProvider())
                .AddKeyValue("path.key", "value1")
                .AddKeyValue("path__key", "value2")
                .Build();

            Assert.AreEqual("value1", configuration.GetValue<string>("path.key"));
            Assert.AreEqual("value1", configuration.GetValue<string>("path:key"));
            Assert.AreEqual("value2", configuration.GetValue<string>("path__key"));
        }

        [TestMethod]
        public void SupportLegacyKeysNewOnly()
        {
            var configuration = new ConfigurationBuilder(new ConsoleLogProvider())
                .AddKeyValue("Rhetos:ConfigurationProvider:LegacyKeysSupport", "Convert")
                .AddKeyValue("Rhetos:AppSecurity:AllClaimsForUsers", "newValue")
                .Build();

            Assert.AreEqual("newValue", configuration.GetValue<string>("Rhetos:AppSecurity:AllClaimsForUsers"));
        }

        [TestMethod]
        public void SupportLegacyKeysOldOnly()
        {
            var configuration = new ConfigurationBuilder(new ConsoleLogProvider())
                .AddKeyValue("Rhetos:ConfigurationProvider:LegacyKeysSupport", "Convert")
                .AddKeyValue("Security.AllClaimsForUsers", "oldValue")
                .Build();

            Assert.AreEqual("oldValue", configuration.GetValue<string>("Rhetos:AppSecurity:AllClaimsForUsers"));
        }

        [TestMethod]
        public void SupportLegacyKeysNoSupportByDefault()
        {
            var configuration = new ConfigurationBuilder(new ConsoleLogProvider())
                .AddKeyValue("Rhetos:ConfigurationProvider:LegacyKeysSupport", "Ignore")
                .AddKeyValue("Security.AllClaimsForUsers", "oldValue")
                .Build();

            Assert.AreEqual(null, configuration.GetValue<string>("Rhetos:AppSecurity:AllClaimsForUsers"));
        }

        [TestMethod]
        public void SupportLegacyKeysNewKeyOverridesOld()
        {
            var configuration = new ConfigurationBuilder(new ConsoleLogProvider())
                .AddKeyValue("Rhetos:ConfigurationProvider:LegacyKeysSupport", "Convert")
                .AddKeyValue("Rhetos:AppSecurity:AllClaimsForUsers", "newValue")
                .AddKeyValue("Security.AllClaimsForUsers", "oldValue")
                .Build();

            Assert.AreEqual("newValue", configuration.GetValue<string>("Rhetos:AppSecurity:AllClaimsForUsers"));
        }

        [TestMethod]
        public void SupportLegacyKeysError()
        {
            TestUtility.ShouldFail<FrameworkException>(
                () => new ConfigurationBuilder(new ConsoleLogProvider())
                    .AddKeyValue("Rhetos:ConfigurationProvider:LegacyKeysSupport", "Error")
                    .AddKeyValue("Security.AllClaimsForUsers", "oldValue")
                    .Build(),
                "Rhetos:AppSecurity:AllClaimsForUsers",
                "Security.AllClaimsForUsers");
        }
    }
}
