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
using Rhetos.Logging;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.Configuration.Autofac.Test
{
    [TestClass]
    public class AutofacConfigurationTest
    {
        private IEnumerable<string> PluginsFromThisAssembly()
        {
            return new[] { GetType().Assembly.Location };
        }

        [TestMethod]
        public void CorrectBuildOptions()
        {
            var configuration = RhetosHostTestBuilder.GetBuildConfiguration();
            Assert.AreEqual("TestValue", configuration.GetValue<string>("TestBuildSettings"));
            var connectionsString = configuration.GetValue<string>($"ConnectionStrings:RhetosConnectionString");
            TestUtility.AssertContains(connectionsString, new[] { "TestSql", "TestDb" });
        }

        [TestMethod]
        public void CorrectOptionsAddedByKeyValue()
        {
            var configuration = RhetosHostTestBuilder.GetBuildConfiguration();
            Assert.AreEqual(true, configuration.GetOptions<ConfigurationProviderOptions>().LegacyKeysWarning);
            Assert.AreEqual(60.0, configuration.GetOptions<LoggingOptions>().DelayedLogTimout);
        }

        [TestMethod]
        public void CorrectRegistrationsBuildTime()
        {
            var configuration = RhetosHostTestBuilder.GetBuildConfiguration();
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
            var deployment = new ApplicationDeploymentAccessor();

            var rhetosHostBuilder = new RhetosHostTestBuilder()
                .ConfigureConfiguration(RhetosHostTestBuilder.GetRuntimeConfiguration)
                .UseBuilderLogProvider(new NLogProvider())
                .OverrideContainerConfiguration(deployment.SetDbUpdateComponents);

            using (var rhetosHost = rhetosHostBuilder.Build())
            {
                var registrationsDump = DumpSortedRegistrations(rhetosHost.GetRootContainer());
                System.Diagnostics.Trace.WriteLine(registrationsDump);
                TestUtility.AssertAreEqualByLine(_expectedRegistrationsDbUpdate, registrationsDump);

                TestAmbiguousRegistations(rhetosHost.GetRootContainer(),
                    expectedOverridenRegistrations: new Dictionary<Type, string> { { typeof(IUserInfo), "NullUserInfo" } });
            }
        }

        [TestMethod]
        public void CorrectRegistrationsRuntimeWithInitialization()
        {
            // we construct the object, but need only its 'almost' static .AddAppInitilizationComponents
            var deployment = new ApplicationDeploymentAccessor();
            var rhetosHostBuilder = new RhetosHostTestBuilder()
                .ConfigureConfiguration(RhetosHostTestBuilder.GetRuntimeConfiguration)
                .ConfigureContainer(deployment.AddAppInitializationComponents);

            using (var rhetosHost = rhetosHostBuilder.Build())
            {
                var registrationsDump = DumpSortedRegistrations(rhetosHost.GetRootContainer());
                System.Diagnostics.Trace.WriteLine(registrationsDump);
                TestUtility.AssertAreEqualByLine(_expectedRegistrationsRuntimeWithInitialization, registrationsDump);

                TestAmbiguousRegistations(rhetosHost.GetRootContainer(),
                    expectedMultiplePlugins: new[] { "Rhetos.Dsl.IDslModelIndex" },
                    expectedOverridenRegistrations: new Dictionary<Type, string> {
                        { typeof(IUserInfo), "ProcessUserInfo" },
                        { typeof(ILogProvider), "NLogProvider" }
                    });
            }
        }

        private string DumpSortedRegistrations(IComponentContext container)
        {
            var registrations = container.ComponentRegistry.Registrations
                    .Select(registration => registration.ToString())
                    .OrderBy(text => text)
                    .ToList();

            return string.Join(Environment.NewLine, registrations);
        }

        private void TestAmbiguousRegistations(IComponentContext container, IEnumerable<string> expectedMultiplePlugins = null, IDictionary<Type, string> expectedOverridenRegistrations = null)
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
@"Activator = ApplicationGenerator (ReflectionActivator), Services = [Rhetos.Deployment.ApplicationGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = BuildOptions (DelegateActivator), Services = [Rhetos.Utilities.BuildOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = CodeBuilder (ReflectionActivator), Services = [Rhetos.Compiler.ICodeBuilder], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = CodeGenerator (ReflectionActivator), Services = [Rhetos.Compiler.ICodeGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = ConceptDataMigrationGenerator (ReflectionActivator), Services = [Rhetos.Extensibility.IGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = ConceptMetadata (ReflectionActivator), Services = [Rhetos.Dsl.ConceptMetadata], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = ConfigurationProvider (ProvidedInstanceActivator), Services = [Rhetos.Utilities.IConfiguration], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = ExternallyOwned, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DatabaseModelBuilder (ReflectionActivator), Services = [Rhetos.DatabaseGenerator.DatabaseModelBuilder], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DatabaseModelDependencies (ReflectionActivator), Services = [Rhetos.DatabaseGenerator.DatabaseModelDependencies], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DatabaseModelFile (ReflectionActivator), Services = [Rhetos.DatabaseGenerator.DatabaseModelFile], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DatabaseModelGenerator (ReflectionActivator), Services = [Rhetos.Extensibility.IGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DatabaseSettings (DelegateActivator), Services = [Rhetos.Utilities.DatabaseSettings], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DataMigrationScriptsFile (ReflectionActivator), Services = [Rhetos.Deployment.IDataMigrationScriptsFile], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DataMigrationScriptsGenerator (ReflectionActivator), Services = [Rhetos.Extensibility.IGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DelayedLogProvider (ReflectionActivator), Services = [Rhetos.Utilities.IDelayedLogProvider], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DiskDslScriptLoader (ReflectionActivator), Services = [Rhetos.Dsl.IDslScriptsProvider], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DomGenerator (ReflectionActivator), Services = [Rhetos.Extensibility.IGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DslContainer (ReflectionActivator), Services = [Rhetos.Dsl.DslContainer], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DslDocumentationFile (ReflectionActivator), Services = [Rhetos.Dsl.DslDocumentationFile], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DslDocumentationFileGenerator (ReflectionActivator), Services = [Rhetos.Extensibility.IGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DslModel (ReflectionActivator), Services = [Rhetos.Dsl.IDslModel], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DslModelFile (ReflectionActivator), Services = [Rhetos.Dsl.IDslModelFile], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DslModelIndexByReference (ReflectionActivator), Services = [Rhetos.Dsl.IDslModelIndex], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DslModelIndexByType (ReflectionActivator), Services = [Rhetos.Dsl.IDslModelIndex], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DslParser (ReflectionActivator), Services = [Rhetos.Dsl.IDslParser], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DslSyntax (DelegateActivator), Services = [Rhetos.Dsl.DslSyntax], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DslSyntaxFile (ReflectionActivator), Services = [Rhetos.Dsl.DslSyntaxFile], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DslSyntaxFileGenerator (ReflectionActivator), Services = [Rhetos.Extensibility.IGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DslSyntaxFromPlugins (ReflectionActivator), Services = [Rhetos.Dsl.DslSyntaxFromPlugins], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = EntityFrameworkMappingGenerator (ReflectionActivator), Services = [Rhetos.Extensibility.IGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = FilesUtility (ReflectionActivator), Services = [Rhetos.Utilities.FilesUtility], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = InitialDomCodeGenerator (ReflectionActivator), Services = [Rhetos.Dom.InitialDomCodeGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = InitializationConcept (ReflectionActivator), Services = [Rhetos.Dsl.IConceptInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = InstalledPackages (ProvidedInstanceActivator), Services = [Rhetos.Deployment.InstalledPackages], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = InstalledPackagesGenerator (ReflectionActivator), Services = [Rhetos.Extensibility.IGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = InstalledPackagesProvider (ReflectionActivator), Services = [Rhetos.Deployment.InstalledPackagesProvider], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = LifetimeScope (DelegateActivator), Services = [Autofac.ILifetimeScope, Autofac.IComponentContext], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = ExternallyOwned, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = LoggingOptions (DelegateActivator), Services = [Rhetos.Utilities.LoggingOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = MacroOrderRepository (ReflectionActivator), Services = [Rhetos.Dsl.IMacroOrderRepository], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = MsSqlUtility (ReflectionActivator), Services = [Rhetos.Utilities.ISqlUtility], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = NLogProvider (ReflectionActivator), Services = [Rhetos.Logging.ILogProvider], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = NullImplementation (ReflectionActivator), Services = [Rhetos.DatabaseGenerator.IConceptDatabaseDefinition], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = NullUserInfo (ReflectionActivator), Services = [Rhetos.Utilities.IUserInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = PluginInfoCollection (DelegateActivator), Services = [Rhetos.Extensibility.PluginInfoCollection], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = ResourcesGenerator (ReflectionActivator), Services = [Rhetos.Extensibility.IGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = RhetosBuildEnvironment (DelegateActivator), Services = [Rhetos.Utilities.RhetosBuildEnvironment, Rhetos.Utilities.IAssetsOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = RhetosTargetEnvironment (DelegateActivator), Services = [Rhetos.Utilities.RhetosTargetEnvironment], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = SourceWriter (ReflectionActivator), Services = [Rhetos.Compiler.ISourceWriter], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = TestSecurityUserInfo (ReflectionActivator), Services = [Rhetos.Utilities.IUserInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = Tokenizer (ReflectionActivator), Services = [Rhetos.Dsl.ITokenizer], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = XmlUtility (ReflectionActivator), Services = [Rhetos.Utilities.XmlUtility], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline";

        const string _expectedRegistrationsDbUpdate =
@"Activator = ConceptApplicationRepository (ReflectionActivator), Services = [Rhetos.DatabaseGenerator.IConceptApplicationRepository], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = ConceptDataMigrationExecuter (ReflectionActivator), Services = [Rhetos.DatabaseGenerator.IConceptDataMigrationExecuter], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = ConfigurationProvider (ProvidedInstanceActivator), Services = [Rhetos.Utilities.IConfiguration], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = ExternallyOwned, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = ConnectionString (ProvidedInstanceActivator), Services = [Rhetos.Utilities.ConnectionString], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = ConnectionTesting (ReflectionActivator), Services = [Rhetos.Deployment.ConnectionTesting], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DatabaseAnalysis (ReflectionActivator), Services = [Rhetos.DatabaseGenerator.DatabaseAnalysis], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DatabaseCleaner (ReflectionActivator), Services = [Rhetos.Deployment.DatabaseCleaner], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DatabaseDeployment (ReflectionActivator), Services = [Rhetos.Deployment.DatabaseDeployment], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DatabaseGenerator (ReflectionActivator), Services = [Rhetos.DatabaseGenerator.IDatabaseGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DatabaseModel (DelegateActivator), Services = [Rhetos.DatabaseGenerator.DatabaseModel], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DatabaseModelFile (ReflectionActivator), Services = [Rhetos.DatabaseGenerator.DatabaseModelFile], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DatabaseOptions (DelegateActivator), Services = [Rhetos.Utilities.DatabaseOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DatabaseSettings (DelegateActivator), Services = [Rhetos.Utilities.DatabaseSettings], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DataMigrationScripts (DelegateActivator), Services = [Rhetos.Deployment.DataMigrationScripts], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DataMigrationScriptsExecuter (ReflectionActivator), Services = [Rhetos.Deployment.DataMigrationScriptsExecuter], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DataMigrationScriptsFile (ReflectionActivator), Services = [Rhetos.Deployment.IDataMigrationScriptsFile], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DbUpdateOptions (DelegateActivator), Services = [Rhetos.Utilities.DbUpdateOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DelayedLogProvider (ReflectionActivator), Services = [Rhetos.Utilities.IDelayedLogProvider], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = FilesUtility (ReflectionActivator), Services = [Rhetos.Utilities.FilesUtility], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = LifetimeScope (DelegateActivator), Services = [Autofac.ILifetimeScope, Autofac.IComponentContext], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = ExternallyOwned, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = LoggingOptions (DelegateActivator), Services = [Rhetos.Utilities.LoggingOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = MsSqlExecuter (ReflectionActivator), Services = [Rhetos.Utilities.ISqlExecuter], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = MsSqlUtility (ReflectionActivator), Services = [Rhetos.Utilities.ISqlUtility], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = NLogProvider (ReflectionActivator), Services = [Rhetos.Logging.ILogProvider], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = NullUserInfo (ReflectionActivator), Services = [Rhetos.Utilities.IUserInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = PersistenceTransaction (ReflectionActivator), Services = [Rhetos.Persistence.IPersistenceTransaction, Rhetos.IUnitOfWork], Lifetime = Autofac.Core.Lifetime.MatchingScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = PersistenceTransactionOptions (DelegateActivator), Services = [Rhetos.Utilities.PersistenceTransactionOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = RhetosAppOptions (DelegateActivator), Services = [Rhetos.Utilities.RhetosAppOptions, Rhetos.Utilities.IAssetsOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = SqlTransactionBatches (ReflectionActivator), Services = [Rhetos.Utilities.ISqlTransactionBatches], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = SqlTransactionBatchesOptions (DelegateActivator), Services = [Rhetos.Utilities.SqlTransactionBatchesOptions], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = TestSecurityUserInfo (ReflectionActivator), Services = [Rhetos.Utilities.IUserInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = UnitOfWorkFactory (ReflectionActivator), Services = [Rhetos.UnitOfWorkFactory, Rhetos.IUnitOfWorkFactory], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = XmlUtility (ReflectionActivator), Services = [Rhetos.Utilities.XmlUtility], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline";

        const string _expectedRegistrationsRuntimeWithInitialization =
@"Activator = AppSecurityOptions (DelegateActivator), Services = [Rhetos.Utilities.AppSecurityOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = AuthorizationManager (ReflectionActivator), Services = [Rhetos.Security.IAuthorizationManager], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = ConceptMetadata (ReflectionActivator), Services = [Rhetos.Dsl.ConceptMetadata], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = ConfigurationProvider (ProvidedInstanceActivator), Services = [Rhetos.Utilities.IConfiguration], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = ExternallyOwned, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = ConnectionString (ProvidedInstanceActivator), Services = [Rhetos.Utilities.ConnectionString], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = ConsoleLogProvider (ReflectionActivator), Services = [Rhetos.Logging.ILogProvider], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DatabaseOptions (DelegateActivator), Services = [Rhetos.Utilities.DatabaseOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DatabaseSettings (DelegateActivator), Services = [Rhetos.Utilities.DatabaseSettings], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DbUpdateOptions (DelegateActivator), Services = [Rhetos.Utilities.DbUpdateOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DelayedLogProvider (ReflectionActivator), Services = [Rhetos.Utilities.IDelayedLogProvider], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DomLoader (ReflectionActivator), Services = [Rhetos.Dom.IDomainObjectModel], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DslContainer (ReflectionActivator), Services = [Rhetos.Dsl.DslContainer], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DslModelFile (ReflectionActivator), Services = [Rhetos.Dsl.IDslModel], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DslModelIndexByReference (ReflectionActivator), Services = [Rhetos.Dsl.IDslModelIndex], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = DslModelIndexByType (ReflectionActivator), Services = [Rhetos.Dsl.IDslModelIndex], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = EfMappingViewCacheFactory (ReflectionActivator), Services = [Rhetos.Persistence.EfMappingViewCacheFactory], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = EfMappingViewsFileStore (ReflectionActivator), Services = [Rhetos.Persistence.EfMappingViewsFileStore], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = EfMappingViewsInitializer (ReflectionActivator), Services = [Rhetos.Persistence.EfMappingViewsInitializer], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = FilesUtility (ReflectionActivator), Services = [Rhetos.Utilities.FilesUtility], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = InstalledPackages (DelegateActivator), Services = [Rhetos.Deployment.InstalledPackages], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = InstalledPackagesProvider (ReflectionActivator), Services = [Rhetos.Deployment.InstalledPackagesProvider], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = LifetimeScope (DelegateActivator), Services = [Autofac.ILifetimeScope, Autofac.IComponentContext], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = ExternallyOwned, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = LoggingOptions (DelegateActivator), Services = [Rhetos.Utilities.LoggingOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = MsSqlExecuter (ReflectionActivator), Services = [Rhetos.Utilities.ISqlExecuter], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = MsSqlUtility (ReflectionActivator), Services = [Rhetos.Utilities.ISqlUtility], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = NLogProvider (ReflectionActivator), Services = [Rhetos.Logging.ILogProvider], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = NoLocalizer (ReflectionActivator), Services = [Rhetos.Utilities.ILocalizer], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = NullAuthorizationProvider (ReflectionActivator), Services = [Rhetos.Security.IAuthorizationProvider], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = PersistenceTransaction (ReflectionActivator), Services = [Rhetos.Persistence.IPersistenceTransaction, Rhetos.IUnitOfWork], Lifetime = Autofac.Core.Lifetime.MatchingScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = PersistenceTransactionOptions (DelegateActivator), Services = [Rhetos.Utilities.PersistenceTransactionOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = ProcessingEngine (ReflectionActivator), Services = [Rhetos.Processing.IProcessingEngine], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = ProcessUserInfo (ReflectionActivator), Services = [Rhetos.Utilities.IUserInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = RhetosAppOptions (DelegateActivator), Services = [Rhetos.Utilities.RhetosAppOptions, Rhetos.Utilities.IAssetsOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = SqlTransactionBatches (ReflectionActivator), Services = [Rhetos.Utilities.ISqlTransactionBatches], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = SqlTransactionBatchesOptions (DelegateActivator), Services = [Rhetos.Utilities.SqlTransactionBatchesOptions], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = TestSecurityUserInfo (ReflectionActivator), Services = [Rhetos.Utilities.IUserInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = UnitOfWorkFactory (ReflectionActivator), Services = [Rhetos.UnitOfWorkFactory, Rhetos.IUnitOfWorkFactory], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = XmlDataTypeProvider (ReflectionActivator), Services = [Rhetos.Processing.IDataTypeProvider], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline
Activator = XmlUtility (ReflectionActivator), Services = [Rhetos.Utilities.XmlUtility], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope, Pipeline = Autofac.Core.Pipeline.ResolvePipeline";
    }
}
