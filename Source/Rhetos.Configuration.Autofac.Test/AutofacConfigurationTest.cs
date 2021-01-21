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
using Rhetos.Deployment;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Rhetos.Configuration.Autofac.Test
{
    [TestClass]
    public class AutofacConfigurationTest
    {
        private class RhetosHostTestBuilder : RhetosHostBuilderBase
        {
            protected override ContainerBuilder CreateContainerBuilder(IConfiguration configuration)
            {
                var pluginAssemblies = new[] { Assembly.GetExecutingAssembly() };
                var pluginTypes = Array.Empty<Type>();
                var pluginScanner = new RuntimePluginScanner(pluginAssemblies, pluginTypes, _builderLogProvider);
                return new RhetosContainerBuilder(configuration, _builderLogProvider, pluginScanner);
            }
        }

        private class ApplicationDeploymentAccessor : ApplicationDeployment, ITestAccessor
        {
            public ApplicationDeploymentAccessor(Action<IConfigurationBuilder> configureConfiguration, ILogProvider logProvider) : 
                base(HostBuilderFactoryWithConfiguration(configureConfiguration), logProvider){}

            public IRhetosHostBuilder CreateDbUpdateHostBuilder() => this.Invoke(nameof(CreateDbUpdateHostBuilder));

            public void AddAppInitializationComponents(ContainerBuilder builder) => this.Invoke(nameof(AddAppInitializationComponents), builder);

            private static Func<IRhetosHostBuilder> HostBuilderFactoryWithConfiguration(Action<IConfigurationBuilder> configureConfiguration)
            {
                return () => new RhetosHostTestBuilder()
                    .ConfigureConfiguration(configureConfiguration);
            }
        }

        private class ApplicationBuildAccessor : ApplicationBuild, ITestAccessor
        {
            public ApplicationBuildAccessor(IConfiguration configuration, ILogProvider logProvider, IEnumerable<string> pluginAssemblies, InstalledPackages installedPackages) :
                base(configuration, logProvider, pluginAssemblies, installedPackages)
            { }

            public RhetosContainerBuilder CreateBuildComponentsContainer() => this.Invoke("CreateBuildComponentsContainer");
        }

        public IConfiguration GetBuildConfiguration()
        {
            string rhetosAppRootPath = AppDomain.CurrentDomain.BaseDirectory;

            // This code is mostly copied from Rhetos CLI build-time configuration.

            var configuration = new ConfigurationBuilder(new ConsoleLogProvider())
                .AddOptions(new RhetosBuildEnvironment
                {
                    ProjectFolder = rhetosAppRootPath,
                    OutputAssemblyName = Assembly.GetEntryAssembly().GetName().Name,
                    CacheFolder = Path.Combine(rhetosAppRootPath, "BuildCacheTest"),
                    GeneratedAssetsFolder = Path.Combine(rhetosAppRootPath, "GeneratedAssetsTest"), // Custom for testing
                    GeneratedSourceFolder = "GeneratedSourceTest",
                })
                .AddOptions(new RhetosTargetEnvironment
                {
                    TargetPath = @"TargetPathTest",
                    TargetAssetsFolder = @"TargetAssetsTest",
                })
                .AddKeyValue(ConfigurationProvider.GetKey((ConfigurationProviderOptions o) => o.LegacyKeysWarning), true)
                .AddKeyValue(ConfigurationProvider.GetKey((LoggingOptions o) => o.DelayedLogTimout), 60.0)
                .AddJsonFile(Path.Combine(rhetosAppRootPath, RhetosBuildEnvironment.ConfigurationFileName), optional: true)
                .Build();

            return configuration;
        }

        public void GetRuntimeConfiguration(IConfigurationBuilder configurationBuilder)
        {
            string rhetosAppRootPath = AppDomain.CurrentDomain.BaseDirectory;
            string currentAssemblyPath = GetType().Assembly.Location;
            var allOtherAssemblies = Directory.GetFiles(Path.GetDirectoryName(currentAssemblyPath), "*.dll")
                .Except(new[] { currentAssemblyPath })
                .Select(path => Path.GetFileName(path))
                .ToList();

            // Simulating common run-time configuration of Rhetos CLI.
            configurationBuilder
                .AddKeyValue(ConfigurationProvider.GetKey((DatabaseOptions o) => o.SqlCommandTimeout), 0)
                .AddKeyValue(ConfigurationProvider.GetKey((ConfigurationProviderOptions o) => o.LegacyKeysWarning), true)
                .AddKeyValue(ConfigurationProvider.GetKey((LoggingOptions o) => o.DelayedLogTimout), 60.0)
                .AddConfigurationManagerConfiguration()
                .AddRhetosAppEnvironment(rhetosAppRootPath)
                .AddJsonFile(Path.Combine(rhetosAppRootPath, DbUpdateOptions.ConfigurationFileName), optional: true)
                // shortTransactions
                .AddKeyValue(ConfigurationProvider.GetKey((DbUpdateOptions o) => o.ShortTransactions), true)
                // skipRecompute
                .AddKeyValue(ConfigurationProvider.GetKey((DbUpdateOptions o) => o.SkipRecompute), true)
                .AddOptions(new RhetosAppOptions
                {
                    RhetosRuntimePath = currentAssemblyPath,
                })
                .AddOptions(new PluginScannerOptions
                {
                    // Ignore other MEF plugins from assemblies that might get bundled in the same testing output folder.
                    IgnoreAssemblyFiles = allOtherAssemblies
                });
        }

        private IEnumerable<string> PluginsFromThisAssembly()
        {
            return new[] { GetType().Assembly.Location };
        }

        [TestMethod]
        public void CorrectBuildOptions()
        {
            var configuration = GetBuildConfiguration();
            Assert.AreEqual("TestValue", configuration.GetValue<string>("TestBuildSettings"));
            var connectionsString = configuration.GetValue<string>($"ConnectionStrings:ServerConnectionString:ConnectionString");
            TestUtility.AssertContains(connectionsString, new[] { "TestSql", "TestDb" });
        }

        [TestMethod]
        public void CorrectOptionsAddedByKeyValue()
        {
            var configuration = GetBuildConfiguration();
            Assert.AreEqual(true, configuration.GetOptions<ConfigurationProviderOptions>().LegacyKeysWarning);
            Assert.AreEqual(60.0, configuration.GetOptions<LoggingOptions>().DelayedLogTimout);
        }

        [TestMethod]
        public void CorrectRegistrationsBuildTime()
        {
            var configuration = GetBuildConfiguration();
            var build = new ApplicationBuildAccessor(configuration, new NLogProvider(), PluginsFromThisAssembly(), new InstalledPackages());
            var builder = build.CreateBuildComponentsContainer();

            using (var container = builder.Build())
            {
                var registrationsDump = DumpSortedRegistrations(container);
                System.Diagnostics.Trace.WriteLine(registrationsDump);
                TestUtility.AssertAreEqualByLine(_expectedRegistrationsBuild, registrationsDump);

                TestAmbiguousRegistations(container,
                    expectedMultiplePlugins: new[] { "Rhetos.Dsl.IDslModelIndex", "Rhetos.Extensibility.IGenerator" },
                    expectedOverridenRegistrations: new Dictionary<Type, string> { { typeof(IUserInfo), "NullUserInfo" } });
            }
        }

        [TestMethod]
        public void CorrectRegistrationsDbUpdate()
        {
            var deployment = new ApplicationDeploymentAccessor(GetRuntimeConfiguration, new NLogProvider());
            var rhetosHostBuilder = deployment.CreateDbUpdateHostBuilder();

            using (var rhetosHost = rhetosHostBuilder.Build())
            {
                var registrationsDump = DumpSortedRegistrations(rhetosHost.Container);
                System.Diagnostics.Trace.WriteLine(registrationsDump);
                TestUtility.AssertAreEqualByLine(_expectedRegistrationsDbUpdate, registrationsDump);

                TestAmbiguousRegistations(rhetosHost.Container,
                    expectedOverridenRegistrations: new Dictionary<Type, string> { { typeof(IUserInfo), "NullUserInfo" } });
            }
        }

        [TestMethod]
        public void CorrectRegistrationsRuntimeWithInitialization()
        {
            // we construct the object, but need only its 'almost' static .AddAppInitilizationComponents
            var deployment = new ApplicationDeploymentAccessor(GetRuntimeConfiguration, new NLogProvider());
            var rhetosHostBuilder = new RhetosHostTestBuilder()
                .ConfigureConfiguration(GetRuntimeConfiguration)
                .ConfigureContainer(deployment.AddAppInitializationComponents);

            using (var rhetosHost = rhetosHostBuilder.Build())
            {
                var registrationsDump = DumpSortedRegistrations(rhetosHost.Container);
                System.Diagnostics.Trace.WriteLine(registrationsDump);
                TestUtility.AssertAreEqualByLine(_expectedRegistrationsRuntimeWithInitialization, registrationsDump);

                TestAmbiguousRegistations(rhetosHost.Container,
                    expectedMultiplePlugins: new[] { "Rhetos.Dsl.IDslModelIndex" },
                    expectedOverridenRegistrations: new Dictionary<Type, string> { { typeof(IUserInfo), "ProcessUserInfo" } });
            }
        }

        private string DumpSortedRegistrations(IContainer container)
        {
            var registrations = container.ComponentRegistry.Registrations
                    .Select(registration => registration.ToString())
                    .OrderBy(text => text)
                    .ToList();

            return string.Join(Environment.NewLine, registrations);
        }

        private void TestAmbiguousRegistations(IContainer container, IEnumerable<string> expectedMultiplePlugins = null, IDictionary<Type, string> expectedOverridenRegistrations = null)
        {
            expectedMultiplePlugins = expectedMultiplePlugins ?? Array.Empty<string>();
            expectedOverridenRegistrations = expectedOverridenRegistrations ?? new Dictionary<Type, string> { };
            var expectedOverridenServices = expectedOverridenRegistrations.Keys.Select(serviceType => serviceType.FullName).ToList();

            var multipleActivatorsByService = container.ComponentRegistry.Registrations
                .SelectMany(registation => registation.Services.Select(service => new { registation.Activator, Service = service }))
                .GroupBy(registation => registation.Service.Description, registration => registration.Activator.LimitType.Name)
                .Where(serviceActivators => serviceActivators.Count() > 1)
                .ToDictionary(serviceActivators => serviceActivators.Key, serviceActivators => serviceActivators.ToList());

            var errors = new List<string>();

            errors.AddRange(expectedMultiplePlugins
                .Except(multipleActivatorsByService.Keys)
                .Select(service => $"Service '{service}' is expected to have multiple plugins."));

            errors.AddRange(expectedOverridenServices
                .Except(multipleActivatorsByService.Keys)
                .Select(service => $"Service '{service}' is expected to have overridden registrations."));

            errors.AddRange(multipleActivatorsByService.Keys
                .Except(expectedMultiplePlugins)
                .Except(expectedOverridenServices)
                .Select(service => $"Service '{service}' has multiple registrations. Add {nameof(expectedMultiplePlugins)} or {nameof(expectedOverridenRegistrations)}." +
                    string.Concat(multipleActivatorsByService[service].Select(a => $"\r\n- {a}"))));

            foreach (var overridenService in expectedOverridenRegistrations)
            {
                string expectedActivator = overridenService.Value;
                string actualActivator = container.Resolve(overridenService.Key).GetType().Name;
                if (expectedActivator != actualActivator)
                    errors.Add($"Service '{overridenService.Key.FullName}' has activator '{actualActivator}' instead of '{expectedActivator}'.");
            }

            if (errors.Any())
                Assert.Fail(string.Join("\r\n", errors.Select((error, index) => $"{index + 1}. {error}")));
        }

        const string _expectedRegistrationsBuild =
@"Activator = ApplicationGenerator (ReflectionActivator), Services = [Rhetos.Deployment.ApplicationGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = AppSettingsGenerator (ReflectionActivator), Services = [Rhetos.Extensibility.IGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = BuildOptions (DelegateActivator), Services = [Rhetos.Utilities.BuildOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = CodeBuilder (ReflectionActivator), Services = [Rhetos.Compiler.ICodeBuilder], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = CodeGenerator (ReflectionActivator), Services = [Rhetos.Compiler.ICodeGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = ConceptDataMigrationGenerator (ReflectionActivator), Services = [Rhetos.Extensibility.IGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = ConceptMetadata (ReflectionActivator), Services = [Rhetos.Dsl.ConceptMetadata], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = ConfigurationProvider (ProvidedInstanceActivator), Services = [Rhetos.Utilities.IConfiguration], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = ExternallyOwned
Activator = DatabaseModelBuilder (ReflectionActivator), Services = [Rhetos.DatabaseGenerator.DatabaseModelBuilder], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DatabaseModelDependencies (ReflectionActivator), Services = [Rhetos.DatabaseGenerator.DatabaseModelDependencies], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DatabaseModelFile (ReflectionActivator), Services = [Rhetos.DatabaseGenerator.DatabaseModelFile], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DatabaseModelGenerator (ReflectionActivator), Services = [Rhetos.Extensibility.IGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DatabaseSettings (DelegateActivator), Services = [Rhetos.Utilities.DatabaseSettings], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DataMigrationScriptsFile (ReflectionActivator), Services = [Rhetos.Deployment.IDataMigrationScriptsFile], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DataMigrationScriptsGenerator (ReflectionActivator), Services = [Rhetos.Extensibility.IGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DelayedLogProvider (ReflectionActivator), Services = [Rhetos.Utilities.IDelayedLogProvider], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DiskDslScriptLoader (ReflectionActivator), Services = [Rhetos.Dsl.IDslScriptsProvider], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DomGenerator (ReflectionActivator), Services = [Rhetos.Extensibility.IGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DslContainer (ReflectionActivator), Services = [Rhetos.Dsl.DslContainer], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DslModel (ReflectionActivator), Services = [Rhetos.Dsl.IDslModel], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DslModelFile (ReflectionActivator), Services = [Rhetos.Dsl.IDslModelFile], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DslModelIndexByReference (ReflectionActivator), Services = [Rhetos.Dsl.IDslModelIndex], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DslModelIndexByType (ReflectionActivator), Services = [Rhetos.Dsl.IDslModelIndex], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DslParser (ReflectionActivator), Services = [Rhetos.Dsl.IDslParser], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = EntityFrameworkMappingGenerator (ReflectionActivator), Services = [Rhetos.Extensibility.IGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = FilesUtility (ReflectionActivator), Services = [Rhetos.Utilities.FilesUtility], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = InitialDomCodeGenerator (ReflectionActivator), Services = [Rhetos.Dom.InitialDomCodeGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = InitializationConcept (ReflectionActivator), Services = [Rhetos.Dsl.IConceptInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = InstalledPackages (ProvidedInstanceActivator), Services = [Rhetos.Deployment.IInstalledPackages, Rhetos.Deployment.InstalledPackages], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = InstalledPackagesGenerator (ReflectionActivator), Services = [Rhetos.Extensibility.IGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = InstalledPackagesProvider (ReflectionActivator), Services = [Rhetos.Deployment.InstalledPackagesProvider], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = LifetimeScope (DelegateActivator), Services = [Autofac.ILifetimeScope, Autofac.IComponentContext], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = ExternallyOwned
Activator = LoggingOptions (DelegateActivator), Services = [Rhetos.Utilities.LoggingOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = MacroOrderRepository (ReflectionActivator), Services = [Rhetos.Dsl.IMacroOrderRepository], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = MsSqlUtility (ReflectionActivator), Services = [Rhetos.Utilities.ISqlUtility], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = NLogProvider (ReflectionActivator), Services = [Rhetos.Logging.ILogProvider], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = NullImplementation (ReflectionActivator), Services = [Rhetos.DatabaseGenerator.IConceptDatabaseDefinition], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = NullUserInfo (ReflectionActivator), Services = [Rhetos.Utilities.IUserInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = PluginInfoCollection (DelegateActivator), Services = [Rhetos.Extensibility.PluginInfoCollection], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = ResourcesGenerator (ReflectionActivator), Services = [Rhetos.Extensibility.IGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = RhetosBuildEnvironment (DelegateActivator), Services = [Rhetos.Utilities.RhetosBuildEnvironment, Rhetos.Utilities.IAssetsOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = RhetosTargetEnvironment (DelegateActivator), Services = [Rhetos.Utilities.RhetosTargetEnvironment], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = SourceWriter (ReflectionActivator), Services = [Rhetos.Compiler.ISourceWriter], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = TestSecurityUserInfo (ReflectionActivator), Services = [Rhetos.Utilities.IUserInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = Tokenizer (ReflectionActivator), Services = [Rhetos.Dsl.Tokenizer], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = XmlUtility (ReflectionActivator), Services = [Rhetos.Utilities.XmlUtility], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope";

        const string _expectedRegistrationsDbUpdate =
@"Activator = ConceptApplicationRepository (ReflectionActivator), Services = [Rhetos.DatabaseGenerator.IConceptApplicationRepository], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = ConceptDataMigrationExecuter (ReflectionActivator), Services = [Rhetos.DatabaseGenerator.IConceptDataMigrationExecuter], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = ConfigurationProvider (ProvidedInstanceActivator), Services = [Rhetos.Utilities.IConfiguration], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = ExternallyOwned
Activator = ConnectionString (ProvidedInstanceActivator), Services = [Rhetos.Utilities.ConnectionString], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DatabaseAnalysis (ReflectionActivator), Services = [Rhetos.DatabaseGenerator.DatabaseAnalysis], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DatabaseCleaner (ReflectionActivator), Services = [Rhetos.Deployment.DatabaseCleaner], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DatabaseDeployment (ReflectionActivator), Services = [Rhetos.Deployment.DatabaseDeployment], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DatabaseGenerator (ReflectionActivator), Services = [Rhetos.DatabaseGenerator.IDatabaseGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DatabaseModel (DelegateActivator), Services = [Rhetos.DatabaseGenerator.DatabaseModel], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DatabaseModelFile (ReflectionActivator), Services = [Rhetos.DatabaseGenerator.DatabaseModelFile], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DatabaseSettings (DelegateActivator), Services = [Rhetos.Utilities.DatabaseSettings], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DataMigrationScripts (DelegateActivator), Services = [Rhetos.Deployment.DataMigrationScripts], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DataMigrationScriptsExecuter (ReflectionActivator), Services = [Rhetos.Deployment.DataMigrationScriptsExecuter], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DataMigrationScriptsFile (ReflectionActivator), Services = [Rhetos.Deployment.IDataMigrationScriptsFile], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DbUpdateOptions (DelegateActivator), Services = [Rhetos.Utilities.DbUpdateOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DelayedLogProvider (ReflectionActivator), Services = [Rhetos.Utilities.IDelayedLogProvider], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = FilesUtility (ReflectionActivator), Services = [Rhetos.Utilities.FilesUtility], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = LifetimeScope (DelegateActivator), Services = [Autofac.ILifetimeScope, Autofac.IComponentContext], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = ExternallyOwned
Activator = LoggingOptions (DelegateActivator), Services = [Rhetos.Utilities.LoggingOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = MsSqlExecuter (ReflectionActivator), Services = [Rhetos.Utilities.ISqlExecuter], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = MsSqlUtility (ReflectionActivator), Services = [Rhetos.Utilities.ISqlUtility], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = NLogProvider (ReflectionActivator), Services = [Rhetos.Logging.ILogProvider], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = NullUserInfo (ReflectionActivator), Services = [Rhetos.Utilities.IUserInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = RhetosAppEnvironment (DelegateActivator), Services = [Rhetos.Utilities.RhetosAppEnvironment], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = RhetosAppOptions (DelegateActivator), Services = [Rhetos.Utilities.RhetosAppOptions, Rhetos.Utilities.IAssetsOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = SqlTransactionBatches (ReflectionActivator), Services = [Rhetos.Utilities.SqlTransactionBatches], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = SqlTransactionBatchesOptions (DelegateActivator), Services = [Rhetos.Utilities.SqlTransactionBatchesOptions], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = TestSecurityUserInfo (ReflectionActivator), Services = [Rhetos.Utilities.IUserInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = XmlUtility (ReflectionActivator), Services = [Rhetos.Utilities.XmlUtility], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope";

        const string _expectedRegistrationsRuntimeWithInitialization =
@"Activator = AppSecurityOptions (DelegateActivator), Services = [Rhetos.Utilities.AppSecurityOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = AuthorizationManager (ReflectionActivator), Services = [Rhetos.Security.IAuthorizationManager], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = ConceptMetadata (ReflectionActivator), Services = [Rhetos.Dsl.ConceptMetadata], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = ConfigurationProvider (ProvidedInstanceActivator), Services = [Rhetos.Utilities.IConfiguration], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = ExternallyOwned
Activator = ConnectionString (ProvidedInstanceActivator), Services = [Rhetos.Utilities.ConnectionString], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DatabaseSettings (DelegateActivator), Services = [Rhetos.Utilities.DatabaseSettings], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DbUpdateOptions (DelegateActivator), Services = [Rhetos.Utilities.DbUpdateOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DelayedLogProvider (ReflectionActivator), Services = [Rhetos.Utilities.IDelayedLogProvider], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DomLoader (ReflectionActivator), Services = [Rhetos.Dom.IDomainObjectModel], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DslContainer (ReflectionActivator), Services = [Rhetos.Dsl.DslContainer], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DslModelFile (ReflectionActivator), Services = [Rhetos.Dsl.IDslModel], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DslModelIndexByReference (ReflectionActivator), Services = [Rhetos.Dsl.IDslModelIndex], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DslModelIndexByType (ReflectionActivator), Services = [Rhetos.Dsl.IDslModelIndex], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = EfMappingViewCacheFactory (ReflectionActivator), Services = [Rhetos.Persistence.EfMappingViewCacheFactory], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = EfMappingViewsFileStore (ReflectionActivator), Services = [Rhetos.Persistence.EfMappingViewsFileStore], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = EfMappingViewsInitializer (ReflectionActivator), Services = [Rhetos.Persistence.EfMappingViewsInitializer], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = FilesUtility (ReflectionActivator), Services = [Rhetos.Utilities.FilesUtility], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = InstalledPackages (DelegateActivator), Services = [Rhetos.Deployment.InstalledPackages, Rhetos.Deployment.IInstalledPackages], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = InstalledPackagesProvider (ReflectionActivator), Services = [Rhetos.Deployment.InstalledPackagesProvider], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = LifetimeScope (DelegateActivator), Services = [Autofac.ILifetimeScope, Autofac.IComponentContext], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = ExternallyOwned
Activator = LoggingOptions (DelegateActivator), Services = [Rhetos.Utilities.LoggingOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = MsSqlExecuter (ReflectionActivator), Services = [Rhetos.Utilities.ISqlExecuter], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = MsSqlUtility (ReflectionActivator), Services = [Rhetos.Utilities.ISqlUtility], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = NLogProvider (ReflectionActivator), Services = [Rhetos.Logging.ILogProvider], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = NoLocalizer (ReflectionActivator), Services = [Rhetos.Utilities.ILocalizer], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = NullAuthorizationProvider (ReflectionActivator), Services = [Rhetos.Security.IAuthorizationProvider], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = PersistenceTransaction (ReflectionActivator), Services = [Rhetos.Persistence.IPersistenceTransaction], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = ProcessingEngine (ReflectionActivator), Services = [Rhetos.Processing.IProcessingEngine], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = ProcessUserInfo (ReflectionActivator), Services = [Rhetos.Utilities.IUserInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = RhetosAppEnvironment (DelegateActivator), Services = [Rhetos.Utilities.RhetosAppEnvironment], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = RhetosAppOptions (DelegateActivator), Services = [Rhetos.Utilities.RhetosAppOptions, Rhetos.Utilities.IAssetsOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = SqlTransactionBatches (ReflectionActivator), Services = [Rhetos.Utilities.SqlTransactionBatches], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = SqlTransactionBatchesOptions (DelegateActivator), Services = [Rhetos.Utilities.SqlTransactionBatchesOptions], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = TestSecurityUserInfo (ReflectionActivator), Services = [Rhetos.Utilities.IUserInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = XmlDataTypeProvider (ReflectionActivator), Services = [Rhetos.Processing.IDataTypeProvider], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = XmlUtility (ReflectionActivator), Services = [Rhetos.Utilities.XmlUtility], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope";
    }
}
