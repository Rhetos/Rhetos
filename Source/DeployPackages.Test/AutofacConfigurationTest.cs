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
using Rhetos;
using Rhetos.Logging;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Rhetos.Deployment;

namespace DeployPackages.Test
{
    [TestClass]
    public class AutofacConfigurationTest
    {
        private readonly IConfigurationProvider _configurationProvider;
        public AutofacConfigurationTest()
        {
            _configurationProvider = new ConfigurationBuilder()
                .AddRhetosAppConfiguration(AppDomain.CurrentDomain.BaseDirectory)
                .AddKeyValue(nameof(AssetsOptions.AssetsFolder), AppDomain.CurrentDomain.BaseDirectory)
                .AddConfigurationManagerConfiguration()
                .Build();

            LegacyUtilities.Initialize(_configurationProvider);
        }

        private IEnumerable<string> PluginsFromThisAssembly()
        {
            return new[] { GetType().Assembly.Location };
        }

        [TestMethod]
        public void CorrectRegistrationsBuildTime()
        {
            var deployment = new ApplicationDeployment(_configurationProvider, new NLogProvider(), PluginsFromThisAssembly);
            var builder = deployment.CreateBuildComponentsContainer(new InstalledPackages());

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
            var deployment = new ApplicationDeployment(_configurationProvider, new NLogProvider(), PluginsFromThisAssembly);
            var builder = deployment.CreateDbUpdateComponentsContainer();

            using (var container = builder.Build())
            {
                var registrationsDump = DumpSortedRegistrations(container);
                System.Diagnostics.Trace.WriteLine(registrationsDump);
                TestUtility.AssertAreEqualByLine(_expectedRegistrationsDbUpdate, registrationsDump);

                TestAmbiguousRegistations(container,
                    expectedOverridenRegistrations: new Dictionary<Type, string> { { typeof(IUserInfo), "NullUserInfo" } });
            }
        }

        [TestMethod]
        public void CorrectRegistrationsRuntimeWithInitialization()
        {
            var deployment = new ApplicationDeployment(_configurationProvider, new NLogProvider(), PluginsFromThisAssembly);
            var builder = deployment.CreateAppInitializationComponentsContainer();

            using (var container = builder.Build())
            {
                var registrationsDump = DumpSortedRegistrations(container);
                System.Diagnostics.Trace.WriteLine(registrationsDump);
                TestUtility.AssertAreEqualByLine(_expectedRegistrationsRuntimeWithInitialization, registrationsDump);

                TestAmbiguousRegistations(container,
                    expectedMultiplePlugins: new[] { "Rhetos.Dsl.IDslModelIndex" },
                    expectedOverridenRegistrations: new Dictionary<Type, string> { { typeof(IUserInfo), "ProcessUserInfo" } });
            }
        }

        [TestMethod]
        public void CorrectRegistrationsServerRuntime()
        {
            var builder = new RhetosContainerBuilder(_configurationProvider, new NLogProvider(), PluginsFromThisAssembly);
            Rhetos.Global.AddRhetosComponents(builder);

            using (var container = builder.Build())
            {
                var registrationsDump = DumpSortedRegistrations(container);
                System.Diagnostics.Trace.WriteLine(registrationsDump);
                TestUtility.AssertAreEqualByLine(_expectedRegistrationsServerRuntime, registrationsDump);

                TestAmbiguousRegistations(container,
                    expectedMultiplePlugins: new[] { "Rhetos.Dsl.IDslModelIndex" },
                    expectedOverridenRegistrations: new Dictionary<Type, string> { { typeof(IUserInfo), "TestWebSecurityUserInfo" } });
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
Activator = AssemblyGenerator (ReflectionActivator), Services = [Rhetos.Compiler.IAssemblyGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = AssetsOptions (DelegateActivator), Services = [Rhetos.Utilities.AssetsOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = BuildOptions (DelegateActivator), Services = [Rhetos.Utilities.BuildOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = CodeBuilder (ReflectionActivator), Services = [Rhetos.Compiler.ICodeBuilder], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = CodeGenerator (ReflectionActivator), Services = [Rhetos.Compiler.ICodeGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = ConceptDataMigrationGenerator (ReflectionActivator), Services = [Rhetos.Extensibility.IGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = ConceptMetadata (ReflectionActivator), Services = [Rhetos.Dsl.ConceptMetadata], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = Configuration (ReflectionActivator), Services = [Rhetos.Utilities.IConfiguration], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = ConfigurationProvider (ProvidedInstanceActivator), Services = [Rhetos.IConfigurationProvider], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = ConnectionString (ProvidedInstanceActivator), Services = [Rhetos.Utilities.ConnectionString], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DatabaseModelBuilder (ReflectionActivator), Services = [Rhetos.DatabaseGenerator.DatabaseModelBuilder], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DatabaseModelDependencies (ReflectionActivator), Services = [Rhetos.DatabaseGenerator.DatabaseModelDependencies], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DatabaseModelFile (ReflectionActivator), Services = [Rhetos.DatabaseGenerator.DatabaseModelFile], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DatabaseModelGenerator (ReflectionActivator), Services = [Rhetos.Extensibility.IGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DataMigrationScriptsFile (ReflectionActivator), Services = [Rhetos.Deployment.DataMigrationScriptsFile], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DataMigrationScriptsGenerator (ReflectionActivator), Services = [Rhetos.Extensibility.IGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
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
Activator = InitializationConcept (ReflectionActivator), Services = [Rhetos.Dsl.IConceptInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = InstalledPackages (ProvidedInstanceActivator), Services = [Rhetos.Deployment.IInstalledPackages, Rhetos.Deployment.InstalledPackages], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = InstalledPackagesGenerator (ReflectionActivator), Services = [Rhetos.Extensibility.IGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = LifetimeScope (DelegateActivator), Services = [Autofac.ILifetimeScope, Autofac.IComponentContext], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = ExternallyOwned
Activator = MacroOrderRepository (ReflectionActivator), Services = [Rhetos.Dsl.IMacroOrderRepository], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = MsSqlExecuter (ReflectionActivator), Services = [Rhetos.Utilities.ISqlExecuter], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = MsSqlUtility (ReflectionActivator), Services = [Rhetos.Utilities.ISqlUtility], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = NLogProvider (ReflectionActivator), Services = [Rhetos.Logging.ILogProvider], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = NullImplementation (ReflectionActivator), Services = [Rhetos.DatabaseGenerator.IConceptDatabaseDefinition], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = NullUserInfo (ReflectionActivator), Services = [Rhetos.Utilities.IUserInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = PluginScannerCache (ReflectionActivator), Services = [Rhetos.Extensibility.IGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = SqlTransactionBatches (ReflectionActivator), Services = [Rhetos.Utilities.SqlTransactionBatches], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = TestWebSecurityUserInfo (ReflectionActivator), Services = [Rhetos.Utilities.IUserInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = Tokenizer (ReflectionActivator), Services = [Rhetos.Dsl.Tokenizer], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = XmlUtility (ReflectionActivator), Services = [Rhetos.Utilities.XmlUtility], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope";

        const string _expectedRegistrationsDbUpdate =
@"Activator = AssetsOptions (DelegateActivator), Services = [Rhetos.Utilities.AssetsOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = BuildOptions (DelegateActivator), Services = [Rhetos.Utilities.BuildOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = ConceptApplicationRepository (ReflectionActivator), Services = [Rhetos.DatabaseGenerator.IConceptApplicationRepository], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = ConceptDataMigrationExecuter (ReflectionActivator), Services = [Rhetos.DatabaseGenerator.IConceptDataMigrationExecuter], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = Configuration (ReflectionActivator), Services = [Rhetos.Utilities.IConfiguration], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = ConfigurationProvider (ProvidedInstanceActivator), Services = [Rhetos.IConfigurationProvider], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = ConnectionString (ProvidedInstanceActivator), Services = [Rhetos.Utilities.ConnectionString], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DatabaseCleaner (ReflectionActivator), Services = [Rhetos.Deployment.DatabaseCleaner], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DatabaseDeployment (ReflectionActivator), Services = [Rhetos.Deployment.DatabaseDeployment], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DatabaseGenerator (ReflectionActivator), Services = [Rhetos.DatabaseGenerator.IDatabaseGenerator], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DatabaseGeneratorOptions (DelegateActivator), Services = [Rhetos.DatabaseGenerator.DatabaseGeneratorOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DatabaseModel (DelegateActivator), Services = [Rhetos.DatabaseGenerator.DatabaseModel], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DatabaseModelFile (ReflectionActivator), Services = [Rhetos.DatabaseGenerator.DatabaseModelFile], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DataMigrationScripts (DelegateActivator), Services = [Rhetos.Deployment.DataMigrationScripts], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DataMigrationScriptsExecuter (ReflectionActivator), Services = [Rhetos.Deployment.DataMigrationScriptsExecuter], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DataMigrationScriptsFile (ReflectionActivator), Services = [Rhetos.Deployment.DataMigrationScriptsFile], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = FilesUtility (ReflectionActivator), Services = [Rhetos.Utilities.FilesUtility], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = LifetimeScope (DelegateActivator), Services = [Autofac.ILifetimeScope, Autofac.IComponentContext], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = ExternallyOwned
Activator = MsSqlExecuter (ReflectionActivator), Services = [Rhetos.Utilities.ISqlExecuter], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = MsSqlUtility (ReflectionActivator), Services = [Rhetos.Utilities.ISqlUtility], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = NLogProvider (ReflectionActivator), Services = [Rhetos.Logging.ILogProvider], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = NullUserInfo (ReflectionActivator), Services = [Rhetos.Utilities.IUserInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = SqlTransactionBatches (ReflectionActivator), Services = [Rhetos.Utilities.SqlTransactionBatches], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = TestWebSecurityUserInfo (ReflectionActivator), Services = [Rhetos.Utilities.IUserInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = XmlUtility (ReflectionActivator), Services = [Rhetos.Utilities.XmlUtility], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope";

        const string _expectedRegistrationsRuntimeWithInitialization =
@"Activator = AssetsOptions (DelegateActivator), Services = [Rhetos.Utilities.AssetsOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = AuthorizationManager (ReflectionActivator), Services = [Rhetos.Security.IAuthorizationManager], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = Configuration (ReflectionActivator), Services = [Rhetos.Utilities.IConfiguration], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = ConfigurationProvider (ProvidedInstanceActivator), Services = [Rhetos.IConfigurationProvider], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = ConnectionString (ProvidedInstanceActivator), Services = [Rhetos.Utilities.ConnectionString], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DomLoader (ReflectionActivator), Services = [Rhetos.Dom.IDomainObjectModel], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DslContainer (ReflectionActivator), Services = [Rhetos.Dsl.DslContainer], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DslModelFile (ReflectionActivator), Services = [Rhetos.Dsl.IDslModel], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DslModelIndexByReference (ReflectionActivator), Services = [Rhetos.Dsl.IDslModelIndex], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DslModelIndexByType (ReflectionActivator), Services = [Rhetos.Dsl.IDslModelIndex], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = FilesUtility (ReflectionActivator), Services = [Rhetos.Utilities.FilesUtility], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = InstalledPackages (DelegateActivator), Services = [Rhetos.Deployment.InstalledPackages, Rhetos.Deployment.IInstalledPackages], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = LifetimeScope (DelegateActivator), Services = [Autofac.ILifetimeScope, Autofac.IComponentContext], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = ExternallyOwned
Activator = MsSqlExecuter (ReflectionActivator), Services = [Rhetos.Utilities.ISqlExecuter], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = MsSqlUtility (ReflectionActivator), Services = [Rhetos.Utilities.ISqlUtility], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = NLogProvider (ReflectionActivator), Services = [Rhetos.Logging.ILogProvider], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = NoLocalizer (ReflectionActivator), Services = [Rhetos.Utilities.ILocalizer], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = NullAuthorizationProvider (ReflectionActivator), Services = [Rhetos.Security.IAuthorizationProvider], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = PersistenceTransaction (ReflectionActivator), Services = [Rhetos.Persistence.IPersistenceTransaction], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = ProcessingEngine (ReflectionActivator), Services = [Rhetos.Processing.IProcessingEngine], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = ProcessUserInfo (ReflectionActivator), Services = [Rhetos.Utilities.IUserInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = RhetosAppOptions (DelegateActivator), Services = [Rhetos.Utilities.RhetosAppOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = SecurityOptions (DelegateActivator), Services = [Rhetos.Utilities.SecurityOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = SqlTransactionBatches (ReflectionActivator), Services = [Rhetos.Utilities.SqlTransactionBatches], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = TestWebSecurityUserInfo (ReflectionActivator), Services = [Rhetos.Utilities.IUserInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = WindowsSecurity (ReflectionActivator), Services = [Rhetos.Security.IWindowsSecurity], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = XmlDataTypeProvider (ReflectionActivator), Services = [Rhetos.Processing.IDataTypeProvider], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = XmlUtility (ReflectionActivator), Services = [Rhetos.Utilities.XmlUtility], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope";

        const string _expectedRegistrationsServerRuntime =
@"Activator = AssetsOptions (DelegateActivator), Services = [Rhetos.Utilities.AssetsOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = AuthorizationManager (ReflectionActivator), Services = [Rhetos.Security.IAuthorizationManager], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = Configuration (ReflectionActivator), Services = [Rhetos.Utilities.IConfiguration], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = ConfigurationProvider (ProvidedInstanceActivator), Services = [Rhetos.IConfigurationProvider], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = ConnectionString (ProvidedInstanceActivator), Services = [Rhetos.Utilities.ConnectionString], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DomLoader (ReflectionActivator), Services = [Rhetos.Dom.IDomainObjectModel], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DslContainer (ReflectionActivator), Services = [Rhetos.Dsl.DslContainer], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DslModelFile (ReflectionActivator), Services = [Rhetos.Dsl.IDslModel], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = DslModelIndexByReference (ReflectionActivator), Services = [Rhetos.Dsl.IDslModelIndex], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = DslModelIndexByType (ReflectionActivator), Services = [Rhetos.Dsl.IDslModelIndex], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = FilesUtility (ReflectionActivator), Services = [Rhetos.Utilities.FilesUtility], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = GlobalErrorHandler (ReflectionActivator), Services = [Rhetos.Web.GlobalErrorHandler], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = InstalledPackages (DelegateActivator), Services = [Rhetos.Deployment.InstalledPackages, Rhetos.Deployment.IInstalledPackages], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = LifetimeScope (DelegateActivator), Services = [Autofac.ILifetimeScope, Autofac.IComponentContext], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = ExternallyOwned
Activator = MsSqlExecuter (ReflectionActivator), Services = [Rhetos.Utilities.ISqlExecuter], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = MsSqlUtility (ReflectionActivator), Services = [Rhetos.Utilities.ISqlUtility], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = NLogProvider (ReflectionActivator), Services = [Rhetos.Logging.ILogProvider], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = NoLocalizer (ReflectionActivator), Services = [Rhetos.Utilities.ILocalizer], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = NullAuthorizationProvider (ReflectionActivator), Services = [Rhetos.Security.IAuthorizationProvider], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = PersistenceTransaction (ReflectionActivator), Services = [Rhetos.Persistence.IPersistenceTransaction], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = ProcessingEngine (ReflectionActivator), Services = [Rhetos.Processing.IProcessingEngine], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = RhetosAppOptions (DelegateActivator), Services = [Rhetos.Utilities.RhetosAppOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = RhetosService (ReflectionActivator), Services = [Rhetos.RhetosService, Rhetos.IServerApplication], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = SecurityOptions (DelegateActivator), Services = [Rhetos.Utilities.SecurityOptions], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = SqlTransactionBatches (ReflectionActivator), Services = [Rhetos.Utilities.SqlTransactionBatches], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = TestWebSecurityUserInfo (ReflectionActivator), Services = [Rhetos.Utilities.IUserInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = None, Ownership = OwnedByLifetimeScope
Activator = WcfWindowsUserInfo (ReflectionActivator), Services = [Rhetos.Utilities.IUserInfo], Lifetime = Autofac.Core.Lifetime.CurrentScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = WindowsSecurity (ReflectionActivator), Services = [Rhetos.Security.IWindowsSecurity], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = XmlDataTypeProvider (ReflectionActivator), Services = [Rhetos.Processing.IDataTypeProvider], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope
Activator = XmlUtility (ReflectionActivator), Services = [Rhetos.Utilities.XmlUtility], Lifetime = Autofac.Core.Lifetime.RootScopeLifetime, Sharing = Shared, Ownership = OwnedByLifetimeScope";
    }
}
